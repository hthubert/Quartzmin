
namespace Quartz.Plugins.RecentHistory
{
    public static class Extensions
    {
        public static void SetExecutionHistoryStore(this SchedulerContext context, IExecutionHistoryStore store)
        {
            context.Put(typeof(IExecutionHistoryStore).FullName, store);
        }

        public static IExecutionHistoryStore GetExecutionHistoryStore(this SchedulerContext context)
        {
            return (IExecutionHistoryStore)context.Get(typeof(IExecutionHistoryStore).FullName);
        }

        public static void SetExecutionHistoryPlugin(this SchedulerContext context, ExecutionHistoryPlugin plugin) 
        {
            context.Put(typeof(ExecutionHistoryPlugin).FullName, plugin);
        }

        public static ExecutionHistoryPlugin GetExecutionHistoryPlugin(this SchedulerContext context) 
        {
            return (ExecutionHistoryPlugin)context.Get(typeof(ExecutionHistoryPlugin).FullName);
        }
    }
}
