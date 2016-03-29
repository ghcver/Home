using iTextSharp.text;
using iTextSharp.text.pdf;
using Jurassic;
using mshtml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace NavigatedDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            SHDocVw.IWebBrowser2 axBrowser = typeof(WebBrowser).GetProperty("AxIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(browser, null) as SHDocVw.IWebBrowser2;
            axBrowser.Silent = true;
            ((SHDocVw.DWebBrowserEvents_Event)axBrowser).NewWindow += OnWebBrowserNewWindow;

            regexStrings = File.ReadAllLines("RegEx.txt").Select(i => i.Trim()).Where(i => i != string.Empty).ToArray();
        }

        private string[] regexStrings;
        private AutoResetEvent waitEvent = new AutoResetEvent(true);

        private void OnWebBrowserNewWindow(string URL, int Flags, string TargetFrameName, ref object PostData, string Headers, ref bool Processed)
        {
            Processed = true;
            browser.Navigate(URL);
        }

        private void goButton_Click(object sender, RoutedEventArgs e)
        {
            browser.Navigate(this.urlTextBox.Text);
        }

        private void browser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            string html = string.Empty;
            HTMLDocument doc = browser.Document as HTMLDocument;
            if (doc != null)
            {
                html = doc.documentElement.innerHTML;
            }
            if (Regex.IsMatch(html, regexStrings[0]) == true)
            {
                this.downloadButton.IsEnabled = true;
            }
        }

        private void browser_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            this.urlTextBox.Text = e.Uri.AbsoluteUri;
            this.downloadButton.IsEnabled = false;
        }

        private void downloadButton_Click(object sender, RoutedEventArgs e)
        {
            HTMLDocument doc = browser.Document as HTMLDocument;
            string domain = Regex.Match(browser.Source.AbsoluteUri, "^http://.*?(?=/)").Value;
            string refURL = browser.Source.AbsoluteUri;
            string html = doc.documentElement.innerHTML;
            string cookie = BrowserHelper.GetCookie(browser.Source.AbsoluteUri);

            this.downloadButton.IsEnabled = false;
            this.pdfButton.IsEnabled = false;
            this.urlTextBox.IsEnabled = false;
            this.urlTextBox.Text = string.Empty;
            this.goButton.IsEnabled = false;
            this.browser.IsEnabled = false;
            this.browser.Navigate("about:blank");

            string[] workObject = new string[] { domain, html, cookie, refURL };
            Thread tread = new Thread(DoWork);
            tread.Start(workObject);
        }

        private void DoWork(object obj)
        {
            string[] workObject = (string[])obj;
            string domain = workObject[0];
            string html = workObject[1];
            string cookie = workObject[2];
            string refURL = workObject[3];

            ScriptEngine engine = new ScriptEngine();

            if (Directory.Exists("Download") == false)
            {
                Directory.CreateDirectory("Download");
            }

            List<string[]> fileNameList = new List<string[]>();

            string jsPageDef = Regex.Match(html, regexStrings[1]).Value;
            string jsPages = Regex.Match(html, regexStrings[3]).Value;

            dynamic pages = engine.Evaluate(jsPageDef + jsPages);
            for (int k = 0; k < pages.Length && k < 8; k++)
            {
                int begin = 0;
                int end = 0;
                if (pages[k].Length != 2 || int.TryParse(pages[k][0].ToString(), out begin) == false || int.TryParse(pages[k][1].ToString(), out end) == false)
                {
                    continue;
                }
                if (begin > end)
                {
                    continue;
                }

                string pageNamePrefix = null;
                string formatPattern = null;
                string fileNamePrefix = null;
                switch (k)
                {
                    case 0:
                        pageNamePrefix = "cov";
                        formatPattern = "D3";
                        fileNamePrefix = "A-";
                        break;
                    case 1:
                        pageNamePrefix = "bok";
                        formatPattern = "D3";
                        fileNamePrefix = "B-";
                        break;
                    case 2:
                        pageNamePrefix = "leg";
                        formatPattern = "D3";
                        fileNamePrefix = "C-";
                        break;
                    case 3:
                        pageNamePrefix = "fow";
                        formatPattern = "D3";
                        fileNamePrefix = "D-";
                        break;
                    case 4:
                        pageNamePrefix = "!";
                        formatPattern = "D5";
                        fileNamePrefix = "E-";
                        break;
                    case 5:
                        pageNamePrefix = "";
                        formatPattern = "D6";
                        fileNamePrefix = "F-";
                        break;
                    case 6:
                        pageNamePrefix = "att";
                        formatPattern = "D3";
                        fileNamePrefix = "G-";
                        break;
                    case 7:
                        pageNamePrefix = "cov";
                        formatPattern = "D3";
                        fileNamePrefix = "H-";
                        break;
                }

                for (int j = begin; j <= end; j++)
                {
                    fileNameList.Add(new string[] { pageNamePrefix + j.ToString(formatPattern), fileNamePrefix + j.ToString() });
                }
            }

            string imgBaseURL = Regex.Match(html, regexStrings[2]).Value;
            foreach (var fileName in fileNameList)
            {
                string imgURL = domain + imgBaseURL + fileName[0] + "?zoom=2";
                MemoryStream ms = GetResponse(imgURL, cookie, refURL);
                File.WriteAllBytes(string.Format(@"Download\{0}.jpg", fileName[1]), ms.ToArray());
            }

            this.Dispatcher.BeginInvoke(new Action(ShowDone));
        }

        private void Reset()
        {
            this.urlTextBox.IsEnabled = true;
            this.urlTextBox.Text = string.Empty;
            this.goButton.IsEnabled = true;
            this.browser.IsEnabled = true;
            this.browser.Navigate("about:blank");
            this.downloadButton.IsEnabled = false;
            this.pdfButton.IsEnabled = true;
            this.codeImage.Source = null;
            this.codeTextBox.Text = string.Empty;
            this.codeTextBox.IsEnabled = false;
            this.codeButton.IsEnabled = false;
        }

        private void ShowDone()
        {
            MessageBox.Show("Done!");
            Reset();
        }

        private void EnableUI(Stream stream)
        {
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = stream;
            image.EndInit();
            this.codeImage.Source = image;

            this.codeTextBox.IsEnabled = true;
            this.codeTextBox.Focus();
            this.codeButton.IsEnabled = true;
        }

        private void DisableUI()
        {
            this.codeImage.Source = null;
            this.codeTextBox.Text = string.Empty;
            this.codeTextBox.IsEnabled = false;
            this.codeButton.IsEnabled = false;
        }

        private string code;

        private void codeButton_Click(object sender, RoutedEventArgs e)
        {
            code = this.codeTextBox.Text;
            DisableUI();
            waitEvent.Set();
        }

        private void ShowError(string url)
        {
            MessageBox.Show("Forbidden!" + "\r\n" + url, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Reset();
        }

        private MemoryStream GetResponse(string url, string cookie, string refURL)
        {
            while (true)
            {
                try
                {
                    HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                    request.Headers.Add("Cookie", cookie);
                    request.Referer = refURL;
                    request.Method = "GET";
                    request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";
                    HttpWebResponse response = request.GetResponse() as HttpWebResponse;

                    MemoryStream ms = new MemoryStream();
                    response.GetResponseStream().CopyTo(ms);

                    if (response.ContentType.Contains("text/html") == true)
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        string html = new StreamReader(ms).ReadToEnd();
                        if (html.Contains("/processVerifyPng.ac") == true)
                        {
                            string domainName = Regex.Match(url, "^http://.*?(?=/)").Value;
                            string codeImageURL = domainName + "/n/n/processVerifyPng.ac";
                            request = WebRequest.Create(codeImageURL) as HttpWebRequest;
                            request.Headers.Add("Cookie", cookie);
                            request.Method = "GET";
                            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";
                            response = request.GetResponse() as HttpWebResponse;

                            ms = new MemoryStream();
                            response.GetResponseStream().CopyTo(ms);

                            waitEvent.Reset();
                            this.Dispatcher.BeginInvoke(new Action<Stream>(EnableUI), ms);
                            waitEvent.WaitOne();

                            string commitCodeString = domainName + "/n/processVerify.ac?ucode=" + code;
                            request = WebRequest.Create(commitCodeString) as HttpWebRequest;
                            request.Headers.Add("Cookie", cookie);
                            request.Method = "GET";
                            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";
                            response = request.GetResponse() as HttpWebResponse;
                            response.Close();

                            continue;
                        }
                    }

                    ms.Seek(0, SeekOrigin.Begin);
                    return ms;
                }
                catch (Exception ex)
                {
                    WebException wex = ex as WebException;
                    wex = null;
                    if (wex != null && (wex.Response as HttpWebResponse).StatusCode == HttpStatusCode.Forbidden)
                    {
                        this.Dispatcher.BeginInvoke(new Action<string>(ShowError), url);
                        Thread.CurrentThread.Abort();
                    }
                }
            }
        }

        private void pdfButton_Click(object sender, RoutedEventArgs e)
        {
            IEnumerable<FileInfo> images = null;
            if (Directory.Exists("Download") == true)
            {
                DirectoryInfo dirInfo = new DirectoryInfo("Download");
                images = dirInfo.EnumerateFiles("*.jpg").OrderBy(i => i.Name[0]).ThenBy(i => i.Name.Length).ThenBy(i => i.Name);
            }
            if (images != null && images.Count() > 0)
            {
                Document pdfDoc = new Document();
                pdfDoc.SetMargins(0, 0, 0, 0);
                PdfWriter pdfWriter = PdfWriter.GetInstance(pdfDoc, new FileStream(@"Download\Book.pdf", FileMode.Create));
                pdfDoc.Open();
                int mainPageStartNumber = 0;
                bool entryMainPage = false;
                foreach (var image in images)
                {
                    if (entryMainPage == false)
                    {
                        mainPageStartNumber++;
                        if (image.Name[0] == 'F')
                        {
                            entryMainPage = true;
                        }
                    }
                    iTextSharp.text.Image pageImage = iTextSharp.text.Image.GetInstance(image.FullName);
                    pdfDoc.SetPageSize(new Rectangle(pageImage.Width, pageImage.Height));
                    pdfDoc.NewPage();
                    pdfDoc.Add(pageImage);
                }
                PdfPageLabels labels = new PdfPageLabels();
                labels.AddPageLabel(1, PdfPageLabels.LOWERCASE_ROMAN_NUMERALS);
                labels.AddPageLabel(mainPageStartNumber, PdfPageLabels.DECIMAL_ARABIC_NUMERALS);
                pdfWriter.PageLabels = labels;
                pdfDoc.Close();
                MessageBox.Show("Done!");
            }
            else
            {
                MessageBox.Show("No Picture!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }

    public class BrowserHelper
    {
        private const int INTERNET_COOKIE_HTTPONLY = 0x00002000;

        [DllImport("wininet.dll", SetLastError = true)]
        private static extern bool InternetGetCookieEx(
            string url,
            string cookieName,
            StringBuilder cookieData,
            ref int size,
            int flags,
            IntPtr pReserved);

        public static string GetCookie(string url)
        {
            int size = 512;
            StringBuilder sb = new StringBuilder(size);
            if (!InternetGetCookieEx(url, null, sb, ref size, INTERNET_COOKIE_HTTPONLY, IntPtr.Zero))
            {
                if (size < 0)
                {
                    return null;
                }
                sb = new StringBuilder(size);
                if (!InternetGetCookieEx(url, null, sb, ref size, INTERNET_COOKIE_HTTPONLY, IntPtr.Zero))
                {
                    return null;
                }
            }
            return sb.ToString();
        }
    }
}
