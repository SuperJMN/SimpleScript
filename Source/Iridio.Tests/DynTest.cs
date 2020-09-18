﻿using System.Threading.Tasks;
using Iridio.Common.Utils;
using Xunit;

namespace Iridio.Tests
{
    public class DynTest
    {
        [Fact]
        public async Task Test()
        {
            var p = new IntTask();
            var result = await p.InvokeTask("Execute", 1);
        }
    }
}