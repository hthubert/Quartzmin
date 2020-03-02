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
                var waitForExit = context.MergedJobDataMap.GetBoolean(MapDataWaitForExit);

                if (!File.Exists(pwsh))
                {
                    throw new Exception("pwsh not found.");
                }
                var file = context.MergedJobDataMap.GetString(MapDataFile);
                if (!File.Exists(file))
                {
                    throw new Exception($"{file} not found.");
                }
                var args = context.MergedJobDataMap.GetString(MapDataArgs);
                var uselog = context.MergedJobDataMap.GetBoolean(MapDataUseLog);
                Action<string> action = null;
                StreamWriter writer = null;
                if (uselog && waitForExit)
                {
                    writer = new StreamWriter(GetLogStream(context.FireInstanceId));
                    writer.AutoFlush = context.MergedJobDataMap.GetBoolean(MapDataAutoFlush);
                    action = (line) => writer.WriteLine(line);
                }
                var process = new ExternalCall(pwsh).Arguments($"{file} {args}").WinExecWithPipeAsync(action, action);
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
