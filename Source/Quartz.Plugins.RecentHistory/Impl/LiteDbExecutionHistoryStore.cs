using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Quartz.Plugins.RecentHistory.Impl
{
    class LiteDbExecutionHistoryStore : IExecutionHistoryStore
    {
        public string SchedulerName { get; set; }

        public Task<IEnumerable<ExecutionHistoryEntry>> FilterLast(int limit)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOfEveryJob(int limitPerJob)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOfEveryTrigger(int limitPerTrigger)
        {
            throw new NotImplementedException();
        }

        public Task<ExecutionHistoryEntry> Get(string fireInstanceId)
        {
            throw new NotImplementedException();
        }

        public Task<int> GetTotalJobsExecuted()
        {
            throw new NotImplementedException();
        }

        public Task<int> GetTotalJobsFailed()
        {
            throw new NotImplementedException();
        }

        public Task IncrementTotalJobsExecuted()
        {
            throw new NotImplementedException();
        }

        public Task IncrementTotalJobsFailed()
        {
            throw new NotImplementedException();
        }

        public Task Purge()
        {
            throw new NotImplementedException();
        }

        public Task Save(ExecutionHistoryEntry entry)
        {
            throw new NotImplementedException();
        }
    }
}
