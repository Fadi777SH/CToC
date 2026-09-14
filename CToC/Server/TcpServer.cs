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
        
        TcpListener listener;

        public delegate void OnclientConnectionChange(bool State);
        public event OnclientConnectionChange? OnClientConnection;
        public bool State = false;
        public  TcpServer()
        {
            //StartTcpServer();
        }
        

        public async void StartTcpServer()
        {
            var port = 13000;
            var IPaddress = IPAddress.Parse("192.168.100.91");
            listener = new TcpListener(IPaddress, port);

            listener.Start();

            

            using TcpClient client = await listener.AcceptTcpClientAsync();
            if(client.Connected==true||client.Connected==false)
            {
                OnClientConnection?.Invoke(client.Connected);
            }
            _ = ClientHandler(client);
            
        }

        private async Task ClientHandler(TcpClient client)
        {
            using (client) 
            using(NetworkStream? tcpstream = client.GetStream())
            {

                int readtotal;
                byte[] buffer = new byte[255];
                while ((readtotal = await tcpstream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                    Debug.WriteLine(message);
                    
                }
            }
            
        }



        public void stopserver()
        {
            MainWindow.StopClient += listener.Stop;

        }

    }
}
