namespace Soulseek.Tcp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
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

                Console.WriteLine($"Trying to read code...");
                var message = new List<byte>();

                var lengthBytes = await ReadAsync(client, 5, CancellationToken.None).ConfigureAwait(false);
                var length = BitConverter.ToInt32(lengthBytes, 0);
                var code = (int)lengthBytes.Skip(4).ToArray()[0];

                // peer init 
                if (code == 1)
                {
                    Console.WriteLine($"Length: {length}, Code: {code}");

                    var bytesRemaining = length - 1;

                    var restBytes = await ReadAsync(client, bytesRemaining, CancellationToken.None).ConfigureAwait(false);
                    var nameLen = BitConverter.ToInt32(restBytes, 0);
                    Console.WriteLine($"Name len: {nameLen}");
                    var name = Encoding.ASCII.GetString(restBytes.Skip(4).Take(nameLen).ToArray());
                    Console.WriteLine($"Name: {name}");
                    var typeLen = BitConverter.ToInt32(restBytes, 4 + nameLen);
                    Console.WriteLine($"Type len: {typeLen}");
                    var type = Encoding.ASCII.GetString(restBytes.Skip(4 + nameLen + 4).Take(typeLen).ToArray());
                    Console.WriteLine($"Type: {type}");
                    var token = BitConverter.ToInt32(restBytes, 4 + nameLen + 4 + typeLen);
                    Console.WriteLine($"Token: {token}");

                    Console.WriteLine(string.Join(" ", restBytes.Select(x => (int)x).ToArray()));

                    Accepted?.Invoke(this, new ConnectionAcceptedEventArgs(new TcpClientAdapter(client), type, name, token));
                }

                // todo: handle pierce firewall
            }
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        private async Task<byte[]> ReadAsync(TcpClient client, int length, CancellationToken cancellationToken)
        {
            var result = new List<byte>();

            var buffer = new byte[4096];
            var totalBytesRead = 0;

            try
            {
                while (totalBytesRead < length)
                {
                    var bytesRemaining = length - totalBytesRead;
                    var bytesToRead = bytesRemaining > buffer.Length ? buffer.Length : bytesRemaining;

                    var bytesRead = await client.GetStream().ReadAsync(buffer, 0, bytesToRead, cancellationToken).ConfigureAwait(false);

                    if (bytesRead == 0)
                    {
                        Console.WriteLine($"Remote connection closed.");
                        break;
                    }

                    totalBytesRead += bytesRead;
                    var data = buffer.Take(bytesRead);
                    result.AddRange(data);
                }

                return result.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read {length} bytes: {ex.Message}", ex);
                return null;
            }
        }
    }
}
