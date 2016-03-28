using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoVote
{
    public class ProxyFactory
    {
        static ProxyFactory()
        {
            ThreadPool.SetMinThreads(200, 200);
            ProxySource proxySource = (ProxySource)Enum.Parse(typeof(ProxySource), ConfigurationManager.AppSettings["proxySource"]);
            NewProxySource(proxySource);
        }

        private static void NewProxySource(ProxySource proxySource)
        {
            switch (proxySource)
            {
                case ProxySource.kuaidaili:
                    ProxyFactory.proxySource = new kuaidaili();
                    break;
                case ProxySource.youdaili:
                    ProxyFactory.proxySource = new youdaili();
                    break;
                case ProxySource.xicidaili:
                    ProxyFactory.proxySource = new xicidaili();
                    break;
            }
        }

        private static int pageIndex = 1;
        private static ProxySourceBase proxySource;
        private static ConcurrentQueue<Proxy> proxyQueue = new ConcurrentQueue<Proxy>();

        public static void SetProxySource(ProxySource proxySource)
        {
            proxyQueue = new ConcurrentQueue<Proxy>();
            pageIndex = 0;
            NewProxySource(proxySource);
        }

        public static Proxy GetProxy()
        {
            if (proxyQueue.Count == 0)
            {
                FillQueue();
            }
            Proxy proxy;
            proxyQueue.TryDequeue(out proxy);
            return proxy;
        }

        private static void FillQueue()
        {
            List<Proxy> testList = new List<Proxy>();
            int count = 0;
            while (count < 100)
            {
                List<Proxy> proxyList = proxySource.GetList(pageIndex);
                if (proxyList.Count == 0)
                {
                    break;
                }
                testList.AddRange(proxyList);
                count = count + proxyList.Count;
                pageIndex++;
            }

            List<Task> taskList = new List<Task>();
            foreach (var item in testList)
            {
                Proxy proxy = item;
                Task task = Task.Run(() =>
                {
                    try
                    {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://www.baidu.com");
                        request.Timeout = 10 * 1000;
                        WebProxy webProxy = new WebProxy(proxy.Host, proxy.Port);
                        request.Proxy = webProxy;
                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            proxyQueue.Enqueue(proxy);
                        }
                    }
                    catch
                    { }
                });
                taskList.Add(task);
            }

            Task.WaitAll(taskList.ToArray());
        }
    }

    public abstract class ProxySourceBase
    {
        public abstract List<Proxy> GetList(int pageIndex);
    }

    public class kuaidaili : ProxySourceBase
    {
        public override List<Proxy> GetList(int pageIndex)
        {
            List<Proxy> proxyList = new List<Proxy>();
            try
            {
                WebClient client = new WebClient();
                client.Proxy = null;
                byte[] data = client.DownloadData(string.Format(@"http://www.kuaidaili.com/free/inha/{0}/", pageIndex + 1));
                string html = Encoding.UTF8.GetString(data);

                int dataBegin = html.IndexOf("最后验证时间");
                int dataEnd = html.IndexOf("</tbody>", dataBegin);
                int index = dataBegin;
                while (true)
                {
                    int begin = html.IndexOf("<td>", index) + 4;
                    int end = html.IndexOf("</td>", begin);
                    string host = html.Substring(begin, end - begin);
                    index = end;
                    begin = html.IndexOf("<td>", index) + 4;
                    end = html.IndexOf("</td>", begin);
                    int port = int.Parse(html.Substring(begin, end - begin));
                    proxyList.Add(new Proxy(host, port));

                    index = html.IndexOf("<tr>", end);
                    if (index == -1 || index > dataEnd)
                    {
                        break;
                    }
                }
            }
            catch
            { }

            return proxyList;
        }
    }

    public class youdaili : ProxySourceBase
    {
        public override List<Proxy> GetList(int pageIndex)
        {
            throw new NotImplementedException();
        }
    }

    public class xicidaili : ProxySourceBase
    {
        public override List<Proxy> GetList(int pageIndex)
        {
            List<Proxy> proxyList = new List<Proxy>();
            try
            {
                WebClient client = new WebClient();
                client.Proxy = null;
                client.Headers["User-Agent"] = "User - Agent: Mozilla / 5.0(Windows NT 6.1; WOW64; Trident / 7.0; rv: 11.0) like Gecko";
                byte[] data = client.DownloadData(string.Format(@"http://www.xicidaili.com/nn/{0}", pageIndex + 1));
                string html = Encoding.UTF8.GetString(data);

                int dataBegin = html.IndexOf("验证时间");
                int dataEnd = html.IndexOf("</table>", dataBegin);
                int index = html.IndexOf("<img", dataBegin);
                while (true)
                {
                    int begin = html.IndexOf("<td>", index) + 4;
                    int end = html.IndexOf("</td>", begin);
                    string host = html.Substring(begin, end - begin);
                    index = end;
                    begin = html.IndexOf("<td>", index) + 4;
                    end = html.IndexOf("</td>", begin);
                    int port = int.Parse(html.Substring(begin, end - begin));
                    proxyList.Add(new Proxy(host, port));

                    index = html.IndexOf("<img", end);
                    if (index == -1 || index > dataEnd)
                    {
                        break;
                    }
                }
            }
            catch
            { }

            return proxyList;
        }
    }

    public enum ProxySource
    {
        kuaidaili,
        youdaili,
        xicidaili
    }

    public class Proxy
    {
        public Proxy(string host, int port)
        {
            Host = host;
            Port = port;
        }

        public string Host { get; private set; }
        public int Port { get; private set; }
    }
}
