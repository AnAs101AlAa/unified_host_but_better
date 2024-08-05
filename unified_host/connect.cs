using System.Net.Sockets;
using System;
using System.Net;
using System.Text;

namespace unified_host
{
    public partial class connect : Form
    {
        private TextBox portInput;
        private Button confirmPort;
        private Label connectionStatus;
        private Button start;
        private socketServer server;
        
        public connect()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            portInput = new TextBox();
            portInput.Location = new Point(10, 10);
            portInput.Size = new Size(100, 20);

            confirmPort = new Button();
            confirmPort.Text = "Connect";
            confirmPort.Location = new Point(120, 10);
            confirmPort.Click += new EventHandler(ConfirmPort_Click);

            start = new Button();
            start.Text = "start";
            start.Location = new Point(120, 80);
            start.Click += new EventHandler(startcomm);

            connectionStatus = new Label();
            connectionStatus.Location = new Point(10, 40);
            connectionStatus.Size = new Size(200, 20);
            connectionStatus.Text = "Disconnected";

            this.Controls.Add(connectionStatus);
            this.Controls.Add(portInput);
            this.Controls.Add(confirmPort);
        }

        private async void ConfirmPort_Click(object sender, EventArgs e)
        {
            server = new socketServer(int.Parse(portInput.Text), this);
            server.start();

            if (server.isConnected())
            {
                UpdateConnectionStatus("client connected successfully");
            }
            else
            {
                UpdateConnectionStatus("client failed to connect");
                server.stop();
            }
        }

        private void startcomm(object sender, EventArgs e)
        {
            
        }

        public void UpdateConnectionStatus(string status)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateConnectionStatus), status);
            }
            else
            {
                connectionStatus.Text = status;
            }
        }
    }
}
