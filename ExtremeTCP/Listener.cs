using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace ExtremeTCP
{
    public class Listener
    {
        private TcpListener Socket;
        public List<TcpClient> Clients = new List<TcpClient>();

        public OnConnectionDelegate OnConnection;
        public delegate void OnConnectionDelegate(TcpClient Client);
        public OnMessageDelegate OnMessage;
        public delegate void OnMessageDelegate(string Message, TcpClient Client, Sender Sender);

        public void Start(int Port)
        {
            Socket = new TcpListener(IPAddress.Any, Port);
            Socket.Start();
            new Thread(new ThreadStart(ServerThread)).Start();
        }

        public void Stop()
        {
            Socket.Stop();
        }

        public void Multicast(string Message, List<TcpClient> ToExclude = null)
        {
            foreach (var Client in Clients)
            {
                if (ToExclude != null)
                {
                    foreach (var ExcludedClient in ToExclude)
                    {
                        if (ExcludedClient != Client)
                        {
                            Byte[] bytes = new Byte[8192];
                            bytes = Encoding.ASCII.GetBytes(Message + "\0");
                            Client.GetStream().Write(bytes, 0, bytes.Length);
                        }
                    }
                }
                else
                {
                    Byte[] bytes = new Byte[8192];
                    bytes = Encoding.ASCII.GetBytes(Message + "\0");
                    Client.GetStream().Write(bytes, 0, bytes.Length);
                }
            }
        }

        private void ClientThread(object obj)
        {
            var Client = (TcpClient)obj;
            while (true)
            {
                var Data = new Data(Client.GetStream(), this, Client, "\0");
                var Message = Data.Read();
                if (Message != "")
                {
                    OnMessage.Invoke(Message, Client, new Sender(Client.GetStream()));
                }
            }
        }

        private void ServerThread()
        {
            while (true)
            {
                var Client = Socket.AcceptTcpClient();
                Clients.Add(Client);
                OnConnection.Invoke(Client);
                ThreadPool.QueueUserWorkItem(ClientThread, Client);
            }
        }

        public void Disconnect(TcpClient Client)
        {
            Clients.Remove(Client);
        }
    }

    public class Sender
    {
        public Sender(NetworkStream S)
        {
            Stream = S;
        }

        private NetworkStream Stream;
        public void Send(string d)
        {
            Byte[] bytes = new Byte[8192];
            bytes = Encoding.ASCII.GetBytes(d + "\0");
            Stream.Write(bytes, 0, bytes.Length);
        }

    }

    public class Data
    {
        public Data(NetworkStream S, Listener L, TcpClient C, string NullByte)
        {
            Stream = S;
            Listener = L;
            Client = C;
            NullTerminatingByte = NullByte;
        }

        private string NullTerminatingByte;
        private NetworkStream Stream;
        private TcpClient Client;
        private Listener Listener;
        public void Send(string d)
        {
            Byte[] bytes = new Byte[8192];
            bytes = Encoding.ASCII.GetBytes(d + NullTerminatingByte);
            Stream.Write(bytes, 0, bytes.Length);
        }

        public string Read()
        {
            string final = "";
            Byte[] bytes = new Byte[8192];
            var i = Stream.Read(bytes, 0, bytes.Length);
            if (i > 0)
            {
                Listener.Disconnect(Client);
            }
            var data = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
            foreach (char c in data)
            {
                if (c.ToString() != NullTerminatingByte)
                {
                    final += c.ToString();
                }
                else
                {
                    return final;
                }
            }
            return "";
        }
    }
}
