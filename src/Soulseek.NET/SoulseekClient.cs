namespace Soulseek.NET
{
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Maps;
    using Soulseek.NET.Messaging.Requests;
    using Soulseek.NET.Tcp;
    using System;
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

            MessageMapper = new MessageMapper();
        }

        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public string Address { get; private set; }
        public Connection Connection { get; private set; }
        public int Port { get; private set; }

        private MessageMapper MessageMapper { get; set; }

        public async Task ConnectAsync()
        {
            await Connection.ConnectAsync();
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            var request = new LoginRequest(username, password);

            Console.WriteLine($"Logging in as {username}...");

            await Connection.SendAsync(request.ToMessage().ToByteArray());

            return true;
        }

        private void OnConnectionDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine($"Data Received: {e.Data.Length} bytes");
            DataReceived?.Invoke(this, e);

            OnMessageReceived(new Message(e.Data));
        }

        private void OnMessageReceived(Message message)
        {
            var response = new object();
            var maps = MessageMapper.Map(message);

            switch (message.Code)
            {
                case MessageCode.Login:
                    response = new LoginResponse().MapFrom(message);
                    break;
                default:
                    response = null;
                    break;
            }

            MessageReceived?.Invoke(this, new MessageReceivedEventArgs() { Code = message.Code, Response = response });

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