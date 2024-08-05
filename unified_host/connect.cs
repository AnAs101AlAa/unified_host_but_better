using System.Net.Sockets;

namespace unified_host
{
    public partial class connect : Form
    {
        private TextBox portInput;
        private Button confirmPort;
        private Label connectionStatus;
        private UdpListener server;
        private UdpClient client;

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
