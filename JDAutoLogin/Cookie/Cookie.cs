using CefSharp;
using Fiddler;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Drawing;
using System.Net;
using System.Xml.Linq;
using System.Web;
using CefSharp.OffScreen;

namespace JDAutoLogin
{
    public class Cookie
    {
        private AutoResetEvent resetEvent;
        private string username;
        private string password;
        private int trackingID;
        private bool status;
        private string vaildCookie;
        private string message;
        private string authCode;
        private ChromiumWebBrowser browser;

        private static int nextTrackingID = 0;
        private static ConcurrentDictionary<int, Cookie> cookieDic = new ConcurrentDictionary<int, Cookie>();

        static Cookie()
        {
            XDocument config;
            if (File.Exists("config.xml") == true)
            {
                config = XDocument.Load("config.xml");
                FiddlerApplication.Prefs.SetStringPref("fiddler.certmaker.bc.key", config.Root.Element("key").Value);
                FiddlerApplication.Prefs.SetStringPref("fiddler.certmaker.bc.cert", config.Root.Element("cert").Value);
            }
            else
            {
                CertMaker.createRootCert();
                config = new XDocument();
                config.Add(new XElement("Root"));
                config.Root.Add(new XElement("key", FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.bc.key", null)));
                config.Root.Add(new XElement("cert", FiddlerApplication.Prefs.GetStringPref("fiddler.certmaker.bc.cert", null)));
                config.Save("config.xml");
            }
            FiddlerApplication.BeforeRequest += FiddlerApplication_BeforeRequest;
            FiddlerApplication.BeforeResponse += FiddlerApplication_BeforeResponse;
            FiddlerApplication.Startup(9999, false, true, false);

            CefSettings settings = new CefSettings();
            settings.CefCommandLineArgs.Add("proxy-server", "127.0.0.1:9999");
            settings.IgnoreCertificateErrors = true;
            Cef.Initialize(settings);
        }

        private static void FiddlerApplication_BeforeRequest(Session oSession)
        {
            if (oSession.fullUrl.StartsWith("https://passport.jd.com/new/login.aspx?ReturnUrl=https%3A%2F%2Fwww.jd.com%2F&trackingID=") == true)
            {
                oSession.bBufferResponse = true;
            }

            if (oSession.fullUrl == "https://www.jd.com/")
            {
                string cookieString = oSession.RequestHeaders["Cookie"];
                int trackingID = GetTrackingID(cookieString);
                cookieString = cookieString.Replace("trackingID=" + trackingID, "");
                if (cookieString.Length == 0)
                {
                    cookieDic[trackingID].message = "Request too Fast";
                    cookieDic[trackingID].status = false;
                }
                else
                {
                    cookieDic[trackingID].vaildCookie = cookieString;
                    cookieDic[trackingID].status = true;
                }
                
                cookieDic[trackingID].resetEvent.Set();
            }
        }

        private static void FiddlerApplication_BeforeResponse(Session oSession)
        {
            if (oSession.fullUrl.StartsWith("https://passport.jd.com/new/login.aspx?ReturnUrl=https%3A%2F%2Fwww.jd.com%2F&trackingID=") == true)
            {
                string setCookie = "trackingID={0}; Domain=.jd.com; Path=/;";
                setCookie = string.Format(setCookie, HttpUtility.ParseQueryString(oSession.fullUrl).Get("trackingID"));
                oSession.oResponse.headers.Add("Set-Cookie", setCookie);
            }

            if (oSession.fullUrl.StartsWith("https://authcode.jd.com/verify/image") == true)
            {
                int trackingID = GetTrackingID(oSession.RequestHeaders["Cookie"]);
                byte[] data = oSession.ResponseBody;
                //////////
                //File.WriteAllBytes(@"D:\image\" + DateTime.Now.Ticks + ".jpg", data);
                //////////
                Bitmap bitmap = new Bitmap(new MemoryStream(data));
                string authCode = AuthCodeRecognition.Recognize(bitmap);
                int count = 0;
                while (authCode == string.Empty)
                {
                    count++;
                    if(count > 20)
                    {
                        authCode = "FFFF";
                        break;
                    }

                    HttpWebRequest request = WebRequest.Create(oSession.fullUrl) as HttpWebRequest;
                    request.Method = "GET";
                    request.Referer = oSession.RequestHeaders["Referer"];
                    request.Headers["Cookie"] = oSession.RequestHeaders["Cookie"];
                    request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";

                    WebResponse response = request.GetResponse();
                    Stream dataStream = response.GetResponseStream();
                    Bitmap renewBitmap = new Bitmap(dataStream);
                    authCode = AuthCodeRecognition.Recognize(renewBitmap);
                }
                cookieDic[trackingID].authCode = authCode;
            }
        }

        private Cookie(string username, string password, int trackingID)
        {
            this.username = username;
            this.password = password;
            this.trackingID = trackingID;
            this.resetEvent = new AutoResetEvent(false);
        }

        public static CookieResult GetCookie(string username, string password)
        {
            int trackingID = Interlocked.Increment(ref Cookie.nextTrackingID);
            Cookie cookie = new Cookie(username, password, trackingID);
            cookieDic[trackingID] = cookie;

            Task.Factory.StartNew((object obj) => 
            {
                Cookie innerCookie = obj as Cookie;
                string browserAddress = "https://passport.jd.com/new/login.aspx?ReturnUrl=https%3A%2F%2Fwww.jd.com%2F" + "&trackingID=" + innerCookie.trackingID;

                ChromiumWebBrowser browser = new ChromiumWebBrowser(browserAddress, null, new RequestContext(new RequestContextSettings { IgnoreCertificateErrors = true }));
                innerCookie.browser = browser;
                browser.RegisterJsObject("jsObj", new JSInterop(innerCookie));

                browser.FrameLoadEnd += innerCookie.FrameLoadEnd;
            }, cookie);

            cookie.resetEvent.WaitOne(20000);
            cookieDic.TryRemove(trackingID, out cookie);
            if (cookie.browser != null)
            {
                cookie.browser.Dispose();
            }
            return new CookieResult(cookie.status, cookie.message, cookie.vaildCookie);
        }

        private static int GetTrackingID(string cookie)
        {
            var cookieList = cookie.Split(';').Select(i => i.Trim());
            int trackingID = 0;
            foreach (var item in cookieList)
            {
                if (item.StartsWith("trackingID=") == true)
                {
                    string id = item.Split('=')[1];
                    trackingID = int.Parse(id);
                    break;
                }
            }
            return trackingID;
        }

        private class JSInterop
        {
            private Cookie cookie;

            public JSInterop(Cookie cookie)
            {
                this.cookie = cookie;
            }

            public string fillAuthCode()
            {
                int count = 0;
                string authCode = "FFFF";
                while (count < 20)
                {
                    if (this.cookie.authCode == null)
                    {
                        Thread.Sleep(1000);
                        count++;
                    }
                    else
                    {
                        authCode = this.cookie.authCode;
                        break;
                    }
                }
                return authCode;
            }

            public void error(string message)
            {
                this.cookie.status = false;
                this.cookie.message = message;
                this.cookie.resetEvent.Set();
            }
        }

        private async void FrameLoadEnd(object sender, FrameLoadEndEventArgs e)
        {
            ChromiumWebBrowser browser = sender as ChromiumWebBrowser;

            browser.FrameLoadEnd -= this.FrameLoadEnd;

            string js;

            js = "document.getElementById('loginname').value='" + this.username + "';" +
                 "document.getElementById('nloginpwd').value='" + this.password + "';" +
                 "document.getElementById('loginsubmit').click();";
            await browser.EvaluateScriptAsync(js);

            js = @"var intervalID = setInterval(function () {
                                                var content = $('div .msg-error').first().html();
                                                if (content !== '<b></b>') {
                                                    clearInterval(intervalID);
                                                    if(content === '<b></b>请输入验证码'){
                                                        var authCode = jsObj.fillAuthCode();
                                                        document.getElementById('authcode').value=authCode;
                                                        document.getElementById('loginsubmit').click();
                                                        var innerIntervalID = setInterval(function () {
                                                            if(content !== '<b></b>请输入验证码') {
                                                                clearInterval(innerIntervalID);
                                                                jsObj.error(content);
                                                            }
                                                        });
                                                    }
                                                    else{
                                                        jsObj.error(content);
                                                    }
                                                }
                                            }, 1000);";
            await browser.EvaluateScriptAsync(js);
        }
    }

    public struct CookieResult
    {
        public CookieResult(bool status, string message, string cookie)
        {
            Status = status;
            Cookie = cookie;
            Message = message;
        }

        public bool Status;
        public string Cookie;
        public string Message;
    }
}