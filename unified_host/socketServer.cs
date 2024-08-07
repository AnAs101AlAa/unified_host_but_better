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
        public IPAddress ipTarget;
        public UdpClient udpServer;
        public unified_host form;
        public IPEndPoint remoteEndPoint;

        public struct UdpState
        {
            public UdpClient u;

            public IPEndPoint e;
        }

        public socketServer(int port, string ip, unified_host f)
        {
            this.port = port;
            form = f;
            this.ipTarget = IPAddress.Parse(ip);
            remoteEndPoint = new IPEndPoint(ipTarget, port);
            udpServer = new UdpClient(port);
        }

        public static void ReceiveCallback(IAsyncResult ar)
        {
            UdpClient u = ((UdpState)(ar.AsyncState)).u;
            IPEndPoint e = ((UdpState)(ar.AsyncState)).e;

            byte[] receiveBytes = u.EndReceive(ar, ref e);

            MessageBox.Show($"Received: {BitConverter.ToString(receiveBytes)}");
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

        public static byte[] InsertByteArray(byte[] destination, byte[] source, int index)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (index < 0 || index > destination.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            byte[] result = new byte[destination.Length + source.Length];
            Array.Copy(destination, 0, result, 0, index);
            Array.Copy(source, 0, result, index, source.Length);
            Array.Copy(destination, index, result, index + source.Length, destination.Length - index);

            return result;
        }

        public async void example()
        {
            form.UpdateConnectionStatus("sending wake up...");
            IPAddress ipAddress = IPAddress.Parse("192.168.1.4");
            byte[] ipAddressBytes = ipAddress.GetAddressBytes();

            byte[] message = { 0x00, 0x3f };
            byte[] final = InsertByteArray(message, ipAddressBytes, 2);
            byte[] mac = { 0x60, 0x18, 0x95, 0x2D, 0x44, 0xF8 };
            final = InsertByteArray(final, mac, 6);
            byte[] expected = null;

            handleRequest(final, expected);

            await Task.Delay(2000);

            form.UpdateConnectionStatus("sending led ask...");
            byte[] ledask = { 0x00, 0x10 };
            byte[] ledresponse = { 0x00, 0x0E, 0x00, 0x10 };

            handleRequest(ledask, ledresponse);
        }

        public void sendMessage(byte[] message)
        {

            udpServer.Send(message, message.Length, remoteEndPoint);
        }


        public bool handleRequest(byte[] message, byte[] repsonse)
        {
            sendMessage(message);
            if (repsonse == null)
            {
                return true;
            }
            UdpState s = new UdpState();
            s.e = remoteEndPoint;
            s.u = udpServer;

            udpServer.BeginReceive(new AsyncCallback(ReceiveCallback), s);
            return true;
        }
    }
}
