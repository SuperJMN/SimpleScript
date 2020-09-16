﻿using System.Linq;
using MoreLinq;
using SimpleScript.Binding.Model;
using SimpleScript.Zafiro;

namespace SimpleScript.Binding
{
    public class BoundNodeStringifyVisitor : IBoundNodeVisitor
    {
        private readonly IStringAssistant sa = new LineEatingStringAssistant(new StringAssistant());

        public void Visit(BoundScript boundScript)
        {
            boundScript.Functions.ForEach(function =>
            {
                function.Accept(this);
            });
        }

        public void Visit(BoundAssignmentStatement a)
        {
            sa.TabPrint(a.Variable + " = ");
            a.Expression.Accept(this);
            sa.Print(";");
        }

        public void Visit(BoundCondition c)
        {
            sa.Print("(");
            c.Left.Accept(this);
            sa.Print(" " + c.Op.Op + " ");
            c.Right.Accept(this);
            sa.Print(")");
        }

        public void Visit(BoundEchoStatement echo)
        {
        }

        public void Visit(BoundIfStatement ifs)
        {
            sa.TabPrint("if ");
            ifs.Condition.Accept(this);
            ifs.TrueBlock.Accept(this);
            ifs.FalseBlock.MatchSome(b =>
            {
                sa.NewLine();
                sa.TabPrint("else");
                sa.NewLine();
                b.Accept(this);
            });
        }

        public void Visit(BoundFunctionDeclaration fd)
        {
            sa.Print(fd.Name);
            fd.Block.Accept(this);
        }

        public void Visit(BoundBlock block)
        {
            sa.NewLine();
            sa.TabPrint("{");
            sa.NewLine();

            sa.IncreaseIndent();
            block.Statements.ToList().WhenMiddleAndLast(st =>
            {
                st.Accept(this);
                sa.NewLine();
            }, st => st.Accept(this));

            sa.DecreaseIndent();

            sa.NewLine();
            sa.TabPrint("}");
            sa.NewLine();
        }

        public void Visit(BoundNumericExpression ne)
        {
            sa.Print(ne.Value.ToString());
        }

        public void Visit(BoundCallStatement st)
        {
            sa.TabPrint("");
            st.Call.Accept(this);
            sa.Print(";");
        }

        public void Visit(BoundBuiltInFunctionCallExpression functionDeclaration)
        {
            sa.Print(functionDeclaration.Function.Name + "(");
            functionDeclaration.Parameters.ToList().WhenMiddleAndLast(ex =>
            {
                ex.Accept(this);
                sa.Print(", ");
            }, ex => ex.Accept(this));
            sa.Print(")");
        }

        public void Visit(BoundCustomCallExpression callExpression)
        {
            sa.Print(callExpression.FunctionDeclaration.Name + "(");
            callExpression.Parameters.ToList().WhenMiddleAndLast(ex =>
            {
                ex.Accept(this);
                sa.Print(", ");
            }, ex => ex.Accept(this));
            sa.Print(")");
        }

        public void Visit(BoundIdentifier boundIdentifier)
        {
            sa.Print(boundIdentifier.Identifier);
        }

        public void Visit(BoundStringExpression strExpr)
        {
            sa.Print("\"" + strExpr.String + "\"");
        }

        public override string ToString()
        {
            return sa.ToString();
        }
    }
}