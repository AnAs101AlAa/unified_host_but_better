using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;

namespace unified_host
{
    public partial class socketServer
    {
        public int pageCount; //number of packets in flash to write
        public UInt32 totalCheckSum; //checksum of all packets
        public List<byte[]> packetsOut; //list of packets to send
        public int port; //port to connect to
        public IPAddress ipHost; //host IP address
        public UdpClient udpServer; //UDP client
        public IPEndPoint remoteEndPoint; //remote endpoint
        public byte[] calledBackReponse; //actual response from server at any point
        public bool success = false; //success flag on each command sent
        public int pairedDevices = 2;
        public Dictionary<IPAddress, string> devicesResponses = new Dictionary<IPAddress, string>(); //dictionary of devices and their responses
        public IPAddress[] malfuntioning = [];
        public struct UdpState
        {
            public UdpClient u;

            public IPEndPoint e;
        }

        public socketServer(int port)
        {
            this.port = port;
            this.ipHost = GetEthernetIPv4Address();
            remoteEndPoint = new IPEndPoint(IPAddress.Broadcast, port);
            udpServer = new UdpClient(port);
        }
        
        public void stop(unified_host form, sequenceConsole console)
        {
            console.addLine("stopping connection...");
            try
            {
                udpServer.Close();
                udpServer.Dispose();
                console.addLine("disconnected");
            }
            catch (Exception x)
            {
                console.addLine($"An error occurred: {x.Message}");
            }
        }

        //-------------------------------------sequence of operations to be performed-------------------------------------//
        public async Task<bool> programSequence(sequenceConsole console)
        {
            success = false;

            
            console.addLine("sending wake up..."); //send ip and mac address to wake up device
            await operationHandler(0, console);
            if (success)
            {
                console.addLine("wake up sent successfully");
            }
            else
            {
                console.addLine("wake up timed out");
                return false;
            }

            if(!await checkMalfucntion(console))
                {
                return false;
            }

            console.addLine("sending LED ON ask..."); //send LED ON to test LED
            await operationHandler(1, console);
            if (success)
            {
                console.addLine("LED ON sent successfully");
            }
            else
            {
                console.addLine("LED ON timed out");
                return false;
            }

            if (!await checkMalfucntion(console))
            {
                return false;
            }

            console.addLine("sending lED OFF ask..."); //send LED OFF to double check LED
            await operationHandler(2, console);
            if (success)
            {
                console.addLine("LED OFF sent successfully");
            }
            else
            {
                console.addLine("LED OFF timed out");
                return false;
            }

            if (!await checkMalfucntion(console))
            {
                return false;
            }

            console.addLine("sending erase and boot ask..."); //send erase and boot to prepare for flash write
            await operationHandler(3, console);
            if (success)
            {
                console.addLine("erase and boot sent successfully");
            }
            else
            {
                console.addLine("erase and boot timed out");
                return false;
            }

            if (!await checkMalfucntion(console))
            {
                return false;
            }

            console.addLine("sending flash write sequence..."); //send flash write to write new program sequence
            await operationHandler(4, console);
            if (success)
            {
                console.addLine("flash write sent done successfully");
            }
            else
            {
                console.addLine("flash write timed out");
                return false;
            }

            if (!await checkMalfucntion(console))
            {
                return false;
            }

            console.addLine($"sending checksum check... value of checksum calculated is: 0x{BitConverter.ToString(BitConverter.GetBytes(totalCheckSum))}"); //send checksum check to verify flash write
            await operationHandler(5, console);
            if (success)
            {
                console.addLine($"checksum check sent and verified successfully with value 0x{BitConverter.ToString(BitConverter.GetBytes(totalCheckSum))}");
            }
            else
            {
                console.addLine("checksum check timed out");
                return false;
            }

            if (!await checkMalfucntion(console))
            {
                return false;
            }

            console.addLine("sending reset device..."); //reset device to boot into new program
            await operationHandler(6, console);
            if (success)
            {
                console.addLine("reset device sent");
            }
            else
            {
                console.addLine("reset device timed out");
                return false;
            }

            if (!await checkMalfucntion(console))
            {
                return false;
            }
            //sequence completed
            console.addLine("sequence complete...poggers" + Environment.NewLine + "device should be reset and running new program" + Environment.NewLine + "i am not in danger I AM THE DANGER. I AM THE ONE WHO KNOCKS");
            return true;
        }

        //-----------------------------------------------handles logic of making UDP packets and expected responses---------------------------------------------//

        public async Task verifyDeviceConnection(sequenceConsole console)
        {
            byte[] ipAddressBytes = ipHost.GetAddressBytes();
            byte[] message = { 0x00, 0x3f };
            byte[] requestWake = InsertByteArray(message, ipAddressBytes, 2);
            byte[] mac = { 0x60, 0x18, 0x95, 0x2D, 0x44, 0xF8 };
            requestWake = InsertByteArray(requestWake, mac, 6);
            byte[] responseWake = null;
            success = await handleRequest(requestWake, responseWake, 2000);

            if (!success)
                return;

            byte[] requestLED2 = { 0x00, 0x11 };
            byte[] responseLED2 = { 0x00, 0x0E, 0x00, 0x11 };
            success = await handleRequest(requestLED2, responseLED2, 2000);
            pairedDevices = devicesResponses.Count;
            console.addLine($"device connected successfully, {pairedDevices} devices found");
        }

