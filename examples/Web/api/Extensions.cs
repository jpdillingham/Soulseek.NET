namespace WebAPI
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;

    /// <summary>
    ///     Extensions.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        ///     Converts the given path to the local format (normalizes path separators).
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string ToLocalOSPath(this string path)
        {
            return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }

        /// <summary>
        ///     Returns the directory from the given path, regardless of separator format.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string DirectoryName(this string path)
        {
            var separator = path.Contains('\\') ? '\\' : '/';
            var parts = path.Split(separator);
            return string.Join(separator, parts.Take(parts.Length - 1));
        }

        /// <summary>
        ///     Generates a random 32 byte array and returns it as a base 64 string.
        /// </summary>
        /// <param name="rng"></param>
        /// <returns></returns>
        public static string GenerateRandomJwtSigningKey(this RNGCryptoServiceProvider rng)
        {
            byte[] bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
