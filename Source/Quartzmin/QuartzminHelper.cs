using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Quartzmin
{
    public static class QuartzminHelper
    {
        public const string JobWaitForExit = "wait_for_exit";
        public const string JobEnableLog = "enable_log";
        public const string JobExecutionFile = "file";
        public const string JobExecutionArgs = "args";
        public const string LogAutoFlush = "log_auto_flush";
        public const string LogFlushTimeout = "log_flush_timeout";
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
            var path = Path.Combine(LogPath, fireInstanceId + ".txt");
            if (!@readonly)
                return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            if (File.Exists(path))
            {
                return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            return new MemoryStream();
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
