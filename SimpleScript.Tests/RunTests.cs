﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace SimpleScript.Tests
{
    public class RunTests
    {
        [Fact]
        public async Task MyTest()
        {
            MethodInfo inf = typeof(IntTask).GetMethod("Execute");
            var intTask = new IntTask();
            Delegate d = inf.CreateDelegate(intTask);
            var ret =  await (Task<object>) d.Method.Invoke(intTask, new object[] {123});
        }

        //[Fact]
        //public async Task Run()
        //{
        //    IEnumerable<Function> funcs = new[] { new Function(typeof(IntTask)), new Function(typeof(StringTask)), };
        //    var runner = new Runner(funcs);
        //    var compiler = new Compiler(new Parser(), new FileSystemOperations());
        //    var script = compiler.Compile("Root.txt");
        //    var dictionary = new Dictionary<string, object>();
        //    await runner.Run(script, dictionary);
        //}
    }

    public class Function : IFunction
    {
        public Task<object> Invoke(object[] parameters)
        {
            throw new NotImplementedException();
        }

        public string Name { get; }
        public IEnumerable<Argument> Arguments { get; }
        public Type ReturnType { get; }
    }
}