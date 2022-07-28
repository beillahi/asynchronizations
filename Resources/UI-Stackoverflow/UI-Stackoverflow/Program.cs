using System.IO;

// Insipred from https://stackoverflow.com/questions/18476547/continuations-after-await-on-ui-thread-concurrency-data-races

namespace UI_Stackoverflow
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                UI ui = new UI();

                ui.btn_Click(args[0]);

                ui.btn2_Click(args[1]);
            }
        }

        private class UI
        {
            int x;

            public void btn_Click(string file)
            {
                int r;
                var reader = new StreamReader(file);
                string text;

                text = reader.ReadToEnd();

                r = x;

                x = r + 2;
            }

            public void btn2_Click(string file)
            {
                int r;
                var reader = new StreamReader(file);
                string text;

                text = reader.ReadToEnd();

                r = x;

                x = r + 3;
            }
        }
    }
}