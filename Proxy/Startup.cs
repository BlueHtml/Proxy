using AspNetCore.Proxy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Proxy
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            #region 关联数据(仅做关联,未真正绑定)

            services.Configure<ProxyConfig>(Configuration);

            #endregion

            services.AddProxies();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IOptionsMonitor<ProxyConfig> proxyConfig)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            #region 配置数据

            //立即绑定，并且监控数据变化
            proxyConfig.CurrentValue.ToString();

            #endregion

            app.RunProxy(ProxyConfig.ProxiedAddress, new ProxyOptions
            {
                ShouldAddForwardedHeaders = false,//禁止添加跳转和源站标头
                BeforeSend = ((c, hrm) =>
                {
                    //替换
                    if (hrm.Headers.Referrer != null)
                    {
                        hrm.Headers.Referrer = new Uri(hrm.Headers.Referrer.ToString().Replace(c.Request.IsHttps ? "https://" : "http://" + c.Request.Headers["Host"], ProxyConfig.ProxiedAddress, StringComparison.OrdinalIgnoreCase));
                    }
                    hrm.RequestUri = new Uri(hrm.RequestUri.ToString() + c.Request.QueryString.ToUriComponent());

                    return Task.CompletedTask;
                })
            });
        }
    }

    public class ProxyConfig
    {
        public static string ProxiedAddress { get; set; }
    }
}
