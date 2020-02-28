using System;
using System.Collections.Generic;
using System.Text;
using Quartz;

namespace Quartzmin.SelfHost
{
    public static class Extensions
    {
        public static void SetQuartzminPlugin(this SchedulerContext context, QuartzminPlugin plugin)
        {
            context.Put(typeof(QuartzminPlugin).FullName, plugin);
        }

        public static QuartzminPlugin GetQuartzminPlugin(this SchedulerContext context)
        {
            return (QuartzminPlugin)context.Get(typeof(QuartzminPlugin).FullName);
        }
    }
}
