﻿using BadNews.ModelBuilders.News;
using BadNews.Models.News;
using BadNews.Repositories.News;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using BadNews.Elevation;
using BadNews.Repositories.Weather;
using BadNews.Validation;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Serilog;

namespace BadNews
{
    public class Startup
    {
        private readonly IWebHostEnvironment env;
        private readonly IConfiguration configuration;

        // В конструкторе уже доступна информация об окружении и конфигурация
        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            this.env = env;
            this.configuration = configuration;
        }

        // В этом методе добавляются сервисы в DI-контейнер
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<INewsRepository, NewsRepository>();
            services.AddSingleton<INewsModelBuilder, NewsModelBuilder>();
            services.AddSingleton<IValidationAttributeAdapterProvider, StopWordsAttributeAdapterProvider>();
            services.AddSingleton<IWeatherForecastRepository, WeatherForecastRepository>();
            services.Configure<OpenWeatherOptions>(configuration.GetSection("OpenWeather"));
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
            });
            services.AddMemoryCache();
            var mvcBuilder = services.AddControllersWithViews();
            if (env.IsDevelopment())
                mvcBuilder.AddRazorRuntimeCompilation();
        }

        // В этом методе конфигурируется последовательность обработки HTTP-запроса
        public void Configure(IApplicationBuilder app)
        {
            //app.UseDeveloperExceptionPage();
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseExceptionHandler("/Errors/Exception");
            app.UseMiddleware<ElevationMiddleware>();
            app.UseHttpsRedirection();
            app.UseResponseCompression();
            app.UseStaticFiles(new StaticFileOptions()
            {
                OnPrepareResponse = options =>
                {
                    options.Context.Response.GetTypedHeaders().CacheControl =
                        new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
                        {
                            Public = false,
                            MaxAge = TimeSpan.FromDays(1)
                        };
                }
            });
            app.UseSerilogRequestLogging();
            app.UseStatusCodePagesWithReExecute("/StatusCode/{0}");
            
            

            /*app.MapWhen(context => context.Request.Path == "/", rootPathApp =>
            {
                rootPathApp.Run(RenderIndexPage);
            });*/
            
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("status-code", "StatusCode/{code?}", new
                {
                    controller = "Errors",
                    action = "StatusCode"
                });
                endpoints.MapControllerRoute("default", "{controller=News}/{action=Index}/{id?}");
            });
            
            app.MapWhen(context => context.Request.IsElevated(), branchApp =>
            {
                branchApp.UseDirectoryBrowser("/files");
            });
            
        }
        
        
    }
}
