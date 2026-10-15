import unittest, urllib.error, io
from link_health import allowed,classify,probe,reconcile,TITLE,MARKER
from unittest.mock import patch
class Response:
    status=200;url='https://download.microsoft.com/file.iso';headers={'Content-Type':'application/octet-stream'}
    def __enter__(self):return self
    def __exit__(self,*args):return False
    def read(self,*args):raise AssertionError('Media content must never be downloaded')
class LinkTests(unittest.TestCase):
    def test_hosts(self):
        self.assertTrue(allowed('https://download.microsoft.com/file.iso'));self.assertFalse(allowed('https://microsoft.com.evil.test/file'));self.assertFalse(allowed('http://download.microsoft.com/file'));self.assertFalse(allowed('https://user@download.microsoft.com/file'))
    def test_classification(self):
        self.assertEqual(classify(404,''),'broken');self.assertEqual(classify(403,''),'inconclusive');self.assertEqual(classify(429,''),'inconclusive');self.assertEqual(classify(200,'text/html'),'broken');self.assertEqual(classify(206,'application/octet-stream'),'healthy')
    def test_head(self):
        class Opener:
            def open(self,req,timeout):self.request=req;return Response()
        o=Opener();self.assertEqual(probe('https://download.microsoft.com/file.iso',o,lambda _:None)['state'],'healthy');self.assertEqual(o.request.get_method(),'HEAD')
    def test_fallback(self):
        class Opener:
            count=0
            def open(self,req,timeout):
                self.count+=1
                if self.count==1:raise urllib.error.HTTPError(req.full_url,405,'Unsupported',{},io.BytesIO())
                assert req.get_method()=='GET' and req.get_header('Range')=='bytes=0-0';return Response()
        self.assertEqual(probe('https://download.microsoft.com/file.iso',Opener(),lambda _:None)['state'],'healthy')
    def test_issue_dedup(self):
        existing={'title':TITLE,'body':MARKER,'number':12,'state':'open','user':{'login':'github-actions[bot]'}};calls=[]
        def api(*args):calls.append(args);return [existing] if args[2].startswith('/issues?') else {}
        with patch('link_health.api',api):reconcile([{'url':'https://download.microsoft.com/file.iso','state':'broken','status':404,'detail':'Not Found'}],'owner/repo','test-token')
        self.assertEqual(calls[-1][2],'/issues/12');self.assertEqual(calls[-1][-1],'PATCH')
    def test_uncertain_does_not_close(self):
        existing={'title':TITLE,'body':MARKER,'number':12,'state':'open','user':{'login':'github-actions[bot]'}}
        with patch('link_health.api',return_value=[existing]) as api:reconcile([{'url':'https://download.microsoft.com/file.iso','state':'inconclusive','detail':'403'}],'owner/repo','test-token');self.assertEqual(api.call_count,1)
if __name__=='__main__':unittest.main()
