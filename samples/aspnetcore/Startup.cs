using System;
using System.IO;
using Hyperledger.Aries.Storage;
using Jdenticon.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebAgent.Messages;
using WebAgent.Protocols.BasicMessage;
using WebAgent.Utils;
using Hyperledger.Aries.Configuration;

namespace WebAgent
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddLogging();

            Console.WriteLine("sleeping 5 sec...");
            System.Threading.Thread.Sleep(5000);

            // Register agent framework dependency services and handlers
            services.AddAriesFramework(builder =>
            {
                builder.RegisterEdgeAgent<SimpleWebAgent>(c =>
                {
                    c.AgentName = Environment.GetEnvironmentVariable("AGENT_NAME") ?? NameGenerator.GetRandomName();
                    c.EndpointUri = Environment.GetEnvironmentVariable("ENDPOINT_HOST") ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
                    // c.WalletConfiguration = new WalletConfiguration { Id = "WebAgentWallet" };
                    // c.WalletCredentials = new WalletCredentials { Key = "MyWalletKey" };
                    c.WalletConfiguration.Id = c.AgentName;
                    c.GenesisFilename = Path.GetFullPath("pool_genesis.txn");
                    c.PoolName = "TestPool";
                });
            });

            // Register custom handlers with DI pipeline
            services.AddSingleton<BasicMessageHandler>();
            services.AddSingleton<TrustPingMessageHandler>();
            services.AddHostedService<PoolConfigurationService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            // Register agent middleware
            app.UseAriesFramework();

            // fun identicons
            app.UseJdenticon();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
