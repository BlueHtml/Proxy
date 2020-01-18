using AspNetCore.Proxy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IOptionsMonitor<ProxyConfig> proxyConfig, ILogger<Startup> log)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            #region 配置数据

            //立即绑定，并且监控数据变化
            proxyConfig.CurrentValue.ToString();

            #endregion

            app.RunProxy(c =>
            {
                try
                {
                    string[] arr = c.Request.Host.Host.Split('.');
                    if (arr.Length > 2)
                    {
                        arr = arr[0..^2];
                        string first = arr.Length < 3 ? "www." : string.Empty;
                        string last = string.Join('.', arr);

                        if (ProxyConfig.IsAllowAll && arr.Length > 1)
                        {
                            return $"https://{first}{last}";
                        }

                        foreach (var proxyConfig in ProxyConfig.ProxiedAddresses)
                        {
                            if (proxyConfig.Key.Equals(last, StringComparison.OrdinalIgnoreCase)
                            || proxyConfig.Value.Equals(last, StringComparison.OrdinalIgnoreCase))
                            {
                                if (proxyConfig.Value.Count(p => p == '.') > 1)
                                {
                                    first = string.Empty;
                                }
                                return $"https://{first}{proxyConfig.Value}";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "getProxiedAddress is error! IsAllowAll:{0}; ProxiedAddresses:{1}; c.Request.Host.Host:{2}; ProxiedAddresses:{3}", ProxyConfig.IsAllowAll.ToString(),
ProxyConfig.ProxiedAddresses == null ? "null" : string.Join("; ", ProxyConfig.ProxiedAddresses.Select(p => p.Key + ":" + p.Value)),
c.Request.Host.Host, Configuration.GetValue<string>("ProxiedAddresses"));
                }

                c.Items["write"] = "Hello!";
                return "https://www.qq.com";
            },
            new ProxyOptions
            {
                ShouldAddForwardedHeaders = false,//禁止添加跳转和源站标头
                Intercept = async c =>
                 {
                     if (c.Items.TryGetValue("write", out object write))
                     {
                         await c.Response.WriteAsync(write as string);
                         return true;
                     }
                     return false;
                 },
                BeforeSend = (c, hrm) =>
                {
                    //替换
                    if (hrm.Headers.Referrer != null)
                    {
                        //http://so.com.17x.cn/s?ie=utf-8&fr=none&src=360sou_newhome&q=cc
                        //https://so.com/s?ie=utf-8&fr=none&src=360sou_newhome&q=cc
                        hrm.Headers.Referrer = new Uri(hrm.Headers.Referrer.ToString().Replace(
                            c.Request.IsHttps ? "https://" : "http://" + c.Request.Host.Value,
                           "https://" + hrm.Headers.Host,
                            StringComparison.OrdinalIgnoreCase));
                    }

                    hrm.RequestUri = new Uri(hrm.RequestUri + c.Request.QueryString.ToUriComponent());
                    return Task.CompletedTask;
                },
                HandleFailure = (c, ex) => c.Response.WriteAsync("Exception!")
            }); ;
        }
    }

    public class ProxyConfig
    {
        public static bool IsAllowAll { get; set; }
        public static Dictionary<string, string> ProxiedAddresses { get; set; }
    }
}
