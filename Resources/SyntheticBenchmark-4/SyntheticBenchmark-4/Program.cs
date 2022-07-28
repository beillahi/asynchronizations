using System.IO;
using System.Diagnostics;
using System.Net;


namespace SyntheticBenchmark_4
{

    class Program
    {

        static void Main(string[] args)
        {
            Y yY;
            X xX;

            xX = new X();

            yY = new Y();

            Foo(xX);

            Bar(yY);

            Foo(xX);
        }

        // Foo corresponds to Main in the running example of the paper
        static void Foo(X xX)
        {
            string url1, url11;
            string url2, url21;
            int val1, val11;
            int val2, val21;
            int r;

            url11 = xX.ReadFileX("url1.txt");

            url21 = xX.ReadFileX("url2.txt");

            url1 = url11;
            val11 = xX.ContentLength(url1);

            url2 = url21;
            val21 = xX.ContentLength(url2);

            r = xX.x;

            val1 = val11;

            val2 = val21;

            Debug.Assert(r == val1 + val2);
        }

        // Bar is another similar method that reads text directly from files and 
        // returns the lengths of what it read  
        static void Bar(Y yY)
        {
            int val1, val11;
            int val2, val21;

            yY.y = 1;

            val11 = yY.ReadFileY("file1.txt");

            val21 = yY.ReadFileY("file2.txt");

            yY.y = 0;

            val1 = val11;

            val2 = val21;

            Debug.Assert(val1 + val2 >= 2);
        }

        private class X
        {

            public int x;

            public string ReadFileX(string file)
            {
                StreamReader reader, reader2;
                string content, content2;

                reader = new StreamReader(file);

                reader2 = reader;

                content2 = reader2.ReadToEnd();

                content = content2;

                return content;
            }

            public int ContentLength(string url)
            {
                WebRequest request, request2;
                WebResponse response, response2;
                HttpWebResponse response3;
                Stream dataStream, dataStream2;
                StreamReader reader, reader2;
                string postsString, urlContents;
                int r1;

                request = WebRequest.Create(url);

                request2 = request;
                // Get the response.

                response = request2.GetResponse(); // exist GetResponseAsync

                response2 = response;

                response3 = (HttpWebResponse)response2;
                // Get the stream containing content returned by the server.

                dataStream = response3.GetResponseStream();  // exist GetResponseStreamAsync

                dataStream2 = dataStream;

                // Open the stream using a StreamReader for easy access.
                reader = new StreamReader(dataStream2);
                // Read the content.

                reader2 = reader;

                postsString = reader2.ReadToEnd();  // exist ReadToEndAsync

                r1 = x;

                urlContents = postsString;

                x = r1 + urlContents.Length;

                return urlContents.Length;
            }
        }

        private class Y
        {

            public int y;

            public int ReadFileY(string file)
            {
                int r, ret;
                string text, text2;
                StreamReader reader, reader2;

                ret = 0;

                reader2 = new StreamReader(file);

                reader = reader2;

                text2 = reader.ReadToEnd();

                r = y;

                if (r == 1)
                {
                    text = text2;

                    ret = text.Length + 1;
                }

                return ret;
            }
        }
    }
}
