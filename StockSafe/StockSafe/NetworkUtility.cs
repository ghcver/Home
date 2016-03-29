using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace StockSafe
{
    class NetworkUtility
    {
        private static string ip;
        private static int port;
        private static X509Certificate2Collection cerCollection;
        private static string serverName;
        private static bool isInited;

        public static void Init(string ip, int port, X509Certificate2 cer, string serverName)
        {
            NetworkUtility.ip = ip;
            NetworkUtility.port = port;
            NetworkUtility.cerCollection = new X509Certificate2Collection(cer);
            NetworkUtility.serverName = serverName;
            isInited = true;
        }

        public NetworkUtility()
        {
            if (isInited == false)
            {
                throw new Exception("NetworkUtility isn't Inited!!!");
            }
        }

        private TcpClient client;
        private SslStream sslStream;

        private void InitSSL()
        {
            if (sslStream != null)
            {
                sslStream.Close();
            }
            if (client != null)
            {
                client.Close();
            }
            client = new TcpClient(ip, port);
            client.SendTimeout = 10000;
            client.ReceiveTimeout = 10000;
            sslStream = new SslStream(client.GetStream(), false, RemoteCertificateValidationCallback);
            sslStream.AuthenticateAsClient(serverName, cerCollection, SslProtocols.Default, false);
        }

        private bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        public byte[] Send(byte[] data)
        {
            try
            {
                InitSSL();

                byte[] buffer = new byte[client.ReceiveBufferSize];

                while (client.Available > 0)
                {
                    sslStream.Read(buffer, 0, buffer.Length);
                }

                sslStream.Write(data);
                sslStream.Flush();

                int bytes = sslStream.Read(buffer, 0, buffer.Length);
                if (bytes > 0)
                {
                    byte[] result = new byte[bytes];
                    Array.Copy(buffer, result, bytes);
                    return result;
                }
                else
                {
                    throw new Exception("Network is disconneted by Server!!!");
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public string GetHttpResponse(string url)
        {
            while (true)
            {
                try
                {
                    HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                    request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                    request.Method = "GET";
                    request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";
                    HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                    string html = new StreamReader(response.GetResponseStream(), Encoding.UTF8).ReadToEnd();
                    this.CurrentHtml = html;

                    return html;
                }
                catch (Exception ex)
                {

                }
            }
        }

        public string CurrentHtml { get; private set; }
    }
}
