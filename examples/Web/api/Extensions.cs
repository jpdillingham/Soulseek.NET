namespace WebAPI
{
    using System.IO;

    public static class Extensions
    {
        public static string ToLocalOSPath(this string path)
        {
            return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
