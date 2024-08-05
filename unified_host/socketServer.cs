using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace unified_host
{
    public partial class socketServer
    {
        public int port;
        public UdpClient udpServer;
        unified_host form;
        public IPEndPoint remoteEndPoint;

        public socketServer(int port, unified_host f)
        {

            udpServer = new UdpClient(port);
            this.port = port;
            form = f;
            remoteEndPoint = new IPEndPoint(IPAddress.Any, 65500);
            form.UpdateConnectionStatus($"UDP Server is listening on port +{port}");
        }

        public bool isConnected()
        {
            int sec = 10000;
            while (sec > 0)
            {
                if (udpServer.Client.Connected)
                    return true;
                sec--;
            }
            return false;
        }

        public void start()
        {
            form.UpdateConnectionStatus("starting connection...");
            udpServer.Connect("192.168.1.71", port);
        }

        public void stop()
        {
            form.UpdateConnectionStatus("stopping connection...");
            try
            {
                udpServer.Close();
                form.UpdateConnectionStatus("disconnected");
            }
            catch (Exception x)
            {
                Console.WriteLine($"An error occurred: {x.Message}");
            }
        }

        public void sendMessage(byte[] message)
        {
            udpServer.Send(message, message.Length);
        }

        public string receiveMessage()
        {
            byte[] receivedBytes = udpServer.Receive(ref remoteEndPoint);
            return Encoding.UTF8.GetString(receivedBytes);
        }
    }
}
