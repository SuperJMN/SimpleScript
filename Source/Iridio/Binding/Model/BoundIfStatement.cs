﻿using Optional;

namespace Iridio.Binding.Model
{
    public class BoundIfStatement : BoundStatement
    {
        public BoundExpression Condition { get; }
        public BoundBlock TrueBlock { get; }
        public Option<BoundBlock> FalseBlock { get; }

        public BoundIfStatement(BoundExpression condition, BoundBlock trueBlock, Option<BoundBlock> falseBlock)
        {
            Condition = condition;
            TrueBlock = trueBlock;
            FalseBlock = falseBlock;
        }

        public override void Accept(IBoundNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}