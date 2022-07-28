using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace SyntheticBenchmark_1
{

    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();
            var t= Bar();
            t.Wait();
            stopwatch.Stop();

            Console.WriteLine("Elapsed Time is {0} ms", stopwatch.ElapsedMilliseconds);          
        }

        // Case 1
        
            static async Task Bar() {
                var t1 = Foo();
                var t2 = f();

                await t1;
                await t2;
            }

            static async Task Foo() {
                var t = f();
                await t;
                Thread.Sleep(200);
                //await t;
                Thread.Sleep(200);
                //await t;
        }

            static async Task f() {
                var t = Task.Delay(300);
                Thread.Sleep(150);
                await t;
            }
        

        // Case 2
        /*
            static async Task Bar()
            {
                var t1 = Foo();

                await t1;
            }

            static async Task Foo()
            {
                var t = f();
                await t;
                Thread.Sleep(200);
                //await t;
            }

            static async Task f()
            {
                var t = Task.Delay(100).ConfigureAwait(false);
                await t;
            }
        */

        // Case 3
        /*
        static async Task Bar()
        {
            var t1 = Foo();
            var t2 = f();

            await t1;
            await t2;
        }

        static async Task Foo()
        {
            var t = f();
            await t;
            Thread.Sleep(100);
            //await t;
            Thread.Sleep(100);
            //await t;
        }

        static async Task f()
        {
            var t = Task.Delay(150).ConfigureAwait(false);
            
            await t;
        }
        */
        
    }
}
