"""Probe exported ServerCatalog links without downloading media; reconcile one bot issue."""
import argparse, concurrent.futures, datetime, json, os, time, urllib.request, urllib.error, urllib.parse
TITLE = '[link-health] Windows Server download links require attention'
MARKER = '<!-- wid-link-health-v1 -->'
def allowed(url):
    p=urllib.parse.urlsplit(url)
    return p.scheme=='https' and not p.username and p.port in (None,443) and (p.hostname=='aka.ms' or p.hostname=='microsoft.com' or (p.hostname or '').endswith('.microsoft.com'))
class Redirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self,req,fp,code,msg,headers,newurl):
        if not allowed(newurl): raise ValueError('Untrusted redirect destination')
        return super().redirect_request(req,fp,code,msg,headers,newurl)
def classify(status,content_type):
    if status in (404,410):return 'broken'
    if status in (401,403,408,429) or status>=500:return 'inconclusive'
    if 200<=status<300:return 'broken' if 'text/html' in content_type.lower() else 'healthy'
    return 'inconclusive'
def probe(url,opener=None,delay=time.sleep):
    if not allowed(url): return {'url':url,'state':'broken','detail':'Untrusted catalog URL'}
    opener=opener or urllib.request.build_opener(Redirect())
    last=None
    for attempt in range(3):
        try:
            for method in ('HEAD','GET'):
                try:
                    headers={'User-Agent':'WindowsISODownloader-link-health/3.6.0'}
                    if method=='GET':headers['Range']='bytes=0-0'
                    with opener.open(urllib.request.Request(url,method=method,headers=headers),timeout=30) as r:
                        # Close immediately; never read a potentially multi-GB response body.
                        state=classify(r.status,r.headers.get('Content-Type',''))
                        last={'url':url,'state':state,'status':r.status,'final_url':r.url,'detail':r.headers.get('Content-Type','')}
                    break
                except urllib.error.HTTPError as e:
                    if method=='HEAD' and e.code in (403,405,501):e.close();continue
                    last={'url':url,'state':classify(e.code,''),'status':e.code,'detail':str(e)};e.close();break
            if last and last['state']=='healthy':return last
        except Exception as e:last={'url':url,'state':'inconclusive','detail':str(e)}
        if attempt<2:delay(2*(attempt+1))
    return last or {'url':url,'state':'inconclusive','detail':'No response'}
def api(repo,token,path,data=None,method=None):
    body=json.dumps(data).encode() if data is not None else None
    req=urllib.request.Request('https://api.github.com/repos/'+repo+path,data=body,method=method,headers={'Authorization':'Bearer '+token,'User-Agent':'WID-link-health','Accept':'application/vnd.github+json','Content-Type':'application/json','X-GitHub-Api-Version':'2022-11-28'})
    with urllib.request.urlopen(req,timeout=30) as response:return json.load(response)
def reconcile(results,repo,token):
    issues=[]
    for page in range(1,11):
        batch=api(repo,token,f'/issues?state=all&per_page=100&page={page}');issues.extend(batch)
        if len(batch)<100:break
    match=next((i for i in issues if i.get('title')==TITLE and MARKER in (i.get('body') or '') and i.get('user',{}).get('login')=='github-actions[bot]' and 'pull_request' not in i),None)
    broken=[r for r in results if r['state']=='broken'];uncertain=[r for r in results if r['state']=='inconclusive']
    body=MARKER+'\nWeekly ServerCatalog check: '+datetime.datetime.now(datetime.timezone.utc).isoformat()+'\n\n'
    body+='HEAD requests with bounded Range GET fallback; no ISO or VHD content downloaded.\n\n'
    body+='Confirmed failures:\n'+ '\n'.join('- '+r['url']+' — '+str(r.get('status',r['detail'])) for r in broken)
    if uncertain:body+='\n\nInconclusive (not classified as broken):\n'+'\n'.join('- '+r['url']+' — '+r['detail'] for r in uncertain)
    if broken:
        if match:api(repo,token,'/issues/'+str(match['number']),{'body':body,'state':'open'},'PATCH')
        else:api(repo,token,'/issues',{'title':TITLE,'body':body},'POST')
    elif match and not uncertain and match['state']=='open':api(repo,token,'/issues/'+str(match['number']),{'state':'closed','body':body+'\nAll catalog links responded successfully.'},'PATCH')
def main():
    parser=argparse.ArgumentParser();parser.add_argument('catalog');parser.add_argument('--report',default='link-health.json');parser.add_argument('--issues',action='store_true');args=parser.parse_args()
    links=json.load(open(args.catalog,encoding='utf-8-sig'))
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:results=list(pool.map(probe,links))
    with open(args.report,'w',encoding='utf-8') as f:json.dump(results,f,ensure_ascii=False,indent=2)
    print(json.dumps({s:sum(r['state']==s for r in results) for s in ('healthy','broken','inconclusive')}))
    if args.issues:reconcile(results,os.environ['GITHUB_REPOSITORY'],os.environ['GH_TOKEN'])
if __name__=='__main__':main()
