﻿#if NETSTANDARD

using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Quartzmin
{
    public static class ApplicationBuilderExtensions
    {
        public static void UseQuartzmin(this IApplicationBuilder app, QuartzminOptions options, Action<Services> configure = null)
        {
            options = options ?? throw new ArgumentNullException(nameof(options));

            app.UseFileServer(options);

            var services = Services.Create(options);
            configure?.Invoke(services);

            app.Use(async (context, next) =>
            {
                context.Items[typeof(Services)] = services;
                await next.Invoke();
            });

            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    var ex = context.Features.Get<IExceptionHandlerFeature>().Error;
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync(services.ViewEngine.ErrorPage(ex));
                });
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: nameof(Quartzmin),
                    template: "{controller=Scheduler}/{action=Index}");
            });
        }

        private static void UseFileServer(this IApplicationBuilder app, QuartzminOptions options)
        {
            IFileProvider fs;
            if (string.IsNullOrEmpty(options.ContentRootDirectory))
                fs = new ManifestEmbeddedFileProvider(Assembly.GetExecutingAssembly(), "Content");
            else
                fs = new PhysicalFileProvider(options.ContentRootDirectory);

            var fsOptions = new FileServerOptions
            {
                RequestPath = new PathString("/Content"),
                EnableDefaultFiles = false,
                EnableDirectoryBrowsing = false,
                FileProvider = fs
            };

            app.UseFileServer(fsOptions);
        }

        public static void AddQuartzmin(this IServiceCollection services, Action<IMvcCoreBuilder> configure = null)
        {
            var mvc = services.AddMvcCore()
                .AddApplicationPart(Assembly.GetExecutingAssembly())
                .AddMvcOptions(options => options.EnableEndpointRouting = false)
                .AddNewtonsoftJson();
            configure?.Invoke(mvc);
        }

    }
}

#endif
