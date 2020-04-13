using System.Collections.Generic;

namespace SimpleScript.Ast.Model
{
    public class Header
    {
        public IEnumerable<Declaration> Declarations { get; }

        public Header(IEnumerable<Declaration> declarations)
        {
            Declarations = declarations;
        }
    }
}