﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using System.Linq;
using System.Threading;

namespace Quartz.Plugins.RecentHistory.Impl
{
    class LiteDbExecutionHistoryStore : IExecutionHistoryStore
    {
        class SummaryInfo
        {
            public ObjectId Id = ObjectId.NewObjectId();
            public int TotalJobsExecuted;
            public int TotalJobsFailed;
        }

        private LiteDatabase _database;
        private ILiteCollection<ExecutionHistoryEntry> _histories;
        private ILiteCollection<SummaryInfo> _summaries;
        private SummaryInfo _info = new SummaryInfo();

        public string SchedulerName { get; set; }
        public string DataPath { get; set; }

        public void Initialize()
        {
            BsonMapper.Global.IncludeFields = true;
            BsonMapper.Global.Entity<ExecutionHistoryEntry>().Id(x => x.FireInstanceId);
            BsonMapper.Global.Entity<SummaryInfo>().Id(x => x.Id);

            _database = new LiteDatabase(DataPath);
            _histories = _database.GetCollection<ExecutionHistoryEntry>("histories");
            _histories.EnsureIndex(x => x.Job);
            _histories.EnsureIndex(x => x.Trigger);
            _histories.EnsureIndex(x => x.ActualFireTime);
            _summaries = _database.GetCollection<SummaryInfo>("summaries");
            if (_summaries.Count() > 0)
            {
                _info = _summaries.Query().First();
            }
        }
        public void Shutdown()
        {
            _summaries.Upsert(_info);
            _database.Dispose();
        }

        public Task<IEnumerable<ExecutionHistoryEntry>> FilterLast(int limit)
        {
            var result = _histories.Query()
                .OrderByDescending(y => y.ActualFireTime)
                .Limit(limit)
                .ToList();
            result.Reverse();
            return Task.FromResult((IEnumerable<ExecutionHistoryEntry>)result);
        }

        public Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOfEveryJob(int limitPerJob)
        {
            var result = new List<ExecutionHistoryEntry>();
            var jobs = _histories.Query().GroupBy("$.Job").Select("{job: Last(*.Job)}").ToArray();
            foreach (var item in jobs)
            {
                string job = item.Values.First();
                var items = _histories.Query()
                                    .Where(n => n.Job == job)
                                    .OrderByDescending(n => n.ActualFireTime)
                                    .Limit(limitPerJob)
                                    .ToList();
                items.Reverse();
                result.AddRange(items);

            }
            return Task.FromResult((IEnumerable<ExecutionHistoryEntry>)result);
        }

        public Task<IEnumerable<ExecutionHistoryEntry>> FilterLastOfEveryTrigger(int limitPerTrigger)
        {
            var result = new List<ExecutionHistoryEntry>();
            var jobs = _histories.Query().GroupBy("$.Trigger").Select("{trigger: Last(*.Trigger)}").ToArray();
            foreach (var item in jobs)
            {
                string trigger = item.Values.First();
                var items = _histories.Query()
                                    .Where(n => n.Trigger == trigger)
                                    .OrderByDescending(n => n.ActualFireTime)
                                    .Limit(limitPerTrigger)
                                    .ToList();
                items.Reverse();
                result.AddRange(items);
            }
            return Task.FromResult((IEnumerable<ExecutionHistoryEntry>)result);
        }

        public Task<ExecutionHistoryEntry> Get(string fireInstanceId)
        {
            var result = _histories.FindOne(x => x.FireInstanceId == fireInstanceId);
            return Task.FromResult(result);
        }

        public Task<int> GetTotalJobsExecuted()
        {
            return Task.FromResult(_info.TotalJobsExecuted);
        }

        public Task<int> GetTotalJobsFailed()
        {
            return Task.FromResult(_info.TotalJobsFailed);
        }

        public Task IncrementTotalJobsExecuted()
        {
            Interlocked.Increment(ref _info.TotalJobsExecuted);
            return Task.FromResult(0);
        }

        public Task IncrementTotalJobsFailed()
        {
            Interlocked.Increment(ref _info.TotalJobsFailed);
            return Task.FromResult(0);
        }

        public Task<IEnumerable<string>> Purge(int limit)
        {
            return Task.Run(() => {
                var set = new HashSet<string>(FilterLastOfEveryTrigger(limit).Result.Select(n => n.FireInstanceId));
                var deleted = _histories.FindAll().Where(n => !set.Contains(n.FireInstanceId)).Select(n => n.FireInstanceId);
                foreach (var item in deleted)
                {
                    _histories.Delete(item);
                }
                return deleted;
            });
        }

        public Task Save(ExecutionHistoryEntry entry)
        {
            _histories.Upsert(entry);
            return Task.FromResult(0);
        }

        public Task Delete(string fireInstanceId)
        {
            _histories.Delete(fireInstanceId);
            return Task.FromResult(0);
        }
    }
}
