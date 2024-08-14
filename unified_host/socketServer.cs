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
        public int pairedDevices = 0;
        public Dictionary<IPAddress, string> devicesResponses = new Dictionary<IPAddress, string>(); //dictionary of devices and their responses
        public IPAddress[] malfuntioning = [];
        public int responsesGot = 0;
        public Dictionary<IPAddress,sequenceConsole> devicesConsoles = new Dictionary<IPAddress, sequenceConsole>();

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
        public async Task<Dictionary<IPAddress, sequenceConsole>> programSequence(sequenceConsole console)
        {            
            console.addLine("sending wake up..."); //send ip and mac address to wake up device
            await operationHandler(0, console);

            if (!await checkMalfucntion(console))
            {
                console.addLine("wake up failed");
                goto terminateSequence;
            }
            foreach (IPAddress ip in devicesResponses.Keys)
            {
                devicesConsoles[ip].addLine("wake up sent successfully");
            }

            console.addLine("wake up sent successfully");
            await Task.Delay(2000);

            console.addLine("sending erase and boot ask..."); //send erase and boot to prepare for flash write
            await operationHandler(1, console);

            if (!await checkMalfucntion(console))
            {
                console.addLine("devices failed to erase and boot...");
                goto terminateSequence;
            }
            foreach (IPAddress ip in devicesResponses.Keys)
            {
                devicesConsoles[ip].addLine("erase and boot sent successfully");
            }
            console.addLine("erase and boot sent successfully");
            await Task.Delay(2000);



            console.addLine("sending flash write sequence..."); //send flash write to write new program sequence
            await operationHandler(2, console);

            if (!await checkMalfucntion(console))
            {
                console.addLine("some devices failed to flash write...");
                goto terminateSequence;
            }
            foreach (IPAddress ip in devicesResponses.Keys)
            {
                devicesConsoles[ip].addLine("flash write sent successfully");
            }
            console.addLine("flash write sent done successfully");
            await Task.Delay(2000);

            console.addLine($"sending checksum check... value of checksum calculated is: 0x{BitConverter.ToString(BitConverter.GetBytes(totalCheckSum))}"); //send checksum check to verify flash write
            await operationHandler(3, console);

            if (!await checkMalfucntion(console))
            {
                console.addLine("some devices failed to verify checksum...");
                goto terminateSequence;
            }
            foreach (IPAddress ip in devicesResponses.Keys)
            {
                devicesConsoles[ip].addLine("checksum validation sent successfully");
            }
            console.addLine($"checksum check sent and verified successfully with value 0x{BitConverter.ToString(BitConverter.GetBytes(totalCheckSum))}");
            await Task.Delay(2000);

            console.addLine("sending reset device..."); //reset device to boot into new program
            await operationHandler(4, console);

            if (!await checkMalfucntion(console))
            {
                console.addLine("some devices failed to reset...");
                goto terminateSequence;
            }
            foreach (IPAddress ip in devicesResponses.Keys)
            {
                devicesConsoles[ip].addLine("device reset sent successfully");
            }
            console.addLine("reset device sent");
            await Task.Delay(2000);


            //sequence completed
            console.addLine("sequence complete...");
        terminateSequence:
            return devicesConsoles;
        }

        //-----------------------------------------------handles logic of making UDP packets and expected responses---------------------------------------------//

        public async Task verifyDeviceConnection(sequenceConsole console)
        {
            //send a LED OFF command to fish for responsive IPs
            if(ipHost == null)
            {
                MessageBox.Show("No network connection found, please connect to a network and try again");
                return;
            }
            byte[] ipAddressBytes = ipHost.GetAddressBytes();
            byte[] message = { 0x00, 0x3f };
            byte[] requestWake = InsertByteArray(message, ipAddressBytes, 2);
            requestWake = InsertByteArray(requestWake, GetMacAddress(), 6);
            byte[] responseWake = null;
            await handleRequest(requestWake, responseWake, 2000);

            byte[] requestLED2 = { 0x00, 0x11 };
            byte[] responseLED2 = { 0x00, 0x0E, 0x00, 0x11 };
            await handleRequest(requestLED2, responseLED2, 2000);
            pairedDevices = devicesResponses.Count;
            console.addLine($"{pairedDevices} devices found");

            //initialize consoles for each device connected and detected
            devicesConsoles.Clear();
            foreach (IPAddress ip in devicesResponses.Keys)
            {
                devicesConsoles[ip] = new sequenceConsole();
            }
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
                    await handleRequest(requestWake, responseWake, 2000, console);
                    break;
                case 1: //erase memory and go to boot operation
                    byte[] requesBoot = { 0x00, 0x52 };
                    byte[] responseBoot = { 0x00, 0x48};
                    await handleRequest(requesBoot, responseBoot, 40000, console);
                    break;
                case 2: //flash write operation
                    int currPage = 0;
                    foreach (byte[] packet in packetsOut)
                    {
                        byte[] requestWrite = { 0x00, 0x55};
                        requestWrite = InsertByteArray(requestWrite, IntToTwoBytes(currPage), 2);
                        requestWrite = InsertByteArray(requestWrite, packet, 4);
                        byte[] responseWrite = { 0x00, 0x4A};
                        responseWrite = InsertByteArray(responseWrite, IntToTwoBytes(currPage), 2);
                        await handleRequest(requestWrite, responseWrite, 20000, console);
                        if (!await checkMalfucntion(console)){
                            console.addLine($"flash write failed at page: {currPage+1} out of {pageCount}");
                            break;
                        }
                        currPage++;
                    }
                    break;
                case 3: //checksum check operation
                    byte[] requestChecksum = { 0x00, 0x4D };
                    requestChecksum = InsertByteArray(requestChecksum, IntToTwoBytes(pageCount), 2);
                    byte[] responseChecksum = { 0x00, 0x4F };
                    byte[] checkSumBytes = BitConverter.GetBytes(totalCheckSum);
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(checkSumBytes);
                    }
                    responseChecksum = InsertByteArray(responseChecksum, checkSumBytes, 2);
                    await handleRequest(requestChecksum, responseChecksum, 20000, console);
                    break;
                case 4: //reset device operation
                    byte[] requestReset = { 0x00, 0x50 };
                    byte[] responseReset = null;
                    await handleRequest(requestReset, responseReset, 20000, console);
                    break;
            }
        }

        public async Task<bool> checkMalfucntion(sequenceConsole console) //if all devices non responsive send false
        {
            if (pairedDevices == 0)
            {
                console.addLine("devices not responding, check connection and try again");
                return false;
            }
            await Task.Delay(2000);
            return true;
        }
       //------------------------------------------------------for send and receive requests functionallity-----------------------------------------------//
      //------------------------------------------------------this is legacy code now do not touch this pls----------------------------------------------//

        public async Task handleRequest(byte[] message, byte[] response, int timeoutMilliseconds = 5000, sequenceConsole console = null)
        {
            sendMessage(message);
            if (response == null)
            {
                return;
            }

            UdpState s = new UdpState();
            s.e = remoteEndPoint;
            s.u = udpServer;

            int attemptNumber = 0;
        trial:
            bool tryAgain = false;
            if (attemptNumber == 5)
            {
                foreach (IPAddress currip in devicesResponses.Keys)
                {
                    if (devicesResponses[currip] != BitConverter.ToString(response))
                    {
                        malfuntioning.Append(currip);
                        devicesResponses.Remove(currip);
                        pairedDevices--;
                    }
                }
                return;
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
                                devicesConsoles[currip].addLine($"response not matched, retrying... attempt({attemptNumber + 1}/5)");
                            tryAgain = true;
                        }
                    }
                    if(tryAgain)
                    {
                        attemptNumber++;
                        goto trial;
                    }
                    return;
                }
            }
            foreach(IPAddress currip in devicesResponses.Keys)
            {
                if (devicesResponses[currip] != BitConverter.ToString(response))
                {
                    malfuntioning.Append(currip);
                    devicesResponses.Remove(currip);
                    pairedDevices--;
                }
            }
            return;
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
                        responsesGot = 0;
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
