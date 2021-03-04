namespace Soulseek.Network.Tcp
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class Extensions
    {
        public static async Task<(string ProxyAddress, int ProxyPort)> ConnectThroughProxyAsync(
            this ITcpClient client,
            string proxyAddress,
            int proxyPort,
            string destinationAddress,
            int destinationPort,
            string username = null,
            string password = null,
            CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrEmpty(proxyAddress))
            {
                throw new ArgumentNullException(nameof(proxyAddress));
            }

            if (proxyPort < IPEndPoint.MinPort || proxyPort > IPEndPoint.MaxPort)
            {
                throw new ArgumentOutOfRangeException(nameof(proxyPort), proxyPort, $"Proxy port must be within {IPEndPoint.MinPort} and {IPEndPoint.MaxPort}, inclusive");
            }

            if (string.IsNullOrEmpty(destinationAddress))
            {
                throw new ArgumentNullException(nameof(destinationAddress));
            }

            if (destinationPort < IPEndPoint.MinPort || destinationPort > IPEndPoint.MaxPort)
            {
                throw new ArgumentOutOfRangeException(nameof(destinationPort), destinationPort, $"Destination port must be within {IPEndPoint.MinPort} and {IPEndPoint.MaxPort}, inclusive");
            }

            if (username == default != (password == default))
            {
                throw new ArgumentException("Username and password must agree; supply both or neither");
            }

            if (username != default && username.Length > 255)
            {
                throw new ArgumentOutOfRangeException(nameof(username), "The username must be less than or equal to 255 characters");
            }

            if (password != default && password.Length > 255)
            {
                throw new ArgumentOutOfRangeException(nameof(password), "The password must be less than or equal to 255 characters");
            }

            const byte SOCKS_5 = 0x05;

            const byte AUTH_ANONYMOUS = 0x00;
            const byte AUTH_USERNAME = 0x02;
            const byte AUTH_VERSION = 0x1;

            const byte CONNECT = 0x01;

            const byte IPV4 = 0x01;
            const byte DOMAIN = 0x03;
            const byte IPV6 = 0x04;

            const byte EMPTY = 0x00;
            const byte ERROR = 0xFF;

            cancellationToken ??= CancellationToken.None;

            static async Task<byte[]> ReadAsync(NetworkStream stream, int length, CancellationToken cancellationToken)
            {
                var buffer = new byte[1024];
                var bytesRead = await stream.ReadAsync(buffer, 0, length, cancellationToken).ConfigureAwait(false);
                return buffer.AsSpan().Slice(0, bytesRead).ToArray();
            }

            await client.ConnectAsync(proxyAddress, proxyPort);
            var stream = client.GetStream();

            // https://tools.ietf.org/html/rfc1928

            // The client connects to the server, and sends a version identifier/method selection message:
            // +-----+----------+----------+
            // | VER | NMETHODS | METHODS  |
            // +-----+----------+----------+
            // | 1   | 1        | 1 to 255 |
            // +-----+----------+----------+
            var auth = new byte[] { SOCKS_5, 0x01, AUTH_ANONYMOUS };

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                auth = new byte[] { SOCKS_5, 0x02, AUTH_ANONYMOUS, AUTH_USERNAME };
            }

            await stream.WriteAsync(auth, cancellationToken.Value);

            // The server selects from one of the methods given in METHODS, and sends a METHOD selection message:
            // +-----+--------+
            // | VER | METHOD |
            // +-----+--------+
            // | 1   | 1      |
            // +-----+--------+
            var authResponse = await ReadAsync(stream, 2, cancellationToken.Value);

            if (authResponse[0] != SOCKS_5)
            {
                throw new IOException($"Invalid SOCKS version (expected: {SOCKS_5}, received: {authResponse[0]})");
            }

            switch (authResponse[1])
            {
                case AUTH_ANONYMOUS:
                    break;
                case AUTH_USERNAME:
                    // https://tools.ietf.org/html/rfc1929

                    // Once the SOCKS V5 server has started, and the client has selected the
                    // Username / Password Authentication protocol, the Username / Password
                    // subnegotiation begins.  This begins with the client producing a
                    // Username / Password request:
                    // +-----+------+----------+------+----------+
                    // | VER | ULEN | UNAME    | PLEN | PASSWD   |
                    // +-----+------+----------+------+----------+
                    // | 1   | 1    | 1 to 255 | 1    | 1 to 255 |
                    // +-----+------+----------+------+----------+
                    var creds = new List<byte>() { AUTH_VERSION };

                    creds.Add((byte)username.Length);
                    creds.AddRange(Encoding.ASCII.GetBytes(username));

                    creds.Add((byte)password.Length);
                    creds.AddRange(Encoding.ASCII.GetBytes(password));

                    await stream.WriteAsync(creds.ToArray(), cancellationToken.Value);

                    // The server verifies the supplied UNAME and PASSWD, and sends the
                    // following response:
                    // +----+--------+
                    // |VER | STATUS |
                    // +----+--------+
                    // | 1  |   1    |
                    // +----+--------+
                    var credsResponse = await ReadAsync(stream, 2, cancellationToken.Value);

                    if (credsResponse.Length != 2)
                    {
                        throw new IOException("Abnormal authentication response from server");
                    }

                    if (credsResponse[0] != AUTH_VERSION)
                    {
                        throw new IOException($"Invalid authentication subnegotiation version (expected: {AUTH_VERSION}, received: {credsResponse[0]})");
                    }

                    if (credsResponse[1] != EMPTY)
                    {
                        throw new IOException($"Authentication failed: error code {credsResponse[1]}");
                    }

                    break;
                case ERROR:
                    throw new IOException($"No acceptable authentication methods");
                default:
                    throw new IOException($"Unknown auth METHOD response from server: {authResponse[1]}");
            }

            // The SOCKS request is formed as follows:
            // +----+-----+-------+------+----------+----------+
            // |VER | CMD |  RSV  | ATYP | DST.ADDR | DST.PORT |
            // +----+-----+-------+------+----------+----------+
            // | 1  |  1  | X'00' |  1   | Variable |    2     |
            // +----+-----+-------+------+----------+----------+
            var connection = new List<byte>() { SOCKS_5, CONNECT, EMPTY, DOMAIN };

            connection.Add((byte)destinationAddress.Length);
            connection.AddRange(Encoding.ASCII.GetBytes(destinationAddress));

            connection.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)destinationPort)));

            await stream.WriteAsync(connection.ToArray(), cancellationToken.Value);

            // The SOCKS request information is sent by the client as soon as it has
            // established a connection to the SOCKS server, and completed the
            // authentication negotiations.  The server evaluates the request, and
            // returns a reply formed as follows:
            // +-----+-----+-------+------+----------+----------+
            // | VER | REP | RSV   | ATYP | BND.ADDR | BND.PORT |
            // +-----+-----+-------+------+----------+----------+
            // | 1   | 1   | X'00' | 1    | Variable | 2        |
            // +-----+-----+-------+------+----------+----------+
            var connectionResponse = await ReadAsync(stream, 4, CancellationToken.None);

            if (connectionResponse[0] != SOCKS_5)
            {
                throw new IOException($"Invalid SOCKS version (expected: {SOCKS_5}, received: {authResponse[0]})");
            }
            if (connectionResponse[1] != EMPTY)
            {
                string msg = (connectionResponse[1]) switch
                {
                    0x01 => "General SOCKS server failure",
                    0x02 => "Connection not allowed by ruleset",
                    0x03 => "Network unreachable",
                    0x04 => "Host unreachable",
                    0x05 => "Connection refused",
                    0x06 => "TTL expired",
                    0x07 => "Command not supported",
                    0x08 => "Address type not supported",
                    _ => $"Unknown SOCKS error {connectionResponse[1]}",
                };

                throw new IOException($"SOCKS connection failed: {msg}");
            }

            string boundAddress;
            ushort boundPort;

            try
            {
                switch (connectionResponse[3])
                {
                    case IPV4:
                        var boundIPBytes = await ReadAsync(stream, 4, CancellationToken.None);
                        boundAddress = new IPAddress(BitConverter.ToUInt32(boundIPBytes, 0)).ToString();
                        break;
                    case DOMAIN:
                        var lengthBytes = await ReadAsync(stream, 1, CancellationToken.None);

                        if (lengthBytes[0] == ERROR)
                        {
                            throw new IOException("Invalid domain name");
                        }

                        var boundDomainBytes = await ReadAsync(stream, lengthBytes[0], CancellationToken.None);
                        boundAddress = Encoding.ASCII.GetString(boundDomainBytes);
                        break;
                    case IPV6:
                        var boundIPv6Bytes = await ReadAsync(stream, 16, CancellationToken.None);
                        boundAddress = new IPAddress(boundIPv6Bytes).ToString();
                        break;
                    default:
                        throw new IOException($"Unknown SOCKS Address type (expected: one of {IPV4}, {DOMAIN}, {IPV6}, received: {connectionResponse[3]})");
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"Invalid address response from server: {ex.Message}");
            }

            var boundPortBytes = await ReadAsync(stream, 2, CancellationToken.None);
            boundPort = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(boundPortBytes, 0));

            return (boundAddress, boundPort);
        }
    }
}
