using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using Quartzmin;
using Skyline;

namespace QuartzminServer
{
    using static QuartzminHelper;

    class DotnetJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            return Task.Run(() => {
                var dotnet = context.MergedJobDataMap.GetString("dotnet");
                if (!File.Exists(dotnet) && !ExistsOnPath(dotnet))
                {
                    throw new Exception("dotnet not found.");
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
                var process = new ExternalCall(dotnet).Arguments($"{file} {args}").WinExecWithPipeAsync(logger.StdOutput, logger.ErrOutput);
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
