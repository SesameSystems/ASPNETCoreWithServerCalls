using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace ASPNETCoreWithServerCalls.Codes
{

    [Serializable]
    public class SPDatabaseDetailsResponse
    {

        public SPDatabaseDetailsResponse()
        {
            Currency = "€";
            Languages = new List<KeyAndValueItem>();
        }

        public int BaseClassificationIdForTargetSelection { get; set; }

        public string BaseClassificationMnemonicsForTargetSelection { get; set; }

        public double Population { get; set; }

        public double Sample { get; set; }

        public string Currency { get; set; }

        [XmlArray("Languages", IsNullable = false)]
        [XmlArrayItem("Item", Type = typeof(KeyAndValueItem), IsNullable = false)]
        public List<KeyAndValueItem> Languages { get; set; }

    }

}
