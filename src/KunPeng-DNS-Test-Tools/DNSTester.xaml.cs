using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Data;
using System.Net.NetworkInformation;
using MyDnsPackage;
using System.Net.Sockets;
using System.ComponentModel;
using System.Windows.Threading;
using System.Threading;

namespace DNS_Tester
{
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    public partial class DNSTester : Window
    {
        public BackgroundWorker bw , bw1;
        public DataTable dt = new DataTable();
        public DispatcherTimer timer = new DispatcherTimer();
        public string _host = "", _dnsname = "", whois = "";
        public int state = 0;//记录异步进程的状态
        public DNSTester()
        {
            InitializeComponent();
            dt.Clear();
            dt.Columns.Add("dnsServer", typeof(string));
            dt.Columns.Add("dnsType", typeof(string));
            dt.Columns.Add("dnsIP", typeof(string));
            dt.Columns.Add("dnsTTL", typeof(string));
            timer.Interval = new TimeSpan(0, 0, 5);
            //timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += bw_Cancel;  //你的事件
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void closeBtn_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Close();
        }

        private void minBtn_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }


        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            if (bw.CancellationPending)
                return;
            else
            {
                dt.Rows.Add("DNS记录", "记录类型", "解析结果", "TTL");
                MyDns mydns = new MyDns();
                if (!mydns.Search(_host, QueryType.A, _dnsname, null))
                {
                    UMessageBox.Show("提示", mydns.header.RCODE.ToString());
                    return;
                }
                foreach (MyDnsRecord item in mydns.record.Records)
                {
                    dt.Rows.Add(item.Name, item.QType.ToString(), item.RDDate.ToString().Replace(" ", ""), item.TTL.ToString());
                }
                if (!mydns.Search(_host, QueryType.MX, _dnsname, null))
                {
                    UMessageBox.Show("提示", mydns.header.RCODE.ToString());
                    return;
                }
                foreach (MyDnsRecord item in mydns.record.Records)
                {
                    dt.Rows.Add(item.Name, item.QType.ToString(), item.RDDate.ToString().Replace(" ", "").Replace('|', '\r'), item.TTL.ToString());
                }
                if (!mydns.Search(_host, QueryType.CNAME, _dnsname, null))
                {
                    UMessageBox.Show("提示", mydns.header.RCODE.ToString());
                    return;
                }
                foreach (MyDnsRecord item in mydns.record.Records)
                {
                    dt.Rows.Add(item.Name, item.QType.ToString(), item.RDDate.ToString().Replace(" ", "").Replace('|', '\r'), item.TTL.ToString());
                }
                if (!mydns.Search(_host, QueryType.TXT, _dnsname, null))
                {
                    UMessageBox.Show("提示", mydns.header.RCODE.ToString());
                    return;
                }
                foreach (MyDnsRecord item in mydns.record.Records)
                {
                    dt.Rows.Add(item.Name, item.QType.ToString(), item.RDDate.ToString().Replace(' ', '\r'), item.TTL.ToString());
                }
                bw.ReportProgress(100);
            }
        }

        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (bw.CancellationPending)
                return;
            else
            {
                if (e.ProgressPercentage == 100)
                {
                    DataTable _dt = new DataTable();
                    _dt = dt.Copy();
                    dt.Clear();
                    txDNSInfo.Text = "DNS服务器：" + _dnsname;
                    ListRecord.DataContext = _dt;
                    btnCheck1.Visibility = Visibility.Visible;
                    _btnCheck1.Visibility = Visibility.Hidden;
                    canvas2.Visibility = Visibility.Visible;
                    timer.Stop();
                    state = 0;
                }
            }
        }

        private void bw_Cancel(object sender, EventArgs e)
        {
            bw.CancelAsync();
            bw.Dispose();
            timer.Stop();
            txDNSInfo.Text = "DNS服务器：" + _dnsname;
            DataTable _dt = new DataTable();
            _dt = dt.Clone();
            _dt.Clear();
            _dt.Rows.Add("DNS记录", "记录类型", "解析结果", "TTL");
            _dt.Rows.Add(_host, "", "无法解析,通信超时。", "");
            ListRecord.DataContext = _dt;
            btnCheck1.Visibility = Visibility.Visible;
            _btnCheck1.Visibility = Visibility.Hidden;
            canvas2.Visibility = Visibility.Visible;
            state = 0;
        }

        private void btnCheck1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!IsValidHttp(txDomain1.Text.Trim()))
                {
                    UMessageBox.Show("提示", "域名格式不正确!请重新输入。");
                    return;
                }
                if (ckDNSserver.IsChecked == true)
                {
                    if (!IsValidIp(txDNSserver.Text.Trim()))
                    {
                        UMessageBox.Show("提示", "DNS格式不正确!请重新输入。");
                        return;
                    }
                    else
                        _dnsname = txDNSserver.Text;
                }
                else
                {
                    if (cbDNSserver.SelectedIndex == 0)
                    {
                        NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
                        IPInterfaceProperties adapterProperties = adapters[0].GetIPProperties();
                        _dnsname = adapterProperties.DnsAddresses[0].ToString();
                    }
                    else
                    {
                        int m = cbDNSserver.SelectedValue.ToString().IndexOf("(");
                        int n = cbDNSserver.SelectedValue.ToString().IndexOf(")");
                        _dnsname = cbDNSserver.SelectedValue.ToString().Substring(m + 1, n - m - 1);
                    }
                }
                state = 1;
                _host = txDomain1.Text;
                btnCheck1.Visibility = Visibility.Hidden;
                _btnCheck1.Visibility = Visibility.Visible;
                timer.Start();
                dt.Clear();
                bw = new BackgroundWorker();
                bw.WorkerReportsProgress = true;
                bw.WorkerSupportsCancellation = true;
                bw.DoWork += bw_DoWork;
                bw.ProgressChanged += bw_ProgressChanged;
                bw.RunWorkerAsync(0);
            }
            catch (Exception ex)
            {
                UMessageBox.Show("异常提示", " 无法处理请求,因为以下问题发生:\n" + ex.Message);  //这里是异常处理  例如网络连接或主机不能解析等其它问题就显示出来给用户
            }

        }

        void bw_ProgressChanged2(object sender, ProgressChangedEventArgs e)
        {
            if (bw1.CancellationPending)
                return;
            else
            {
                if (e.ProgressPercentage == 100)
                {
                    txWhois.Text = whois;
                    btnCheck2.Visibility = Visibility.Visible;
                    _btnCheck2.Visibility = Visibility.Hidden;
                    canvas4.Visibility = Visibility.Visible;
                    state = 0;
                }
            }
        }

        private void bw_DoWork2(object sender, DoWorkEventArgs e)
        {
            if (bw1.CancellationPending)
                return;
            else
            {
                whois = LookUp(_host);
                bw1.ReportProgress(100);
            }
        }

        private void btnCheck2_Click(object sender, RoutedEventArgs e)
        {

            if (!IsValidHttp(txDomain2.Text.Trim()))
            {
                UMessageBox.Show("提示","域名格式不正确!请重新输入。");
                return;
            }
            state = 1;
            _host = txDomain2.Text;
            btnCheck2.Visibility = Visibility.Hidden;
            _btnCheck2.Visibility = Visibility.Visible;
            bw1 = new BackgroundWorker();
            bw1.WorkerReportsProgress = true;
            bw1.WorkerSupportsCancellation = true;
            bw1.DoWork += bw_DoWork2;
            bw1.ProgressChanged += bw_ProgressChanged2;
            bw1.RunWorkerAsync(0);
        }


        private void txDomain1_KeyPress(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {  //大家在敲完后都习惯性回车确定  这里就是在Input里输入完后直接回车，相当于点击了“马上检测”这个按钮
                btnCheck1_Click(this, null);
            }
        }

        private void txDomain2_KeyPress(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {  //大家在敲完后都习惯性回车确定  这里就是在Input里输入完后直接回车，相当于点击了“马上检测”这个按钮
                btnCheck2_Click(this, null);
            }
        }

        public string LookUp(string domain)
        {
            string result = "";
            string[] temp = domain.Split('.');
            string suffix = temp[temp.Length - 1].ToLower();// get the last;
            //定义whois服务器库
            //Dictionary<string, string> serverList = new Dictionary<string, string>();
            //serverList.Add("com", "whois.crsnic.net");
            //serverList.Add("cn", "whois.cnnic.net.cn");
            //serverList.Add("edu", "whois.educause.net");
            //serverList.Add("net", "whois.crsnic.net");
            //serverList.Add("org", "whois.crsnic.net");
            //serverList.Add("info", "whois.afilias.com");
            //serverList.Add("de", "whois.denic.de");
            //serverList.Add("nl", "whois.domain-registry.nl");
            //serverList.Add("eu", "whois.eu");

            //if (!serverList.Keys.Contains(suffix))
            //{
            //    result = string.Format("不支持此域名", suffix);
            //    return result;
            //}
            //string server = serverList[suffix];
            string server = suffix + ".whois-servers.net";
            TcpClient client = new TcpClient();
            NetworkStream ns;
            try
            {
                client.Connect(server, 43);
                ns = client.GetStream();
                byte[] buffer = Encoding.ASCII.GetBytes(domain + "\rn");
                ns.Write(buffer, 0, buffer.Length);

                buffer = new byte[8192];

                int i = ns.Read(buffer, 0, buffer.Length);
                while (i > 0)
                {
                    Encoding encoding = Encoding.UTF8;
                    result += encoding.GetString(buffer, 0, i);
                    i = ns.Read(buffer, 0, buffer.Length);
                }
            }
            catch (SocketException)
            {
                result = "链接失败";
                return result;
            }
            ns.Close();
            client.Close();

            return result;
        }

        /// <summary>
        /// 正则验证IP地址格式
        /// </summary>
        /// <param name="strIn"></param>
        /// <returns></returns>
        bool IsValidIp(string strIn)
        {
            return Regex.IsMatch(strIn, @"((25[0-5])|(2[0-4]\d)|(1\d\d)|([1-9]\d)|\d)(\.((25[0-5])|(2[0-4]\d)|(1\d\d)|([1-9]\d)|\d)){3}");
        }

        /// <summary>
        /// 正则验证HTTP地址格式
        /// </summary>
        /// <param name="strIn"></param>
        /// <returns></returns>
        bool IsValidHttp(string strIn)
        {
            return Regex.IsMatch(strIn, @"^([\w-]+\.)+[\w-]+(/[\w-./?%&=]*)?$");
        }

        private void btnDNStest_Click(object sender, RoutedEventArgs e)
        {
            if (state == 0)
            {
                canvas0.Visibility = Visibility.Hidden;
                canvas1.Visibility = Visibility.Visible;
                canvas2.Visibility = Visibility.Hidden;
                canvas3.Visibility = Visibility.Hidden;
                canvas4.Visibility = Visibility.Hidden;
                imgDNStest.Visibility = Visibility.Hidden;
                imgWhois.Visibility = Visibility.Visible;
                
            }
        }

        private void btnWhois_Click(object sender, RoutedEventArgs e)
        {
            if (state == 0)
            {
                canvas0.Visibility = Visibility.Hidden;
                canvas1.Visibility = Visibility.Hidden;
                canvas2.Visibility = Visibility.Hidden;
                canvas3.Visibility = Visibility.Visible;
                canvas4.Visibility = Visibility.Hidden;
                imgDNStest.Visibility = Visibility.Visible;
                imgWhois.Visibility = Visibility.Hidden;
            }
        }

        private void btnHome_Click(object sender, RoutedEventArgs e)
        {
            if (state == 0)
            {
                canvas0.Visibility = Visibility.Visible;
                canvas1.Visibility = Visibility.Hidden;
                canvas2.Visibility = Visibility.Hidden;
                canvas3.Visibility = Visibility.Hidden;
                canvas4.Visibility = Visibility.Hidden;
                imgDNStest.Visibility = Visibility.Hidden;
                imgWhois.Visibility = Visibility.Hidden;
            }
        }

        private void ckDNSserver_Click(object sender, RoutedEventArgs e)
        {
            if (ckDNSserver.IsChecked == true)
            {
                cbDNSserver.Visibility = Visibility.Hidden;
                txDNSserver.Visibility = Visibility.Visible;
            }
            else
            {
                cbDNSserver.Visibility = Visibility.Visible;
                txDNSserver.Visibility = Visibility.Hidden;
            }
        }

    }
}
