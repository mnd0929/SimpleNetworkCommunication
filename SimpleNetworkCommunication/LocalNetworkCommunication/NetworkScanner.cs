using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleNetworkCommunication
{
    public partial class NetworkScanner : Form
    {
        public int mainPort = 55555;
        public delegate void DeviceSelected(NetworkAddress message);
        public event DeviceSelected UserSelectedDevice;
        public NetworkScanner()
        {
            InitializeComponent();

            dataGridView1.MultiSelect = false;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        }

        public async void RefreshList()
        {
            button2.Enabled = false;
            NetPingers netPingers = new NetPingers();
            List<IPAddress> iPAddresses = new List<IPAddress>();
            dataGridView1.Rows.Clear();

            statusLabel.Text = "Сканирование сети...";

            await Task.Run(() =>
            {
                iPAddresses = netPingers.GetIpAdresses();
            });

            statusLabel.Text = "Определение имен хоста...";

            foreach (IPAddress iPAddress in iPAddresses)
            {
                bool isActive = true;
                string netRole = null;
                string hostName = "Unknown";

                try
                {
                    new Thread(() =>
                    {
                        try
                        {
                            hostName = Dns.GetHostEntry(iPAddress).HostName;
                        }
                        catch { }

                        try
                        {
                            ClientInfo clientInfo = new ClientInfo();
                            clientInfo.GetClientInfo(iPAddress.ToString(), hostName);

                            isActive = clientInfo.isActive;
                            netRole = clientInfo.NetRole;
                        }
                        catch { }

                        try
                        {
                            int i = dataGridView1.Rows.Add(hostName, iPAddress.ToString(), mainPort, isActive, netRole);
                        }
                        catch { }
                    }).Start();
                }
                catch { }
            }

            statusLabel.Text = null;

            await Task.Delay(15000);

            button2.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            RefreshList();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                UserSelectedDevice(new NetworkAddress
                (
                    IPAddress.Parse(dataGridView1.CurrentRow.Cells[1].Value.ToString()),
                    int.Parse(dataGridView1.CurrentRow.Cells[2].Value.ToString())
                ));

                this.Dispose();
            }
            catch { }
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                button1.Enabled = true;
                button3.Enabled = dataGridView1.CurrentRow.Cells[0].Value.ToString() != "Unknown";
            }
            else
            {
                button3.Enabled = false;
                button1.Enabled = false;
            }
        }

        private void NetworkScanner_Load(object sender, EventArgs e)
        {
            RefreshList();
        }

        private void statusLabel_TextChanged(object sender, EventArgs e)
        {
            pictureBox1.Visible = !string.IsNullOrWhiteSpace((sender as Label).Text);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            MessageBox.Show(new ClientInfo().GetMashineInfo(dataGridView1.CurrentRow.Cells[0].Value.ToString()), "Информация о машине", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
