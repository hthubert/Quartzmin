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
                if (!File.Exists(pwsh) && !ExistsOnPath(pwsh))
                {
                    throw new Exception("pwsh not found.");
                }
                var file = context.MergedJobDataMap.GetString(JobExecutionFile);
                if (!File.Exists(file))
                {
                    throw new Exception($"{file} not found.");
                }
                var args = context.MergedJobDataMap.GetString(JobExecutionArgs);
                var logger = new JobLogger(context);
                if (logger.EnableLog && !logger.ConsoleLog)
                {
                    args = args.Replace(LogFilePlaceholder, GetLogPath(context.FireInstanceId));
                }
                var process = new ExternalCall(pwsh).Arguments($"{file} {args}").WinExecWithPipeAsync(logger.StdOutput, logger.ErrOutput);
                context.MergedJobDataMap.TryGetValue(JobEnableLog, out var waitForExit, true);
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
                }
                logger.Dispose();
                if (process.HasExited && process.ExitCode != 0)
                {
                    throw new Exception($"exit code {process.ExitCode}");
                }
            }, context.CancellationToken);
        }
    }
}
