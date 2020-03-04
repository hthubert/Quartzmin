using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace Quartzmin.Controllers
{
    using static QuartzminHelper;
    public class LogsController : PageControllerBase
    {
        [HttpGet]
        public IActionResult Download(string id) 
        {
            return File(GetLogStream(id, true), "text/plain", $"{id}.txt");
        }
    }
}
