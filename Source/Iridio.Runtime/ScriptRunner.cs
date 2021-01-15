﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Iridio.Binding;
using Iridio.Binding.Model;
using Iridio.Common;
using Iridio.Parsing;
using Iridio.Parsing.Model;
using Iridio.Runtime.ReturnValues;
using MoreLinq.Extensions;
using Optional.Async.Extensions;
using Optional.Collections;
using Optional.Unsafe;
using Zafiro.Core;
using Zafiro.Core.Patterns.Either;

namespace Iridio.Runtime
{
    public class ScriptRunner : IScriptRunner
    {
        private readonly IEnumerable<IFunction> functions;
        private IDictionary<string, object> variables;
        private readonly ISubject<string> messages = new Subject<string>();

        public ScriptRunner(IEnumerable<IFunction> functions)
        {
            this.functions = functions;
        }

        public Task<Either<RuntimeErrors, Success>> Run(Script script, IDictionary<string, object> variables)
        {
            this.variables = variables;

            return Execute(script);
        }

        public IObservable<string> Messages => messages.AsObservable();

        private Task<Either<RuntimeErrors, Success>> Execute(Script compiled)
        {
            return Execute(compiled.MainProcedure);
        }

        private Task<Either<RuntimeErrors, Success>> Execute(BoundProcedure main)
        {
            return Execute(main.Block);
        }

        private async Task<Either<RuntimeErrors, Success>> Execute(BoundBlock block)
        {
            foreach (var st in block.Statements)
            {
                var result = await Execute(st);
                if (!result.IsRight())
                {
                    return result.Left.ValueOrFailure();
                }
            }

            return new Success();
        }

        private async Task<Either<RuntimeErrors, Success>> Execute(BoundStatement statement)
        {
            switch (statement)
            {
                case BoundAssignmentStatement assignment:
                    return await Execute(assignment);
                case BoundCallStatement call:
                    return await Execute(call);
                case BoundEchoStatement echo:
                    messages.OnNext(echo.Message);
                    break;
                case BoundIfStatement ifStatement:
                    return await Execute(ifStatement);
                default:
                    throw new ArgumentOutOfRangeException(nameof(statement));
            }

            return Either.Success<RuntimeErrors, Success>(new Success());
        }

        private async Task<Either<RuntimeErrors, Success>> Execute(BoundCallStatement boundCallStatement)
        {
            var either = await Evaluate(boundCallStatement.Call);
            return either.MapRight(o => new Success());
        }

        private async Task<Either<RuntimeErrors, Success>> Execute(BoundIfStatement boundIfStatement)
        {
            var eitherComparison = await IsMet(boundIfStatement.Condition);

            var result = eitherComparison.MapRight(async isMet =>
            {
                if (isMet)
                {
                    var executeResult = await Execute(boundIfStatement.TrueBlock);

                    return executeResult;
                }

                var optionalTask = boundIfStatement.FalseBlock.Map(Execute);
                var task = optionalTask.Match(t => t, () => Task.FromResult(Either.Success<RuntimeErrors, Success>(new Success())));
                return await task;
            });

            return await result.RightTask();
        }

        private async Task<Either<RuntimeErrors, bool>> IsMet(BoundCondition condition)
        {
            var left = await Evaluate(condition.Left);
            var right = await Evaluate(condition.Right);
            return CombineExtensions.Combine(left, right, (x, y) => Compare(x, y, condition.Op), RuntimeErrors.Concat);
        }

        private Either<RuntimeErrors, bool> Compare(object a, object b, BooleanOperator op)
        {
            if (a is string strA && b is string strB)
            {
                if (op.Op == "==")
                {
                    return Either.Success<RuntimeErrors, bool>(strB.Equals(strA));
                }

                if (op.Op == "!=")
                {
                    return Either.Success<RuntimeErrors, bool>(!strB.Equals(strA));
                }
            }

            if (a is int x && b is int y)
            {
                if (op.Op == ">")
                {
                    return Either.Success<RuntimeErrors, bool>(x > y);
                }

                if (op.Op == "<")
                {
                    return Either.Success<RuntimeErrors, bool>(x < y);
                }

                if (op.Op == "==")
                {
                    return Either.Success<RuntimeErrors, bool>(x == y);
                }

                if (op.Op == "!=")
                {
                    return Either.Success<RuntimeErrors, bool>(x != y);
                }

                if (op.Op == ">=")
                {
                    return Either.Success<RuntimeErrors, bool>(x >= y);
                }

                if (op.Op == "<=")
                {
                    return Either.Success<RuntimeErrors, bool>(x <= y);
                }
            }

            return Either.Error<RuntimeErrors, bool>(new RuntimeErrors(new TypeMismatch()));
        }

