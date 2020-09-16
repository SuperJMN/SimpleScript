﻿using System.Collections.Generic;

namespace SimpleScript.Binding.Model
{
    public class BoundBlock : IBoundNode
    {
        public IEnumerable<BoundStatement> Statements { get; }

        public BoundBlock(IEnumerable<BoundStatement> statements)
        {
            Statements = statements;
        }

        public void Accept(IBoundNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}