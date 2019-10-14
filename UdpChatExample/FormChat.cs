using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace UdpChatExample
{
    public partial class FormChat : Form
    {
        delegate void AddListBoxItemCallback(string text);
        AddListBoxItemCallback listBoxCallback;
        //使用接收端口号
        private int port = 8001;
        private UdpClient udpClient;
        public FormChat()
        {
            InitializeComponent();
            listBoxCallback = new AddListBoxItemCallback(AddListBoxItem);
        }
        private void AddListBoxItem(string text)
        {
            //如果listBoxReceive被不同的线程访问则通过委托处理
            if(listBoxReceive.InvokeRequired)
            {
                this.Invoke(listBoxCallback, text);
            }
            else
            {
                listBoxReceive.Items.Add(text);
                listBoxReceive.SelectedIndex = listBoxReceive.Items.Count - 1;
            }
        }

        /// <summary>
        /// 在后台运行的接收线程
        /// </summary>
        private void ReceiveData()
        {
            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            //在本机的指定的端口接收
            //udpClient = new UdpClient(port); //此方式不兼容 ipv6
            udpClient = new UdpClient(port, localIPs[0].AddressFamily); //此方式兼容 ipv6
            IPEndPoint remote = null;
            //接收从远程主机发送过来的信息
            while(true)
            {
                try
                {
                    //关闭 udpClient 时此句会产生异常
                    byte[] bytes = udpClient.Receive(ref remote);
                    string str = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                    AddListBoxItem(string.Format("来自{0}：{1}", remote, str));
                }
                catch
                {
                    //退出循环，结束线程
                    break;
                }
            }
        }

        private void SendData()
        {
            IPAddress remoteIP;
            if(IPAddress.TryParse(textBoxRemoteIP.Text,out remoteIP)==false)
            {
                MessageBox.Show("远程IP格式不正确");
                return;
            }
            IPEndPoint iep = new IPEndPoint(remoteIP, port);
            byte[] bytes = Encoding.UTF8.GetBytes(textBoxSend.Text);
            UdpClient myUdpClient = new UdpClient(remoteIP.AddressFamily); //此方式兼容 ipv6
            try
            {
                myUdpClient.Send(bytes, bytes.Length, iep);
                textBoxSend.Clear();
                myUdpClient.Close();
                textBoxSend.Focus();
            }
            catch(Exception err)
            {
                MessageBox.Show(err.Message, "发送失败");
            }
            finally
            {
                myUdpClient.Close();
            }
        }

        private void FormChat_Load(object sender, EventArgs e)
        {
            //设置listBox 样式
            listBoxReceive.HorizontalScrollbar = true;
            listBoxReceive.Dock = DockStyle.Fill;
            //获取本机第一个可用IP 地址
            IPAddress myIP = (IPAddress)Dns.GetHostAddresses(Dns.GetHostName()).GetValue(0);
            //为了在同一台机器调试，此IP 也作为默认远程IP
            textBoxRemoteIP.Text = myIP.ToString();
            //创建一个线程接收远程主机发来的信息
            Thread myThread = new Thread(new ThreadStart(ReceiveData));
            //将线程设为后台运行
            myThread.IsBackground = true;
            myThread.Start();
            textBoxSend.Focus();
        }

        /// <summary>
        /// 单击发送按钮触发的事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSend_Click(object sender, EventArgs e)
        {
            SendData();
        }

        /// <summary>
        /// 在textBoxSend 中按下并释放Enter 键后触发的事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxSend_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
                SendData();
        }

        private void FormChat_FormClosing(object sender, FormClosingEventArgs e)
        {
            udpClient.Close();
        }
    }
}
