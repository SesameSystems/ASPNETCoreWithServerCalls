using Forge.Persistence.Formatters;
using Forge.Persistence.Serialization;
using Sesame.Communication.Data;
using Sesame.Communication.External.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Xml.Serialization;

namespace ASPNETCoreWithServerCalls.Codes
{

    public sealed class ComProxy : ClientProxyBase
    {

        private static readonly Encoding ENCODING = Encoding.Unicode;

        private static readonly GZipFormatter COMPRESSION = new GZipFormatter();

        private const string GET_SP_DATABASES = "GET_SP_DATABASES";
        private const string GET_SP_DATABASE_DETAILS = "GET_SP_DATABASE_DETAILS";

        private static DatabaseResponse mContainer = null;
        private static Dictionary<string, SPDatabaseDetailsResponse> mDatabaseDetails = new Dictionary<string, SPDatabaseDetailsResponse>();

        private static readonly bool ENABLE_XML_LOGGING = true;
        private static readonly int PROCESS_ID = Process.GetCurrentProcess().Id;
        private static int ID = 0;

        public ComProxy()
        {
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public DatabaseResponse GetDatabases()
        {
            if (mContainer != null)
                return mContainer;

            RemoteMessageBinary request = new RemoteMessageBinary();
            request.CommunicationPattern = CommunicationPatternEnum.Request;
            request.MessageTypeCustomId = GET_SP_DATABASES.ToString();
            request.Timeout = SesameConfiguration.Instance.DefaultRequestWaitTimeoutInMS;

            RemoteMessageBase response = SendMessage(this, request);
            if (response is RemoteMessageBinary)
            {
                // deserialize response content
                RemoteMessageBinary arrayResponse = (RemoteMessageBinary)response;
                return mContainer = DeserializeResponseData<DatabaseResponse>(arrayResponse);
            }
            else if (response is RemoteMessageError)
            {
                RemoteMessageError errorMessage = (RemoteMessageError)response;
                if (errorMessage.ErrorCode == (int)RemoteMessageErrorCodesEnum.Unknown_Message_Type)
                    throw new ApplicationException("Unknown message. No backend configured for understand that message or it is offline.");
                else
                    throw errorMessage.GetException();
            }
            else
                throw new InvalidDataException();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public SPDatabaseDetailsResponse GetDatabaseDetails(SPDatabaseDetailsRequest requestData)
        {
            if (requestData == null)
                throw new ArgumentNullException(nameof(requestData));
            if (mDatabaseDetails.ContainsKey(requestData.DatabaseId))
                return mDatabaseDetails[requestData.DatabaseId];

            RemoteMessageBinary request = new RemoteMessageBinary();
            request.CommunicationPattern = CommunicationPatternEnum.Request;
            request.MessageTypeCustomId = string.Format("{0}_{1}", requestData.DatabaseId, GET_SP_DATABASE_DETAILS.ToString());
            request.Timeout = SesameConfiguration.Instance.DefaultRequestWaitTimeoutInMS;
            request.Data = SerializeRequestData<SPDatabaseDetailsRequest>(requestData);

            RemoteMessageBase response = SendMessage(this, request);
            if (response is RemoteMessageBinary)
            {
                // deserialize response content
                RemoteMessageBinary arrayResponse = (RemoteMessageBinary)response;
                SPDatabaseDetailsResponse detailsResponse = DeserializeResponseData<SPDatabaseDetailsResponse>(arrayResponse);
                mDatabaseDetails[requestData.DatabaseId] = detailsResponse;
                return detailsResponse;
            }
            else if (response is RemoteMessageError)
            {
                RemoteMessageError errorMessage = (RemoteMessageError)response;
                if (errorMessage.ErrorCode == (int)RemoteMessageErrorCodesEnum.Unknown_Message_Type)
                    throw new ApplicationException("Unknown message. No backend configured for understand that message or it is offline.");
                else
                    throw errorMessage.GetException();
            }
            else
                throw new InvalidDataException();
        }

        /// <summary>
        /// Handles the push message.
        /// </summary>
        /// <param name="receivedMessage">The received message.</param>
        /// <param name="isHandled">if set to <c>true</c> [is handled].</param>
        protected override void HandlePushMessage(RemoteMessageBase receivedMessage, ref bool isHandled)
        {
        }

        private static byte[] SerializeRequestData<TRequestData>(TRequestData data) where TRequestData : class, new()
        {
            XmlSerializer requestSerializer = new XmlSerializer(typeof(TRequestData));
            using (MemoryStream ms = new MemoryStream())
            {
                SerializationHelper.Write<TRequestData>(data, ms, new XmlDataFormatter<TRequestData>() { Encoding = ENCODING }, true);
                if (ENABLE_XML_LOGGING)
                {
                    using (MemoryStream stream = new MemoryStream(ms.ToArray()))
                    {
                        stream.Position = 0;
                        int id = Interlocked.Increment(ref ID);
                        SaveContentIntoFile(string.Format("P{0}_C{1}_{2}.xml", PROCESS_ID.ToString(), id.ToString(), typeof(TRequestData).Name), COMPRESSION.Read(stream));
                    }
                }
                return ms.ToArray();
            }
        }

        private static TResponseData DeserializeResponseData<TResponseData>(RemoteMessageBinary message) where TResponseData : class, new()
        {
            if (ENABLE_XML_LOGGING)
            {
                using (MemoryStream stream = new MemoryStream(message.Data))
                {
                    stream.Position = 0;
                    int id = Interlocked.Increment(ref ID);
                    SaveContentIntoFile(string.Format("P{0}_C{1}_{2}.xml", PROCESS_ID.ToString(), id.ToString(), typeof(TResponseData).Name), COMPRESSION.Read(stream));
                }
            }
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(message.Data, 0, message.Data.Length);
                ms.Position = 0;
                return SerializationHelper.Read<TResponseData>(ms, new XmlDataFormatter<TResponseData>() { Encoding = ENCODING }, true);
            }
        }

        /// <summary>Gets or sets the wait timeout for a remote method call.</summary>
        /// <value>The wait timeout value.</value>
        public static int WaitTimeout { get; set; } = 300000;

        /// <summary>
        /// Saves the scheme into file.
        /// </summary>
        /// <param name="filename">The filename.</param>
        private static void SaveSchemeIntoFile(string filename, Type schemeForType)
        {
            XmlSchemas schemas = new XmlSchemas();
            XmlSchemaExporter exporter = new XmlSchemaExporter(schemas);

            //Import the type as an XML mapping
            XmlTypeMapping mapping = new XmlReflectionImporter().ImportTypeMapping(schemeForType);

            //Export the XML mapping into schemas
            exporter.ExportTypeMapping(mapping);

            //Print out the schemas
            using (FileStream fs = new FileStream(Path.Combine(@"C:\XMLFiles", filename), FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                foreach (object schema in schemas)
                {
                    ((System.Xml.Schema.XmlSchema)schema).Write(fs);
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private static void SaveContentIntoFile(string filename, byte[] data)
        {
            using (FileStream fs = new FileStream(Path.Combine(@"C:\XMLFiles", filename), FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                fs.Write(data, 0, data.Length);
            }
        }

    }

}
