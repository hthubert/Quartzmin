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

    public sealed class JobLogger: IDisposable
    {
        private readonly StreamWriter _writer;
        private readonly ActionBlock<string> _action;

        public JobLogger(IJobExecutionContext context) 
        {
            _writer = new StreamWriter(GetLogStream(context.FireInstanceId));
            _writer.AutoFlush = context.MergedJobDataMap.GetBoolean(MapDataAutoFlush);
            _action = new ActionBlock<string>(ProcessLog);
            StdOutput = line => _action.Post(line);
            ErrOutput = line => _action.Post(line);
        }

        private void ProcessLog(string line)
        {
            _writer.WriteLine(line);
        }
        
        public void Info(string msg)
        {
            _action.Post($"{DateTime.Now: yyyy-MM-dd HH\\:mm\\:ss} {msg}");
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
