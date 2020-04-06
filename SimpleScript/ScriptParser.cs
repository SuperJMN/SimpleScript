﻿using SimpleScript.Ast;
using SimpleScript.Ast.Model;
using Superpower;

namespace SimpleScript
{
    public class ScriptParser : IScriptParser
    {
        public ScriptSyntax Parse(string source)
        {
            var tokenizer = Tokenizer.Create().Tokenize(source);
            var script = SimpleParser.SimpleScript.Parse(tokenizer);
            return script;
        }
    }
}