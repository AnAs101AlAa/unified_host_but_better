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
             UdpClient udpServer = new UdpClient(65500); // Create a UDP client listening on port 1302
            Console.WriteLine("UDP Server is listening on port 65500");

            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 65500);

            try
            {
                while (true)
                {
                    // Receive data from any remote endpoint
                    byte[] receivedBytes = udpServer.Receive(ref remoteEndPoint);

                    // Convert received bytes to a string
                    string receivedData = Encoding.UTF8.GetString(receivedBytes);
                    Console.WriteLine($"Received data from {remoteEndPoint}: {receivedData}");

                    // Prepare a response message
                    string responseMessage = "You suck";
                    byte[] responseBytes = Encoding.UTF8.GetBytes(responseMessage);

                    // Send the response to the remote endpoint
                    udpServer.Send(responseBytes, responseBytes.Length, remoteEndPoint);
                    Console.WriteLine($"Sent response to {remoteEndPoint}");
                }
            }
            catch (Exception x)
            {
                Console.WriteLine($"An error occurred: {x.Message}");
            }
            finally
            {
                udpServer.Close();
            }
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
