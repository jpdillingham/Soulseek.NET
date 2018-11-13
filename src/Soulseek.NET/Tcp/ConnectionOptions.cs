namespace Soulseek.NET.Tcp
{
    /// <summary>
    ///     Options for connections.
    /// </summary>
    public class ConnectionOptions
    {
        /// <summary>
        ///     Gets or sets the read and write buffer size for underlying TCP connections.
        /// </summary>
        public int BufferSize { get; set; } = 4096;

        /// <summary>
        ///     Gets or sets the connection timeout for client and peer TCP connections.
        /// </summary>
        public int ConnectTimeout { get; set; } = 5;

        /// <summary>
        ///     Gets or sets the read timeout for peer TCP connections. Once connected and after reading data, if a no additional
        ///     data is read within this threshold the connection will be forcibly disconnected.
        /// </summary>
        public int ReadTimeout { get; set; } = 5;
    }
}
