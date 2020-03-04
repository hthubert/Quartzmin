using Quartz;
using Quartzmin.Helpers;
using Quartzmin.Models;
using Quartz.Plugins.RecentHistory;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;

#region Target-Specific Directives
#if NETSTANDARD
using Microsoft.AspNetCore.Mvc;
#endif
#if NETFRAMEWORK
using System.Web.Http;
using IActionResult = System.Web.Http.IHttpActionResult;
#endif
#endregion

namespace Quartzmin.Controllers
{
    using static QuartzminHelper;
    public class ExecutionsController : PageControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentlyExecutingJobs = await Scheduler.GetCurrentlyExecutingJobs();

            var list = new List<object>();

            foreach (var exec in currentlyExecutingJobs)
            {
                var enableLog = exec.MergedJobDataMap.GetBoolean(MapDataEnableLog);
                var item = new {
                    Id = exec.FireInstanceId,
                    JobGroup = exec.JobDetail.Key.Group,
                    JobName = exec.JobDetail.Key.Name,
                    TriggerGroup = exec.Trigger.Key.Group,
                    TriggerName = exec.Trigger.Key.Name,
                    ScheduledFireTime = exec.ScheduledFireTimeUtc?.LocalDateTime.ToDefaultFormat(),
                    ActualFireTime = exec.FireTimeUtc.LocalDateTime.ToDefaultFormat(),
                    RunTime = exec.JobRunTime.ToString("hh\\:mm\\:ss"),
                    EnableLog = enableLog,
                    LogTail = enableLog ? string.Join(Environment.NewLine, LogTail(exec.FireInstanceId)) : string.Empty
                };
                list.Add(item); 
            }

            return View(list);
        }

        public class InterruptArgs
        {
            public string Id { get; set; }
        }

        [HttpPost, JsonErrorResponse]
        public async Task<IActionResult> Interrupt([FromBody] InterruptArgs args)
        {
            if (!await Scheduler.Interrupt(args.Id))
                throw new InvalidOperationException("Cannot interrupt execution " + args.Id);

            return NoContent();
        }
    }
}
