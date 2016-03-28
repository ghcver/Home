using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace AutoVote
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(new Action(DoWork));
            Console.ReadLine();
        }

        private static void DoWork()
        {
            Random rnd = new Random();
            int i = 0;
            while (i < 10)
            {
                Console.WriteLine("----------- " + DateTime.Now.ToString() + " -----------");
                try
                {
                    DoWorkInternal();
                }
                catch
                {}
                i++;
                Thread.Sleep(rnd.Next(1, 10) * 1000 * 5);
            }

            Console.WriteLine("----------- Done -----------");
        }

        private static HttpWebRequest CreateWebRequest(string url, Proxy proxy)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = @"Mozilla/5.0 (Linux; U; Android 5.1.1; zh-cn; SCL-AL00 Build/HonorSCL-AL00) AppleWebKit/533.1 (KHTML, like Gecko)Version/4.0 MQQBrowser/5.4 TBS/025491 Mobile Safari/533.1 MicroMessenger/6.3.15.49_r8aff805.760 NetType/WIFI Language/zh_CN";
            bool useProxy = bool.Parse(ConfigurationManager.AppSettings["useProxy"]);
            if (proxy != null)
            {
                WebProxy webProxy = new WebProxy(proxy.Host, proxy.Port);
                request.Proxy = webProxy;
            }
            return request;
        }

        private static void DoWorkInternal()
        {
            string url;
            HttpWebRequest request;
            HttpWebResponse response;
            Stream dataStream;
            CookieContainer cookieContainer;
            string tpid;
            string xxid;
            string forWho;

            bool useProxy = bool.Parse(ConfigurationManager.AppSettings["useProxy"]);
            Proxy proxy = null;
            if (useProxy == true)
            {
                proxy = ProxyFactory.GetProxy();
            }

            url = ConfigurationManager.AppSettings["url"];
            request = CreateWebRequest(url, proxy);
            cookieContainer = new CookieContainer();
            request.CookieContainer = cookieContainer;
            response = (HttpWebResponse)request.GetResponse();
            dataStream = response.GetResponseStream();

            StreamReader reader = new StreamReader(dataStream, Encoding.GetEncoding("gb2312"));
            string responseString = reader.ReadToEnd();
            reader.Close();
            NameValueCollection queryparams = HttpUtility.ParseQueryString(new Uri(url).Query);
            tpid = queryparams["tpid"];
            xxid = queryparams["xxid"];
            int index = responseString.IndexOf("selected");
            index = responseString.IndexOf("-", index);
            forWho = responseString.Substring(index + 1, responseString.IndexOf("<", index) - index - 1).Trim('\t');

            dataStream.Close();
            response.Close();

            ////////////////////////////////////////////////////////

            url = "http://erweima.ping10.cn/include/regecode.php";
            request = CreateWebRequest(url, proxy);
            request.CookieContainer = cookieContainer;
            response = (HttpWebResponse)request.GetResponse();
            dataStream = response.GetResponseStream();

            Bitmap bitmap = new Bitmap(dataStream);
            string code = CAPTCHA.GetCode(bitmap);

            dataStream.Close();
            response.Close();

            //////////////////////////////////////////////////////////////

            string name = NameFactory.GetName();
            Console.WriteLine(name);
            name = HttpUtility.UrlEncode(name);
            string comment = CommentFactory.GetComment(forWho);
            Console.WriteLine(comment);
            comment = HttpUtility.UrlEncode(comment);
            string postDataString = string.Format("uname={0}&xxid={1}&Content={2}&Code={3}&ac=ly&tpid={4}", name, xxid, comment, code, tpid);
            byte[] postData = Encoding.UTF8.GetBytes(postDataString);

            url = "http://erweima.ping10.cn/m/guestbook.php";
            request = CreateWebRequest(url, proxy);
            request.ServicePoint.Expect100Continue = false;
            request.CookieContainer = cookieContainer;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = postData.Length;
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(postData, 0, postData.Length);
            requestStream.Close();

            response = (HttpWebResponse)request.GetResponse();
            dataStream = response.GetResponseStream();

            reader = new StreamReader(dataStream, Encoding.GetEncoding("gb2312"));
            responseString = reader.ReadToEnd();
            reader.Close();

            dataStream.Close();
            response.Close();

            Console.WriteLine(responseString);
        }
    }

    class CommentFactory
    {
        private static Random rnd;
        private static List<string> commentList;
        static CommentFactory()
        {
            rnd = new Random();
            commentList = "工作能力強|有远见|有社会责任感|{0}我们支持你|值得信赖的人|正能量|善良|{0}我们为你加油|诲人不倦|传送正能量|以德服人|精益求精|呕心沥血|诚实守信|为人正直善良|{0}加油你会成功的|投票支持|再接再厉|工作态度好认真负责|非常优秀|服务社会|勇于创新|你是我们的楷模|对工作尽心尽责|年轻有为|值得大家学习|为你点赞|你是最棒的|造福大连|为您加油|为{0}投票|祝{0}成功|祝{0}的事业越做越大|持续发展|奉献社会|实干兴帮|领军人物，加油哦|利国利民|为大连经济繁荣做贡献|工作敬业|支持，支持，支持，支持|非常有潜力|行业领军人才".Split('|').ToList();
        }

        public static string GetComment(string name)
        {
            string s1 = commentList[rnd.Next(commentList.Count)];
            string s2 = commentList[rnd.Next(commentList.Count)];
            string s;
            if (s1.Contains("{0}") == true)
            {
                s = s2 + "，" +  s1;
            }
            else
            {
                s = s1 + "，" + s2;
            }

            s = string.Format(s, name);
            return s;
        }
    }

    class NameFactory
    {
        private static List<char> familyNameList;
        private static List<string> nameList;
        private static Random rnd;

        static NameFactory()
        {
            rnd = new Random();
            familyNameList = "吴高罗韩萧许吕贾阎朱马谢董邓彭蔡叶胡郭郑于袁曾蒋薛孙何宋冯曹沈卢魏徐林梁唐程傅苏丁杨赵黄周吴李王张刘陈".ToList();
            nameList = "筠|柔|竹|霭|凝|晓|欢|霄|枫|芸|菲|寒|伊|亚|宜|可|姬|舒|影|荔|枝|思|丽|秀|娟|英|华|慧|巧|美|娜|静|淑|惠|珠|翠|雅|芝|玉|萍|红|娥|玲|芬|芳|燕|彩|春|菊|勤|珍|贞|莉|兰|凤|洁|梅|琳|素|云|莲|真|环|雪|荣|爱|妹|霞|香|月|莺|媛| 艳|瑞|凡|佳|嘉|琼|桂|娣|叶|璧|璐|娅|琦|晶|妍|茜|秋|珊|莎|锦|黛|青|倩|婷|姣|婉|娴|瑾|颖|露|瑶|怡|婵|雁|蓓|纨|仪|荷|丹|蓉|眉|君|琴|蕊|薇|菁|梦|岚| 苑|婕|馨|瑗|琰|韵|融|园|艺|咏|卿|聪|澜|纯|毓|悦|昭|冰|爽|琬|茗|羽|希|宁|欣|飘|育|滢|馥|问萍|青蕾|雁云|芷枫|千旋|向梅|含蓝|怀丝|梦文|幼芙|晓云|雨旋|秋安|雁风|碧槐|从海|语雪|幼凡|秋卉|曼蕾|问蕾|访兰|寄莲|紫绿|新雁|恨容|水柳|南云|曼阳|幼蓝|忆巧|灵荷|怜兰|听曼|碧双|忆雁|夜松|映莲|听曼|秋易|绿莲|宛秋|雁安|问旋|以蓝|若亦|幻丝|山凡|南云|寄蕊|绿春|思海|寄天|友秋|紫玉|从筠|雪海|白筠|灵芙|安莲|惜梅|雪蕾|寄容|秋波|冷云|秋儿|怀菱|亦柏|易槐|怀卉|紫桃|向蕊|易青|千蕊|怜露|灵旋|怀梅|天柏|半白|碧安|秋枫|傲丝|春柔|冰岚|雅翠|易白|夜灵|静柔|醉绿|乐蕊|寄蓝|乐彤|迎琴|之亦|雨寒|谷山|凝安|曼萍|碧露|书南|山薇|念珊|芷雁|尔蕾|绮雪|傲萱|新琴|绿蝶|慕旋|怀易|傲云|晓梅|诗菱|灵珊|幻香|若云|如霜|晓晴|灵山|恨桃|梦凝|幻彤|觅波|慕玉|念山|乐桃|语寒|怀海|孤蝶|灵凝|慕蓝|紫青|千兰|孤柔|语曼|问海|寄筠|安露|听晴|冷寒|之翠|碧灵|凡丝|诗波|友芙|寄莲|之蕊|海琴|宛筠|半山|依槐|觅曼|碧菱|半文|访儿|芷珍|绿春|春蝶|怜槐|映露|雨卉|灵亦|惜莲|念菡|南凡|曼桃|笑灵|惜安|凌筠|翠菡|寒雁|以山|秋彤|巧兰|山雪|寒绿|忆易|依萱|如菡|含萱|惜梦|绮莲|翠易|冷筠|乐槐|傲青|幼灵|春柔|恨易|青文|初竹|从旋|沛山|映凝|静柳|雪云|醉蕊|巧荷|思蓝|翠秋".Split('|').ToList();
        }

        public static string GetName()
        {
            return familyNameList[rnd.Next(familyNameList.Count)] + nameList[rnd.Next(nameList.Count)];
        }
    }

    class CAPTCHA
    {
        private static Dictionary<string, Bitmap> letterDic;

        static CAPTCHA()
        {
            letterDic = new Dictionary<string, Bitmap>();
            foreach (var item in Directory.EnumerateFiles("letters", "*.png"))
            {
                letterDic[Path.GetFileNameWithoutExtension(item)] = new Bitmap(item);
            }
        }

        public static string GetCode(Bitmap bitmap)
        {
            string result = GetChar(bitmap.Clone(Rectangle.FromLTRB(2, 6, 9, 15), PixelFormat.Format24bppRgb))
                          + GetChar(bitmap.Clone(Rectangle.FromLTRB(11, 6, 18, 15), PixelFormat.Format24bppRgb))
                          + GetChar(bitmap.Clone(Rectangle.FromLTRB(20, 6, 27, 15), PixelFormat.Format24bppRgb))
                          + GetChar(bitmap.Clone(Rectangle.FromLTRB(29, 6, 36, 15), PixelFormat.Format24bppRgb));

            return result;
        }

        private static string GetChar(Bitmap bitmap)
        {
            Dictionary<string, int> hitDic = new Dictionary<string, int>();
            foreach (var item in letterDic.Keys)
            {
                hitDic[item] = 0;
            }

            for (int i = 0; i < bitmap.Width; i++)
            {
                for (int j = 0; j < bitmap.Height; j++)
                {
                    Color color = bitmap.GetPixel(i, j);
                    if (color != Color.FromArgb(255, 255, 255, 255) && color != Color.FromArgb(255, 0, 0, 0))
                    {
                        continue;
                    }
                    foreach (var item in hitDic.Keys.ToArray())
                    {
                        if (letterDic[item].GetPixel(i, j) == color)
                        {
                            hitDic[item] = hitDic[item] + 1;
                        }
                    }
                }
            }

            return hitDic.OrderByDescending(i => i.Value).First().Key;
        }
    }
}
