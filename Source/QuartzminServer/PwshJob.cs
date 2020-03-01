using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using Skyline;

namespace QuartzminServer
{
    using static Helper;

    public class PwshJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            return Task.Run(() => {
                var pwsh = context.MergedJobDataMap.GetString("pwsh");
                if (!File.Exists(pwsh))
                {
                    throw new Exception("pwsh not found.");
                }
                var script = context.MergedJobDataMap.GetString("script");
                if (!File.Exists(script))
                {
                    throw new Exception("script not found.");
                }
                var args = context.MergedJobDataMap.GetString("args");
                var uselog = context.MergedJobDataMap.GetBoolean("uselog");
                Action<string> action = null;
                StreamWriter writer = null;
                if (uselog)
                {
                    writer = new StreamWriter(GetLogStream(context.FireInstanceId));
                    writer.AutoFlush = true;
                    action = (line) => writer.WriteLine(line);
                }
                var process = new ExternalCall(pwsh).Arguments($"").WinExecWithPipeAsync(action, action);
                writer.Close();
            }, context.CancellationToken);
        }
    }
}
