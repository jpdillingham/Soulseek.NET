namespace WebAPI.DTO
{
    using System.Net;

    public class UserAddress
    {
        public IPAddress IPAddress { get; set; }
        public int Port { get; set; }
    }
}
