using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsRadio
{
    class Program
    {

        static UdpClient receiver;
        static bool finish = false;
        static long totalBytes = 0;
        static byte[] recvData;

        static Queue<byte> buffer;
        static UdpClient sender;
        static byte[] sendData;
        static byte[] sendDataHeader = { 0x88, 0x23, 0x80, 0x02, 0x00, 0x00, 0x00, 0x00,
            0x02, 0x00, 0x74, 0x11, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF};
        static UInt32 timeStamp = 0;

        

        static void Main(string[] args)
        {
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 1234);
            receiver = new UdpClient(RemoteIpEndPoint);
            recvData = new byte[1000000];

            //IPEndPoint RemoteIpEndPoint2 = new IPEndPoint(IPAddress.Parse("255.255.255.255"), 2100);
            //sender = new UdpClient(RemoteIpEndPoint2);

            sender = new UdpClient();
            IPAddress multicastaddress = IPAddress.Parse("231.129.64.32");
            sender.JoinMulticastGroup(multicastaddress);
            IPEndPoint remoteep = new IPEndPoint(multicastaddress, 2100);

            sendData = new byte[640];

            buffer = new Queue<byte>(100000000);
            

            while (!finish)
            {
                //ReceiveData();

                recvData = receiver.Receive(ref RemoteIpEndPoint);

                if (recvData.Length != 0)
                {
                    totalBytes += recvData.Length;

                    for (int i = 0; i < recvData.Length; i++) buffer.Enqueue(recvData[i]);
                }

                while (buffer.Count > 640)
                {
                    byte[] timeStampbytes = BitConverter.GetBytes(timeStamp);
                    sendDataHeader[4] = timeStampbytes[0];
                    sendDataHeader[5] = timeStampbytes[1];
                    sendDataHeader[6] = timeStampbytes[2];
                    sendDataHeader[7] = timeStampbytes[3];

                    for (int i = 0; i < 640; i++) sendData[i] = buffer.Dequeue();

                    List<byte> concat = new List<byte>();
                    concat.AddRange(sendDataHeader);
                    concat.AddRange(sendData);
                    sender.Send(concat.ToArray(), 660, remoteep);

                    timeStamp++;
                }

                Console.Write(string.Format("\rTotal: {0}\tQueue count: {1}", totalBytes, buffer.Count));

                Thread.Sleep(1);
                
            }


        }

        static async void ReceiveData()
        {
            UdpReceiveResult x = await receiver.ReceiveAsync();

            if (x.Buffer.Length != 0)
            {
                totalBytes += x.Buffer.Length;
                
                for (int i = 0; i < x.Buffer.Length; i++) buffer.Enqueue(x.Buffer[i]);

                
            }
        }
    }
}
