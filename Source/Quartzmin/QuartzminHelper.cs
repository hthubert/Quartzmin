using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Quartzmin
{
    public static class QuartzminHelper
    {
        public const string MapDataWaitForExit = "wait_for_exit";
        public const string MapDataEnableLog = "enable_log";
        public const string MapDataFile = "file";
        public const string MapDataArgs = "args";
        public const string MapDataAutoFlush = "auto_flush";
        public static string LogPath;
        public static string UserPath;
        public static string PwdPath;

        static QuartzminHelper()
        {
            UserPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Replace('\\', '/');
            PwdPath = Path.GetDirectoryName(typeof(QuartzminHelper).Assembly.Location).Replace('\\', '/');
            LogPath = Path.Combine(PwdPath, "logs");
            if (!Directory.Exists(LogPath))
            {
                Directory.CreateDirectory(LogPath);
            }
        }

        public static string RelativeToAbs(string path)
        {
            return path.Replace("~", UserPath).Replace("`pwd`", PwdPath);
        }

        public static Stream GetLogStream(string fireInstanceId, bool @readonly = false)
        {
            var file = Path.Combine(LogPath, fireInstanceId + ".txt");
            if (@readonly)
            {
                if (File.Exists(file))
                {
                    return File.OpenRead(file);
                }
                return new MemoryStream();
            }
            return new FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        }

        public static List<string> LogTail(string fireInstanceId, int size = 1024) 
        {
            var lines = new List<string>();
            using (var reader = new StreamReader(GetLogStream(fireInstanceId, true)))
            {
                if (reader.BaseStream.Length > size)
                {
                    reader.BaseStream.Seek(-size, SeekOrigin.End);
                }

                reader.ReadLine();
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }
            return lines;
        }
    }
}
