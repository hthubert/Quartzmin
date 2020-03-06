﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private static async Task UpdateCal(IJobExecutionContext context, TusharePro tushare) 
        {
            var logger = new JobLogger(context);
            var (table, error) = await tushare.GetTradeCalendarAsync(DateTime.Today, DateTime.Today.AddMonths(6));
            if (error != null)
            {
                throw new Exception(error);
            }

            var cal = new HolidayCalendar();
            cal.Description = "中国金融市场的交易日历";
            cal.TimeZone = TimeZoneInfo.Local;
            foreach (var row in table.Rows)
            {
                if (row.Get("is_open") == "1")
                {
                }
                else
                {
                    cal.AddExcludedDate(TusharePro.ToDateTime(row.Get("cal_date")));
                    logger.Info($"{row.Get("cal_date")}");
                }
            }
            //await Task.Delay(TimeSpan.FromMinutes(5));
            logger.Dispose();
            await context.Scheduler.AddCalendar(CalendarName, cal, true, true);
        }
        public async Task Execute(IJobExecutionContext context)
        {
            var token = context.MergedJobDataMap.GetString("tushare_token");
            var tushare = new TusharePro(token);

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
