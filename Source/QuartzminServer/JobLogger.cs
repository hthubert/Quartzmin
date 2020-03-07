using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks.Dataflow;
using Quartz;
using Quartzmin;

namespace QuartzminServer
{
    using static QuartzminHelper;

    public sealed class JobLogger : IDisposable
    {
        private readonly StreamWriter _writer;
        private readonly ActionBlock<string> _action;
        private readonly TimeSpan _flushTimeout;
        private DateTime _lastFlushTime = DateTime.MinValue;

        public JobLogger(IJobExecutionContext context)
        {
            _writer = new StreamWriter(GetLogStream(context.FireInstanceId));
            _writer.AutoFlush = context.MergedJobDataMap.GetBoolean(LogAutoFlush);
            _action = new ActionBlock<string>(ProcessLog);
            _flushTimeout = TimeSpan.FromSeconds(context.MergedJobDataMap.GetIntValue(LogFlushTimeout));
            StdOutput = line => _action.Post(line);
            ErrOutput = line => _action.Post(line);
        }

        private void ProcessLog(string line)
        {
            _writer.WriteLine(line);

            if (_writer.AutoFlush || _flushTimeout == TimeSpan.Zero)
                return;
            if (DateTime.Now - _lastFlushTime <= _flushTimeout)
                return;
            _lastFlushTime = DateTime.Now;
            _writer.Flush();
        }

        public void WriteLog(string level, string msg)
        {
            _action.Post($"{DateTime.Now: yyyy-MM-dd HH\\:mm\\:ss} [{level}] {msg}");
        }

        internal void Error(Exception ex)
        {
            WriteLog("Error", ex.Message);
            _action.Post(ex.StackTrace);
        }

        public void Info(string msg)
        {
            WriteLog("Info", msg);
        }

        public Action<string> StdOutput { get; }
        public Action<string> ErrOutput { get; }

        public void Dispose()
        {
            _action.Complete();
            _action.Completion.Wait();
            _writer.Close();
        }
    }
}
