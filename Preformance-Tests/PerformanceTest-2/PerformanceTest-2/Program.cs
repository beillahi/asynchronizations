using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace PerformanceTest_2
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();
            var t = Bar();
            t.Wait();
            stopwatch.Stop();

            Console.WriteLine("Elapsed Time is {0} ms", stopwatch.ElapsedMilliseconds);
        }

        // Case 2

        static async Task Bar()
        {
            var t1 = Foo();
            var t2 = IO();

            await t1;
            await t2;
        }

        static async Task Foo()
        {
            var t = IO();
            //await t;
            Thread.Sleep(200);
            await t;
            Thread.Sleep(200);
            //await t;
        }

        static async Task IO()
        {
            var t = Task.Delay(300);

            await t;
        }


    }
}
