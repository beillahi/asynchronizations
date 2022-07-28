using System.IO;
using System.Diagnostics;


namespace SyntheticBenchmark_1
{

    class Program
    {

        static void Main(string[] args)
        {
            Y yY;

            yY = new Y();

            Bar(yY);

            Bar(yY);
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
