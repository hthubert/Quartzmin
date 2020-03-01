using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Quartz;

namespace QuartzminServer
{
    class DotnetJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {            
            throw new NotImplementedException();
        }
    }
}
