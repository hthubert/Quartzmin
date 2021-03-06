﻿using System;
using System.IO;
using Quartz;
using Quartzmin;

namespace QuartzminServer
{
    using static QuartzminHelper;

    public class PwshJob : ProgramJob
    {
        protected override string GetProgramFile(IJobExecutionContext context)
        {
            var pwsh = context.MergedJobDataMap.GetString("pwsh");
            if (!File.Exists(pwsh) && !ExistsOnPath(pwsh))
            {
                throw new Exception("pwsh not found.");
            }
            return pwsh;
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
