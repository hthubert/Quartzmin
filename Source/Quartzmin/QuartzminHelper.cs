using System;
using System.Collections.Generic;
using System.IO;
using Quartz;

namespace Quartzmin
{
    public static class QuartzminHelper
    {
        public const string JobWaitForExit = "wait_for_exit";
        public const string JobEnableLog = "enable_log";
        public const string JobEnableConsoleLog = "enable_console_log";
        public const string JobExecutionFile = "file";
        public const string JobExecutionArgs = "args";
        public const string LogAutoFlush = "log_auto_flush";
        public const string LogFlushTimeout = "log_flush_timeout";
        public const string LogFilePlaceholder = "#logfile#";
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

        public static bool TryGetValue<T>(this JobDataMap map, string key, out T value, T defaultValue = default) 
        {
            var exist = map.TryGetValue(key, out var data);
            if (exist)
            {
                value = (T)data;
            }
            else{
                value = defaultValue;
            }
            return exist;
        }

        public static string RelativeToAbs(string path)
        {
            return path.Replace("~", UserPath).Replace("`pwd`", PwdPath);
        }

        public static void DeleteLogFile(string fireInstanceId)
        {
            var path = GetLogPath(fireInstanceId);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public static Stream GetLogStream(string fireInstanceId, bool @readonly = false)
        {
            string path = GetLogPath(fireInstanceId);
            if (!@readonly)
                return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            if (File.Exists(path))
            {
                return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            return new MemoryStream();
        }

        public static string GetLogPath(string fireInstanceId)
        {
            return Path.Combine(LogPath, fireInstanceId + ".txt");
        }

        public static List<string> LogTail(string fireInstanceId, int size = 1024)
        {
            var lines = new List<string>();
            using (var reader = new StreamReader(GetLogStream(fireInstanceId, true)))
            {
                var skipFirst = false;
                if (reader.BaseStream.Length > size)
                {
                    skipFirst = true;
                    reader.BaseStream.Seek(-size, SeekOrigin.End);
                }

                if (skipFirst)
                {
                    reader.ReadLine();
                }
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }
            return lines;
        }

        public static bool ExistsOnPath(string fileName)
        {
            return GetFullPath(fileName) != null;
        }

        public static string GetFullPath(string fileName)
        {
            if (File.Exists(fileName))
                return Path.GetFullPath(fileName);

            var values = Environment.GetEnvironmentVariable("PATH");
            foreach (var path in values.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            return null;
        }
    }
}
