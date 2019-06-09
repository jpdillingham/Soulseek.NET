namespace Soulseek.Tcp
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    internal class Listener : IListener
    {
        public event EventHandler<ConnectionAcceptedEventArgs> Accepted;

        public Listener(int port, ITcpListener tcpListener = null)
        {
            Port = port;
            TcpListener = tcpListener ?? new TcpListenerAdapter(new TcpListener(IPAddress.Parse("0.0.0.0"), port));
        }

        public void Start()
        {
            TcpListener.Start();
            Task.Run(() => ListenContinuouslyAsync()).Forget();
        }

        public void Stop()
        {
            TcpListener.Stop();
        }

        public int Port { get; }
        private ITcpListener TcpListener { get; set; }

        private async Task ListenContinuouslyAsync()
        {
            // todo: use a cancellation token to stop this and AcceptTcpClientAsync when Stop() is called
            
            while (true)
            {
                Console.WriteLine($"Listening for connections on {Port}");
                var client = await TcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
                Console.WriteLine($"Accepted connection from {client.Client.RemoteEndPoint}");

                var ep = (IPEndPoint)client.Client.RemoteEndPoint;

                var connection = new Connection(ep.Address, ep.Port, null, new TcpClientAdapter(client));

                Console.WriteLine($"Trying to read code...");
                var code = await connection.ReadAsync(1).ConfigureAwait(false);

                Console.WriteLine($"Code: {code}");

                //Accepted?.Invoke(this, new ConnectionAcceptedEventArgs(new TcpClientAdapter(client)));
            }
        }
    }
}
