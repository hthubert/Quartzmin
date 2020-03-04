using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;
using Quartzmin;
using Quartzmin.SelfHost;

namespace QuartzminServer
{
    using static QuartzminHelper;
    internal class Program
    {
        private static NameValueCollection LoadConfig()
        {
            var props = new NameValueCollection((NameValueCollection)ConfigurationManager.GetSection("quartz"));
            ParseRelative("quartz.plugin.recentHistory.dataPath");
            ParseRelative("quartz.dataSource.default.connectionString");
            return props;

            void ParseRelative(string key)
            {
                props[key] = RelativeToAbs(props[key]);
            }
        }

        private static async Task<IScheduler> CreateScheduler()
        {
            var factory = new StdSchedulerFactory(LoadConfig());
            var scheduler = await factory.GetScheduler(CancellationToken.None);
            var plugin = scheduler.Context.GetQuartzminPlugin();
            plugin.JobTypes.Add(typeof(TushareJob));
            plugin.JobTypes.Add(typeof(PwshJob));
            plugin.JobTypes.Add(typeof(DotnetJob));
            await scheduler.Start();
            return scheduler;
        }

        private static void Main(string[] args)
        {
            var scheduler = CreateScheduler().Result;
            scheduler.Start();
            while (!scheduler.IsShutdown)
                Thread.Sleep(500);
        }
    }
}
