using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApplication6
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> numberList = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            textBox.Text = string.Empty;
            button.IsEnabled = false;
            Task.Run(new Action(DoWork));
        }

        private void DoWork()
        {
            numberList.Clear();

            string url = "https://www.hn.10086.cn/Shopping/selects/nos_queryPhoneInfo.action";
            byte[] data = Encoding.ASCII.GetBytes("page=0&tdShopSelectionSuc.mobileType=0&tdShopSelectionSuc.selectArea=0731&tdShopSelectionSuc.selectAreaName=%E5%B8%B8%E5%BE%B7&tdShopSelectionSuc.numberSeg=136________&tdShopSelectionSuc.numberRule=&tdShopSelectionSuc.searchStr=___________&tdShopSelectionSuc.endNumberRule=&tdShopSelectionSuc.storedValueStart=&tdShopSelectionSuc.storedValueEnd=&tdShopSelectionSuc.compositor=2&tdShopSelectionSuc.switchList=0&retryQuery=yes&numPriceSort=&numSort=1&pages.pageSize=1000");
            for (int i = 0; i < 20; i++)
            {
                WebClient client = new WebClient();
                client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                string response = Encoding.UTF8.GetString(client.UploadData(url, data));

                int index = 0;
                while (true)
                {
                    index = response.IndexOf("number=\"", index);
                    if (index == -1)
                    {
                        break;
                    }
                    index = index + 8;
                    string number = response.Substring(index, 11);
                    if (number.IndexOf('4') == -1 && numberList.Contains(number) == false)
                    {
                        numberList.Add(number);
                    }
                }
            }

            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                numberList.Sort();
                textBox.Text = string.Join("\r\n", numberList);
                button.IsEnabled = true;
            }));
        }
    }
}
