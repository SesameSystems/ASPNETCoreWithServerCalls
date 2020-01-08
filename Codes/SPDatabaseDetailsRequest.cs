using System;
using System.Xml.Serialization;

namespace ASPNETCoreWithServerCalls.Codes
{

    [Serializable]
    public class SPDatabaseDetailsRequest
    {

        [XmlIgnore]
        public string DatabaseId { get; set; }

    }

}