        private async Task<Either<RuntimeErrors, Success>> Execute(BoundAssignmentStatement boundAssignmentStatement)
        {
            var evaluation = await Evaluate(boundAssignmentStatement.Expression);
            evaluation.WhenRight(o => variables[boundAssignmentStatement.Variable] = o);
            return evaluation.MapRight(o => new Success());
        }

        private async Task<Either<RuntimeErrors, object>> Evaluate(BoundExpression expression)
        {
            switch (expression)
            {
                case BoundIdentifier boundIdentifier:
                    return Evaluate(boundIdentifier);
                case BoundBuiltInFunctionCallExpression boundBuiltInFunctionCallExpression:
                    return await Evaluate(boundBuiltInFunctionCallExpression);
                case BoundStringExpression boundStringExpression:
                    return Evaluate(boundStringExpression);
                case BoundProcedureCallExpression boundCustomCallExpression:
                    return await Evaluate(boundCustomCallExpression);
                case BoundNumericExpression boundNumericExpression:
                    return Either.Success<RuntimeErrors, object>(boundNumericExpression.Value);
            }

            throw new ArgumentOutOfRangeException(nameof(expression));
        }

        private Either<RuntimeErrors, object> Evaluate(BoundStringExpression boundStringExpression)
        {
            var pattern = "(?<=(?<!{)(?:{{)*){([^{}]*)}(?=(?:}})*(?!}))";
            var str = boundStringExpression.String;
            var r = str.ReplaceWithRegex(pattern, FindReplacement);
            return r.RegexReplace("{{", "{")
                .RegexReplace("}}", "}");
        }

        private Replacement FindReplacement(Match arg)
        {
            var refName = arg.Groups[1].Value;
            var value = variables[refName];
            return new Replacement(value.ToString(), arg.Groups[0]);
        }

        private Either<RuntimeErrors, string> Replace(string str)
        {
            var references = References.FromString(str).ToList();
            var undefinedReferences = references.Except(variables.Keys).ToList();
            if (undefinedReferences.Any())
            {
                return new RuntimeErrors(undefinedReferences.Select(s => new ReferenceToUnsetVariable(s)));
            }

            var refsAndValues = from variable in references
                                from value in DictionaryExtensions.GetValueOrNone(variables, variable).ToEnumerable()
                                select new { variable, value };

            var refsAndValuesList = refsAndValues.ToList();

            var pattern = string.Join("|", refsAndValuesList.Select(arg => $"{{{arg.variable}}}"));
            if (pattern == "")
            {
                return str;
            }

            var regex = new Regex(pattern);
            var valuesDict = refsAndValuesList.ToDictionary(x => x.variable, x => x.value);
            var result = regex.Replace(str, match =>
            {
                var skipLast = MoreLinq.MoreEnumerable.SkipLast(match.Value.Skip(1), 1);
                var key = new string(skipLast.ToArray());
                return valuesDict[key].ToString();
            });

            return result;
        }

        private async Task<Either<RuntimeErrors, object>> Evaluate(BoundProcedureCallExpression call)
        {
            return (await Execute(call.Procedure.Block)).MapRight(s => (object)s);
        }

        private Task<Either<RuntimeErrors, object>> Evaluate(BoundBuiltInFunctionCallExpression call)
        {
            var function = OptionCollectionExtensions.FirstOrNone(functions, f => f.Name == call.Function.Name);
            return function.MatchAsync(f => Call(f, call.Parameters), () => Either.Error<RuntimeErrors, object>(new RuntimeErrors(new UndeclaredFunction(call.Function.Name))));
        }

        private async Task<Either<RuntimeErrors, object>> Call(IFunction function, IEnumerable<BoundExpression> parameters)
        {
            var eitherParameters = await parameters.AsyncSelect(async expression => await Evaluate(expression));
            var combined = eitherParameters.Combine((a, b) => a);

            try
            {
                var mapSuccess = combined.MapRight(async p => await function.Invoke(p.ToArray()));
                var right = await mapSuccess.RightTask();
                return right;
            }
            catch (Exception ex)
            {
                switch (ex.InnerException)
                {
                    case TaskCanceledException tc:
                        return Either.Error<RuntimeErrors, object>(new RuntimeErrors(new ExecutionCanceled(tc.Message)));
                    case Exception inner:
                        return Either.Error<RuntimeErrors, object>(new RuntimeErrors(new IntegratedFunctionFailed(function, inner)));
                    default:
                        return Either.Error<RuntimeErrors, object>(new RuntimeErrors(new IntegratedFunctionFailed(function, ex)));
                }
            }
        }

        private Either<RuntimeErrors, object> Evaluate(BoundIdentifier identifier)
        {
            if (variables.TryGetValue(identifier.Identifier, out var val))
            {
                return Either.Success<Errors, object>(val);
            }

            return Either.Error<Errors, object>(new Errors(new Error(ErrorKind.VariableNotFound, $"Could not find variable '{identifier.Identifier}'")));
        }
    }
}