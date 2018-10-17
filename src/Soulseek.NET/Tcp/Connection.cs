namespace Soulseek.NET.Tcp
{
    using Soulseek.NET.Messaging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;

    public class Connection
    {
        public Connection(string address = "server.slsknet.org", int port = 2242, int bufferSize = 1024)
        {
            Address = address;
            Port = port;
            BufferSize = bufferSize;
        }

        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        public string Address { get; private set; }
        public int Port { get; private set; }
        public int BufferSize { get; private set; }
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

        private TcpClient Server { get; set; }

        public async Task ConnectAsync()
        {
            var ip = Dns.GetHostEntry(Address).AddressList[0];
            Server = new TcpClient();

            try
            {
                ChangeServerState(ConnectionState.Connecting);
                await Server.ConnectAsync(ip, Port);
                ChangeServerState(ConnectionState.Connected);
            }
            catch (Exception ex)
            {
                ChangeServerState(ConnectionState.Disconnected);
                throw new ServerException($"Failed to connect to {Address}:{Port}: {ex.Message}", ex);
            }

            await Task.Run(() => ReadAlways());
        }

        public async Task ReadAlways()
        {
            var responses = new List<byte[]>();
            var stream = Server.GetStream();

            while (true)
            {
                var currentResponse = new List<byte>();

                do
                {
                    var buffer = new byte[BufferSize];
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                    currentResponse.AddRange(buffer.Take(bytesRead));
                    Console.WriteLine($"Read {bytesRead} bytes");
                } while (stream.DataAvailable);

                var head = 0;

                while (head < currentResponse.Count())
                {
                    var len = BitConverter.ToInt32(currentResponse.ToArray(), head);
                    var code = BitConverter.ToInt32(currentResponse.ToArray(), head + 4);

                    var dataLen = len - 4;
                    var data = currentResponse.Skip(head + 8).Take(dataLen).ToArray();
                    var dataStr = Encoding.ASCII.GetString(data);

                    if (data.Length < dataLen)
                    {
                        Console.WriteLine("===========================================================");
                    }

                    Console.WriteLine($"\nMessage length: {len}, code: {(MessageCode)code}, data: {dataStr}\n");
                    //Console.WriteLine($"\nMessage length: {len}, code: {(MessageCode)code}\n");

                    head += 4 + len;
                }
            }
        }

        public async Task<IEnumerable<byte[]>> ReadAsync()
        {
            var responses = new List<byte[]>();
            var currentResponse = new List<byte>();

            var stream = Server.GetStream();

            Console.WriteLine("Reading...");
            do
            {
                var buffer = new byte[BufferSize];
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                currentResponse.AddRange(buffer.Take(bytesRead));
                Console.WriteLine($"Read {bytesRead} bytes");

                if (bytesRead < buffer.Length)
                {
                    Console.WriteLine("New Message");
                    Console.WriteLine(Encoding.ASCII.GetString(currentResponse.ToArray()));
                    responses.Add(currentResponse.ToArray());
                    currentResponse = new List<byte>();
                }
            } while (stream.DataAvailable);

            // edge case where bytesRead == buffer.Length
            if (currentResponse.Count() > 0)
            {
                responses.Add(currentResponse.ToArray());
            }

            return responses;
        }

        public async Task WriteAsync(byte[] buffer)
        {
            var stream = Server.GetStream();
            await stream.WriteAsync(buffer, 0, buffer.Length);
        }

        private void ChangeServerState(ConnectionState state)
        {
            State = state;
            ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs() { State = state });
        }
    }
}