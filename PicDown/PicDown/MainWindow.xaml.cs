using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace PicDown
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void CommandBinding_Paste_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CommandBinding_Paste_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.textBox.Text = Clipboard.GetText();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            string uid = this.textBox.Text.Trim();
            if (uid == string.Empty)
            {
                return;
            }

            Task.Factory.StartNew(Download, new object[] { this.comboBox.SelectedIndex, uid });

            this.button.IsEnabled = false;
        }

        private void Download(object obj)
        {
            object[] paras = (object[])obj;
            int index = (int)paras[0];
            string uid = (string)paras[1];

            try
            {
                switch (index)
                {
                    case 0:
                        ShiJiJiaYuan(uid);
                        break;
                    case 1:
                        ZhenAi(uid);
                        break;
                }
            }
            catch (Exception ex)
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show(ex.ToString(), "Error!!!");
                }));
            }
            finally
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.button.IsEnabled = true;
                }));
            }
        }

        private void ZhenAi(string uid)
        {
            string url = "http://album.zhenai.com/u/" + uid;
            WebClient client = new WebClient();
            byte[] webByte = client.DownloadData(url);

            int beginIndex = FindIndex(webByte, Encoding.UTF8.GetBytes("AblumsThumbsListID"), 0);
            int endIndex = FindIndex(webByte, Encoding.UTF8.GetBytes("</div>"), beginIndex);
            webByte = webByte.Skip(beginIndex).Take(endIndex - beginIndex).ToArray();

            List<string> imageList = new List<string>();
            beginIndex = 0;
            while (true)
            {
                int urlBegin = FindIndex(webByte, Encoding.UTF8.GetBytes("data-big-img=\""), beginIndex);
                if (urlBegin == -1)
                {
                    break;
                }
                int urlEnd = FindIndex(webByte, Encoding.UTF8.GetBytes("\""), urlBegin + 14);
                string imageUrl = Encoding.UTF8.GetString(webByte.Skip(urlBegin + 14).Take(urlEnd - urlBegin - 14).ToArray());
                imageList.Add(imageUrl);
                beginIndex = urlEnd;
            }

            if (imageList.Count > 0)
            {
                string rootFolder = @"Download\ZhenAi";
                string imageFolder = System.IO.Path.Combine(rootFolder, uid);
                Directory.CreateDirectory(imageFolder);

                string body = string.Empty;
                for (int i = 0; i < imageList.Count; i++)
                {
                    byte[] data = client.DownloadData(imageList[i]);
                    string dataType = client.ResponseHeaders["Content-Type"];
                    if(string.IsNullOrEmpty(dataType)== false && dataType.StartsWith("image/"))
                    {
                        string fileExt = dataType.Split('/')[1];
                        string fileName = i + "." + fileExt;
                        File.WriteAllBytes(System.IO.Path.Combine(imageFolder, fileName), data);
                        body = body + "<img src='" + System.IO.Path.Combine(uid, fileName) + "'/>";
                    }
                }

                string htmlFileName = rootFolder + "\\" + uid + ".htm";
                File.WriteAllText(htmlFileName, string.Format("<!doctype html><html><body>{0}</body></html>", body));

                Process.Start(htmlFileName);
            }
        }

        private void ShiJiJiaYuan(string uid)
        {
            string url = "http://www.jiayuan.com/" + uid;
            WebClient client = new WebClient();
            byte[] webByte = client.DownloadData(url);

            int beginIndex = FindIndex(webByte, Encoding.UTF8.GetBytes("class=\"big_pic"), 0);
            int endIndex = FindIndex(webByte, Encoding.UTF8.GetBytes("</div>"), beginIndex);
            webByte = webByte.Skip(beginIndex).Take(endIndex - beginIndex).ToArray();

            List<string> imageList = new List<string>();
            beginIndex = 0;
            while (true)
            {
                int urlBegin = FindIndex(webByte, Encoding.UTF8.GetBytes("_src=\""), beginIndex);
                if (urlBegin == -1)
                {
                    break;
                }
                int urlEnd = FindIndex(webByte, Encoding.UTF8.GetBytes("\""), urlBegin + 6);
                string imageUrl = Encoding.UTF8.GetString(webByte.Skip(urlBegin + 6).Take(urlEnd - urlBegin - 6).ToArray());
                imageList.Add(imageUrl);
                beginIndex = urlEnd;
            }

            if(imageList.Count > 0)
            {
                string rootFolder = @"Download\ShiJiJiaYuan";
                string imageFolder = System.IO.Path.Combine(rootFolder, uid);
                Directory.CreateDirectory(imageFolder);

                string body = string.Empty;
                for(int i = 0; i < imageList.Count; i++)
                {
                    byte[] data = client.DownloadData(imageList[i]);
                    string dataType = client.ResponseHeaders["Content-Type"];
                    if (string.IsNullOrEmpty(dataType) == false && dataType.StartsWith("image/"))
                    {
                        string fileExt = dataType.Split('/')[1];
                        string fileName = i + "." + fileExt;
                        File.WriteAllBytes(System.IO.Path.Combine(imageFolder, fileName), data);
                        body = body + "<img src='" + System.IO.Path.Combine(uid, fileName) + "'/>";
                    }
                }

                string htmlFileName = rootFolder + "\\" + uid + ".htm";
                File.WriteAllText(htmlFileName, string.Format("<!doctype html><html><body>{0}</body></html>", body));

                Process.Start(htmlFileName);
            }
        }

        private int FindIndex(byte[] target, byte[] pattern, int beginIndex)
        {
            if (target == null || target.Length == 0)
            {
                return -1;
            }
            if (pattern == null || pattern.Length == 0)
            {
                return -1;
            }
            if (pattern.Length > target.Length)
            {
                return -1;
            }
            if (beginIndex < 0)
            {
                return -1;
            }

            for (int i = beginIndex; i < target.Length; i++)
            {
                if (i + pattern.Length > target.Length)
                {
                    return -1;
                }

                bool hit = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (target[i + j] != pattern[j])
                    {
                        hit = false;
                        break;
                    }
                }
                if (hit == true)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
