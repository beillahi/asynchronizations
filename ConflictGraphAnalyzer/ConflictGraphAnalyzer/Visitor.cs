using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

// Install-Package Newtonsoft.Json -Version 12.0.3

namespace ConflictGraphAnalyzer
{
    class ConflictGraphs
    {

        private List<string> sysMethods;

        private List<List<string>> sysMethodsConflicts;

        private List<ConflictGraph> allConflictGraphs;

        private ConflictGraph gconflictGraph;

        public long runTime { get; set; }
        public int nbMethods { get; set; }
        public int nbInvocations { get; set; }
        public int nbAsyncInvocations { get; set; }
        public int nbPotentialMovableAwaits { get; set; }
        public int nbMovableAwaits { get; set; }
        public int nbRepairedDataRaces { get; set; }
        public int nbAsychronizations { get; set; }
        //public int nbAllAsychronizations { get; set; }
        public int distanceAwaits { get; set; }
        

        public ConflictGraphs(string[] args)
        {

            allConflictGraphs = new List<ConflictGraph>();

            nbAsychronizations = 0;
            //nbAllAsychronizations = 0;
            nbRepairedDataRaces = 0;
            nbMovableAwaits = 0;
            nbMethods = 0;
            nbInvocations = 0;
            nbAsyncInvocations = 0;
            nbPotentialMovableAwaits = 0;
            distanceAwaits = 0;
            runTime = 0;

            Setups();

            if (args.Length > 0)
            {
                ConflictGraph conflictGraph = new ConflictGraph();

                int iden = 0;

                if (args.Length > 1)
                {
                    try
                    {
                        iden = Int32.Parse(args[1]);
                        //Console.WriteLine($"The project Id is: '{iden}'");
                    }
                    catch (FormatException)
                    {
                       // Console.WriteLine($"Unable to parse '{args[1]}'");
                    }
                }

                conflictGraph.ProjectSetUp(args[0], iden, sysMethods, sysMethodsConflicts);

                var watch = System.Diagnostics.Stopwatch.StartNew();

                conflictGraph.GraphSetUp();

                nbMethods = conflictGraph.progMethods.Count;

                nbInvocations = conflictGraph.TotalNbInvocations();

                if (conflictGraph.isAsync)
                {
                    conflictGraph.DesynchronizeAsynchrony();
                    nbPotentialMovableAwaits = conflictGraph.NbPotentialMovableAwaits();
                    conflictGraph.RepairGraph();
                    distanceAwaits = conflictGraph.DistanceMovableAwaits();
                }
                else
                {
                    conflictGraph.Desynchronize();
                    nbPotentialMovableAwaits = conflictGraph.NbPotentialMovableAwaits();
                    conflictGraph.RepairGraph();
                    distanceAwaits = conflictGraph.DistanceMovableAwaits();
                }

                nbAsyncInvocations = conflictGraph.TotalAsyncNbInvocations();

                Debug.Assert(nbInvocations >= nbAsyncInvocations);

                //nbAllAsychronizations = nbAllAsychronizations + 1;

                gconflictGraph = conflictGraph.DeepCopy();

                nbRepairedDataRaces = nbRepairedDataRaces + gconflictGraph.nbRepairedDR;

                allConflictGraphs.Add(gconflictGraph);

                FindConflictGraphs(conflictGraph);

                nbAsychronizations = allConflictGraphs.Count;

                Debug.Assert(nbPotentialMovableAwaits >= nbMovableAwaits);

                watch.Stop();

                runTime = watch.ElapsedMilliseconds;
            }
            }

        public void FindConflictGraphs(ConflictGraph conflictGraph)
        {
            int skip = 0;

            ConflictGraph cpconflictGraph;
            cpconflictGraph = conflictGraph.DeepCopy();

            cpconflictGraph.nbRepairedDR = 0;
            cpconflictGraph.nbMovedAwait = 0;

            while (cpconflictGraph.MoveAwait(skip))
            {
                cpconflictGraph.RepairGraph();
                skip = skip + 1;
                //nbAllAsychronizations = nbAllAsychronizations + 1;
                //nbRepairedDataRaces = nbRepairedDataRaces + cpconflictGraph.nbRepairedDR;
                //allConflictGraphs.Add(cpconflictGraph);
                if (AddConflictGraph(cpconflictGraph))
                {
                    FindConflictGraphs(cpconflictGraph);
                }
                cpconflictGraph = conflictGraph.DeepCopy();
            }

            if (skip > nbMovableAwaits)
            {
                nbMovableAwaits = skip;
            }
        }

