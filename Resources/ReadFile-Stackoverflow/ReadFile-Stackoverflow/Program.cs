using System.IO;

// Insipred from https://stackoverflow.com/questions/28840059/race-condition-in-async-await-code

namespace ReadFile_Stackoverflow
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                Readfile readfile = new Readfile();

                readfile.ReadFile(args[0]);

                readfile.ReadFile(args[1]);
            }
        }

        private class Readfile
        {
            int readingFiles;

            public string ReadFile(string file)
            {
                int r;

                r = readingFiles;
                readingFiles = r + 1;

                var reader = new StreamReader(file);

                string text;
                text = reader.ReadToEnd();

                string ret;
                ret = text;
                //var text = await Stream.ReadFileAsync(file);

                r = readingFiles;
                readingFiles = r - 1;

                return ret;
            }
        }
    }
}
