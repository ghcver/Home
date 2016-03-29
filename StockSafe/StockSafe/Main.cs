using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using WP7SDK.Common.Biz.Quote;
using WP7SDK.Common.Biz.Quote.Base;
using WP7SDK.Common.Biz.Quote.Fields;
using WP7SDK.Common.Biz.Trade;
using WP7SDK.Common.Biz.Trade.Stock;
using WP7SDK.Common.Events;
using WP7SDK.Interfaces.Events;

namespace StockSafe
{
    class Main
    {
        public void DoWork(object obj)
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args != null && args.Length >= 2 && args[1] == "-d")
            {
                Debugger.Launch();
            }

            StockSafe service = (StockSafe)obj;
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            try
            {
                DoWorkWrap();
            }
            catch (Exception ex)
            {
                LogUtility.Log(ex.ToString());
                LogUtility.Log("Service Stop!!!");
                service.Stop();
            }
        }

        private string fundAccount;
        private string fundAccountPassword;
        private string communicationPassword;
        private string phoneNumber;
        private string branchNO;
        private string shAccount;
        private string szAccount;
        private decimal doneBackPercent;
        private string queryPriceURL;
        private NetworkUtility network;

        private void DoWorkWrap()
        {
            XmlReaderSettings settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Parse };
            XmlReader reader = XmlReader.Create("Config.xml", settings);
            XDocument doc = XDocument.Load(reader);
            reader.Close();
            XElement root = doc.Root;

            string host = root.Element("host").Value;
            int port = int.Parse(root.Element("port").Value);
            string cerPath = root.Element("cerPath").Value;
            string cerPassword = root.Element("cerPassword").Value;
            string remoteCerCN = root.Element("remoteCerCN").Value;
            int checkPeriod = int.Parse(root.Element("checkPeriod").Value);

            this.doneBackPercent = decimal.Parse(root.Element("doneBackPercent").Value);
            this.queryPriceURL = root.Element("queryPriceURL").Value;
            this.fundAccount = root.Element("fundAccount").Value;
            this.fundAccountPassword = root.Element("fundAccountPassword").Value;
            this.communicationPassword = root.Element("communicationPassword").Value;
            this.phoneNumber = root.Element("phoneNumber").Value;
            this.branchNO = root.Element("branchNO").Value;
            this.shAccount = root.Element("shAccount").Value;
            this.szAccount = root.Element("szAccount").Value;

            List<Stock> stockList = new List<Stock>();
            foreach (var item in root.Element("stockList").Elements("stock"))
            {
                string code = item.Attribute("code").Value;
                bool isStockCode = Regex.IsMatch(code, "^\\d{6}$");
                if (isStockCode == false)
                {
                    throw new Exception("Stock Code Error!!!");
                }

                int volume = int.Parse(item.Attribute("volume").Value);
                if (volume <= 0)
                {
                    throw new Exception("Stock Volume Error!!!");
                }

                decimal? stopPrice = item.Attribute("stopPrice").Value == string.Empty ? null : (decimal?)decimal.Parse(item.Attribute("stopPrice").Value);
                if (stopPrice != null)
                {
                    stopPrice = Math.Round(stopPrice.Value, 2);
                    if (stopPrice <= 0)
                    {
                        throw new Exception("Stock Stop Price Error!!!");
                    }
                }

                decimal? donePrice = item.Attribute("donePrice").Value == string.Empty ? null : (decimal?)decimal.Parse(item.Attribute("donePrice").Value);
                if (donePrice != null)
                {
                    donePrice = Math.Round(donePrice.Value, 2);
                    if (donePrice <= 0)
                    {
                        throw new Exception("Stock Done Price Error!!!");
                    }
                }

                if (stopPrice == null && donePrice == null)
                {
                    throw new Exception("Stock Stop and Done Price cannot be null both!!!");
                }

                decimal sellPrice = decimal.Parse(item.Attribute("sellPrice").Value);
                sellPrice = Math.Round(sellPrice, 2);
                if (sellPrice <= 0)
                {
                    throw new Exception("Stock Sell Price Error!!!");
                }

                stockList.Add(new Stock(code, volume, stopPrice, donePrice, sellPrice));
            }
            if (stockList.Count == 0)
            {
                throw new Exception("No Stock!!!");
            }

            NetworkUtility.Init(host, port, new X509Certificate2(cerPath, cerPassword), remoteCerCN);
            network = new NetworkUtility();

            while (true)
            {
                try
                {
                    Check(stockList);
                }
                catch (Exception ex)
                {
                    LogUtility.Log(ex.ToString());
                }

                Thread.Sleep(checkPeriod * 1000);
            }
        }

        private void Check(List<Stock> stockList)
        {
            foreach (var stock in stockList)
            {
                if (stock.Sold == true)
                {
                    continue;
                }

                try
                {
                    TimeSpan quotetime = GetQuoteTime(stock);
                    if (quotetime == new TimeSpan(9, 30, 0) || quotetime == new TimeSpan(15, 0, 0))
                    {
                        continue;
                    }

                    StockPrice price = GetStockPrice(stock, quotetime);
                    
                    bool doSell = false;

                    if (stock.DonePrice != null)
                    {
                        decimal currentBackPercent = (price.CurrentPrice - price.OneMinutePrice) * 100 / price.OneMinutePrice;
                        if (price.OneMinutePrice >= stock.DonePrice && currentBackPercent <= doneBackPercent)
                        {
                            doSell = true;
                        }

                        if (price.TwoMinutePrice != 0)
                        {
                            if (price.TwoMinutePrice >= stock.DonePrice && price.OneMinutePrice <= price.TwoMinutePrice)
                            {
                                doSell = true;
                            }
                        }
                    }

                    if (stock.StopPrice != null)
                    {
                        if(price.CurrentPrice <= stock.StopPrice.Value)
                        {
                            doSell = true;
                        }
                    }

                    if (doSell == true)
                    {
                        EntrustCfmPacket entrustCfmPacket = new EntrustCfmPacket();
                        entrustCfmPacket.AppendRow();
                        AddFixParameter(entrustCfmPacket);
                        entrustCfmPacket.StockCode = stock.Code;
                        entrustCfmPacket.StockAccount = stock.Code[0] == '6' ? shAccount : szAccount;
                        entrustCfmPacket.SeatNo = "";
                        entrustCfmPacket.FundAccount = fundAccount;
                        entrustCfmPacket.ExchangeType = stock.Code[0] == '6' ? "1" : "2"; //上海股票或者深圳股票
                        entrustCfmPacket.EntrustProp = "0"; //限价委托
                        entrustCfmPacket.EntrustPrice = (double)stock.SellPrice; //委托价格
                        entrustCfmPacket.EntrustBs = "2"; //1买入，2卖出
                        entrustCfmPacket.EntrustAmount = stock.Volume; //交易股票数
                        entrustCfmPacket.BatchNo = 0;
                        entrustCfmPacket.Balance = 0;

                        INetworkEvent evt = EventFactory.GetEvent();
                        evt.BizPacket = entrustCfmPacket;
                        evt.IsUseCommonQuery = false;
                        byte[] data = network.Send(evt.pack());

                        data = EventReceived(data);

                        EntrustCfmPacket entrustPacket = new EntrustCfmPacket(data);
                        if (entrustPacket.EntrustNo > 0)
                        {
                            stock.Sold = true;
                            LogUtility.Log(stock.Code + " Sold!!!");
                        }
                    }
                }
                catch(Exception ex)
                {
                    LogUtility.Log(network.CurrentHtml + "\r\n" + ex.ToString());
                }
            }
        }

        private TimeSpan GetQuoteTime(Stock stock)
        {
            string timeJson = network.GetHttpResponse(string.Format(queryPriceURL, stock.Market, stock.Code, "242"));
            dynamic timeJsonObj = JsonConvert.DeserializeObject(timeJson);
            if (timeJsonObj.cssweb_code != "success")
            {
                throw new Exception("Get Time Error!!!");
            }
            string quotetimeString = timeJsonObj.quotetime;
            TimeSpan quotetime = DateTime.Parse(quotetimeString).TimeOfDay;

            return quotetime;
        }

        private StockPrice GetStockPrice(Stock stock, TimeSpan quotetime)
        {
            if(quotetime.Hours>=13)
            {
                quotetime = quotetime - new TimeSpan(1, 30, 0);
            }
            TimeSpan opentime = new TimeSpan(9, 30, 0);

            TimeSpan timeSpan = quotetime - opentime;
            int from = (int)timeSpan.TotalMinutes - 1;

            if(from <0)
            {
                throw new Exception("from Parameter Less 0");
            }

            string dataJson = network.GetHttpResponse(string.Format(queryPriceURL, stock.Market, stock.Code, from));
            dynamic dataJsonObj = JsonConvert.DeserializeObject(dataJson);
            if (dataJsonObj.cssweb_code != "success")
            {
                throw new Exception("Get Price Error!!!");
            }
            dynamic data = dataJsonObj.data;
            int dataLength = data.Length;

            StockPrice price = new StockPrice(data[dataLength - 1][0], data[dataLength - 2][0], dataLength > 2 ? data[dataLength - 3][0] : 0);

            return price;
        }

        private byte[] EventReceived(byte[] data)
        {
            int index = 0;

            while (index < data.Length)
            {
                if (data.Length - index < 4)
                {
                    break;
                }

                int length = EventUtils.CheckHeadAvailable(data.ToList().GetRange(index, 4).ToArray(), 0);
                if (length < 0)
                {
                    length = 0;
                }

                if (length > 0)
                {
                    byte[] state = data.ToList().GetRange(index, length + 4).ToArray();
                    INetworkEvent evt = EventFactory.GetEvent();
                    evt.unpack(state, true);
                    return evt.getMessageBody();
                }

                index = index + 4 + length;
            }
            return null;
        }

        private void AddFixParameter(TradePacket packet)
        {
            packet.FundAccount = fundAccount;
            packet.Version = "";
            packet.Password = fundAccountPassword;
            packet.EntrustWay = "8"; //委托方式
            packet.OpStation = phoneNumber;
            packet.BranchNo = branchNO; //营业部ID
            packet.MacAddr = "";
            packet.EntrustSafety = "1";
            packet.MobileCode = phoneNumber;
            packet.ClientId = fundAccount;
            packet.Cpuid = "";
            packet.DiskSerialId = "st2cQ76LdM1vZAWNGrypB08gR04=";
            packet.ClientVer = "1.0.0.0";
            packet.SafetyInfo = "";
            packet.UserData = "";
            packet.SessionNo = "";
            packet.RequestNum = "1000";
        }
    }

    class Stock
    {
        public Stock(string code, int volume, decimal? stopPrice, decimal? donePrice, decimal sellPrice)
        {
            Code = code;
            Volume = volume;
            StopPrice = stopPrice;
            DonePrice = donePrice;
            SellPrice = sellPrice;
            Sold = false;

            QuoteFieldRequestPacket quoteFieldRequestPacket = new QuoteFieldRequestPacket();
            quoteFieldRequestPacket.AddCodeInfo(new StockInfo(code, (short)(code[0] == '6' ? 4353 : 4609))); //4353 -> 上海，4609 -> 深圳
            quoteFieldRequestPacket.AddField(new byte[] { 1, 2, 49, 46, 77 });

            INetworkEvent evt = EventFactory.GetEvent();
            evt.BizPacket = quoteFieldRequestPacket;
            evt.IsUseCommonQuery = false;
            QueryPriceBytes = evt.pack();

            Market = code[0] == '6' ? "sh" : "sz";
        }
        public string Code { get; private set; }
        public int Volume { get; private set; }
        public decimal? StopPrice { get; private set; }
        public decimal? DonePrice { get; private set; }
        public decimal SellPrice { get; private set; }
        public bool Sold { get; set; }
        public byte[] QueryPriceBytes { get; private set; }
        public string Market { get; private set; }
    }

    class LogUtility
    {
        private static object locker = new object(); 
        public static void Log(string logString)
        {
            lock(locker)
            {
                File.AppendAllText("Log.txt", DateTime.Now.ToString() + " : " + logString + "\r\n");
            }
        }
    }

    class StockPrice
    {
        public StockPrice(decimal currentPrice, decimal oneMinutePrice, decimal twoMinutePrice)
        {
            CurrentPrice = currentPrice;
            OneMinutePrice = oneMinutePrice;
            TwoMinutePrice = twoMinutePrice;
        }
        public decimal CurrentPrice { get; private set; }
        public decimal OneMinutePrice { get; private set; }
        public decimal TwoMinutePrice { get; private set; }
    }
}
