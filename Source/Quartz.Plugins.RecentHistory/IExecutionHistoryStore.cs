﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Quartz.Plugins.RecentHistory
{
    [Serializable]
    public class ExecutionHistoryEntry
    {
        public string FireInstanceId { get; set; }
        public string SchedulerInstanceId { get; set; }
        public string SchedulerName { get; set; }
        public string Job { get; set; }
        public string Trigger { get; set; }
        public DateTime? ScheduledFireTime { get; set; }
        public DateTime ActualFireTime { get; set; }
        public bool Recovering { get; set; }
        public bool Vetoed { get; set; }
        public DateTime? FinishedTime { get; set; }
        public string ExceptionMessage { get; set; }
        public string ExceptionDetail { get; set; }
        public string LogFile { get; set; }
    }

    public interface IExecutionHistoryStore
    {
        string SchedulerName { get; set; }
        string DataPath { get; set; }

        void Initialize();
        void Shutdown();

        Task<ExecutionHistoryEntry> Get(string fireInstanceId);
        Task Save(ExecutionHistoryEntry entry);
        Task<IEnumerable<string>> Purge();

        Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOfEveryJob(int limitPerJob);
        Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOfEveryTrigger(int limitPerTrigger);
        Task<IEnumerable<ExecutionHistoryEntry>> FilterLast(int limit);

        Task<int> GetTotalJobsExecuted();
        Task<int> GetTotalJobsFailed();

        Task IncrementTotalJobsExecuted();
        Task IncrementTotalJobsFailed();
    }
}
