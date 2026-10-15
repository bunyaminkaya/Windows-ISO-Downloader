using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using WindowsISODownloader;
internal static class ResumeTests
{
    static int count;
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);count++;}
    internal static async Task RunAsync()
    {
        var folder=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        var partial=Path.Combine(folder,"test.iso.part");var uri=new Uri("https://download.microsoft.com/test.iso");var all=Encoding.ASCII.GetBytes("ABCDEFGH");string hash=Convert.ToHexString(SHA256.HashData(all));
        try {
            using var cancel=new CancellationTokenSource();
            using var firstHandler=new Handler((request,n)=>Reply(new InterruptStream(all,()=>cancel.Cancel()),"\"v1\"",8));using var first=new HttpClient(firstHandler);
            bool canceled=false;try {await ResumableDownload.RunAsync(first,uri,partial,"key",".iso",1,hash,_=>{},cancel.Token,TimeSpan.Zero);}catch(OperationCanceledException){canceled=true;}
            Check(canceled && new FileInfo(partial).Length==3 && File.Exists(partial+".json"),"Pause did not retain resumable data");
            using var resumeHandler=new Handler((request,n)=>{Check(request.Headers.Range?.Ranges.Single().From==3,"Range offset wrong");Check(request.Headers.IfRange?.EntityTag?.Tag=="\"v1\"","If-Range missing");return Partial(all[3..],"\"v1\"",3,8);});using var resumed=new HttpClient(resumeHandler);
            var actual=await ResumableDownload.RunAsync(resumed,uri,partial,"key",".iso",1,hash,_=>{},default,TimeSpan.Zero);Check(actual==hash && File.ReadAllBytes(partial).SequenceEqual(all),"Resume corrupted bytes");Check(!File.Exists(partial+".json"),"Completed sidecar not removed");
            await File.WriteAllBytesAsync(partial,all[..3]);await File.WriteAllTextAsync(partial+".json",JsonSerializer.Serialize(new ResumeMetadata("key","\"v1\"",8)));
            var different=Encoding.ASCII.GetBytes("12345678");using var changedHandler=new Handler((r,n)=>Reply(new MemoryStream(different),"\"v2\"",8));using var changed=new HttpClient(changedHandler);
            await ResumableDownload.RunAsync(changed,uri,partial,"key",".iso",1,null,_=>{},default,TimeSpan.Zero);Check(File.ReadAllBytes(partial).SequenceEqual(different),"Changed ETag response was appended");
            await File.WriteAllBytesAsync(partial,all[..3]);await File.WriteAllTextAsync(partial+".json",JsonSerializer.Serialize(new ResumeMetadata("key","\"v1\"",8)));
            using var badHandler=new Handler((r,n)=>Partial(all[3..],"\"v1\"",2,8));using var bad=new HttpClient(badHandler);bool rejected=false;try{await ResumableDownload.RunAsync(bad,uri,partial,"key",".iso",1,null,_=>{},default,TimeSpan.Zero);}catch(InvalidDataException){rejected=true;}Check(rejected && !File.Exists(partial),"Invalid Content-Range accepted");
            using var retryHandler=new Handler((r,n)=>n==1?new HttpResponseMessage(HttpStatusCode.ServiceUnavailable):Reply(new MemoryStream(all),"\"v1\"",8));using var retry=new HttpClient(retryHandler);await ResumableDownload.RunAsync(retry,uri,partial,"key",".iso",1,hash,_=>{},default,TimeSpan.Zero);Check(retryHandler.Calls==2,"Automatic retry did not happen");
            File.Delete(partial);using var wrongHashHandler=new Handler((r,n)=>Reply(new MemoryStream(all),"\"v1\"",8));using var wrongHashClient=new HttpClient(wrongHashHandler);rejected=false;try{await ResumableDownload.RunAsync(wrongHashClient,uri,partial,"key",".iso",1,new string('0',64),_=>{},default,TimeSpan.Zero);}catch(InvalidDataException){rejected=true;}Check(rejected && !File.Exists(partial),"Hash mismatch not rejected/cleaned");
            await File.WriteAllBytesAsync(partial, all[..3]); await File.WriteAllTextAsync(partial+".json",JsonSerializer.Serialize(new ResumeMetadata("different-key","\"v1\"",8)));
            using var identityHandler=new Handler((r,n)=>{Check(r.Headers.Range==null,"Different selection reused old data");return Reply(new MemoryStream(all),"\"v1\"",8);});using var identityClient=new HttpClient(identityHandler);await ResumableDownload.RunAsync(identityClient,uri,partial,"key",".iso",1,hash,_=>{},default,TimeSpan.Zero);
            await File.WriteAllBytesAsync(partial, all[..3]); await File.WriteAllTextAsync(partial+".json",JsonSerializer.Serialize(new ResumeMetadata("key","W/\"v1\"",8)));
            using var weakHandler=new Handler((r,n)=>{Check(r.Headers.Range==null,"Weak ETag used for resume");return Reply(new MemoryStream(all),"\"v1\"",8);});using var weakClient=new HttpClient(weakHandler);await ResumableDownload.RunAsync(weakClient,uri,partial,"key",".iso",1,hash,_=>{},default,TimeSpan.Zero);
            File.Delete(partial);
            using var interruptedHandler=new Handler((r,n)=>n==1?Reply(new FaultStream(all),"\"v1\"",8):Partial(all[3..],"\"v1\"",3,8));using var interruptedClient=new HttpClient(interruptedHandler);await ResumableDownload.RunAsync(interruptedClient,uri,partial,"key",".iso",1,hash,_=>{},default,TimeSpan.Zero);Check(interruptedHandler.Calls==2 && File.ReadAllBytes(partial).SequenceEqual(all),"Interrupted response was not automatically resumed");
            await File.WriteAllBytesAsync(partial,all[..3]);await File.WriteAllTextAsync(partial+".json",JsonSerializer.Serialize(new ResumeMetadata("key","\"v1\"",null)));
            using var unknownHandler=new Handler((r,n)=>Partial(all[3..],"\"v1\"",3,8));using var unknownClient=new HttpClient(unknownHandler);var knownHash=await ResumableDownload.RunAsync(unknownClient,uri,partial,"key",".iso",1,hash,_=>{},default,TimeSpan.Zero);Check(knownHash==hash,"Strong ETag resume with previously unknown length failed");
            Console.WriteLine($"PASS: {count} resume/retry/ETag/hash controls.");
        }finally{Directory.Delete(folder,true);}
    }
    static HttpResponseMessage Reply(Stream body,string etag,long length){var r=new HttpResponseMessage(HttpStatusCode.OK){Content=new StreamContent(body)};r.Headers.ETag=new EntityTagHeaderValue(etag);r.Content.Headers.ContentLength=length;return r;}
    static HttpResponseMessage Partial(byte[] body,string etag,long offset,long total){var r=Reply(new MemoryStream(body),etag,body.Length);r.StatusCode=HttpStatusCode.PartialContent;r.Content.Headers.ContentRange=new ContentRangeHeaderValue(offset,total-1,total);return r;}
    sealed class Handler(Func<HttpRequestMessage,int,HttpResponseMessage> callback):HttpMessageHandler {internal int Calls;protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){var r=callback(request,++Calls);r.RequestMessage=request;return Task.FromResult(r);}}
    sealed class FaultStream(byte[] bytes):MemoryStream(bytes) {int reads;public override ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken token=default){if(reads++==0)return base.ReadAsync(buffer[..3],token);throw new IOException("Simulated network drop");}}
    sealed class InterruptStream(byte[] bytes,Action cancel):MemoryStream(bytes) {int reads;public override ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken token=default){if(reads++==0)return base.ReadAsync(buffer[..3],token);cancel();throw new OperationCanceledException(token);}}
}
