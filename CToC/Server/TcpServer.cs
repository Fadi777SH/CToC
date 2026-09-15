using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CToC.Server
{
    public  class TcpServer
    {
        

        public async Task Sender(IPAddress IPAddressOfPC1 , IPAddress IPAddressOfPC2)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddressOfPC1 ,22));

            socket.Listen(10);

            var Accept = await socket.AcceptAsync();
            byte[] MSG = new byte[255];
            var numofbytes = Accept.Receive(MSG,0,MSG.Length ,0);
            Debug.WriteLine(numofbytes);

            Debug.WriteLine(Encoding.Default.GetString(MSG));
            
            //socket.Close();
        }
        public void Reciever(IPAddress IPAddressOfPC1, IPAddress IPAddressOfPC2)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var endpint = new IPEndPoint(IPAddressOfPC1,13000);
            socket.Connect(endpint);

            byte[] MSG = Encoding.ASCII.GetBytes("ww");

            socket.Send(MSG, 0, MSG.Length, 0);

            byte[] MSG2 = new byte[255];

            var RecievedMSG = socket.Receive(MSG2);

            byte[] MSG3 = new byte[RecievedMSG];

            for(int i=0; i<MSG2.Length; i++)
            {
                MSG3[i] = MSG2[i];
            }

            var s = Encoding.Default.GetString(MSG3);
            Debug.WriteLine(s);
            socket.Close();
            
        }
    }
}
