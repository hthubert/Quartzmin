using System.Collections.Generic;
using System.Threading.Tasks;

namespace Quartz.Plugins.RecentHistory
{
    public interface IExecutionHistoryStore
    {
        string SchedulerName { get; set; }
        string DataPath { get; set; }

        void Initialize();
        void Shutdown();

        Task<ExecutionHistoryEntry> Get(string fireInstanceId);
        Task Save(ExecutionHistoryEntry entry);
        Task Delete(string fireInstanceId);
        Task<IEnumerable<string>> Purge(int limit);

        Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOfEveryJob(int limitPerJob);
        Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOfEveryTrigger(int limitPerTrigger);
        Task<IEnumerable<ExecutionHistoryEntry>> FilterLast(int limit);

        Task<int> GetTotalJobsExecuted();
        Task<int> GetTotalJobsFailed();

        Task IncrementTotalJobsExecuted();
        Task IncrementTotalJobsFailed();
    }
}
