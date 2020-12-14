using System;
using System.Threading.Tasks;
using Quartz;
using Quartz.Plugins.RecentHistory;
using Quartzmin;

namespace QuartzminServer
{
    using static QuartzminHelper;
    public class HistoryPurgeJob : IJob
    {
        internal static IExecutionHistoryStore HistoryStore { get; set; }

        public Task Execute(IJobExecutionContext context)
        {
            return Task.Run(() => {
                var logger = new JobLogger(context);

                var limit = context.MergedJobDataMap.GetInt("limit");
                if (limit == 0)
                {
                    return;
                }
                if (HistoryStore == null) 
                {
                    throw new Exception($"HistoryStore is null.");
                }
                var removed = HistoryStore.Purge(limit).Result;
                foreach (var item in removed)
                {
                    try
                    {
                        logger.Info($"remove log {item}");
                        DeleteLogFile(item);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                    }                    
                }
            });
        }
    }
}
