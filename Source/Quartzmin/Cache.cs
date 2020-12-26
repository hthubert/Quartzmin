using System;
using System.Collections.Generic;

namespace Quartzmin
{
    public class Cache
    {
        private readonly Services _services;
        private readonly List<string> _jobTypes = new List<string>();
        public Cache(Services services)
        {
            _services = services;
        }

        public string[] JobTypes { get; private set; }

        public void AddJobType(Type type)
        {
            _jobTypes.Add(type.RemoveAssemblyDetails());
            JobTypes = _jobTypes.ToArray();
        }

    }
}
