using System;
using System.IO;
using Quartz;
using Quartzmin;

namespace QuartzminServer
{
    using static QuartzminHelper;

    public class DotnetJob : ProgramJob
    {
        protected override string GetProgramFile(IJobExecutionContext context)
        {
            var dotnet = context.MergedJobDataMap.GetString("dotnet");
            if (!File.Exists(dotnet) && !ExistsOnPath(dotnet))
            {
                throw new Exception("dotnet not found.");
            }
            return dotnet;
        }

        protected override string GetCommandArgs(IJobExecutionContext context)
        {
            var file = context.MergedJobDataMap.GetString(JobExecutionFile);
            if (!File.Exists(file))
            {
                throw new Exception($"{file} not found.");
            }
            var args = context.MergedJobDataMap.GetString(JobExecutionArgs);
            return $"{file} {args}";
        }
    }
}
