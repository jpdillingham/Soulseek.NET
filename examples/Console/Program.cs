namespace Console
{
    using Soulseek.NET;
    using Soulseek.NET.Messaging;
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;

    class Program
    {
        public static object AsyncContext { get; private set; }

        public static void StartClient()
        {
            // Data buffer for incoming data.  
            byte[] bytes = new byte[1024];

            // Connect to a remote device.  
            try
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry("server.slsknet.org");
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, 2242);

                // Create a TCP/IP  socket.  
                Socket sender = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.  
                try
                {
                    sender.Connect(remoteEP);

                    Console.WriteLine("Socket connected to {0}",
                        sender.RemoteEndPoint.ToString());

                    // Encode the data string into a byte array.  

                    var writer = new MessageBuilder();
                    writer
                        .Code(MessageCode.Login)
                        .WriteString("username")
                        .WriteString("password")
                        .WriteInteger(181)
                        .WriteString("d51c9a7e9353746a6020f9602d452929")
                        .WriteInteger(1);

                    var message = writer.Build();

                    Console.WriteLine("Constructed message: " + BitConverter.ToString(message));

                    // Send the data through the socket.  
                    int bytesSent = sender.Send(message);

                    // Receive the response from the remote device.  
                    int bytesRec = sender.Receive(bytes);
                    Console.WriteLine("Echoed test = {0}", BitConverter.ToString(bytes));

                    var reader = new MessageReader(bytes);
                    Console.WriteLine($"Length: {reader.Length()}");
                    Console.WriteLine($"Code: {reader.Code()}");
                    Console.WriteLine($"Result: {reader.ReadByte()}");
                    Console.WriteLine($"Message: {reader.ReadString()}");

                    // Release the socket.  
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();

                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


        static async Task Main(string[] args)
        {
            //StartClient();

            var client = new Client();
            client.ServerStateChanged += Client_ServerStateChanged;

            client.Connect();

            client.Login("username", "password");

            Console.Write("Enter commands.");

            while (true)
            {
                var cmd = Console.ReadLine();
                client.Login("username", cmd);
            }
        }

        private static void Client_ServerStateChanged(object sender, ServerStateChangedEventArgs e)
        {
            Console.WriteLine($"Server state changed to {e.State}");
        }
    }
}
