using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using QuantBox;
using Quartz;
using Quartz.Impl.Calendar;

namespace QuartzminServer
{
    [DisallowConcurrentExecution]
    public class TushareJob : IJob
    {
        private const string CalendarName = "交易日历";

        private static async Task<HolidayCalendar> GetCalendar(IScheduler scheduler)
        {
            var names = await scheduler.GetCalendarNames();
            if (names.Contains(CalendarName))
            {
                return (HolidayCalendar)await scheduler.GetCalendar(CalendarName);
            }
            var cal = new HolidayCalendar();
            cal.Description = "中国金融市场的交易日历";
            cal.TimeZone = TimeZoneInfo.Local;
            return cal;
        }

        private static async Task UpdateCal(IJobExecutionContext context, TusharePro tushare)
        {
            var logger = new JobLogger(context);
            var (table, error) = await tushare.GetTradeCalendarAsync(DateTime.Today, DateTime.Today.AddMonths(6));
            if (error != null)
            {
                throw new Exception(error);
            }

            var dates = new List<DateTime>();
            foreach (var row in table.Rows)
            {
                if (row.Get("is_open") == "1")
                {
                    continue;
                }
                dates.Add(TusharePro.ToDateTime(row.Get("cal_date")));
            }

            var updated = false;
            var cal = await GetCalendar(context.Scheduler);
            var exists = cal.ExcludedDates.ToList();
            var last = exists.LastOrDefault();
            if (cal.ExcludedDates.Count == 0 || (last - DateTime.Today).Days < 7)
            {
                cal.ExcludedDates.ToList().ForEach(n => cal.RemoveExcludedDate(n));
                dates.ForEach(n => cal.AddExcludedDate(n));
                updated = true;
            }
            else
            {
                foreach (var date in dates)
                {
                    if (date > last)
                    {
                        break;
                    }

                    if (!exists.Contains(date))
                    {
                        updated = true;
                        cal.AddExcludedDate(date);
                    }
                }
            }

            logger.Dispose();
            if (updated)
            {
                await context.Scheduler.AddCalendar(CalendarName, cal, true, true);    
            }
        }
        public async Task Execute(IJobExecutionContext context)
        {
            var token = context.MergedJobDataMap.GetString("tushare_token");
            var tushare = new TusharePro(token, 10);

            var action = context.MergedJobDataMap.GetString("tushare_action");
            switch (action)
            {
                case "update_cal":
                    await UpdateCal(context, tushare);
                    break;
                default:
                    throw new Exception($" {action}");
            }
        }
    }
}
