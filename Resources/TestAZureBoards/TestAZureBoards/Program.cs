using System.Text;
using System.IO;

// From https://github.com/sumanbabum/TestAZureBoards/blob/master/snippets/csharp/VS_Snippets_VBCSharp/csasyncfileaccess/cs/class1.cs

namespace TestAZureBoards
{
    class Program
    {
        static void Main(string[] args)
        {
            Class1 c = new Class1();

            c.ProcessWrite();
        }

        class Class1
        {
            //<Snippet2>
            public void ProcessWrite()
            {
                string filePath;
                filePath = @"temp2.txt";

                string text;
                text = "Hello World\r\n";

                WriteText(filePath, text);
            }

            //private async Task WriteTextAsync(string filePath, string text)
            private void WriteText(string filePath, string text)
            {
                byte[] encodedText = Encoding.Unicode.GetBytes(text);

                using (FileStream sourceStream = new FileStream(filePath,
                    FileMode.Append, FileAccess.Write, FileShare.None,
                    bufferSize: 4096, useAsync: true))
                {
                    //await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
                    sourceStream.Write(encodedText, 0, encodedText.Length);
                };
            }
        }
    }
}
