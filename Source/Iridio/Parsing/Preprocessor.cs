﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using Iridio.Common.Utils;
using Zafiro.Core.FileSystem;

namespace Iridio.Parsing
{
    public class Preprocessor : IPreprocessor
    {
        private readonly IFileSystemOperations fileSystemOperations;

        public Preprocessor(IFileSystemOperations fileSystemOperations)
        {
            this.fileSystemOperations = fileSystemOperations;
        }

        public string Process(string input)
        {
            var result = from line in input.Lines()
                where !IsComment(line)
                let processed = ExpandIfNeeded(line)
                select processed;

            return string.Join(Environment.NewLine, result);
        }

        private bool IsComment(string line)
        {
            return Regex.IsMatch(line, @"\s*//");
        }

        private string ExpandIfNeeded(string line)
        {
            var match = Regex.Match(line, @"#include\s+(.*)");
            if (match.Success)
            {
                var file = match.Groups[1].Value;
                var input = fileSystemOperations.ReadAllText(file);
                return Process(input);
            }

            return line;
        }
    }
}