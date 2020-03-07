using Quartz.Plugins.RecentHistory;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Quartzmin.Helpers;

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
    public class HistoryController : PageControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var store = Scheduler.Context.GetExecutionHistoryStore();

            ViewBag.HistoryEnabled = store != null;

            if (store == null)
                return View(null);

            IEnumerable<ExecutionHistoryEntry> history = await store.FilterLast(100);

            var list = new List<object>();

            foreach (var h in history.OrderByDescending(x => x.ActualFireTime))
            {
                string state = "Finished", icon = "check";
                var endTime = h.FinishedTime;

                if (h.Vetoed)
                {
                    state = "Vetoed";
                    icon = "ban";
                }
                else if (!string.IsNullOrEmpty(h.ExceptionMessage))
                {
                    state = "Failed";
                    icon = "close";
                }
                else if (h.FinishedTime == null)
                {
                    state = "Running";
                    icon = "play";
                    endTime = DateTime.Now;
                }

                var jobKey = h.Job.Split('.');
                var triggerKey = h.Trigger.Split('.');

                list.Add(new {
                    Entity = h,

                    JobGroup = jobKey[0],
                    JobName = jobKey[1],
                    TriggerGroup = triggerKey[0],
                    TriggerName = triggerKey[1],

                    ScheduledFireTime = h.ScheduledFireTime?.ToDefaultFormat(),
                    ActualFireTime = h.ActualFireTime.ToDefaultFormat(),
                    FinishedTime = h.FinishedTime?.ToDefaultFormat(),
                    Duration = (endTime - h.ActualFireTime)?.ToString("hh\\:mm\\:ss"),
                    State = state,
                    StateIcon = icon,
                });
            }

            return View(list);
        }

        [HttpPost, JsonErrorResponse]
        public async Task<IActionResult> Delete([FromBody]string id)
        {
            var store = Scheduler.Context.GetExecutionHistoryStore();
            await store.Delete(id);
            return Ok();
        }

    }
}
