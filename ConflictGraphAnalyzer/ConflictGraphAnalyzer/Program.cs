using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Diagnostics;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace ConflictGraphAnalyzer
{
    class Program
    {
        //public List<IMethod> SystemMethods;

        static void Main(string[] args)
        {
            // Attempt to set the version of MSBuild.
            var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();

            VisualStudioInstance instance;
            if (visualStudioInstances.Length > 1)
            {
                instance = visualStudioInstances[1];
            }
            else if (visualStudioInstances.Length == 1)
            {
                instance = visualStudioInstances[0];
            }
            else
            {
                throw new InvalidOperationException("Cannot find an instance of MSBuild");
            }

            //Console.WriteLine($"Using MSBuild at '{instance.MSBuildPath}' to load projects.");

            // NOTE: Be sure to register an instance with the MSBuildLocator 
            //       before calling MSBuildWorkspace.Create()
            //       otherwise, MSBuildWorkspace won't MEF compose.
            MSBuildLocator.RegisterInstance(instance);

            if (args.Length == 2)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();

                ConflictGraphs conflictGraphs = new ConflictGraphs(args);

                watch.Stop();

                long elapsedMs = watch.ElapsedMilliseconds;

                var settings = new JsonSerializerSettings()
                {
                    ContractResolver = new IgnorePropertiesResolver(true)
                };
                var json1 = JsonConvert.SerializeObject(conflictGraphs, settings);

                Console.WriteLine($"===============================");
                Console.WriteLine($"The properties are: '{json1}'");
                Console.WriteLine($"===============================");
            }
            else
            {
                Console.WriteLine($"===============================");
                Console.WriteLine($"Usage: .\\ConflictGraphAnalyzer\\ConflictGraphAnalyzer\\bin\\Debug\\net472\\ConflictGraphAnalyzer.exe ./Resources/SyntheticBenchmark-1/SyntheticBenchmark-1.sln 0");
                Console.WriteLine($"===============================");

                Console.WriteLine($"===============================");
                Console.WriteLine($"Results Reproduction");
                Console.WriteLine($"===============================");


                Console.WriteLine($"===============================");
                Console.WriteLine($"Assembly Path");
                var asmPath = Assembly.GetExecutingAssembly().Location;
                string projectDirPath = asmPath.Split(new string[] { "ConflictGraphAnalyzer\\ConflictGraphAnalyzer\\bin\\Debug" }, StringSplitOptions.None)[0];
                Console.WriteLine($"===============================");

                var inputs = new List<string[]> {
                    new string[3] { "SyntheticBenchmark-1", Path.Combine(projectDirPath, "Resources/SyntheticBenchmark-1/SyntheticBenchmark-1.sln"), "0"},
                    new string[3] { "SyntheticBenchmark-2", Path.Combine(projectDirPath, "Resources/SyntheticBenchmark-2/SyntheticBenchmark-2.sln"), "0"},
                    new string[3] { "SyntheticBenchmark-3", Path.Combine(projectDirPath, "Resources/SyntheticBenchmark-3/SyntheticBenchmark-3.sln"), "0"},
                    new string[3] { "SyntheticBenchmark-4", Path.Combine(projectDirPath, "Resources/SyntheticBenchmark-4/SyntheticBenchmark-4.sln"), "0"},
                    new string[3] { "SyntheticBenchmark-5", Path.Combine(projectDirPath, "Resources/SyntheticBenchmark-5/SyntheticBenchmark-5.sln"), "0"},
                    new string[3] { "Azure-Remote", Path.Combine(projectDirPath, "Resources/Azure-Remote/Azure-Remote.sln"), "0"},
                    new string[3] { "Azure-Webjobs", Path.Combine(projectDirPath, "Resources/Azure-Webjobs/Azure-Webjobs.sln"), "0"},
                    new string[3] { "FritzDectCore", Path.Combine(projectDirPath, "Resources/FritzDectCore/FritzDectCore.sln"), "0"},
                    new string[3] { "MultiPlatform", Path.Combine(projectDirPath, "Resources/MultiPlatform/MultiPlatform.sln"), "0"},
                    new string[3] { "NetRpc", Path.Combine(projectDirPath, "Resources/NetRpc/NetRpc.sln"), "0"},
                    new string[3] { "TestAZureBoards", Path.Combine(projectDirPath, "Resources/TestAZureBoards/TestAZureBoards.sln"), "0"},
                    new string[3] { "VBForums-Viewer", Path.Combine(projectDirPath, "Resources/VBForums-Viewer/VBForums-Viewer.sln"), "0"},
                    new string[3] { "Voat", Path.Combine(projectDirPath, "Resources/Voat/Voat.sln"), "6"},
                    new string[3] { "WordpressRESTClient", Path.Combine(projectDirPath, "Resources/WordpressRESTClient/WordpressRESTClient.sln"), "0"},
                    new string[3] { "ReadFile-Stackoverflow", Path.Combine(projectDirPath, "Resources/ReadFile-Stackoverflow/ReadFile-Stackoverflow.sln"), "0"},
                    new string[3] { "UI-Stackoverflow", Path.Combine(projectDirPath, "Resources/UI-Stackoverflow/UI-Stackoverflow.sln"), "0"}};

                Stopwatch watch;
                JsonSerializerSettings settings = new JsonSerializerSettings()
                {
                    ContractResolver = new IgnorePropertiesResolver(true)
                };

                long elapsedMs = 0;
                string json1;
                JObject json2;

                var outputs = new List<Tuple<string, long, JObject>>();
                //IEnumerable<Tuple<long, JObject>> outputs = new[];



                for (int i = 0; i < inputs.Count(); i++)
                {
                    watch = System.Diagnostics.Stopwatch.StartNew();

                    ConflictGraphs conflictGraphs = new ConflictGraphs(new string[] { inputs[i][1], inputs[i][2] });

                    watch.Stop();

                    elapsedMs = watch.ElapsedMilliseconds;

                    json1 = JsonConvert.SerializeObject(conflictGraphs, settings);
                    json2 = JObject.Parse(json1);

                    outputs.Add(Tuple.Create(inputs[i][0], elapsedMs, json2));
                }

                var printoutputs = outputs.AsEnumerable();

                Console.WriteLine(printoutputs.ToStringTable(new[] { "Application Name", "nbMethods", "nbInvocations", "nbAsyncInvocations", "nbPotentialMovableAwaits", "nbMovableAwaits", "nbRepairedDataRaces", "nbAsychronizations", "Time duration (ms)" },
                  a => a.Item1, a => a.Item3["nbMethods"], a => a.Item3["nbInvocations"], a => a.Item3["nbAsyncInvocations"], a => a.Item3["nbPotentialMovableAwaits"], a => a.Item3["nbMovableAwaits"], a => a.Item3["nbRepairedDataRaces"], a => a.Item3["nbAsychronizations"], a => a.Item3["runTime"]));

            }


        }         

    }


    public static class TableParser
    {
        public static string ToStringTable<T>(
          this IEnumerable<T> values,
          string[] columnHeaders,
          params Func<T, object>[] valueSelectors)
        {
            return ToStringTable(values.ToArray(), columnHeaders, valueSelectors);
        }

        public static string ToStringTable<T>(
          this T[] values,
          string[] columnHeaders,
          params Func<T, object>[] valueSelectors)
        {
            Debug.Assert(columnHeaders.Length == valueSelectors.Length);

            var arrValues = new string[values.Length + 1, valueSelectors.Length];

            // Fill headers
            for (int colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
            {
                arrValues[0, colIndex] = columnHeaders[colIndex];
            }

            // Fill table rows
            for (int rowIndex = 1; rowIndex < arrValues.GetLength(0); rowIndex++)
            {
                for (int colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
                {
                    arrValues[rowIndex, colIndex] = valueSelectors[colIndex]
                      .Invoke(values[rowIndex - 1]).ToString();
                }
            }

            return ToStringTable(arrValues);
        }

        public static string ToStringTable(this string[,] arrValues)
        {
            int[] maxColumnsWidth = GetMaxColumnsWidth(arrValues);
            var headerSpliter = new string('-', maxColumnsWidth.Sum(i => i + 3) - 1);

            var sb = new StringBuilder();
            for (int rowIndex = 0; rowIndex < arrValues.GetLength(0); rowIndex++)
            {
                for (int colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
                {
                    // Print cell
                    string cell = arrValues[rowIndex, colIndex];
                    cell = cell.PadRight(maxColumnsWidth[colIndex]);
                    sb.Append(" | ");
                    sb.Append(cell);
                }

                // Print end of line
                sb.Append(" | ");
                sb.AppendLine();

                // Print splitter
                if (rowIndex == 0)
                {
                    sb.AppendFormat(" |{0}| ", headerSpliter);
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        private static int[] GetMaxColumnsWidth(string[,] arrValues)
        {
            var maxColumnsWidth = new int[arrValues.GetLength(1)];
            for (int colIndex = 0; colIndex < arrValues.GetLength(1); colIndex++)
            {
                for (int rowIndex = 0; rowIndex < arrValues.GetLength(0); rowIndex++)
                {
                    int newLength = arrValues[rowIndex, colIndex].Length;
                    int oldLength = maxColumnsWidth[colIndex];

                    if (newLength > oldLength)
                    {
                        maxColumnsWidth[colIndex] = newLength;
                    }
                }
            }

            return maxColumnsWidth;

        }
    }
}
