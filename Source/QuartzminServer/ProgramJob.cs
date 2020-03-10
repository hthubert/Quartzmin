using System;
using System.IO;
using System.Threading.Tasks;
using Quartz;
using Quartzmin;
using Skyline;

namespace QuartzminServer
{
    using static QuartzminHelper;

    public class ProgramJob : IJob
    {
        protected virtual string GetProgramFile(IJobExecutionContext context)
        {
            var file = context.MergedJobDataMap.GetString(JobExecutionFile);
            if (!File.Exists(file))
            {
                throw new Exception($"{file} not found.");
            }
            return file;
        }

        protected virtual string GetCommandArgs(IJobExecutionContext context)
        {
            return context.MergedJobDataMap.GetString(JobExecutionArgs) ?? string.Empty;
        }

        public Task Execute(IJobExecutionContext context)
        {
            return Task.Run(() => {
                var file = GetProgramFile(context);
                context.MergedJobDataMap.TryGetValue(JobExecutionDir, out var dir, Path.GetDirectoryName(file));

                var args = GetCommandArgs(context);
                var logger = new JobLogger(context);
                if (logger.EnableLog && !logger.ConsoleLog)
                {
                    args = args.Replace(LogFilePlaceholder, GetLogPath(context.FireInstanceId));
                }
                var process = new ExternalCall(file).Arguments(args).SetWorkingDir(dir).WinExecWithPipeAsync(logger.StdOutput, logger.ErrOutput);
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
