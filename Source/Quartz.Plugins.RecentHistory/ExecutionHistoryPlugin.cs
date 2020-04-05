using Quartz.Impl.Matchers;
using Quartz.Spi;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Quartz.Plugins.RecentHistory
{
    public class ExecutionHistoryPlugin : ISchedulerPlugin, IJobListener
    {
        private IScheduler _scheduler;
        private IExecutionHistoryStore _store;

        public string Name { get; set; }
        public string DataPath { get; set; }
        public Type StoreType { get; set; }

        public Func<IJobExecutionContext, bool> EnableLogGetter { get; set; }

        public Task Initialize(string pluginName, IScheduler scheduler, CancellationToken cancellationToken = default)
        {            
            Name = pluginName;
            _scheduler = scheduler;
            _scheduler.ListenerManager.AddJobListener(this, EverythingMatcher<JobKey>.AllJobs());
            _scheduler.Context.SetExecutionHistoryPlugin(this);
            return Task.FromResult(0);
        }

        public async Task Start(CancellationToken cancellationToken = default)
        {
            _store = _scheduler.Context.GetExecutionHistoryStore();

            if (_store == null)
            {
                if (StoreType != null)
                    _store = (IExecutionHistoryStore)Activator.CreateInstance(StoreType);

                if (_store == null)
                    throw new Exception(nameof(StoreType) + " is not set.");
                
                _scheduler.Context.SetExecutionHistoryStore(_store);
            }

            _store.SchedulerName = _scheduler.SchedulerName;
            _store.DataPath = DataPath;
            _store.Initialize();

            await Task.FromResult(0);
        }

        public Task Shutdown(CancellationToken cancellationToken = default)
        {
            _store.Shutdown();
            return Task.FromResult(0);
        }

        public async Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            var entry = new ExecutionHistoryEntry
            {
                FireInstanceId = context.FireInstanceId,
                SchedulerInstanceId = context.Scheduler.SchedulerInstanceId,
                SchedulerName = context.Scheduler.SchedulerName,
                ActualFireTime = context.FireTimeUtc.LocalDateTime,
                ScheduledFireTime = context.ScheduledFireTimeUtc?.LocalDateTime,
                Recovering = context.Recovering,
                Job = context.JobDetail.Key.ToString(),
                Trigger = context.Trigger.Key.ToString(),
                EnableLog = EnableLogGetter(context)
            };
            await _store.Save(entry);
        }

        public async Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken = default)
        {
            var entry = await _store.Get(context.FireInstanceId);
            if (entry != null)
            {
                entry.FinishedTime = DateTime.Now;
                var exception = jobException?.GetBaseException();
                if (exception != null)
                {
                    entry.ExceptionMessage = exception.Message;
                    entry.ExceptionDetail = exception.ToString();
                }                
                await _store.Save(entry);
            }
            if (jobException == null)
                await _store.IncrementTotalJobsExecuted();
            else
                await _store.IncrementTotalJobsFailed();
        }

        public async Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            var entry = await _store.Get(context.FireInstanceId);
            if (entry != null)
            {
                entry.Vetoed = true;
                await _store.Save(entry);
            }
        }
    }
}
