using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Quartzmin.Controllers
{
    using static QuartzminHelper;

    [Authorize]
    public class LogsController : PageControllerBase
    {
        [HttpGet]
        public IActionResult Download(string id) 
        {
            return File(GetLogStream(id, true), "text/plain", $"{id}.txt");
        }

        [HttpGet]
        public IActionResult View(string id)
        {
            return Content(new StreamReader(GetLogStream(id, true)).ReadToEnd());
        }
    }
}
