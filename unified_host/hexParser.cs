using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace unified_host
{
    public class hexParser
    {
        public static void parseLinesIntoPackets(string[] lines, socketServer server)
        {
            UInt32 totalCheckSum = 0;
            int step = 0;
            List<byte[]> packets = new List<byte[]>();
            byte[] currentPacket = Enumerable.Repeat((byte)0xFF, 1024).ToArray();
            totalCheckSum += 1024*0xFF;

            foreach(string line in lines)
            {
                string recordType = line.Substring(7, 2);
                if(recordType == "01") { //end of file line
                    if(step != 0)
                        packets.Add(currentPacket);
                    break;
                }
                else if(recordType == "02" || recordType == "04") { // add extended address to all following lines
                    continue;
                }

                
                string linesize = line.Substring(1, 2);
                int datasize = Convert.ToInt32(linesize, 16);

                //close up packet and add to array if overflowing data 
                if(step >= 1024)
                {
                    step = 0;
                    totalCheckSum += 1024*0xFF;
                    packets.Add(currentPacket);
                    currentPacket = Enumerable.Repeat((byte)0xFF, 1024).ToArray();
                }

                // collect line data into array
                byte[] linedata = { };
                for (int i = 9; i < 2 * datasize + 9; i += 2)
                {
                    linedata = linedata.Concat(new byte[] { Convert.ToByte(line.Substring(i, 2), 16) }).ToArray();
                    totalCheckSum -= 0xFF;
                    totalCheckSum += Convert.ToByte(line.Substring(i, 2), 16);
                }

                //insert line data into current going packet
                linedata.CopyTo(currentPacket, step);
                step += datasize;
            }
            server.packetsOut = packets;
            server.totalCheckSum = totalCheckSum;
            server.pageCount = packets.Count;
        }
    }
}
