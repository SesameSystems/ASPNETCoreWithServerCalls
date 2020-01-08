namespace ASPNETCoreWithServerCalls.Codes
{

    public class SesameConfiguration
    {

        public static SesameConfiguration Instance { get; set; } = new SesameConfiguration();

        public int DefaultRequestWaitTimeoutInMS { get; set; } = 300000;

        public string SesameServiceUrl { get; set; } = "net.tcp://localhost:37008/SesameExternalService/tcp";

    }

}
