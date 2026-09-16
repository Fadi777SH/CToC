using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace CToC.Server
{
    public  class TcpServer
    {
        Socket Accept;
        public static event Action? PC2DisConnect;
        public async Task Sender(IPAddress IPAddressOfPC1 , IPAddress IPAddressOfPC2)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddressOfPC1 ,22));

            

            socket.Listen(10);

            var Endpoint = new IPEndPoint(IPAddressOfPC2, 22);
            new Thread(async delegate ()
            {

                Accept = await socket.AcceptAsync();

                socket.Close();
                while (true)
                {
                    try
                    {
                        byte[] buffer = new byte[255];
                        var rec = await Accept.ReceiveAsync(buffer,0);
                        if (rec <= 0)
                        {
                            throw new SocketException();
                        }
                        Array.Resize(ref buffer, rec);
                        Debug.WriteLine(Encoding.Default.GetString(buffer));
                        await Accept.SendAsync(Encoding.ASCII.GetBytes("hello sir\n"));

                    }
                    catch
                    {
                        
                        MessageBox.Show("Error");
                        PC2DisConnect?.Invoke();
                        break;
                        
                        
                    }
                }
            }).Start();
            
            
        }
        public async Task Reciever(IPAddress IPAddressOfPC1, IPAddress IPAddressOfPC2)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var endpint = new IPEndPoint(IPAddressOfPC1,22);
            socket.Connect(endpint);
            new Thread(async delegate ()
            {

                Accept = await socket.AcceptAsync();

                socket.Close();
                while (true)
                {
                    try
                    {
                        byte[] buffer = new byte[255];
                        var rec = await Accept.ReceiveAsync(buffer, 0);
                        if (rec <= 0)
                        {
                            throw new SocketException();
                        }
                        Array.Resize(ref buffer, rec);
                        Debug.WriteLine(Encoding.Default.GetString(buffer));
                        await Accept.SendAsync(Encoding.ASCII.GetBytes("hello sir\n"));

                    }
                    catch
                    {

                        MessageBox.Show("Error");
                        Accept.Close();
                        PC2DisConnect?.Invoke();
                        break;

                    }
                }
            }).Start();


        }
    }
}