        public bool AddConflictGraph(ConflictGraph conflictGraph)
        {

            //int k = 0;
            bool equal = true;
            foreach (ConflictGraph cf in allConflictGraphs)
            {

                for (int i = 0; i < conflictGraph.cgMethods.Count(); i++)
                {
                    List<Tuple<Accesses, string, IOperation>> cp = new List<Tuple<Accesses, string, IOperation>>(conflictGraph.cgMethods[i].conflictUnit);

                    List<Tuple<Accesses, string, IOperation>> cp2 = new List<Tuple<Accesses, string, IOperation>>(cf.cgMethods[i].conflictUnit);

                    int index_await = 0;
                    foreach (Tuple<Accesses, string, IOperation> unit in cp)
                    {
                        if (unit.Item1 == Accesses.Await)
                        {
                            int index_invoc = -1;
                            for (int n = index_await; n > -1; n--)
                            {
                                if (cp[n].Item1 == Accesses.Invocation && cp[n].Item2 == unit.Item2)
                                {
                                    index_invoc = n;
                                    break;
                                }
                            }

                            int s = 0;
                            for (int j = index_invoc + 1; j < index_await; j++)
                            {
                                if (cp[j].Item1 == Accesses.Invocation ||
                                    (cp[j].Item1 == Accesses.Read) ||
                                    (cp[j].Item1 == Accesses.Write))
                                {
                                    s = s + 1;
                                }
                            }

                            int index_await2 = 0;
                            int s2 = 0;
                            foreach (Tuple<Accesses, string, IOperation> unit2 in cp2)
                            {
                                if (unit2.Item1 == Accesses.Await && unit2.Item2 == unit.Item2)
                                {
                                    int index_invoc2 = -1;
                                    for (int n = index_await; n > -1; n--)
                                    {
                                        if (cp2[n].Item1 == Accesses.Invocation && cp2[n].Item2 == unit2.Item2)
                                        {
                                            index_invoc2 = n;
                                            break;
                                        }
                                    }

                                    for (int j = index_invoc2 + 1; j < index_await2; j++)
                                    {
                                        if (cp2[j].Item1 == Accesses.Invocation ||
                                            (cp2[j].Item1 == Accesses.Read) ||
                                            (cp2[j].Item1 == Accesses.Write))
                                        {
                                            s2 = s2 + 1;
                                        }
                                    }
                                    continue;
                                }
                                index_await2 = index_await2 + 1;
                            }

                            if (s != s2)
                            {
                                equal = false;
                                continue;
                            }
                        }

                        if (equal == false)
                        {
                            continue;
                        }

                        index_await = index_await + 1;
                    }
                    if (equal == false)
                    {
                        continue;
                    }
                }

                if (equal == true)
                {
                    return false;
                }

                equal = true;
            }
            nbRepairedDataRaces = nbRepairedDataRaces + conflictGraph.nbRepairedDR;
            allConflictGraphs.Add(conflictGraph);
            return true;
        }

        private void Setups()
        {
            sysMethods = new List<string> { "Write", "CopyTo", "ReadLine", "ReadToEnd", "GetResponse", "GetResponseStream",
                "GetRequestStream", "UploadFromStream", "CreateIfNotExists",
                "UploadText" , "AddDeviceAsync", "GetTwinAsync", "UpdateTwinAsync"};

            sysMethodsConflicts = new List<List<string>>();

            sysMethodsConflicts.Add(new List<string>() { "Write" , "Close", "GetRequestStream"});  ///Write
            sysMethodsConflicts.Add(new List<string>() { "CopyTo", "Close" });  ///CopyTo
            sysMethodsConflicts.Add(new List<string>());  ///ReadLine
            sysMethodsConflicts.Add(new List<string>());  ///ReadToEnd
            sysMethodsConflicts.Add(new List<string>());  ///GetResponse
            sysMethodsConflicts.Add(new List<string>());  ///GetResponseStream
            sysMethodsConflicts.Add(new List<string>() { "Write", "Close" });  ///GetRequestStream
            sysMethodsConflicts.Add(new List<string>());  ///UploadFromStream
            sysMethodsConflicts.Add(new List<string> { "GetBlockBlobReference" });  ///CreateIfNotExists
            sysMethodsConflicts.Add(new List<string>());  ///UploadText
            sysMethodsConflicts.Add(new List<string>() { "UpdateTwinAsync", "GetTwinAsync" });  ///AddDeviceAsync
            sysMethodsConflicts.Add(new List<string>() { "UpdateTwinAsync", "AddDeviceAsync" });  ///GetTwinAsync
            sysMethodsConflicts.Add(new List<string>() { "GetTwinAsync", "AddDeviceAsync" });  ///UpdateTwinAsync
        }

    }

    public class IgnorePropertiesResolver : DefaultContractResolver
    {
        bool IgnoreBase = false;
        public IgnorePropertiesResolver(bool ignoreBase)
        {
            IgnoreBase = ignoreBase;
        }
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var allProps = base.CreateProperties(type, memberSerialization);
            if (!IgnoreBase) return allProps;

            //Choose the properties you want to serialize/deserialize
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            return allProps.Where(p => props.Any(a => a.Name == p.PropertyName)).ToList();
        }
    }
}
