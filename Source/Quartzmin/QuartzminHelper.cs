using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Quartzmin
{
    public static class QuartzminHelper
    {
        public static string UserPath;
        public static string PwdPath;

        static QuartzminHelper()
        {
            UserPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Replace('\\', '/');
            PwdPath = Path.GetDirectoryName(typeof(QuartzminHelper).Assembly.Location).Replace('\\', '/');
        }

        public static string RelativeToAbs(string path)
        {
            return path.Replace("~", UserPath).Replace("`pwd`", PwdPath);
        }

        public static Stream GetLogStream(string fireInstanceId, bool @readonly = false)
        {
            var file = Path.Combine(PwdPath, "logs", fireInstanceId + ".txt");
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
