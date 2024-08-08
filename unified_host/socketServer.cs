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
        public IPAddress ipHost;
        public UdpClient udpServer;
        public unified_host form;
        public IPEndPoint remoteEndPoint;
        public byte[] calledBackReponse;
        public bool success = false;

        public struct UdpState
        {
            public UdpClient u;

            public IPEndPoint e;
        }

        public socketServer(int port, string remoteip, string hostip, unified_host f)
        {
            this.port = port;
            form = f;
            this.ipTarget = IPAddress.Parse(remoteip);
            this.ipHost = IPAddress.Parse(hostip);
            remoteEndPoint = new IPEndPoint(ipTarget, port);
            udpServer = new UdpClient(port);
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

        //-------------------------------------sequence of operations to be performed-------------------------------------//
        public async void programSequence()
        {
            form.UpdateConnectionStatus("sending wake up...");
            await operationHandler(0);
            if (success)
            {
                form.UpdateConnectionStatus("wake up sent");
            }
            else
            {
                form.UpdateConnectionStatus("wake up timed out");
                return;
            }

            success = false;
            await Task.Delay(2000);

            form.UpdateConnectionStatus("sending LED ON ask...");
            await operationHandler(1);
            if (success)
            {
                form.UpdateConnectionStatus("LED ON sent");
            }
            else
            {
                form.UpdateConnectionStatus("LED ON timed out");
                return;
            }

            success = false;
            await Task.Delay(2000);

            form.UpdateConnectionStatus("sending lED OFF ask...");
            await operationHandler(2);
            if (success)
            {
                form.UpdateConnectionStatus("LED OFF sent");
            }
            else
            {
                form.UpdateConnectionStatus("LED OFF timed out");
                return;
            }

            success = false;
            await Task.Delay(2000);

            form.UpdateConnectionStatus("sending boot ask...");
            await operationHandler(3);
            if (success)
            {
                form.UpdateConnectionStatus("boot sent");
            }
            else
            {
                form.UpdateConnectionStatus("boot timed out");
                return;
            }

            success = false;
            await Task.Delay(2000);

            form.UpdateConnectionStatus("sending flash write...");
            await operationHandler(4);
            if (success)
            {
                form.UpdateConnectionStatus("flash write sent");
            }
            else
            {
                form.UpdateConnectionStatus("flash write timed out");
                return;
            }

            success = false;
            await Task.Delay(2000);

            form.UpdateConnectionStatus("sending reset device...");
            await operationHandler(5);
            if (success)
            {
                form.UpdateConnectionStatus("reset device sent");
            }
            else
            {
                form.UpdateConnectionStatus("reset device timed out");
                return;
            }

            success = false;
            await Task.Delay(2000);
            form.UpdateConnectionStatus("sequence completed");
        }

        //-------------------------------------handles logic of making UDP packets and expected responses-------------------------------------//
        public async Task operationHandler(int opcode)
        {
            switch (opcode)
            {
                case 0: //handshake operation
                    byte[] ipAddressBytes = ipHost.GetAddressBytes();
                    byte[] message = { 0x00, 0x3f };
                    byte[] requestWake = InsertByteArray(message, ipAddressBytes, 2);
                    byte[] mac = { 0x60, 0x18, 0x95, 0x2D, 0x44, 0xF8 };
                    requestWake = InsertByteArray(requestWake, mac, 6);
                    byte[] responseWake = null;
                    success = await handleRequest(requestWake, responseWake, 2000);
                    break;
                case 1: //LED ON operation
                    byte[] requestLED1 = { 0x00, 0x10 };
                    byte[] responseLED1 = { 0x00, 0x0E, 0x00, 0x10 };
                    success = await handleRequest(requestLED1, responseLED1, 2000);
                    break;
                case 2: //LED OFF operation
                    byte[] requestLED2 = { 0x00, 0x11 };
                    byte[] responseLED2 = { 0x00, 0x0E, 0x00, 0x11 };
                    success = await handleRequest(requestLED2, responseLED2, 2000);
                    break;
                case 3: //erase memory and go to boot operation
                    byte[] requesBoot = { 0x00, 0x52 };
                    byte[] responseBoot = { 0x00, 0x48};
                    success = await handleRequest(requesBoot, responseBoot, 20000);
                    break;
                case 4: //flash write operation
                    int currPage = 0;
                    foreach (byte[] packet in form.filePackets)
                    {
                        byte[] requestWrite = { 0x00, 0x55};
                        requestWrite = InsertByteArray(requestWrite, IntToTwoBytes(currPage), 2);
                        requestWrite = InsertByteArray(requestWrite, packet, 4);
                        byte[] responseWrite = { 0x00, 0x4A};
                        responseWrite = InsertByteArray(responseWrite, IntToTwoBytes(currPage), 2);
                        success = await handleRequest(requestWrite, responseWrite, 20000);
                        if (!success){
                            break;
                        }
                        currPage++;
                    }
                    break;
                case 5:
                    byte[] requestReset = { 0x00, 0x50 };
                    byte[] responseReset = null;
                    success = await handleRequest(requestReset, responseReset, 20000);
                    break;

            }
        }
            //-------------------------------------for send and receive requests functionallity-------------------------------------//
            //-------------------------------------this is legacy code now do not touch this pls------------------------------------//
            public async Task<bool> handleRequest(byte[] message, byte[] response, int timeoutMilliseconds = 5000)
        {
            sendMessage(message);
            if (response == null)
            {
                return true;
            }

            UdpState s = new UdpState();
            s.e = remoteEndPoint;
            s.u = udpServer;

            int counter = 0;
            trial:
            if(counter == 5)
            {
                form.UpdateConnectionStatus("Receive operation timed out.");
                return false;
            }
            using (var cts = new CancellationTokenSource())
            {
                var receiveTask = Task.Run(() => ReceiveCallback(s, cts.Token), cts.Token);

                if (await Task.WhenAny(receiveTask, Task.Delay(timeoutMilliseconds, cts.Token)) == receiveTask)
                {
                    if(calledBackReponse != null && BitConverter.ToString(calledBackReponse) != BitConverter.ToString(response))
                    {
                        sendMessage(message);
                        counter++;
                        goto trial;
                    }
                    else if(calledBackReponse != null && BitConverter.ToString(calledBackReponse) == BitConverter.ToString(response))
                    {
                        return true;
                    }
                }
                form.UpdateConnectionStatus("Receive operation timed out.");
                return false;
            }
        }

        public void sendMessage(byte[] message)
        {

            udpServer.Send(message, message.Length, remoteEndPoint);
        }

        public void ReceiveCallback(UdpState state, CancellationToken token)
        {
            try
            {
                UdpClient u = state.u;
                IPEndPoint e = state.e;

                while (!token.IsCancellationRequested)
                {
                    if (u.Available > 0)
                    {
                        calledBackReponse = u.Receive(ref e);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                form.UpdateConnectionStatus($"Receive operation failed: {ex.Message}");
            }
        }


        //-------------------------------------utility functions-------------------------------------//
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
        public static byte[] IntToTwoBytes(int value)
        {
            byte[] bytes = BitConverter.GetBytes((ushort)value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }
    }
}
