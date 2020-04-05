using Quartz;
using Quartz.Spi;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
#if NETSTANDARD
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
#endif

namespace Quartzmin.SelfHost
{
    public class QuartzminPlugin : ISchedulerPlugin
    {
        public string Url { get; set; }
        public string DefaultDateFormat { get; set; }
        public string DefaultTimeFormat { get; set; }
        public string Logo { get; set; }
        public string ProductName { get; set; }
        public string VirtualPathRoot { get; set; }
        public Action<IServiceCollection> ConfigureServicesAction { get; set; } = null;
        public Action<IMvcCoreBuilder> ConfigureMvcAction { get; set; } = null;
        public Action<Services> ConfigureQuartzminAction { get; set; } = null;
        public Action<IApplicationBuilder> ConfigureAppAction { get; set; } = null;

        private IScheduler _scheduler;
        private IDisposable _webApp;

        public Task Initialize(string pluginName, IScheduler scheduler, CancellationToken cancellationToken = default)
        {
            _scheduler = scheduler;
            _scheduler.Context.SetQuartzminPlugin(this);
            return Task.FromResult(0);
        }

        public async Task Start(CancellationToken cancellationToken = default)
        {
            var host = Microsoft.AspNetCore.WebHost.CreateDefaultBuilder().Configure(app => {
                ConfigureAppAction?.Invoke(app);
                app.UseQuartzmin(CreateQuartzminOptions(), ConfigureQuartzminAction);
            }).ConfigureServices(services => {
                ConfigureServicesAction?.Invoke(services);
                services.AddQuartzmin(ConfigureMvcAction);
            })
            .ConfigureLogging(logging => {
                logging.ClearProviders();
            })
            .UseUrls(Url)
            .Build();

            _webApp = host;

            await host.StartAsync(CancellationToken.None);
        }

        public Task Shutdown(CancellationToken cancellationToken = default(CancellationToken))
        {
            _webApp.Dispose();
            return Task.FromResult(0);
        }

        private QuartzminOptions CreateQuartzminOptions()
        {
            var options = new QuartzminOptions {
                Scheduler = _scheduler,
            };

            if (!string.IsNullOrEmpty(DefaultDateFormat))
            {
                options.DefaultDateFormat = DefaultDateFormat;
            }
            if (!string.IsNullOrEmpty(DefaultTimeFormat))
            {
                options.DefaultTimeFormat = DefaultTimeFormat;
            }
            if (!string.IsNullOrEmpty(Logo))
            {
                options.Logo = Logo;
            }
            if (!string.IsNullOrEmpty(ProductName))
            {
                options.ProductName = ProductName;
            }
            if (!string.IsNullOrEmpty(VirtualPathRoot))
            {
                options.VirtualPathRoot = VirtualPathRoot;
            }
            return options;
        }

    }
}
