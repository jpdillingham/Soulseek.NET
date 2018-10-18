namespace Soulseek.NET
{
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Login;
    using Soulseek.NET.Tcp;
    using System;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class SoulseekClient
    {
        public SoulseekClient(string address = "server.slsknet.org", int port = 2242)
        {
            Address = address;
            Port = port;

            Connection = new Connection(Address, Port);
            Connection.StateChanged += OnConnectionStateChanged;
            Connection.DataReceived += OnConnectionDataReceived;
        }

        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<RawMessageReceivedEventArgs> RawMessageReceived;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public string Address { get; private set; }
        public Connection Connection { get; private set; }
        public int Port { get; private set; }

        public async Task ConnectAsync()
        {
            await Connection.ConnectAsync();
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            var request = new LoginRequest(username, password).ToBytes();

            Console.WriteLine($"Logging in as {username}...");

            await Connection.SendAsync(request);

            return true;
        }

        private void OnConnectionDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine($"Data Received: {e.Data.Length} bytes");
            DataReceived?.Invoke(this, e);

            OnRawMessageReceived(e.Data);
        }

        private void OnRawMessageReceived(byte[] message)
        {
            var reader = new MessageReader(message);
            Console.WriteLine($"Raw Message Recieved; Length: {reader.Length}, Code: {reader.Code}, Data ({reader.Payload.Length}): {Encoding.ASCII.GetString(reader.Payload).ToCharArray().Take(30)} ");
            RawMessageReceived?.Invoke(this, new RawMessageReceivedEventArgs() { Reader = reader });

            OnMessageReceived(reader.Code, reader.RawBytes);
        }

        private void OnMessageReceived(MessageCode code, byte[] message)
        {
            var response = new object();

            switch (code)
            {
                case MessageCode.Login:
                    response = new LoginResponse(message);
                    break;
                default:
                    response = null;
                    break;
            }

            MessageReceived?.Invoke(this, new MessageReceivedEventArgs() { Code = code, Response = response });

            if (response is LoginResponse lr)
            {
                Console.WriteLine($"Status: {lr.Status}");
                Console.WriteLine($"Message: {lr.Message}");
                Console.WriteLine($"IPAddress: {lr.IPAddress}");
            }
        }



        private void OnConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            Console.WriteLine($"Connection State Changed: {e.State} ({e.Message ?? "Unknown"})");
            ConnectionStateChanged?.Invoke(this, e);
        }
    }
}