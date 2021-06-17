using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Hangfire;
using Hangfire.Server;
using Hangfire.MemoryStorage;
using Hangfire.Dashboard;

namespace Covid19TW
{
    class Program
    {
        static void Main(string[] args)
        {

            IoC.Register();

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddHangfire(hangfireConfiguration =>
            {
                hangfireConfiguration.UseMemoryStorage();
            });


            using var app = builder.Build();

            app.UseHangfireServer();

            app.UseHangfireDashboard("/hangfire",
                new DashboardOptions
                {
                    Authorization = new[] { new DashboardAccessAuthFilter() },
                    IsReadOnlyFunc = (context) =>
                    {
                        return false;
                    }
                });

            app.UseHangfireDashboard();

            Hangfire.RecurringJob.AddOrUpdate("dailyData",
                                () => IoC.Get<IDataManager>().SetCountryDataNoCache(), 
                                "0 0 7-12 ? ? ?", TimeZoneInfo.Local);            

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.MapGet("/getCountry", async context =>
            {
                IDataManager dataManager = IoC.Get<IDataManager>();
                var country = dataManager.GetCountry(out string updatedTime);
                await context.Response.WriteAsJsonAsync(new
                {
                    country,
                    updatedTime
                });
            });


            app.MapGet("/info", async context =>
            {
                //BackgroundJob.Enqueue(() => Console.WriteLine("hello world."));
                System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(
                    System.Reflection.Assembly.GetEntryAssembly().Location);
                string version = fvi.FileVersion;                    
                IDataManager dataManager = IoC.Get<IDataManager>();
                var country = dataManager.GetCountry(out string updatedTime);
                await context.Response.WriteAsJsonAsync(new
                {
                    assets = Helper.MapPath("assets"),
                    version = version                    
                });
            });
            //MapPath

            app.MapGet("/", context =>
            {
                context.Response.Redirect("/html/index.html");
                return Task.CompletedTask;
            });

            //string assetsPath = @"C:\Git2019\Utilities\covid19tw\assets";
            string assetsPath = Helper.MapPath("assets");
            if (System.IO.Directory.Exists(assetsPath) == false)
            {
                assetsPath = @"C:\Git2019\Utilities\covid19tw\assets";
            }
            if (System.IO.Directory.Exists(assetsPath) == false)
            {
                Console.WriteLine("assets path not found.");
                return;
            }
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(assetsPath),
                RequestPath = new PathString("/html"),
                ServeUnknownFileTypes = true
            });
                        
            app.Run();
        }
    }

    public class DashboardAccessAuthFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var clientIp = context.Request.RemoteIpAddress;
            if (clientIp == "::1" || clientIp == "127.0.0.1" || clientIp == "211.72.124.67")
            {
                return true;
            }
            return false;
        }
    }
}