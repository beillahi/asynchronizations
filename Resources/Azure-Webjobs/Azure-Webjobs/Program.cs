using System;
using System.Configuration;
using System.IO;
using System.Net;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

// from https://github.com/Azure/azure-webjobs-sdk-samples/tree/master/PhluffyLogs
// Configuration issue: https://stackoverflow.com/questions/1274852/the-name-configurationmanager-does-not-exist-in-the-current-context
// Install-Package Microsoft.WindowsAzure.ConfigurationManager -Version 3.2.3
// Install-Package WindowsAzure.Storage -Version 9.3.3

namespace Azure_Webjobs
{
    class Program
    {
        static void Main(string[] args)
        {
            DeviceInformation newdeviceInfo = new DeviceInformation();

            if (args.Length > 0)
            {
                WebRequest request;
                request = WebRequest.Create(args[0]);

                WebRequest request2;
                request2 = request;
                // Get the response.
                WebResponse response;
                response = request2.GetResponse(); // exist GetResponseAsync

                WebResponse response2;
                response2 = response;

                HttpWebResponse response3;
                response3 = (HttpWebResponse)response2;
                // Get the stream containing content returned by the server.
                Stream dataStream;
                dataStream = response3.GetResponseStream();  // exist GetResponseStreamAsync

                Stream newStream;
                newStream = dataStream;

                LogController newLogController = new LogController();

                newLogController.StoreResults(newdeviceInfo, newStream);
            }
        }

        private class DeviceInformation
        {
            public string Manufacturer { get; set; }

            public string Model { get; set; }
        }

        public static class ConfigurationReader
        {
            public static string ReadAppSetting(string settingName)
            {
                if (string.IsNullOrWhiteSpace(settingName))
                {
                    throw new ArgumentNullException("settingName");
                }

                var settingValue = ConfigurationManager.AppSettings[settingName];
                if (string.IsNullOrEmpty(settingValue))
                {
                    settingValue = Environment.GetEnvironmentVariable(settingName);
                }

                if (string.IsNullOrEmpty(settingValue))
                {
                    throw new InvalidOperationException("Could not find the value for setting " + settingName);
                }

                return settingValue;
            }

            public static string ReadConnectionString(string connectionStringName)
            {
                if (string.IsNullOrWhiteSpace(connectionStringName))
                {
                    throw new ArgumentNullException("settingName");
                }

                var connectionString = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                {
                    connectionString = Environment.GetEnvironmentVariable(connectionStringName);
                }

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Could not find the value for connection string " + connectionStringName);
                }

                return connectionString;
            }
        }

        public class StorageNames
        {
            public static string BenchmarkLogsContainerName
            {
                get
                {
                    return ConfigurationReader.ReadAppSetting("BenchmarkLogsContainerName");
                }
            }

            public static string BenchmarkDeviceInfoBlobPrefix
            {
                get
                {
                    return ConfigurationReader.ReadAppSetting("BenchmarkDeviceInfoBlobPrefix");
                }
            }

            public static string BenchmarkResultsBlobPrefix
            {
                get
                {
                    return ConfigurationReader.ReadAppSetting("BenchmarkResultsBlobPrefix");
                }
            }

            public static string BenchmarkResultsTable
            {
                get
                {
                    return ConfigurationReader.ReadAppSetting("BenchmarkResultsTable");
                }
            }

            public static string BenchmarksTable
            {
                get
                {
                    return ConfigurationReader.ReadAppSetting("BenchmarksTable");
                }
            }
        }

        private static class StorageAccount
        {
            public static CloudStorageAccount Create()
            {
                string connectionString;
                connectionString = ConfigurationReader.ReadConnectionString("AzureWebJobsStorage");
                return CloudStorageAccount.Parse(connectionString);
            }
        }

        private class LogController
        {
            private readonly CloudStorageAccount _storageAccount = StorageAccount.Create();

            //private async Task StoreResults(DeviceInformation deviceInfo, Stream results)
            public void StoreResults(DeviceInformation deviceInfo, Stream results)
            {
                string logName = GenerateUniqueLogName();
                string deviceInfoBlobName = logName + StorageNames.BenchmarkDeviceInfoBlobPrefix;
                string resultsBlobName = logName + StorageNames.BenchmarkResultsBlobPrefix;

                CloudBlobClient blobClient;
                blobClient = _storageAccount.CreateCloudBlobClient();
                CloudBlobContainer logsContainer;
                logsContainer = blobClient.GetContainerReference(StorageNames.BenchmarkLogsContainerName);

                //await logsContainer.CreateIfNotExistsAsync();
                logsContainer.CreateIfNotExists();

                // Upload the results first because the device blob is the one triggering the webjob
                CloudBlockBlob resultsBlob;
                resultsBlob = logsContainer.GetBlockBlobReference(resultsBlobName);

                //await resultsBlob.UploadFromStreamAsync(results);
                resultsBlob.UploadFromStream(results);

                CloudBlockBlob deviceInfoBlob;
                deviceInfoBlob = logsContainer.GetBlockBlobReference(deviceInfoBlobName);
                //await deviceInfoBlob.UploadTextAsync(JsonConvert.SerializeObject(deviceInfo));
                deviceInfoBlob.UploadText(JsonConvert.SerializeObject(deviceInfo));
            }

            private string GenerateUniqueLogName()
            {
                return Guid.NewGuid().ToString("N");
            }
        }
    }
}
