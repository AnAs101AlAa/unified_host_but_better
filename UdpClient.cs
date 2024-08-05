using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace unified_host
{
    public class UdpClient
    {
        private System.Net.Sockets.UdpClient client;
        private IPEndPoint serverEndPoint;

        public UdpClient(string ipAddress, int port)
        {
            client = new System.Net.Sockets.UdpClient();
            serverEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
        }

        public async Task SendMessage(string message)
        {
            var buffer = Encoding.UTF8.GetBytes(message);
            await client.SendAsync(buffer, buffer.Length, serverEndPoint);
        }

        public async Task<string> ReceiveMessage()
        {
            var result = await client.ReceiveAsync();
            return Encoding.UTF8.GetString(result.Buffer);
        }

        public void Close()
        {
            client.Close();
        }
    }
}
