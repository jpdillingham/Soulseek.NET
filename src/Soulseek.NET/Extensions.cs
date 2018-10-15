namespace Soulseek.NET
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public static class Extensions
    {
        public static string ToHexString(this IEnumerable<byte> bytes)
        {
            StringBuilder Result = new StringBuilder(bytes.Count() * 2);
            string HexAlphabet = "0123456789ABCDEF";

            foreach (byte B in bytes)
            {
                Result.Append(HexAlphabet[(int)(B >> 4)]);
                Result.Append(HexAlphabet[(int)(B & 0xF)]);
            }

            return Result.ToString();
        }
    }
}
