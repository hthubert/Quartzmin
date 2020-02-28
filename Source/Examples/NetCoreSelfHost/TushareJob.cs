using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Quartz;

namespace Quartzmin
{
    public class TushareJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            //context.
            return Task.FromResult(0);
        }
    }
}
