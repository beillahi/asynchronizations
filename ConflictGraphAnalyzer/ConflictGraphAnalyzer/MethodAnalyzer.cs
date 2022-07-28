using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace ConflictGraphAnalyzer
{
    class MethodAnalyzer
    {
        // TODO: ADD Methods parser here
        ///public List<string> InvokedMethods;
        public List<Tuple<string, string, string, IOperation>> InvokedMethods;
        public List<string> methodInvocReturnRegs;
        public List<Tuple<string, string>> awaitInvocRel;
        public List<Tuple<Accesses, string, IOperation>> conflictUnit;
        public string methodName;
        public bool isAsync;
        private string invocSuffix;

        public MethodAnalyzer(string mName)
        {
            methodName = mName;
            InvokedMethods = new List<Tuple<string, string, string, IOperation>>();
            conflictUnit = new List<Tuple<Accesses, string, IOperation>>();
            methodInvocReturnRegs = new List<string>();
            awaitInvocRel = new List<Tuple<string, string>>();
            isAsync = false;
            invocSuffix = "invoSuffix000";
        }


        public void SetUp(ImmutableArray<BasicBlock> bb, List<string> sharedFields)
        {
            MethodSummary(bb, sharedFields);
        }


        public MethodAnalyzer DeepCopy()
        {
            MethodAnalyzer deepcopyMethodAnalyzer = new MethodAnalyzer(this.methodName);

            foreach (var ele in this.InvokedMethods)
                deepcopyMethodAnalyzer.InvokedMethods.Add(new Tuple<string, string, string, IOperation> (ele.Item1, ele.Item2, ele.Item3, ele.Item4));
            foreach (var ele in this.conflictUnit)
                deepcopyMethodAnalyzer.conflictUnit.Add(new Tuple<Accesses, string, IOperation>(ele.Item1, ele.Item2, ele.Item3));
            foreach (var ele in this.awaitInvocRel)
                deepcopyMethodAnalyzer.awaitInvocRel.Add(new Tuple<string, string>(ele.Item1, ele.Item2));
            foreach (var ele in this.methodInvocReturnRegs)
                deepcopyMethodAnalyzer.methodInvocReturnRegs.Add(ele);
            deepcopyMethodAnalyzer.isAsync = this.isAsync;
            deepcopyMethodAnalyzer.invocSuffix = this.invocSuffix;

            return deepcopyMethodAnalyzer;
        }

        public string FindInvokedMethod(string regName)
        {
            foreach (Tuple<string, string, string, IOperation> tuple in InvokedMethods)
            {
                if (tuple.Item2 == regName)
                {
                    return tuple.Item1;
                }
            }
            return null;
        }


        public string FindInvokedArgument(string regName)
        {
            foreach (Tuple<string, string, string, IOperation> tuple in InvokedMethods)
            {
                if (tuple.Item2 == regName)
                {
                    return tuple.Item3;
                }
            }
            return null;
        }

        private void MethodSummary(ImmutableArray<BasicBlock> bb, List<string> sharedFields)
        {
            foreach (BasicBlock b in bb)
            {
                var a = b.GetType();
                var c = b.BranchValue;

                if (b.Operations != null)
                {
                    foreach (IOperation inst in b.Operations)
                    {
                        //Console.WriteLine($"inst.Syntax.ToSring() '{inst.Syntax.ToString()}'");

                        if (inst.Kind.ToString() == "ExpressionStatement")
                        {
                            IExpressionStatementOperation exprInst = (IExpressionStatementOperation)inst;

                            if (exprInst.Operation != null)
                            {
                                //if (exprInst.Operation.Kind.ToString() == Kind = SimpleAssignment)
                                //if (exprInst.Operation.Kind.ToString() == Kind = SimpleAssignment)
                                if (exprInst.Operation.Kind.ToString() == "SimpleAssignment")
                                {
                                    ISimpleAssignmentOperation assignInst = (ISimpleAssignmentOperation) exprInst.Operation;
                                    ProcessSimpleAssignmentOper(assignInst, sharedFields, inst);
                                }
                                else if (exprInst.Operation.Kind.ToString() == "Await")
                                {
                                    IAwaitOperation awaitInst = (IAwaitOperation)exprInst.Operation;
                                    
                                    if (awaitInst.Operation.Kind.ToString() == "Invocation")
                                    {
                                        IInvocationOperation invocInst = (IInvocationOperation)awaitInst.Operation;
                                        string leftOperand = "";
                                        ProcessInvocationOper(invocInst, inst, leftOperand);
                                        isAsync = true;
                                        //conflictUnit.Add(new Tuple<Accesses, string, IOperation>(Accesses.Invocation, "", inst));
                                    }
                                    else if (awaitInst.Operation.Kind.ToString() == "Invalid")
                                    {
                                        ExpressionSyntax localvar = (ExpressionSyntax)awaitInst.Operation.Syntax;
                                        //var v = localvar.Kind().ToString();
                                        if (localvar.Kind().ToString() == "InvocationExpression")
                                        {
                                            InvocationExpressionSyntax invocInst = (InvocationExpressionSyntax) localvar;
                                            string leftOperand = "";
                                            ProcessInvocationExpr(invocInst, inst, leftOperand);
                                            isAsync = true;
                                        }
                                    }
                                    else if (awaitInst.Operation.Kind.ToString() == "IdentifierName")
                                    {
                                        //IdentifierNameSyntax indentInst = (IdentifierNameSyntax)awaitInst.Operation;
                                        isAsync = true;
                                        //rightOperand = indentInst.Identifier.ValueText;
                                        //awaitInvocRel.Add(new Tuple<string, string>(rightOperand, leftOperand));
                                    }
                                    conflictUnit.Add(new Tuple<Accesses, string, IOperation>(Accesses.Await, invocSuffix, inst));
                                }
                                else if (exprInst.Operation.Kind.ToString() == "Invocation")
                                {
                                    IInvocationOperation invocInst = (IInvocationOperation) exprInst.Operation;
                                    string leftOperand = "";
                                    ProcessInvocationOper(invocInst, inst, leftOperand);
                                    //conflictUnit.Add(new Tuple<Accesses, string, IOperation>(Accesses.Invocation, "", inst));

                                    // invocSyntax = Regex.Replace(invocSyntax, @"(\w.*)(\.Wait\(\))$", "$1");
                                    //invocSyntax = Regex.Replace(invocSyntax, @"(\w.*)(\.)(\w.*)$", "$3");

                                }
                            }
                        }
                        else if  (inst.Kind.ToString() == "SimpleAssignment")
                        {
                            ISimpleAssignmentOperation exprInst = (ISimpleAssignmentOperation)inst;
                            ProcessSimpleAssignmentOper(exprInst, sharedFields, inst);
                        }
                    }
                }

            }
        }

        private void ProcessSimpleAssignmentOper(ISimpleAssignmentOperation assignOper, List<string> sharedFields, IOperation inst)
        {
            if (assignOper.Syntax.Kind().ToString() == "SimpleAssignmentExpression")
            {
                AssignmentExpressionSyntax assignSynt = (AssignmentExpressionSyntax)assignOper.Syntax;

                string leftOperand = "";
                int leftIndex = -1;


                if (assignSynt.Left.Kind().ToString() == "IdentifierName")
                {
                    IdentifierNameSyntax leftOperandExpr = (IdentifierNameSyntax)assignSynt.Left;
                    leftOperand = leftOperandExpr.Identifier.ToString();
                    leftIndex = sharedFields.IndexOf(leftOperand);
                }
                else if (assignSynt.Left.Kind().ToString() == "SimpleMemberAccessExpression")
                {
                    MemberAccessExpressionSyntax leftOperandExpr = (MemberAccessExpressionSyntax)assignSynt.Left;
                    leftOperand = leftOperandExpr.Name.ToString();
                    leftIndex = sharedFields.IndexOf(leftOperand);
                }


                string rightOperand = "";
                int rightIndex = -1;


                if (assignSynt.Right.Kind().ToString() == "IdentifierName")
                {
                    IdentifierNameSyntax rightOperandExpr = (IdentifierNameSyntax)assignSynt.Right;
                    rightOperand = rightOperandExpr.Identifier.ToString();
                    rightIndex = sharedFields.IndexOf(rightOperand);

                    if (rightIndex != -1 || methodInvocReturnRegs.IndexOf(rightOperand) != -1)
                    {
                        //Debug.Assert(leftIndex == 0);
                        conflictUnit.Add(new Tuple<Accesses, string, IOperation>(Accesses.Read, rightOperand, inst));
                    }
                }
                else if (assignSynt.Right.Kind().ToString() == "SimpleMemberAccessExpression")
                {
                    MemberAccessExpressionSyntax rightOperandExpr = (MemberAccessExpressionSyntax)assignSynt.Right;
                    rightOperand = rightOperandExpr.Name.ToString();
                    rightIndex = sharedFields.IndexOf(rightOperand);

                    if (rightIndex != -1)
                    {
                        //Debug.Assert(leftIndex == 0);
                        conflictUnit.Add(new Tuple<Accesses, string, IOperation>(Accesses.Write, rightOperand, inst));
                    }
                }
                else if (assignSynt.Right.Kind().ToString() == "InvocationExpression")
                {
                    InvocationExpressionSyntax invocInst = (InvocationExpressionSyntax)assignSynt.Right;
                    ProcessInvocationExpr(invocInst, inst, leftOperand);
                    //conflictUnit.Add(new Tuple<Accesses, string, IOperation>(Accesses.Invocation, "", inst));
                }
                else if (assignSynt.Right.Kind().ToString() == "AwaitExpression")
                {
                    AwaitExpressionSyntax awaitInst = (AwaitExpressionSyntax)assignSynt.Right;

                    if (awaitInst.Expression.Kind().ToString() == "InvocationExpression")
                    {
                        InvocationExpressionSyntax invocInst = (InvocationExpressionSyntax)awaitInst.Expression;
                        ProcessInvocationExpr(invocInst, inst, leftOperand);
                        isAsync = true;
                    }
                    else if(awaitInst.Expression.Kind().ToString() == "IdentifierName")
                    {
                        IdentifierNameSyntax indentInst = (IdentifierNameSyntax)awaitInst.Expression;
                        isAsync = true;
                        rightOperand = indentInst.Identifier.ValueText;
                        awaitInvocRel.Add(new Tuple<string, string>(rightOperand, leftOperand));
                        methodInvocReturnRegs.Add(leftOperand);
                    }
                    conflictUnit.Add(new Tuple<Accesses, string, IOperation>(Accesses.Await, leftOperand, inst));
                }

                if (leftIndex != -1)
                {
                    //Debug.Assert(rightIndex == 0);
                    conflictUnit.Add(new Tuple<Accesses, string, IOperation>(Accesses.Write, leftOperand, inst));
                }
            }
        }

        private void ProcessInvocationExpr(InvocationExpressionSyntax invocExpr, IOperation inst, string leftOperand)
        {
            if (leftOperand != "" && methodInvocReturnRegs.IndexOf(leftOperand) == -1)
            {
                methodInvocReturnRegs.Add(leftOperand);
            }


            string localLeftOperand;
            if(leftOperand != "")
            {
                localLeftOperand = leftOperand;
            }
            else
            {
                invocSuffix = invocSuffix + "0";
                localLeftOperand = invocSuffix;
            }
                    
            var localinvocExpr = invocExpr.Expression;

            if (localinvocExpr.Kind().ToString() == "IdentifierName")
            {
                IdentifierNameSyntax newInvocExpr = (IdentifierNameSyntax)localinvocExpr;
                string invocName = newInvocExpr.Identifier.ToString();                             
                InvokedMethods.Add(new Tuple<string, string, string, IOperation>(invocName, localLeftOperand, null, inst));
                conflictUnit.Add(new Tuple<Accesses, string, IOperation>(Accesses.Invocation, localLeftOperand, inst));
            }
            else if (localinvocExpr.Kind().ToString() == "SimpleMemberAccessExpression")
            {
                MemberAccessExpressionSyntax newInvocExpr = (MemberAccessExpressionSyntax)localinvocExpr;
                string invocName = newInvocExpr.Name.ToString();
                string invocArg = newInvocExpr.Expression.ToString(); 
               InvokedMethods.Add(new Tuple<string, string, string, IOperation>(invocName, localLeftOperand, invocArg, inst));
               conflictUnit.Add(new Tuple<Accesses, string, IOperation>(Accesses.Invocation, localLeftOperand, inst));
            }
        }

        private void ProcessInvocationOper(IInvocationOperation invocOper, IOperation inst, string leftOperand)
        {
            if (leftOperand != "" && methodInvocReturnRegs.IndexOf(leftOperand) == -1)
            {
                methodInvocReturnRegs.Add(leftOperand);
            }

            string localLeftOperand;
            if (leftOperand != "")
            {
                localLeftOperand = leftOperand;
            }
            else
            {               
                invocSuffix = invocSuffix + "0";
                localLeftOperand = invocSuffix;
            }

            InvocationExpressionSyntax invocSyntax = (InvocationExpressionSyntax)invocOper.Syntax;
            var invocExpr = invocSyntax.Expression;

            if (invocExpr.Kind().ToString() == "IdentifierName")
            {
                IdentifierNameSyntax newInvocExpr = (IdentifierNameSyntax)invocExpr;
                string invocName = newInvocExpr.Identifier.ToString();
                InvokedMethods.Add(new Tuple<string, string, string, IOperation>(invocName, localLeftOperand, null, inst));
                conflictUnit.Add(new Tuple<Accesses, string, IOperation>(Accesses.Invocation, localLeftOperand, inst));
            }
            else if (invocExpr.Kind().ToString() == "SimpleMemberAccessExpression")
            {
                MemberAccessExpressionSyntax newInvocExpr = (MemberAccessExpressionSyntax)invocExpr;

                if (newInvocExpr.Expression.Kind().ToString() == "InvocationExpression")
                {
                    InvocationExpressionSyntax newInvocInst = (InvocationExpressionSyntax)newInvocExpr.Expression;
                    ProcessInvocationExpr(newInvocInst, inst, leftOperand);

                    if (newInvocExpr.Name.ToString() == "Wait")
                    {
                        conflictUnit.Add(new Tuple<Accesses, string, IOperation>(Accesses.Await, invocSuffix, null));
                    }
                }
                else
                {
                    string invocName = newInvocExpr.Name.ToString();
                    string invocArg = newInvocExpr.Expression.ToString();
                    InvokedMethods.Add(new Tuple<string, string, string, IOperation>(invocName, localLeftOperand, invocArg, inst));
                    conflictUnit.Add(new Tuple<Accesses, string, IOperation>(Accesses.Invocation, localLeftOperand, inst));
                }
            }

        }

       

    }
}
