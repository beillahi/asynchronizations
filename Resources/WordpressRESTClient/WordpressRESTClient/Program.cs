using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Net;

// From https://github.com/jaythe2012/WordpressRESTClient/blob/master/WordpressRESTClient/WordpressRESTClient/RESTClient.cs
// Install-Package Newtonsoft.Json -Version 12.0.3

namespace WordpressRESTClient
{
    class Program
    {

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                RESTClient rESTClient;
                rESTClient = new RESTClient(args[0]);

                string file1;
                file1 = rESTClient.GetFileName(1);

                string file2;
                file2 = rESTClient.GetFileName(2);

                string ffile1;
                ffile1 = file1;

                int val1;
                val1 = rESTClient.HandleFile(ffile1);

                string ffile2;
                ffile2 = file2;

                int val2;
                val2 = rESTClient.HandleFile(ffile2);

                int vval1, vval2;

                vval1 = val1;
                vval2 = val2;
                Debug.Assert(rESTClient.count == vval1 + vval2);
            }
        }

        private class RESTClient
        {
            private string hostName;
            public int count;

            public RESTClient(string host)
            {
                hostName = host;
                count = 0;
            }

            public string GetFileName(int limit = 0)
            {
                string url = hostName + "/wp-json/wp/v2/posts";

                if (limit != 0)
                    url = url + "?per_page=" + limit;

                WebRequest request;
                request = WebRequest.Create(url);

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

                Stream dataStream2;
                dataStream2 = dataStream;

                StreamReader reader;
                // Open the stream using a StreamReader for easy access.
                reader = new StreamReader(dataStream2);
                // Read the content.

                StreamReader reader2;
                reader2 = reader;

                string postsString;
                postsString = reader2.ReadToEnd();  // exist ReadToEndAsync

                string postsString2;
                postsString2 = postsString;

                return JsonConvert.DeserializeObject<string>(postsString2);
            }


            public int HandleFile(string file)
            {
                int local_count;
                local_count = count;

                int val;
                val = 0;

                using (StreamReader fileReader = new StreamReader(file))
                {
                    string v;
                    v = fileReader.ReadToEnd();

                    string vv;
                    vv = v;
                    val += vv.Length;
                    // ... A slow-running computation.
                    for (int i = 0; i < vv.Length; i++)
                    {
                        val = val + vv[i].GetHashCode();
                    }
                }

                local_count = local_count + val;
                count = local_count;
                return val;
            }
        }
    }
}
