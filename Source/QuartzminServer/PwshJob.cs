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
                var enableLog = context.MergedJobDataMap.GetBoolean(MapDataEnableLog);
                JobLogger logger = null;
                if (enableLog && waitForExit)
                {
                    logger = new JobLogger(context);
                }
                var process = new ExternalCall(pwsh).Arguments($"{file} {args}").WinExecWithPipeAsync(logger?.StdOutput, logger?.ErrOutput);
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
                    logger?.Dispose();
                }
            }, context.CancellationToken);
        }
    }
}
