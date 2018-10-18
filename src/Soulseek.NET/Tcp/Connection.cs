namespace Soulseek.NET.Tcp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    public class Connection : IConnection
    {
        public Connection(string address = "server.slsknet.org", int port = 2242, TcpClient tcpClient = null)
        {
            Address = address;
            Port = port;
            TcpClient = tcpClient;
        }

        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<ConnectionStateChangedEventArgs> StateChanged;

        public string Address { get; private set; }
        public int Port { get; private set; }
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

        private TcpClient TcpClient { get; set; }
        private NetworkStream Stream { get; set; }

        public async Task ConnectAsync()
        {
            if (State != ConnectionState.Disconnected)
            {
                throw new ConnectionStateException($"Invalid attempt to connect a connected or transitioning connection (current state: {State})");
            }

            var ip = Dns.GetHostEntry(Address).AddressList[0];

            try
            {
                ChangeServerState(ConnectionState.Connecting, $"Connecting to {ip}:{Port}");

                await TcpClient.ConnectAsync(ip, Port);
                Stream = TcpClient.GetStream();

                ChangeServerState(ConnectionState.Connected, $"Connected to {ip}:{Port}");
            }
            catch (Exception ex)
            {
                ChangeServerState(ConnectionState.Disconnected, $"Connection Error: {ex.Message}");

                throw new ServerException($"Failed to connect to {Address}:{Port}: {ex.Message}", ex);
            }

            Task.Run(() => Read());
        }

        public void Disconnect(string message = null)
        {
            if (State == ConnectionState.Disconnected || State == ConnectionState.Disconnecting)
            {
                throw new ConnectionStateException($"Invalid attempt to disconnect a disconnected or transitioning connection (current state: {State})");
            }

            ChangeServerState(ConnectionState.Disconnecting, message);

            Stream.Close();
            TcpClient.Close();
            TcpClient.Dispose();

            ChangeServerState(ConnectionState.Disconnected, message);
        }

        private void CheckConnection()
        {
            if (!TcpClient.Connected)
            {
                Disconnect($"The server connection was closed unexpectedly.");
            }
        }

        public async Task SendAsync(byte[] bytes)
        {
            CheckConnection();

            if (State != ConnectionState.Connected)
            {
                throw new ConnectionStateException($"Invalid attempt to send to a disconnected or transitioning connection (current state: {State})");
            }

            if (bytes == null || bytes.Length == 0)
            {
                throw new ArgumentException($"Invalid attempt to send empty data.", nameof(bytes));
            }

            try
            {
                await Stream.WriteAsync(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Disconnect($"Write Error: {ex.Message}");
            }
        }

        private void ChangeServerState(ConnectionState state, string message)
        {
            State = state;
            StateChanged?.Invoke(this, new ConnectionStateChangedEventArgs() { State = state, Message = message });
        }

        private async Task Read()
        {
            var buffer = new List<byte>();

            try
            {
                while (true)
                {
                    do
                    {
                        var bytes = new byte[TcpClient.ReceiveBufferSize];
                        var bytesRead = await Stream.ReadAsync(bytes, 0, bytes.Length);

                        buffer.AddRange(bytes.Take(bytesRead));

                        var headMessageLength = BitConverter.ToInt32(buffer.ToArray(), 0) + 4;

                        if (buffer.Count >= headMessageLength)
                        {
                            DataReceived?.Invoke(this, new DataReceivedEventArgs() { Data = buffer.Take(headMessageLength).ToArray() });
                            buffer.RemoveRange(0, headMessageLength);
                        }
                    } while (Stream.DataAvailable);
                }
            }
            catch (Exception ex)
            {
                Disconnect($"Read Error: {ex.Message}");
            }
        }
    }
}