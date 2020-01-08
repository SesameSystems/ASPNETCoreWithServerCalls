using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace ASPNETCoreWithServerCalls.Codes
{

    [Serializable]
    public class DatabaseResponse
    {

        public DatabaseResponse()
        {
            Items = new List<KeyAndValueItem>();
        }

        [XmlArray("Databases")]
        [XmlArrayItem("Database", Type = typeof(KeyAndValueItem))]
        public List<KeyAndValueItem> Items { get; set; }

    }

}
