using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Linq;

// from https://github.com/Azure/remote-monitoring-services-dotnet
// Install-Package Microsoft.Azure.Devices -Version 1.29.0

namespace Azure_Remote
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 2)
            {
                Device newAzureDevice = new Device();

                Twin newAzureTwin = new Twin();

                DeviceServiceModel newDevice = new DeviceServiceModel(newAzureDevice, newAzureTwin, args[0]);

                RegistryManager newRegistry = RegistryManager.CreateFromConnectionString(args[0]);

                Devices newDevices = new Devices(newRegistry, args[1]);


                newDevices.CreateAsync(newDevice).Wait();
            }
        }

        private class DeviceTwinName
        {
            public HashSet<string> Tags { get; set; }

            public HashSet<string> ReportedProperties { get; set; }
        }

        public class TwinServiceModel
        {
            public string ETag { get; set; }
            public string DeviceId { get; set; }
            public string ModuleId { get; set; }
            public bool IsSimulated { get; set; }
            public Dictionary<string, JToken> DesiredProperties { get; set; }
            public Dictionary<string, JToken> ReportedProperties { get; set; }
            public Dictionary<string, JToken> Tags { get; set; }

            public TwinServiceModel()
            {
            }

            public TwinServiceModel(
                string etag,
                string deviceId,
                Dictionary<string, JToken> desiredProperties,
                Dictionary<string, JToken> reportedProperties,
                Dictionary<string, JToken> tags,
                bool isSimulated)
            {
                this.ETag = etag;
                this.DeviceId = deviceId;
                this.DesiredProperties = desiredProperties;
                this.ReportedProperties = reportedProperties;
                this.Tags = tags;
                this.IsSimulated = isSimulated;
            }

            public TwinServiceModel(Twin twin)
            {
                if (twin != null)
                {
                    this.ETag = twin.ETag;
                    this.DeviceId = twin.DeviceId;
                    this.ModuleId = twin.ModuleId;
                    this.Tags = TwinCollectionToDictionary(twin.Tags);
                    this.DesiredProperties = TwinCollectionToDictionary(twin.Properties.Desired);
                    this.ReportedProperties = TwinCollectionToDictionary(twin.Properties.Reported);
                    this.IsSimulated = this.Tags.ContainsKey("IsSimulated") && this.Tags["IsSimulated"].ToString() == "Y";
                }
            }

            public Twin ToAzureModel()
            {
                var properties = new TwinProperties
                {
                    Desired = DictionaryToTwinCollection(this.DesiredProperties),
                    Reported = DictionaryToTwinCollection(this.ReportedProperties),
                };

                return new Twin(this.DeviceId)
                {
                    ETag = this.ETag,
                    Tags = DictionaryToTwinCollection(this.Tags),
                    Properties = properties
                };
            }

            /*
            JValue:  string, integer, float, boolean
            JArray:  list, array
            JObject: dictionary, object
            JValue:     JToken, IEquatable<JValue>, IFormattable, IComparable, IComparable<JValue>, IConvertible
            JArray:     JContainer, IList<JToken>, ICollection<JToken>, IEnumerable<JToken>, IEnumerable
            JObject:    JContainer, IDictionary<string, JToken>, ICollection<KeyValuePair<string, JToken>>, IEnumerable<KeyValuePair<string, JToken>>, IEnumerable, INotifyPropertyChanged, ICustomTypeDescriptor, INotifyPropertyChanging
            JContainer: JToken, IList<JToken>, ICollection<JToken>, IEnumerable<JToken>, IEnumerable, ITypedList, IBindingList, IList, ICollection, INotifyCollectionChanged
            JToken:     IJEnumerable<JToken>, IEnumerable<JToken>, IEnumerable, IJsonLineInfo, ICloneable, IDynamicMetaObjectProvider
            */
            private static Dictionary<string, JToken> TwinCollectionToDictionary(TwinCollection x)
            {
                var result = new Dictionary<string, JToken>();

                if (x == null) return result;

                foreach (KeyValuePair<string, object> twin in x)
                {
                    try
                    {
                        if (twin.Value is JToken)
                        {
                            result.Add(twin.Key, (JToken)twin.Value);
                        }
                        else
                        {
                            result.Add(twin.Key, JToken.Parse(twin.Value.ToString()));
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                }

                return result;
            }

            private static TwinCollection DictionaryToTwinCollection(Dictionary<string, JToken> x)
            {
                var result = new TwinCollection();

                if (x != null)
                {
                    foreach (KeyValuePair<string, JToken> item in x)
                    {
                        try
                        {
                            result[item.Key] = item.Value;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }
                }

                return result;
            }
        }

        public class AuthenticationMechanismServiceModel
        {
            public AuthenticationMechanismServiceModel()
            {
            }

            internal AuthenticationMechanismServiceModel(AuthenticationMechanism azureModel)
            {
                switch (azureModel.Type)
                {
                    case Microsoft.Azure.Devices.AuthenticationType.Sas:
                        this.PrimaryKey = azureModel.SymmetricKey.PrimaryKey;
                        this.SecondaryKey = azureModel.SymmetricKey.SecondaryKey;
                        break;
                    case Microsoft.Azure.Devices.AuthenticationType.SelfSigned:
                        this.AuthenticationType = AuthenticationType.SelfSigned;
                        this.PrimaryThumbprint = azureModel.X509Thumbprint.PrimaryThumbprint;
                        this.SecondaryThumbprint = azureModel.X509Thumbprint.SecondaryThumbprint;
                        break;
                    case Microsoft.Azure.Devices.AuthenticationType.CertificateAuthority:
                        this.AuthenticationType = AuthenticationType.CertificateAuthority;
                        this.PrimaryThumbprint = azureModel.X509Thumbprint.PrimaryThumbprint;
                        this.SecondaryThumbprint = azureModel.X509Thumbprint.SecondaryThumbprint;
                        break;
                    default:
                        throw new ArgumentException("Not supported authentcation type");
                }
            }

            public string PrimaryKey { get; set; }

            public string SecondaryKey { get; set; }

            public string PrimaryThumbprint { get; set; }

            public string SecondaryThumbprint { get; set; }

            public AuthenticationType AuthenticationType { get; set; }

            public AuthenticationMechanism ToAzureModel()
            {
                var auth = new AuthenticationMechanism();

                switch (this.AuthenticationType)
                {
                    case AuthenticationType.Sas:
                        {
                            auth.SymmetricKey = new SymmetricKey()
                            {
                                PrimaryKey = this.PrimaryKey,
                                SecondaryKey = this.SecondaryKey
                            };

                            auth.Type = Microsoft.Azure.Devices.AuthenticationType.Sas;

                            break;
                        }
                    case AuthenticationType.SelfSigned:
                        {
                            auth.X509Thumbprint = new X509Thumbprint()
                            {
                                PrimaryThumbprint = this.PrimaryThumbprint,
                                SecondaryThumbprint = this.SecondaryThumbprint
                            };

                            auth.Type = Microsoft.Azure.Devices.AuthenticationType.SelfSigned;

                            break;
                        }
                    case AuthenticationType.CertificateAuthority:
                        {
                            auth.X509Thumbprint = new X509Thumbprint()
                            {
                                PrimaryThumbprint = this.PrimaryThumbprint,
                                SecondaryThumbprint = this.SecondaryThumbprint
                            };

                            auth.Type = Microsoft.Azure.Devices.AuthenticationType.CertificateAuthority;

                            break;
                        }
                    default:
                        throw new ArgumentException("Not supported authentcation type");
                }

                return auth;
            }
        }

        public enum AuthenticationType
        {
            //
            // Summary:
            //     Shared Access Key
            Sas = 0,

            //
            // Summary:
            //     Self-signed certificate
            SelfSigned = 1,

            //
            // Summary:
            //     Certificate Authority
            CertificateAuthority = 2
        }

        public class DeviceServiceModel
        {
            public string Etag { get; set; }
            public string Id { get; set; }
            public int C2DMessageCount { get; set; }
            public DateTime LastActivity { get; set; }
            public bool Connected { get; set; }
            public bool Enabled { get; set; }
            public bool IsEdgeDevice { get; set; }
            public DateTime LastStatusUpdated { get; set; }
            public TwinServiceModel Twin { get; set; }
            public string IoTHubHostName { get; set; }
            public AuthenticationMechanismServiceModel Authentication { get; set; }

            public DeviceServiceModel(
                string etag,
                string id,
                int c2DMessageCount,
                DateTime lastActivity,
                bool connected,
                bool enabled,
                bool isEdgeDevice,
                DateTime lastStatusUpdated,
                TwinServiceModel twin,
                AuthenticationMechanismServiceModel authentication,
                string ioTHubHostName)
            {
                this.Etag = etag;
                this.Id = id;
                this.C2DMessageCount = c2DMessageCount;
                this.LastActivity = lastActivity;
                this.Connected = connected;
                this.Enabled = enabled;
                this.IsEdgeDevice = isEdgeDevice;
                this.LastStatusUpdated = lastStatusUpdated;
                this.Twin = twin;
                this.IoTHubHostName = ioTHubHostName;
                this.Authentication = authentication;
            }

            /// <summary>
            /// Additional constructor which allows passing an additional isConnected field.
            /// This allows providing a different method of checking whether a device is connected or
            /// not for edge devices.
            /// </summary>
            /// <param name="azureDevice">Device from service</param>
            /// <param name="azureTwin">Device's twin</param>
            /// <param name="ioTHubHostName">IoT Hub name</param>
            /// <param name="isConnected">If this is true OR azureDevice.ConnectionState is Connected
            /// then the device is said to be connected.</param>
            public DeviceServiceModel(Device azureDevice, Twin azureTwin, string ioTHubHostName, bool isConnected) :
                this(
                    etag: azureDevice.ETag,
                    id: azureDevice.Id,
                    c2DMessageCount: azureDevice.CloudToDeviceMessageCount,
                    lastActivity: azureDevice.LastActivityTime,
                    connected: isConnected || azureDevice.ConnectionState.Equals(DeviceConnectionState.Connected),
                    enabled: azureDevice.Status.Equals(DeviceStatus.Enabled),
                    isEdgeDevice: azureDevice.Capabilities?.IotEdge ?? azureTwin.Capabilities?.IotEdge ?? false,
                    lastStatusUpdated: azureDevice.StatusUpdatedTime,
                    twin: new TwinServiceModel(azureTwin),
                    ioTHubHostName: ioTHubHostName,
                    authentication: new AuthenticationMechanismServiceModel(azureDevice.Authentication))
            {
            }

            public DeviceServiceModel(Device azureDevice, Twin azureTwin, string ioTHubHostName) :
                this(
                    azureDevice,
                    azureTwin,
                    ioTHubHostName,
                    azureDevice.ConnectionState.Equals(DeviceConnectionState.Connected))
            {
            }

            public DeviceServiceModel(Twin azureTwin, string ioTHubHostName, bool isConnected) :
                this(
                    etag: azureTwin.ETag,
                    id: azureTwin.DeviceId,
                    c2DMessageCount: azureTwin.CloudToDeviceMessageCount ?? azureTwin.CloudToDeviceMessageCount ?? 0,
                    lastActivity: azureTwin.LastActivityTime ?? azureTwin.LastActivityTime ?? new DateTime(),
                    connected: isConnected || azureTwin.ConnectionState.Equals(DeviceConnectionState.Connected),
                    enabled: azureTwin.Status.Equals(DeviceStatus.Enabled),
                    isEdgeDevice: azureTwin.Capabilities?.IotEdge ?? azureTwin.Capabilities?.IotEdge ?? false,
                    lastStatusUpdated: azureTwin.StatusUpdatedTime ?? azureTwin.StatusUpdatedTime ?? new DateTime(),
                    twin: new TwinServiceModel(azureTwin),
                    ioTHubHostName: ioTHubHostName,
                    authentication: null
                )
            {
            }



            public Device ToAzureModel(bool ignoreEtag = true)
            {
                var device = new Device(this.Id)
                {
                    ETag = ignoreEtag ? null : this.Etag,
                    Status = Enabled ? DeviceStatus.Enabled : DeviceStatus.Disabled,
                    Authentication = this.Authentication == null ? null : this.Authentication.ToAzureModel(),
                    Capabilities = this.IsEdgeDevice ? new DeviceCapabilities()
                    {
                        IotEdge = this.IsEdgeDevice
                    } : null
                };

                return device;
            }
        }

        public class InvalidInputException : Exception
        {
            public InvalidInputException() : base()
            {
            }

            public InvalidInputException(string message) : base(message)
            {
            }

            public InvalidInputException(string message, Exception innerException) : base(message, innerException)
            {
            }
        }


        private class Devices
        {
            private const int MAX_GET_LIST = 1000;
            private const string QUERY_PREFIX = "SELECT * FROM devices";
            private const string MODULE_QUERY_PREFIX = "SELECT * FROM devices.modules";
            private const string DEVICES_CONNECTED_QUERY = "connectionState = 'Connected'";

            private RegistryManager registry;
            private string ioTHubHostName;

            public Devices(RegistryManager registry, string ioTHubHostName)
            {
                this.registry = registry;
                this.ioTHubHostName = ioTHubHostName;
            }

            public async Task<DeviceServiceModel> CreateAsync(DeviceServiceModel device)
            {
                if (device.IsEdgeDevice &&
                    device.Authentication != null &&
                    !device.Authentication.AuthenticationType.Equals(AuthenticationType.Sas))
                {
                    throw new InvalidInputException("Edge devices only support symmetric key authentication.");
                }

                // auto generate DeviceId, if missing
                if (string.IsNullOrEmpty(device.Id))
                {
                    device.Id = Guid.NewGuid().ToString();
                }

                Device azureDevice;
                azureDevice = await registry.AddDeviceAsync(device.ToAzureModel());

                Twin azureTwin;
                if (device.Twin == null)
                {
                    Twin azureTwin1;
                    azureTwin1 = await registry.GetTwinAsync(device.Id);
                    azureTwin = azureTwin1;
                }
                else
                {
                    Twin azureTwin2;
                    azureTwin2 = await registry.UpdateTwinAsync(device.Id, device.Twin.ToAzureModel(), "*");
                    azureTwin = azureTwin2;
                }

                Device azureDevice2;
                azureDevice2 = azureDevice;
                return new DeviceServiceModel(azureDevice2, azureTwin, this.ioTHubHostName);
            }


            public async Task DeleteAsync(string id)
            {
                await this.registry.RemoveDeviceAsync(id);
            }

            public async Task<TwinServiceModel> GetModuleTwinAsync(string deviceId, string moduleId)
            {
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    throw new InvalidInputException("A valid deviceId must be provided.");
                }

                if (string.IsNullOrWhiteSpace(moduleId))
                {
                    throw new InvalidInputException("A valid moduleId must be provided.");
                }

                var twin = await this.registry.GetTwinAsync(deviceId, moduleId);
                return new TwinServiceModel(twin);
            }



            /// <summary>
            /// Get twin result by query
            /// </summary>
            /// <param name="queryPrefix">The query prefix which selects devices or device modules</param>
            /// <param name="query">The query without prefix</param>
            /// <param name="continuationToken">The continuationToken</param>
            /// <param name="numberOfResult">The max result</param>
            /// <returns></returns>
            private async Task<ResultWithContinuationToken<List<Twin>>> GetTwinByQueryAsync(string queryPrefix,
                string query, string continuationToken, int numberOfResult)
            {
                query = string.IsNullOrEmpty(query) ? queryPrefix : $"{queryPrefix} where {query}";

                var twins = new List<Twin>();

                var twinQuery = this.registry.CreateQuery(query);

                QueryOptions options = new QueryOptions();
                options.ContinuationToken = continuationToken;

                while (twinQuery.HasMoreResults && twins.Count < numberOfResult)
                {
                    var response = await twinQuery.GetNextAsTwinAsync(options);
                    options.ContinuationToken = response.ContinuationToken;
                    twins.AddRange(response);
                }

                return new ResultWithContinuationToken<List<Twin>>(twins, options.ContinuationToken);
            }



            private class ResultWithContinuationToken<T>
            {
                public T Result { get; private set; }

                public string ContinuationToken { get; private set; }

                public ResultWithContinuationToken(T queryResult, string continuationToken)
                {
                    this.Result = queryResult;
                    this.ContinuationToken = continuationToken;
                }
            }
        }
    }
}
