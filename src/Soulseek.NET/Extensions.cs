namespace Soulseek.NET
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;

    public static class Extensions
    {
        public static string ToHexString(this IEnumerable<byte> bytes)
        {
            StringBuilder result = new StringBuilder(bytes.Count() * 2);
            string hexAlphabet = "0123456789ABCDEF";

            foreach (byte B in bytes)
            {
                result.Append(hexAlphabet[(int)(B >> 4)]);
                result.Append(hexAlphabet[(int)(B & 0xF)]);
            }

            return result.ToString();
        }

        public static IEnumerable<byte> HexStringToBytes(this string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16));
        }

        public static string ToMD5Hash(this string str)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(str);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return Encoding.ASCII.GetString(hashBytes);
            }
        }
    }
}
