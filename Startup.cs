using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Tasks;
using ASPNETCoreWithServerCalls.Codes;
using Forge.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sesame.Communication.Data.Indentification;
using Sesame.Communication.External.Client;

namespace ASPNETCoreWithServerCalls
{
    public class Startup
    {

        private static ILog LOGGER = null;

        private readonly AutoResetEvent mFaultHandlerEvent = new AutoResetEvent(false);
        private Thread mFaultHandlerThread = null;
        private AutoResetEvent mFaultHandlerStopEvent = null;
        private bool mFaultHandlerRunning = true;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, 
            Microsoft.Extensions.Hosting.IHostApplicationLifetime applicationLifetime, 
            IWebHostEnvironment env, 
            ILoggerFactory loggerFactory,
            IConfiguration config)
        {
            // initialize logger
            loggerFactory.AddLog4Net();
            Forge.Logging.LogManager.LOGGER = Forge.Logging.Log4net.Log4NetManager.Instance;
            Forge.Logging.Utils.LogUtils.LogAll();
            LOGGER = LogManager.GetLogger(typeof(Startup));

            // bind configuration to POCO
            SesameConfiguration.Instance = config.GetSection("SesameConfiguration").Get<SesameConfiguration>();

            // create wcf binding and endpoint address
            ClientProxyBase.SourceId = ClientIdGenerator.GenerateId(ClientTypeEnum.External);
            NetTcpBinding binding = new NetTcpBinding(SecurityMode.None);
            binding.Name = "TcpEndpoint";
            binding.OpenTimeout = TimeSpan.FromMinutes(1);
            binding.CloseTimeout = TimeSpan.FromMinutes(1);
            binding.ReceiveTimeout = TimeSpan.FromMinutes(10);
            binding.SendTimeout = TimeSpan.FromMinutes(10);
            binding.MaxBufferSize = 2147483647;
            binding.MaxReceivedMessageSize = 2147483647;
            binding.TransferMode = TransferMode.Buffered;
            binding.ReaderQuotas.MaxDepth = 2147483647;
            binding.ReaderQuotas.MaxStringContentLength = 2147483647;
            binding.ReaderQuotas.MaxArrayLength = 2147483647;
            binding.ReaderQuotas.MaxBytesPerRead = 2147483647;
            binding.ReaderQuotas.MaxNameTableCharCount = 2147483647;
            binding.Security.Mode = SecurityMode.None;
            binding.Security.Message.ClientCredentialType = MessageCredentialType.None;
            binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Certificate;
            
            EndpointAddress endpoint = new EndpointAddress(SesameConfiguration.Instance.SesameServiceUrl);

            // initialize communication system
            ClientProxyBase.ConfigureClientProxyForCallback(new ConfigurationForCallback(binding, endpoint));
            ClientProxyBase.Faulted += ClientProxyBase_Faulted;
            try
            {
                ClientProxyBase.Open();
            }
            catch (Exception ex)
            {
                LOGGER.Error(string.Format("Failed to open connection. Reason: {0}", ex.Message));
                mFaultHandlerEvent.Set();
            }
            mFaultHandlerThread = new Thread(new ThreadStart(FaultHandlerThreadMain));
            mFaultHandlerThread.Name = "FaultHandlerThread";
            mFaultHandlerThread.Start();

            // register shutdown
            applicationLifetime.ApplicationStopping.Register(OnShutdown);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }

        private void OnShutdown()
        {
            ClientProxyBase.Faulted -= ClientProxyBase_Faulted;

            mFaultHandlerStopEvent = new AutoResetEvent(false);
            mFaultHandlerRunning = false;
            mFaultHandlerEvent.Set();
            mFaultHandlerStopEvent.WaitOne();
            mFaultHandlerStopEvent.Dispose();
            mFaultHandlerEvent.Dispose();

            ClientProxyBase.Close();

            if (LOGGER.IsInfoEnabled) LOGGER.Info("GLOBAL_ASAX, application stop.");
        }

        private void ClientProxyBase_Faulted(object sender, EventArgs e)
        {
            mFaultHandlerEvent.Set();
        }

        private void FaultHandlerThreadMain()
        {
            while (mFaultHandlerRunning)
            {
                mFaultHandlerEvent.WaitOne();
                if (mFaultHandlerRunning)
                {
                    try
                    {
                        ClientProxyBase.Open();
                    }
                    catch (Exception ex)
                    {
                        LOGGER.Error(string.Format("Failed to open connection. Reason: {0}", ex.Message));
                        Thread.Sleep(1000);
                    }
                }
            }
            mFaultHandlerStopEvent.Set();
        }

    }

}
