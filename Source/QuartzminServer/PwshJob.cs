using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Quartz;
using Quartzmin;
using Skyline;

namespace QuartzminServer
{
    using static QuartzminHelper;

    public class PwshJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            return Task.Run(() => {
                var pwsh = context.MergedJobDataMap.GetString("pwsh");
                var waitForExit = context.MergedJobDataMap.GetBoolean("wait_for_exit");

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
                var uselog = context.MergedJobDataMap.GetBoolean("use_log");
                Action<string> action = null;
                StreamWriter writer = null;
                if (uselog && waitForExit)
                {
                    writer = new StreamWriter(GetLogStream(context.FireInstanceId));
                    writer.AutoFlush = context.MergedJobDataMap.GetBoolean("auto_flush");
                    action = (line) => writer.WriteLine(line);
                }
                var process = new ExternalCall(pwsh).Arguments($"").WinExecWithPipeAsync(action, action);
                if (waitForExit)
                {
                    var ct = context.CancellationToken;
                    while (!process.WaitForExit(10))
                    {
                        if (ct.IsCancellationRequested)
                        {
                            try
                            {
                                process.Kill(true);
                            }
                            catch
                            {
                            }
                            break;
                        }
                    }
                    writer?.Close();
                }
            }, context.CancellationToken);
        }
    }
}
