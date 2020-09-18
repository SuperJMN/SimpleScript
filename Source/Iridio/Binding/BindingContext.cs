﻿using System;
using System.Collections.Generic;

namespace Iridio.Binding
{
    public class BindingContext
    {
        public BindingContext(IEnumerable<IFunction> functions)
        {
            Functions = functions ?? throw new ArgumentNullException(nameof(functions));
        }

        public IEnumerable<IFunction> Functions { get; }
    }
}