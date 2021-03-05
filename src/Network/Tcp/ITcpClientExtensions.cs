namespace Soulseek.Network.Tcp
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class ITcpClientExtensions
    {
        internal static async Task<(string ProxyAddress, int ProxyPort)> ConnectThroughProxyAsync(
            this ITcpClient client,
            IPAddress proxyAddress,
            int proxyPort,
            IPAddress destinationAddress,
            int destinationPort,
            string username = null,
            string password = null,
            CancellationToken? cancellationToken = null)
        {
            if (proxyAddress == default)
            {
                throw new ArgumentNullException(nameof(proxyAddress));
            }

            if (proxyPort < IPEndPoint.MinPort || proxyPort > IPEndPoint.MaxPort)
            {
                throw new ArgumentOutOfRangeException(nameof(proxyPort), proxyPort, $"Proxy port must be within {IPEndPoint.MinPort} and {IPEndPoint.MaxPort}, inclusive");
            }

            if (destinationAddress == default)
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

            var usingCredentials = !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password);
            var buffer = new byte[1024];

            async Task<byte[]> ReadAsync(INetworkStream stream, int length, CancellationToken cancellationToken)
            {
                var bytesRead = await stream.ReadAsync(buffer, 0, length, cancellationToken).ConfigureAwait(false);
                return buffer.AsSpan().Slice(0, bytesRead).ToArray();
            }

            static Task WriteAsync(INetworkStream stream, byte[] data, CancellationToken cancellationToken)
                => stream.WriteAsync(data, 0, data.Length, cancellationToken);

            await client.ConnectAsync(proxyAddress, proxyPort).ConfigureAwait(false);
            var stream = client.GetStream();

            byte[] auth;

            if (usingCredentials)
            {
                auth = new byte[] { SOCKS_5, 0x02, AUTH_ANONYMOUS, AUTH_USERNAME };
            }
            else
            {
                auth = new byte[] { SOCKS_5, 0x01, AUTH_ANONYMOUS };
            }

            await WriteAsync(stream, auth, cancellationToken.Value).ConfigureAwait(false);

            var authResponse = await ReadAsync(stream, 2, cancellationToken.Value).ConfigureAwait(false);

            if (authResponse[0] != SOCKS_5)
            {
                throw new ConnectionProxyException($"Invalid SOCKS version (expected: {SOCKS_5}, received: {authResponse[0]})");
            }

            switch (authResponse[1])
            {
                case AUTH_ANONYMOUS:
                    break;
                case AUTH_USERNAME:
                    if (!usingCredentials)
                    {
                        throw new ConnectionProxyException("Server requests authorization but none was provided");
                    }

                    var creds = new List<byte>() { AUTH_VERSION };

                    creds.Add((byte)username.Length);
                    creds.AddRange(Encoding.ASCII.GetBytes(username));

                    creds.Add((byte)password.Length);
                    creds.AddRange(Encoding.ASCII.GetBytes(password));

                    await WriteAsync(stream, creds.ToArray(), cancellationToken.Value).ConfigureAwait(false);

                    var credsResponse = await ReadAsync(stream, 2, cancellationToken.Value).ConfigureAwait(false);

                    if (credsResponse.Length != 2)
                    {
                        throw new ConnectionProxyException("Abnormal authentication response from server");
                    }

                    if (credsResponse[0] != AUTH_VERSION)
                    {
                        throw new ConnectionProxyException($"Invalid authentication subnegotiation version (expected: {AUTH_VERSION}, received: {credsResponse[0]})");
                    }

                    if (credsResponse[1] != EMPTY)
                    {
                        throw new ConnectionProxyException($"Authentication failed: error code {credsResponse[1]}");
                    }

                    break;
                case ERROR:
                    throw new ConnectionProxyException($"Server does not support the specified authentication method(s)");
                default:
                    throw new ConnectionProxyException($"Unknown auth METHOD response from server: {authResponse[1]}");
            }

            var connection = new List<byte>() { SOCKS_5, CONNECT, EMPTY, DOMAIN };

            connection.Add((byte)destinationAddress.ToString().Length);
            connection.AddRange(Encoding.ASCII.GetBytes(destinationAddress.ToString()));

            connection.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)destinationPort)));

            await WriteAsync(stream, connection.ToArray(), cancellationToken.Value).ConfigureAwait(false);

            var connectionResponse = await ReadAsync(stream, 4, CancellationToken.None).ConfigureAwait(false);

            if (connectionResponse[0] != SOCKS_5)
            {
                throw new ConnectionProxyException($"Invalid SOCKS version (expected: {SOCKS_5}, received: {authResponse[0]})");
            }

            if (connectionResponse[1] != EMPTY)
            {
                string msg = connectionResponse[1] switch
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

                throw new ConnectionProxyException($"SOCKS connection failed: {msg}");
            }

            string boundAddress;
            ushort boundPort;

            try
            {
                switch (connectionResponse[3])
                {
                    case IPV4:
                        var boundIPBytes = await ReadAsync(stream, 4, CancellationToken.None).ConfigureAwait(false);
                        boundAddress = new IPAddress(BitConverter.ToUInt32(boundIPBytes, 0)).ToString();
                        break;
                    case DOMAIN:
                        var lengthBytes = await ReadAsync(stream, 1, CancellationToken.None).ConfigureAwait(false);

                        if (lengthBytes[0] == ERROR)
                        {
                            throw new ConnectionProxyException("Invalid domain name");
                        }

                        var boundDomainBytes = await ReadAsync(stream, lengthBytes[0], CancellationToken.None).ConfigureAwait(false);
                        boundAddress = Encoding.ASCII.GetString(boundDomainBytes);
                        break;
                    case IPV6:
                        var boundIPv6Bytes = await ReadAsync(stream, 16, CancellationToken.None).ConfigureAwait(false);
                        boundAddress = new IPAddress(boundIPv6Bytes).ToString();
                        break;
                    default:
                        throw new ConnectionProxyException($"Unknown SOCKS Address type (expected: one of {IPV4}, {DOMAIN}, {IPV6}, received: {connectionResponse[3]})");
                }
            }
            catch (Exception ex)
            {
                throw new ConnectionProxyException($"Invalid address response from server: {ex.Message}");
            }

            var boundPortBytes = await ReadAsync(stream, 2, CancellationToken.None).ConfigureAwait(false);
            boundPort = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(boundPortBytes, 0));

            return (boundAddress, boundPort);
        }
    }
}
