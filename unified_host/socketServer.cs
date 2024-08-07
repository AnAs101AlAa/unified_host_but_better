using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace unified_host
{

    public static class UdpClientExtensions
    {
        public static async Task<UdpReceiveResult> WithCancellation(this Task<UdpReceiveResult> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
            {
                if (task != await Task.WhenAny(task, tcs.Task))
                {
                    throw new OperationCanceledException(cancellationToken);
                }
            }
            return await task;
        }
    }

    public partial class socketServer
    {
        public int port;
        public IPAddress ip;
        public UdpClient udpServer;
        unified_host form;
        public IPEndPoint remoteEndPoint;
        byte[] request;
        byte[] response;

        public socketServer(int port,string ip, unified_host f)
        {
            udpServer = new UdpClient(port);
            this.port = port;
            form = f;
            this.ip = IPAddress.Parse(ip);
            remoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
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
            udpServer.Connect(ip, port);
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

        public void example()
        {
            form.UpdateConnectionStatus("sending wake up...");
            IPAddress ipAddress = IPAddress.Parse("192.168.1.4");
            byte[] ipAddressBytes = ipAddress.GetAddressBytes();

            byte[] message = { 0x00, 0x3f };
            byte[] final = InsertByteArray(message, ipAddressBytes, 2);
            byte[] mac = { 0x60, 0x18, 0x95, 0x2D, 0x44, 0xF8 };
            final = InsertByteArray(final, mac, 6);
            byte[] expected = null;

            if(!handlerequest(final, expected))
            {
                form.UpdateConnectionStatus("failed to send wake up"); return;
            }
            form.UpdateConnectionStatus("wake up sent");

            Task.Delay(3000).Wait();

            form.UpdateConnectionStatus("sending LED on request...");
            byte[] ledon = { 0x00, 0x10 };
            byte[] ledonr = { 0x00,0x0E,0x00,0x10};

            if (!handlerequest(ledon, ledonr))
            {
                form.UpdateConnectionStatus("failed to send LED on request"); return;
            }
            form.UpdateConnectionStatus("LED on request sent");
        }



        public void sendMessage(byte[] message)
        {

            udpServer.Send(message, message.Length);
        }

        public void receiveMessage()
        {
                response = this.udpServer.Receive(ref this.remoteEndPoint);
        }

        public bool handlerequest(byte[] request, byte[] expected)
        {
            sendMessage(request);
            Thread t = new Thread(receiveMessage);
            t.Start();

            if (expected == null)
            {
                return true;
            }

            while (true)
            {
                if (response != null && BitConverter.ToString(response) == BitConverter.ToString(expected))
                {
                    return true;
                }
            }
        }

    }
}