        public async Task operationHandler(int opcode, sequenceConsole console)
        {
            switch (opcode)
            {
                case 0: //handshake operation
                    byte[] ipAddressBytes = ipHost.GetAddressBytes();
                    byte[] message = { 0x00, 0x3f };
                    byte[] requestWake = InsertByteArray(message, ipAddressBytes, 2);
                    requestWake = InsertByteArray(requestWake, GetMacAddress(), 6);
                    byte[] responseWake = null;
                    success = await handleRequest(requestWake, responseWake, 2000, console);
                    break;
                case 1: //LED ON operation
                    byte[] requestLED1 = { 0x00, 0x10 };
                    byte[] responseLED1 = { 0x00, 0x0E, 0x00, 0x10 };
                    success = await handleRequest(requestLED1, responseLED1, 2000, console);
                    break;
                case 2: //LED OFF operation
                    byte[] requestLED2 = { 0x00, 0x11 };
                    byte[] responseLED2 = { 0x00, 0x0E, 0x00, 0x11 };
                    success = await handleRequest(requestLED2, responseLED2, 2000, console);
                    break;
                case 3: //erase memory and go to boot operation
                    byte[] requesBoot = { 0x00, 0x52 };
                    byte[] responseBoot = { 0x00, 0x48};
                    success = await handleRequest(requesBoot, responseBoot, 20000, console);
                    break;
                case 4: //flash write operation
                    int currPage = 0;
                    foreach (byte[] packet in packetsOut)
                    {
                        byte[] requestWrite = { 0x00, 0x55};
                        requestWrite = InsertByteArray(requestWrite, IntToTwoBytes(currPage), 2);
                        requestWrite = InsertByteArray(requestWrite, packet, 4);
                        byte[] responseWrite = { 0x00, 0x4A};
                        responseWrite = InsertByteArray(responseWrite, IntToTwoBytes(currPage), 2);
                        success = await handleRequest(requestWrite, responseWrite, 20000, console);
                        if (!success){
                            console.addLine($"flash write failed at page: {currPage+1} out of {pageCount}");
                            break;
                        }
                        currPage++;
                    }
                    break;
                case 5: //checksum check operation
                    byte[] requestChecksum = { 0x00, 0x4D };
                    requestChecksum = InsertByteArray(requestChecksum, IntToTwoBytes(pageCount), 2);
                    byte[] responseChecksum = { 0x00, 0x4F };
                    byte[] checkSumBytes = BitConverter.GetBytes(totalCheckSum);
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(checkSumBytes);
                    }
                    responseChecksum = InsertByteArray(responseChecksum, checkSumBytes, 2);
                    success = await handleRequest(requestChecksum, responseChecksum, 20000, console);
                    break;
                case 6: //reset device operation
                    byte[] requestReset = { 0x00, 0x50 };
                    byte[] responseReset = null;
                    success = await handleRequest(requestReset, responseReset, 20000, console);
                    break;

            }
        }

        public async Task<bool> checkMalfucntion(sequenceConsole console)
        {
            if (pairedDevices == 0)
            {
                console.addLine("device not responding, check connection and try again");
                return false;
            }
            success = false;
            await Task.Delay(2000);
            return true;
        }
       //------------------------------------------------------for send and receive requests functionallity-----------------------------------------------//
      //------------------------------------------------------this is legacy code now do not touch this pls----------------------------------------------//

        public async Task<bool> handleRequest(byte[] message, byte[] response, int timeoutMilliseconds = 5000, sequenceConsole console = null)
        {
            sendMessage(message);
            if (response == null)
            {
                return true;
            }

            UdpState s = new UdpState();
            s.e = remoteEndPoint;
            s.u = udpServer;

            int attemptNumber = 0;
        trial:
            bool tryAgain = false;
            if (attemptNumber == 5)
            {
                return false;
            }
            using (var cts = new CancellationTokenSource())
            {
                var receiveTask = Task.Run(() => ReceiveCallback(s, cts.Token), cts.Token);

                if (await Task.WhenAny(receiveTask, Task.Delay(timeoutMilliseconds, cts.Token)) == receiveTask)
                {
                    foreach(IPAddress currip in devicesResponses.Keys)
                    {
                        if (devicesResponses[currip] != BitConverter.ToString(response))
                        {
                            if (attemptNumber == 4)
                            {
                                malfuntioning.Append(currip);
                                devicesResponses.Remove(currip);
                                pairedDevices--;
                            }
                            if (console != null)
                                console.addLine($"response not matched, retrying... attempt({attemptNumber + 1}/5)");
                            tryAgain = true;
                        }
                    }
                    if(tryAgain)
                    {
                        attemptNumber++;
                        goto trial;
                    }
                    return true;
                }
            }
        return false;
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
                IPEndPoint e = new IPEndPoint(IPAddress.Any, port); // Initialize with IPAddress.Any

                while (!token.IsCancellationRequested)
                {
                    if (u.Available > 0)
                    {
                        int responsesGot = 0;
                        while(responsesGot < pairedDevices)
                        {
                            calledBackReponse = u.Receive(ref e);
                            IPAddress ip = e.Address;
                            if(ip.ToString() == ipHost.ToString())
                            {
                                continue;
                            }
                            devicesResponses[ip] = BitConverter.ToString(calledBackReponse);
                            responsesGot++;
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                return;
            }
        }

        //------------------------------------------------------------------utility functions---------------------------------------------------------------//
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
        public static byte[] GetMacAddress()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if ((nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
                {
                    PhysicalAddress address = nic.GetPhysicalAddress();
                    return address.GetAddressBytes();
                }
            }
            return null;
        }
        static IPAddress GetEthernetIPv4Address()
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up &&
                    nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    var ipProperties = nic.GetIPProperties();
                    var ipv4Address = ipProperties.UnicastAddresses
                        .Where(addr => addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        .Select(addr => addr.Address)
                        .FirstOrDefault();

                    if (ipv4Address != null)
                    {
                        return ipv4Address;
                    }
                }
            }
            return null;
        }
    }
}
