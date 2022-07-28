using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Immutable;
using System.IO;
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
using System.Diagnostics;

namespace ConflictGraphAnalyzer
{
    enum Accesses
    {
        Write = 0,
        Read = 1,
        Invocation = 2,
        Await = 3
    }

    class ConflictGraph
    {
        public List<MethodAnalyzer> cgMethods;
        public List<string> cgSharedFields;
        public List<string> systemMethod;
        public List<List<string>> systemMethodConflicts;
        public List<string> progMethods;
        public int nbRepairedDR;
        public int nbMovedAwait;
        public bool isAsync;
        public SyntaxTree syntaxTree;
        public Compilation compilation;
        public string projectName;

        // TODO: ADD Conflict Graph constructor here
        public ConflictGraph()
        {
            cgMethods = new List<MethodAnalyzer>();
            progMethods = new List<string>();

            nbRepairedDR = 0;
            isAsync = false;
        }

        public void ProjectSetUp(string solutionPath, int iden, List<string> sysMethods, List<List<string>> sysMethodsConflicts)
        {         
            systemMethod = new List<string>(sysMethods);
            systemMethodConflicts = new List<List<string>>(sysMethodsConflicts);
            SetupsConfig(solutionPath, iden);
        }

        public void GraphSetUp()
        {
            BuildGraph();
            RankMethods();
        }


        public ConflictGraph DeepCopy()
        {
            ConflictGraph deepcopyConflictGraph = new ConflictGraph();

            foreach (var ele in this.cgMethods)
            {
                MethodAnalyzer newEle = new MethodAnalyzer(ele.methodName);
                newEle = ele.DeepCopy();
                deepcopyConflictGraph.cgMethods.Add(newEle);
            }

            deepcopyConflictGraph.systemMethod = new List<string>(this.systemMethod);
            deepcopyConflictGraph.systemMethodConflicts = new List<List<string>>(this.systemMethodConflicts);

            deepcopyConflictGraph.progMethods = new List<string>(this.progMethods);

            deepcopyConflictGraph.cgSharedFields = new List<string>(this.cgSharedFields);

            deepcopyConflictGraph.nbRepairedDR = this.nbRepairedDR;
            deepcopyConflictGraph.nbMovedAwait = this.nbMovedAwait;

            deepcopyConflictGraph.isAsync = this.isAsync;

            deepcopyConflictGraph.syntaxTree = this.syntaxTree;
            deepcopyConflictGraph.compilation = this.compilation;

            return deepcopyConflictGraph;
        }

        private void SetupsConfig(string solutionPath, int iden)
        {

            // Attempt to set the version of MSBuild.
            // var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();

            //VisualStudioInstance instance;
            //if (visualStudioInstances.Length > 1)
            //{
            //    instance = visualStudioInstances[1];
            //}
            //else
            //{
            //    instance = visualStudioInstances[0];
            //}

            //Console.WriteLine($"Using MSBuild at '{instance.MSBuildPath}' to load projects.");

            // NOTE: Be sure to register an instance with the MSBuildLocator 
            //       before calling MSBuildWorkspace.Create()
            //       otherwise, MSBuildWorkspace won't MEF compose.
            //MSBuildLocator.RegisterInstance(instance);


            using (var workspace = MSBuildWorkspace.Create())
            {
                // Print message for WorkspaceFailed event to help diagnosing project load failures.
                workspace.WorkspaceFailed += (o, e) => Console.WriteLine(e.Diagnostic.Message);

                //Console.WriteLine($"Loading solution '{solutionPath}'");

                // Attach progress reporter so we print projects as they are loaded.
                workspace.OpenSolutionAsync(solutionPath, new ConsoleProgressReporter()).Wait();
                var solution = workspace.OpenSolutionAsync(solutionPath, new ConsoleProgressReporter()).Result;
                //Console.WriteLine($"Finished loading solution '{solutionPath}'");

                // TODO: Do analysis on the projects in the loaded solution
                CSharpParseOptions options = CSharpParseOptions.Default
                .WithFeatures(new[] { new KeyValuePair<string, string>("control-flow-analysis", "") });

                var projIds = solution.ProjectIds;

                Project project;

                if (projIds.Count > 1 && projIds.Count > iden)
                {
                    project = solution.GetProject(projIds[iden]);
                }
                else
                {
                    project = solution.GetProject(projIds[0]);
                }

                projectName = project.Name;

                project.GetCompilationAsync().Wait();
                compilation = project.GetCompilationAsync().Result;

                if (compilation != null && !string.IsNullOrEmpty(compilation.AssemblyName))
                {
                    syntaxTree = compilation.SyntaxTrees.First();
                }
            }
        }

