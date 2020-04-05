using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IdentityModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using Quartz.Impl;
using Quartzmin;
using Quartzmin.SelfHost;
using Quartz.Plugins.RecentHistory;
using Quartzmin.Controllers;

namespace QuartzminServer
{
    using static QuartzminHelper;
    internal class Program
    {
        private static NameValueCollection LoadConfig()
        {
            var props = new NameValueCollection((NameValueCollection)ConfigurationManager.GetSection("quartz"));
            ParseRelative("quartz.plugin.recentHistory.dataPath");
            ParseRelative("quartz.dataSource.default.connectionString");
            return props;

            void ParseRelative(string key)
            {
                props[key] = RelativeToAbs(props[key]);
            }
        }

        private static async Task<IScheduler> CreateScheduler()
        {
            var factory = new StdSchedulerFactory(LoadConfig());
            var scheduler = await factory.GetScheduler(CancellationToken.None);
            {
                var plugin = scheduler.Context.GetQuartzminPlugin();
                plugin.ConfigureAppAction = ConfigureApp;
                plugin.ConfigureMvcAction = ConfigureMvc;
                plugin.ConfigureServicesAction = ConfigureServices;
                plugin.ConfigureQuartzminAction = ConfigureQuartzmin;
            }
            {
                var plugin = scheduler.Context.GetExecutionHistoryPlugin();
                plugin.EnableLogGetter = context => context.MergedJobDataMap.TryGetValue(JobEnableLog, true);
            }
            await scheduler.Start();
            HistoryPurgeJob.HistoryStore = scheduler.Context.GetExecutionHistoryStore();
            return scheduler;
        }

        private static void ConfigureQuartzmin(Services services)
        {
            services.Cache.AddJobType(typeof(TushareJob));
            services.Cache.AddJobType(typeof(PwshJob));
            services.Cache.AddJobType(typeof(DotnetJob));
            services.Cache.AddJobType(typeof(HistoryPurgeJob));
            services.Cache.AddJobType(typeof(ProgramJob));
            ViewFileSystemFactory.RegisterAssembly(nameof(QuartzminServer), typeof(OAuthController).Assembly);
            //services.ViewEngine.RegisterTemplate("OAuth/Index.hbs", GetTemplate("OAuth", "Index"));
        }

        private static string GetTemplate(string controller, string action)
        {
            var name = $"{nameof(QuartzminServer)}.Views.{controller}.{action}.hbs";
            using var stream = typeof(OAuthController).Assembly.GetManifestResourceStream(name);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(x => {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(options => {
                options.Events = new JwtBearerEvents {
                    OnMessageReceived = context => {
                        var cookies = context.Request.Cookies;
                        if (cookies.ContainsKey(OAuthCookieName))
                        {
                            context.Token = cookies[OAuthCookieName];
                        }
                        return Task.CompletedTask;
                    }
                };

                options.TokenValidationParameters = new TokenValidationParameters {
                    NameClaimType = JwtClaimTypes.Name,
                    RoleClaimType = JwtClaimTypes.Role,
                    ValidIssuer = OAuthIssuer,
                    ValidAudience = OAuthAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(OAuthSecret))
                };
            });
        }

        private static void ConfigureMvc(IMvcCoreBuilder services)
        {
            services.AddAuthorization().AddApplicationPart(typeof(OAuthController).Assembly);
        }

        private static void ConfigureApp(IApplicationBuilder app)
        {
            app.UseAuthorization();
            app.UseAuthentication();
        }

        private static void Main(string[] args)
        {
            var scheduler = CreateScheduler().Result;
            scheduler.Start();
            while (!scheduler.IsShutdown)
                Thread.Sleep(500);
        }
    }
}
