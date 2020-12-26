using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
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

        private static async Task UpdateCalYear(IJobExecutionContext context)
        {
            var logger = new JobLogger(context);
            var url = context.MergedJobDataMap.GetString("cal_url");
            var handler = new HttpClientHandler {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            var content = await client.GetStringAsync(url);
            var list = JsonConvert.DeserializeObject<string[]>(content);
            var tradingDays = new HashSet<DateTime>();
            foreach (var date in list)
            {
                tradingDays.Add(TusharePro.ToDateTime(date));
            }
            var year = context.MergedJobDataMap.GetIntValue("cal_year");
            var holidays =new List<DateTime>();
            for (var date = new DateTime(year, 1, 1); date <= new DateTime(year, 12, 31); date = date.AddDays(1))
            {
                if (tradingDays.Contains(date))
                {
                    continue;
                }
                holidays.Add(date);
                logger.Info($"添加节假日:{date}");
            }

            await UpdatedCal(context, holidays);
            logger.Dispose();
        }

        private static async Task UpdateCal(IJobExecutionContext context, TusharePro tushare)
        {
            var logger = new JobLogger(context);
            var (table, error) = await tushare.GetTradeCalendarAsync(DateTime.Today, DateTime.Today.AddMonths(6));
            if (error != null)
            {
                throw new Exception(error);
            }

            var holidays = new List<DateTime>();
            foreach (var row in table.Rows)
            {
                if (row.Get("is_open") == "1")
                {
                    continue;
                }
                holidays.Add(TusharePro.ToDateTime(row.Get("cal_date")));
            }
            await UpdatedCal(context, holidays);
            logger.Dispose();
        }

        private static async Task UpdatedCal(IJobExecutionContext context, List<DateTime> dates)
        {
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
                case "update_cal_year":
                    await UpdateCalYear(context);
                    break;
                default:
                    throw new Exception($" {action}");
            }
        }
    }
}