        public int TotalNbInvocations()
        {
            int ret = 0;
            foreach (MethodAnalyzer m in cgMethods)
            {
                foreach (var unit in m.conflictUnit)
                {
                    if(unit.Item1 == Accesses.Invocation)
                    {
                        ret = ret + 1;
                    }
                }
            }
            return ret;
        }

        public int TotalAsyncNbInvocations()
        {
            int ret = 0;
            foreach (MethodAnalyzer m in cgMethods)
            {
                foreach (var unit in m.conflictUnit)
                {
                    if (unit.Item1 == Accesses.Await)
                    {
                        ret = ret + 1;
                    }
                }
            }
            return ret;
        }

        public int DistanceMovableAwaits()
        {
            int ret2 = 0;
            foreach (MethodAnalyzer m in cgMethods)
            {
                int index_await = 0;
                foreach (var unit in m.conflictUnit)
                {
                    if (unit.Item1 == Accesses.Await)
                    {
                        int index_invoc = -1;
                        for (int n = index_await; n > -1; n--)
                        {
                            if (m.conflictUnit[n].Item1 == Accesses.Invocation && m.conflictUnit[n].Item2 == unit.Item2)
                            {
                                index_invoc = n;
                                break;
                            }
                        }
                        if (index_invoc == -1)
                        {
                            foreach (Tuple<string, string> ele0 in m.awaitInvocRel)
                            {
                                if (unit.Item2 == ele0.Item2)
                                {
                                    for (int n = index_await; n > -1; n--)
                                    {
                                        if (m.conflictUnit[n].Item1 == Accesses.Invocation && m.conflictUnit[n].Item2 == ele0.Item1)
                                        {
                                            index_invoc = n;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        Debug.Assert(index_invoc >= 0);

                        Debug.Assert(index_await >= index_invoc + 1);


                        int i = -1;
                        int ii = 0;
                        for (int j = index_invoc + 1; j < index_await; j++)
                        {
                            if ((m.conflictUnit[j].Item1 == Accesses.Read) ||
                                (m.conflictUnit[j].Item1 == Accesses.Write))
                            {
                                i = j;
                                ii = ii + 1;
                            }
                            if (m.conflictUnit[j].Item1 == Accesses.Invocation)
                            {
                                string name = m.conflictUnit[j].Item2;
                                int k = 1;
                                i = j;
                                var mymeth = FindMethod(name);
                                if (mymeth != null)
                                {
                                    if (mymeth.conflictUnit.Count() > 0)
                                    {
                                        k = mymeth.conflictUnit.Count();
                                    }
                                }
                                ii = ii + k;
                            }
                        }

                        if (i != -1)
                        {
                            ret2 = ret2 + ii;
                        }
                    }
                    index_await = index_await + 1;
                }
            }
            return ret2;
        }

        public int NbPotentialMovableAwaits()
        {
            int ret = 0;
            foreach (MethodAnalyzer m in cgMethods)
            {
                int index_await = 0;
                foreach (var unit in m.conflictUnit)
                {
                    if (unit.Item1 == Accesses.Await)
                    {
                        int index_invoc = -1;
                        for (int n = index_await; n > -1; n--)
                        {
                            if (m.conflictUnit[n].Item1 == Accesses.Invocation && m.conflictUnit[n].Item2 == unit.Item2)
                            {
                                index_invoc = n;
                                break;
                            }
                        }
                        if (index_invoc == -1)
                        {
                            foreach (Tuple<string, string> ele0 in m.awaitInvocRel)
                            {
                                if (unit.Item2 == ele0.Item2)
                                {
                                    for (int n = index_await; n > -1; n--)
                                    {
                                        if (m.conflictUnit[n].Item1 == Accesses.Invocation && m.conflictUnit[n].Item2 == ele0.Item1)
                                        {
                                            index_invoc = n;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        Debug.Assert(index_invoc >= 0);

                        Debug.Assert(index_await >= index_invoc + 1);


                        int i = -1;
                        for (int j = index_invoc + 1; j < index_await; j++)
                        {
                            if ((m.conflictUnit[j].Item1 == Accesses.Invocation) || 
                                (m.conflictUnit[j].Item1 == Accesses.Read ) ||
                                (m.conflictUnit[j].Item1 == Accesses.Write))
                            {
                                i = j;
                            }
                        }

                        if (i != -1)
                        {
                            ret = ret + 1;
                        }
                    }
                    index_await = index_await + 1;
                }
            }
            return ret;
        }


        private MethodAnalyzer FindMethod(string methName)
        {
            foreach (MethodAnalyzer m in cgMethods)
            {
                if (m.methodName  == methName)
                {
                    return m;
                }
            }
            return null;
        }

        private int FindInstruction(string returnReg, List<Tuple<Accesses, string, IOperation>> confUnit, List<Tuple<string, string>> invAwaitRel, Accesses accessType, int rank = 0)
        {
            int j = 0;
            foreach (Tuple<Accesses, string, IOperation> ele in confUnit)
            {
                if (ele.Item1 == accessType && ele.Item2 == returnReg && j >= rank)
                {
                    return j;
                }
                j = j + 1;
            }
            if (accessType ==  Accesses.Await)
            {
                foreach (Tuple<string, string> ele0 in invAwaitRel)
                {
                    if (returnReg == ele0.Item1)
                    {
                        j = 0;
                        foreach (Tuple<Accesses, string, IOperation> ele in confUnit)
                        {
                            if (ele.Item1 == accessType && ele.Item2 == ele0.Item2 && j >= rank)
                            {
                                return j;
                            }
                            j = j + 1;
                        }
                    }
                }
            }
            return -1;
        }

        private int FindDependentInstruction(string returnReg, MethodAnalyzer meth, int index_await)
        {
            int j = 0;

            string invocArg0 = meth.FindInvokedArgument(returnReg);

            string invocMeth0 = meth.FindInvokedMethod(returnReg);

            foreach (Tuple<Accesses, string, IOperation> ele in meth.conflictUnit)
            {
                if (j > index_await)
                {
                    if (ele.Item1 == Accesses.Read  && ele.Item2 == returnReg )
                    {
                        return j;
                    }
                    else if ((ele.Item1 == Accesses.Read || ele.Item1 == Accesses.Write) && ele.Item2 == invocArg0)
                    {
                        return j;
                    }
                    else if (ele.Item1 == Accesses.Invocation && ele.Item2 != returnReg)
                    {
                        string invocArg1 = meth.FindInvokedArgument(ele.Item2);
                        string invocMeth2 = meth.FindInvokedMethod(ele.Item2);
                        if (progMethods.IndexOf(invocMeth0) == -1 && progMethods.IndexOf(invocMeth2) == -1)
                        {
                            if (invocArg1 == invocArg0 || invocArg1 == returnReg)
                            {
                                return j;
                            }
                        }
                    }
                }
                j = j + 1;
            }
            return j;
        }



        private void BuildGraph()
        {
            // get syntax nodes for methods
            var methodNodes = from methodDeclaration in syntaxTree.GetRoot().DescendantNodes()
                       .Where(x => x is MethodDeclarationSyntax)
                select methodDeclaration;

            int cnt = methodNodes.Count();

            // get syntax nodes for fields
            var fieldNodes =  from FieldDeclaration in syntaxTree.GetRoot().DescendantNodes()
                       .Where(x => x is FieldDeclarationSyntax)
                     select FieldDeclaration;

            List<string> sharedFields = new List<string>();
            foreach (FieldDeclarationSyntax field in fieldNodes)
            {
                if (field.Declaration.Variables.Count == 1)
                {
                    sharedFields.Add(field.Declaration.Variables[0].Identifier.ToString());
                }
            }

            cgSharedFields = new List<string>(sharedFields);

            foreach (MethodDeclarationSyntax node in methodNodes)
            {
                var model = compilation.GetSemanticModel(node.SyntaxTree);
                var methodName = node.Identifier.ToString();

                progMethods.Add(methodName);

                //Console.Write(typeof(string).Assembly.ImageRuntimeVersion);
                var graph = ControlFlowGraph.Create(node, model); // CFG is here
                ImmutableArray<BasicBlock> bb = graph.Blocks;

                MethodAnalyzer methodAnalyzer = new MethodAnalyzer(methodName);

                methodAnalyzer.SetUp(bb, sharedFields);

                if (methodAnalyzer.isAsync && ! this.isAsync)
                {
                    this.isAsync = true;
                }
                cgMethods.Add(methodAnalyzer);
            }

        }



        private void RankMethods()
        {
            List<MethodAnalyzer> copy = new List<MethodAnalyzer>(cgMethods);
            List<MethodAnalyzer> scopy = new List<MethodAnalyzer>();
            foreach (MethodAnalyzer m in copy)
            {
                int i = 0;
                int j = 0;
                foreach (MethodAnalyzer m2 in scopy)
                {
                    
                    foreach (Tuple<string, string, string, IOperation> ele in m.InvokedMethods)
                    {
                        if(ele.Item1.Equals(m2.methodName))
                        {
                            i = j+1;
                            break;
                        }      
                    }
                    j = j + 1;
                }
                scopy.Insert(i, m);
            }
            cgMethods = scopy;
        }


        // Move an await by a single step
        // the await to move is determined by the index skip
        // which indicate how many movable awaits to skip before the one 
        // we want to move
        public bool MoveAwait(int skip)
        {
            int cpSkip = skip;
            List<MethodAnalyzer> copy = new List<MethodAnalyzer>(cgMethods);

            int k = 0;
            foreach (MethodAnalyzer m in copy)
            {
                List<Tuple<Accesses, string, IOperation>> cp = new List<Tuple<Accesses, string, IOperation>>(m.conflictUnit);

                int index_await = 0;
                foreach (Tuple<Accesses, string, IOperation> unit in m.conflictUnit)
                {
                    if (unit.Item1 == Accesses.Await)
                    {
                        int index_invoc = -1;
                        for (int n = index_await; n > -1; n--)
                        {
                            if(cp[n].Item1 == Accesses.Invocation && cp[n].Item2 == unit.Item2)
                            {
                                index_invoc = n;
                                break;
                            }
                        }
                        if (index_invoc == -1)
                        {
                            foreach (Tuple<string, string> ele0 in m.awaitInvocRel)
                            {
                                if (unit.Item2 == ele0.Item2)
                                {
                                    for (int n = index_await; n > -1; n--)
                                    {
                                        if (cp[n].Item1 == Accesses.Invocation && cp[n].Item2 == ele0.Item1)
                                        {
                                            index_invoc = n;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        int i = -1;
                        for (int j = index_invoc + 1; j < index_await; j++)
                        {
                            if (cp[j].Item1 == Accesses.Invocation || 
                                (cp[j].Item1 == Accesses.Read && cgSharedFields.IndexOf(cp[j].Item2) != -1) ||
                                (cp[j].Item1 == Accesses.Write && cgSharedFields.IndexOf(cp[j].Item2) != -1))
                            {
                                i = j;
                            }
                        }

                        if (i != -1 && cpSkip > 0)
                        {
                            cpSkip = cpSkip - 1;

                        }
                        else if (i != -1 && cpSkip == 0)
                        {
                            var temp = cp[index_await];
                            cp.RemoveAt(index_await);
                            cp.Insert(i, temp);                  
                            m.conflictUnit = cp;
                            cgMethods[k] = m;
                            return true;
                        }
                    }
                    index_await  = index_await + 1;
                }
                k = k + 1;
            }
            return false;
        }

        public void DesynchronizeAsynchrony()
        {
            nbMovedAwait = 0;
            List<MethodAnalyzer> copy = new List<MethodAnalyzer>(cgMethods);

            int k = 0;
            foreach (MethodAnalyzer m in copy)
            {
                List<Tuple<Accesses, string, IOperation>> cp = new List<Tuple<Accesses, string, IOperation>>(m.conflictUnit);
                //int q = 0;
                foreach (Tuple<Accesses, string, IOperation> unit in m.conflictUnit)
                {
                    if (unit.Item1 == Accesses.Await)
                    {
                        string invokedM = m.FindInvokedMethod(unit.Item2);

                        int indexInvokedMeth = systemMethod.IndexOf(invokedM);

                        int index_await = FindInstruction(unit.Item2, cp, m.awaitInvocRel, Accesses.Await);

                        int index_read;
                        //if (systemMethod.IndexOf(invokedM) != -1)
                        //{
                        //    index_read = FindInstruction(unit.Item2, cp, Accesses.Read);
                        //}

                        index_read = FindDependentInstruction(unit.Item2, m, index_await);

                        int i = -1;
                        for (int j = index_await + 1; j < index_read; j++)
                        {
                            if ((cp[j].Item1 == Accesses.Invocation && indexInvokedMeth == -1) ||
                                (cp[j].Item1 == Accesses.Read && cgSharedFields.IndexOf(cp[j].Item2) != -1) ||
                                (cp[j].Item1 == Accesses.Write && cgSharedFields.IndexOf(cp[j].Item2) != -1) )
                            {
                                i = j;
                            }
                            
                            else if (cp[j].Item1 == Accesses.Invocation && indexInvokedMeth != -1)
                            {
                                string invoc = m.FindInvokedMethod(cp[j].Item2);


                                if (systemMethodConflicts[indexInvokedMeth].IndexOf(invoc) == -1)
                                {
                                    i = j;
                                }

                            }
                            
                        }

                        if (i != -1)
                        {   cp.Insert(i + 1, cp[index_await]);
                            cp.RemoveAt(index_await);
                            nbMovedAwait = nbMovedAwait + 1;
                        }
                    }
                    //q = q + 1;
                }
                m.conflictUnit = cp;     
                cgMethods[k] = m;
                k = k + 1;
            }
        }

        public void Desynchronize()
        {
            List<MethodAnalyzer> copy = new List<MethodAnalyzer>(cgMethods);

            int k = 0;
            foreach (MethodAnalyzer m in copy)
            {
                int i = 0;
                foreach(Tuple<string, string, string, IOperation> tuple in m.InvokedMethods)
                {
                    var invokedMeth = tuple.Item1;

                    int indexInvokedMeth = systemMethod.IndexOf(invokedMeth);

                    if (indexInvokedMeth != -1)
                    {
                        int j = 0;
                        List<Tuple<Accesses, string, IOperation>> cp = new List<Tuple<Accesses, string, IOperation>>(m.conflictUnit);
                        foreach (Tuple<Accesses, string, IOperation> unit in m.conflictUnit)
                        {
                            if (j > i) // await should be after invocation
                            {
                                if ((unit.Item2 == tuple.Item2 || unit.Item2 == tuple.Item3) && (unit.Item1 == Accesses.Read || unit.Item1 == Accesses.Write))
                                {
                                    cp.Insert(j, new Tuple<Accesses, string, IOperation>(Accesses.Await, tuple.Item2, null));
                                    break;
                                }
                                
                                else if (unit.Item1 == Accesses.Invocation && tuple.Item3 != null && unit.Item2 != tuple.Item2)
                                {
                                    string invoc = m.FindInvokedMethod(unit.Item2);
                                    if (progMethods.IndexOf(invoc) == -1 && systemMethodConflicts[indexInvokedMeth].IndexOf(invoc) != -1)
                                    {
                                        string invocArg1 = m.FindInvokedArgument(unit.Item2);
                                        if (invocArg1 == tuple.Item3)
                                        {
                                            cp.Insert(j, new Tuple<Accesses, string, IOperation>(Accesses.Await, tuple.Item2, null));
                                            break;
                                        }
                                    }

                                }
                                
                            }
                            j = j + 1;
                        }
                        // if no one read the return of the await then the await is placed the last
                        if (j == m.conflictUnit.Count)
                        {
                            cp.Add(new Tuple<Accesses, string, IOperation>(Accesses.Await, tuple.Item2, null));
                        }

                        m.conflictUnit = new List<Tuple<Accesses, string, IOperation>>(cp);
                        m.isAsync = true;
                    }
                    else if(FindMethod(invokedMeth) != null)
                    {
                        var mEle = FindMethod(invokedMeth);
                        if (mEle.isAsync)
                        {
                            int j = 0;
                            List<Tuple<Accesses, string, IOperation>> cp = new List<Tuple<Accesses, string, IOperation>>(m.conflictUnit);
                            foreach (Tuple<Accesses, string, IOperation> unit in m.conflictUnit)
                            {
                                if (j > i) // await should be after invocation
                                {
                                    if (unit.Item2 == tuple.Item2 && (unit.Item1 == Accesses.Read || unit.Item1 == Accesses.Write))
                                    {
                                        cp.Insert(j, new Tuple<Accesses, string, IOperation>(Accesses.Await, tuple.Item2, null));
                                        break;
                                    }
                                }
                                j = j + 1;
                            }

                            // if no one read the return of the await then the await is placed the last
                            if (j == m.conflictUnit.Count)
                            {
                                cp.Add(new Tuple<Accesses, string, IOperation>(Accesses.Await, tuple.Item2, null));
                            }

                            m.conflictUnit = new List<Tuple<Accesses, string, IOperation>>(cp);
                            m.isAsync = true;
                        }

                    }
                    i = i + 1;
                }

                cgMethods[k] = m;
                k = k + 1;
            }
        }

        public void RepairGraph()
        {
            nbRepairedDR = 0 ;
            for (var i = 0; i < cgMethods.Count; i++)
            {
                RepairMethod(i);
            }
        }

        private void RepairMethod(int methIndex)
        {
            if (methIndex > -1 && methIndex < cgMethods.Count)
            {
                MethodAnalyzer cpMeth = cgMethods[methIndex];

                //int j = 0;
                // int cnt = cpMeth.conflictUnit.Count;
                List<Tuple<Accesses, string, IOperation>> cp = new List<Tuple<Accesses, string, IOperation>>(cpMeth.conflictUnit);
                foreach (var ele in cp)
                {
                    int dr = -1;

                    if (ele.Item1 == Accesses.Invocation)
                    {
                        string methName = cpMeth.FindInvokedMethod(ele.Item2);
                        MethodAnalyzer newMeth = FindMethod(methName);

                        if (newMeth != null)
                        {
                            if (newMeth.isAsync)
                            {
                                int j = FindInstruction(ele.Item2, cpMeth.conflictUnit, cpMeth.awaitInvocRel, Accesses.Invocation);
                                int cnt = FindInstruction(ele.Item2, cpMeth.conflictUnit, cpMeth.awaitInvocRel, Accesses.Await); // position of the associated await 

                                List<Tuple<Accesses, string, IOperation>> confUnit1 =
                                      new List<Tuple<Accesses, string, IOperation>>(cpMeth.conflictUnit.GetRange(j + 1, cnt - j - 1));

                                var confUnit = newMeth.conflictUnit;
                                int i = 0;
                                string reg = "";
                                foreach (var ele2 in confUnit)
                                {
                                    if (ele2.Item1 == Accesses.Await)
                                    {
                                        reg = ele2.Item2;
                                        break;
                                    }
                                    i = i + 1;
                                }


                                int cnt2 = confUnit.Count;
                                List<Tuple<Accesses, string, IOperation>> confUnit2 =
                                       new List<Tuple<Accesses, string, IOperation>>(confUnit.GetRange(i + 1, cnt2 - i - 1));

                                foreach (var ele2 in confUnit)
                                {
                                    if (ele2.Item1 == Accesses.Invocation && systemMethod.IndexOf(newMeth.FindInvokedMethod(reg)) != -1)
                                    {
                                        confUnit2.Insert(0, ele2);
                                        break;
                                    }
                                    i = i + 1;
                                }


                                dr = IsThereDataRace(cpMeth, confUnit1, newMeth, confUnit2);

                                if (dr != -1)
                                {
                                    var temp1 = cpMeth.conflictUnit[cnt];             // await position
                                                                                      //var temp2 = cpMeth.conflictUnit[j + 1 + dr];       // conflict position to repair
                                    cpMeth.conflictUnit.RemoveAt(cnt);
                                    cpMeth.conflictUnit.Insert(j + 1 + dr, temp1);
                                    //cpMeth.conflictUnit[j + 1 + dr] = temp1;
                                    //cpMeth.conflictUnit[cnt] = temp2;

                                    nbRepairedDR = nbRepairedDR + 1;               // increment the count of nb repaired data races
                                }
                            }
                        }
                        else if (systemMethod.IndexOf(methName) != -1)
                        {
                            int j = FindInstruction(ele.Item2, cpMeth.conflictUnit, cpMeth.awaitInvocRel, Accesses.Invocation);
                            int cnt = FindInstruction(ele.Item2, cpMeth.conflictUnit, cpMeth.awaitInvocRel, Accesses.Await); // position of the associated await 

                            List<Tuple<Accesses, string, IOperation>> confUnit1 =
                                  new List<Tuple<Accesses, string, IOperation>>(cpMeth.conflictUnit.GetRange(j + 1, cnt - j - 1));

                            //int index = systemMethod.IndexOf(methName);

                            List<Tuple<Accesses, string, IOperation>> confUnit2 =
                                       new List<Tuple<Accesses, string, IOperation>> { ele };

                            dr = IsThereDataRace(cpMeth, confUnit1, cpMeth, confUnit2);

                            if (dr != -1)
                            {
                                var temp1 = cpMeth.conflictUnit[cnt];             // await position
                                                                                  //var temp2 = cpMeth.conflictUnit[j + 1 + dr];       // conflict position to repair
                                cpMeth.conflictUnit.RemoveAt(cnt);
                                cpMeth.conflictUnit.Insert(j + 1 + dr, temp1);
                                //cpMeth.conflictUnit[j + 1 + dr] = temp1;
                                //cpMeth.conflictUnit[cnt] = temp2;

                                nbRepairedDR = nbRepairedDR + 1;               // increment the count of nb repaired data races
                            }
                        }

                    }
                    //j = j + 1;
                 }
                cgMethods[methIndex] = cpMeth;
            }
        }

        private int IsThereDataRace(MethodAnalyzer meth1, List<Tuple<Accesses, string, IOperation>> conflictUnit1,
                                    MethodAnalyzer meth2, List<Tuple<Accesses, string, IOperation>> conflictUnit2)
        {
            int j = 0;
            string meth2Name;
            int sysm2Index = -1;

            if (conflictUnit2.Count == 1 && conflictUnit2[0].Item1 == Accesses.Invocation)
            {
                meth2Name = meth2.FindInvokedMethod(conflictUnit2[0].Item2);
                if(systemMethod.IndexOf(meth2Name) != -1)
                {
                    sysm2Index = systemMethod.IndexOf(meth2Name);
                }
            }

            foreach (var ele in conflictUnit1)
            {
                if (ele.Item1 == Accesses.Invocation)
                {
                    string methName = meth1.FindInvokedMethod(ele.Item2);
                    MethodAnalyzer newMeth = FindMethod(methName);
                    if (newMeth != null)
                    {
                        var confUnit = newMeth.conflictUnit;
                        var k = IsThereDataRace(newMeth, confUnit, meth2, conflictUnit2);
                        if (k != -1)
                        {
                            return j;
                        }
                    }
                    else if(progMethods.IndexOf(methName) == -1 && sysm2Index != -1)
                    {
                        if(systemMethodConflicts[sysm2Index].IndexOf(methName) != -1)
                        {
                            string invocArg0 = meth2.FindInvokedArgument(conflictUnit2[0].Item2);

                            string invocArg1 = meth1.FindInvokedArgument(ele.Item2);

                            if (invocArg0 == invocArg1 && invocArg0 != null)
                            {
                                return j;
                            }
                        }
                    }
                    else if (progMethods.IndexOf(methName) == -1 && sysm2Index == -1)
                    {
                        string invocArg1 = meth1.FindInvokedArgument(ele.Item2);

                        foreach (var ele2 in conflictUnit2)
                        {
                            if (ele2.Item1 == Accesses.Invocation)
                            {
                                string methName2 = meth2.FindInvokedMethod(ele2.Item2);
                                MethodAnalyzer newMeth2 = FindMethod(methName2);
                                if (newMeth2 != null)
                                {
                                    var confUnit2 = newMeth2.conflictUnit;
                                    var confUnit1 = new List<Tuple<Accesses, string, IOperation>>();
                                    confUnit1.Add(ele);
                                    var k = IsThereDataRace(meth1, confUnit1, newMeth2, confUnit2);
                                    if (k != -1)
                                    {
                                        return j;
                                    }
                                }
                                else if (systemMethod.IndexOf(methName2) != -1)
                                {
                                    string invocArg0 = meth2.FindInvokedArgument(ele2.Item2);

                                    if (invocArg0 == invocArg1 && systemMethodConflicts[systemMethod.IndexOf(methName2)].IndexOf(methName) != -1)
                                    {
                                        return j;
                                    }
                                }
                            }
                            else if (ele2.Item1 == Accesses.Write && ele2.Item2 == invocArg1)
                            {
                                return j;
                            }
                            else if (ele2.Item1 == Accesses.Read && ele2.Item2 == invocArg1)
                            {
                                return j;
                            }
                        }
                    }
                }
                else if (ele.Item1 == Accesses.Read)
                {
                    if (sysm2Index == -1)
                    {
                        foreach (var ele2 in conflictUnit2)
                        {
                            if (ele2.Item1 == Accesses.Invocation)
                            {
                                string methName2 = meth2.FindInvokedMethod(ele2.Item2);
                                MethodAnalyzer newMeth2 = FindMethod(methName2);
                                if (newMeth2 != null)
                                {
                                    var confUnit2 = newMeth2.conflictUnit;
                                    var confUnit1 = new List<Tuple<Accesses, string, IOperation>>();
                                    confUnit1.Add(ele);
                                    var k = IsThereDataRace(meth1, confUnit1, newMeth2, confUnit2);
                                    if (k != -1)
                                    {
                                        return j;
                                    }
                                }
                                // should this be any system method or only the ones with asynchronous counter parts
                                else if (progMethods.IndexOf(methName2) == -1)
                                {
                                    string invocArg0 = meth2.FindInvokedArgument(ele2.Item2);

                                    if (invocArg0 == ele.Item2)
                                    {
                                            return j;
                                    }
                                }
                            }
                            else if (ele2.Item1 == Accesses.Write && ele2.Item2 == ele.Item2)
                            {
                                return j;
                            }
                        }
                    }
                }
                else if (ele.Item1 == Accesses.Write)
                {
                    if (sysm2Index == -1)
                    {
                        foreach (var ele2 in conflictUnit2)
                        {
                            if (ele2.Item1 == Accesses.Invocation)
                            {
                                string methName2 = meth2.FindInvokedMethod(ele2.Item2);
                                MethodAnalyzer newMeth2 = FindMethod(methName2);
                                if (newMeth2 != null)
                                {
                                    var confUnit2 = newMeth2.conflictUnit;
                                    var confUnit1 = new List<Tuple<Accesses, string, IOperation>>();
                                    confUnit1.Add(ele);
                                    var k = IsThereDataRace(meth1, confUnit1, newMeth2, confUnit2);
                                    if (k != -1)
                                    {
                                        return j;
                                    }
                                }
                                else if (progMethods.IndexOf(methName2) == -1)
                                {
                                    string invocArg0 = meth2.FindInvokedArgument(ele2.Item2);

                                    if (invocArg0 == ele.Item2)
                                    {
                                        return j;
                                    }
                                }
                            }
                            else if (ele2.Item1 == Accesses.Write && ele2.Item2 == ele.Item2)
                            {
                                return j;
                            }
                            else if (ele2.Item1 == Accesses.Read && ele2.Item2 == ele.Item2)
                            {
                                return j;
                            }
                        }
                    }
                }
                j = j + 1;
            }

            return -1;
        }


    private class ConsoleProgressReporter : IProgress<ProjectLoadProgress>
        {
            public void Report(ProjectLoadProgress loadProgress)
            {
                var projectDisplay = Path.GetFileName(loadProgress.FilePath);
                if (loadProgress.TargetFramework != null)
                {
                    projectDisplay += $" ({loadProgress.TargetFramework})";
                }

               // Console.WriteLine($"{loadProgress.Operation,-15} {loadProgress.ElapsedTime,-15:m\\:ss\\.fffffff} {projectDisplay}");
            }
        }
    }
}
