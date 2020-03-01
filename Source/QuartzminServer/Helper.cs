using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace QuartzminServer
{
    internal static class Helper
    {
        public static string UserPath;
        public static string PwdPath;

        static Helper()
        {
            UserPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Replace('\\', '/');
            PwdPath = Path.GetDirectoryName(typeof(Helper).Assembly.Location).Replace('\\', '/');
        }

        public static string RelativeToAbs(string path)
        {
            return path.Replace("~", UserPath).Replace("`pwd`", PwdPath);
        }

        public static Stream GetLogStream(string id, bool @readonly = false)
        {
            var file = Path.Combine(PwdPath, "logs", id + ".txt");
            if (@readonly)
            {
                if (File.Exists(file))
                {
                    return File.OpenRead(file);
                }
                return null;
            }
            return new FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        }
    }
}
