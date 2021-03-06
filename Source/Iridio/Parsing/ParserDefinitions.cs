using Iridio.Parsing.Model;
using Iridio.Tokenization;
using Optional;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

namespace Iridio.Parsing
{
    public class ParserDefinitions
    {
        private static readonly TokenListParser<SimpleToken, BinaryOperator> Add = Token.EqualTo(SimpleToken.Plus).Value(BinaryOperator.Add);
        private static readonly TokenListParser<SimpleToken, BinaryOperator> Subtract = Token.EqualTo(SimpleToken.Hyphen).Value(BinaryOperator.Subtract);
        private static readonly TokenListParser<SimpleToken, BinaryOperator> Multiply = Token.EqualTo(SimpleToken.Asterisk).Value(BinaryOperator.Multiply);
        private static readonly TokenListParser<SimpleToken, BinaryOperator> Divide = Token.EqualTo(SimpleToken.Slash).Value(BinaryOperator.Divide);
        private static readonly TokenListParser<SimpleToken, BinaryOperator> Lte = Token.EqualTo(SimpleToken.LessOrEqual).Value(BinaryOperator.LessThanOrEqual);
        private static readonly TokenListParser<SimpleToken, BinaryOperator> Lt = Token.EqualTo(SimpleToken.Less).Value(BinaryOperator.LessThan);
        private static readonly TokenListParser<SimpleToken, BinaryOperator> Gt = Token.EqualTo(SimpleToken.Greater).Value(BinaryOperator.GreaterThan);
        private static readonly TokenListParser<SimpleToken, BinaryOperator> Gte = Token.EqualTo(SimpleToken.GreaterOrEqual).Value(BinaryOperator.GreaterThanOrEqual);
        private static readonly TokenListParser<SimpleToken, BinaryOperator> Eq = Token.EqualTo(SimpleToken.EqualEqual).Value(BinaryOperator.Equal);
        private static readonly TokenListParser<SimpleToken, BinaryOperator> Neq = Token.EqualTo(SimpleToken.NotEqual).Value(BinaryOperator.NotEqual);
        private static readonly TokenListParser<SimpleToken, BinaryOperator> And = Token.EqualTo(SimpleToken.And).Value(BinaryOperator.And);
        private static readonly TokenListParser<SimpleToken, BinaryOperator> Or = Token.EqualTo(SimpleToken.Or).Value(BinaryOperator.Or);
        private static readonly TokenListParser<SimpleToken, UnaryOperator> Not = Token.EqualTo(SimpleToken.Exclamation).Value(UnaryOperator.Not);

        private static readonly TokenListParser<SimpleToken, Token<SimpleToken>> Identifier =
            Token.EqualTo(SimpleToken.Identifier);

        private static readonly TokenListParser<SimpleToken, string> Text = Token.EqualTo(SimpleToken.Text)
            .Select(x => Unwrap(x.ToStringValue()));

        private static string Unwrap(string str)
        {
            return str.Substring(1, str.Length-2);
        }

        private static readonly TokenListParser<SimpleToken, int> Integer =
            Token.EqualTo(SimpleToken.Integer).Apply(Numerics.IntegerInt32);

        private static readonly TokenListParser<SimpleToken, double> Double =
            Token.EqualTo(SimpleToken.Double).Apply(TextParsers.DoubleParser);

        public static readonly TokenListParser<SimpleToken, Expression> TextExpression =
            Text.Select(x => (Expression) new StringExpression(x));

        public static readonly TokenListParser<SimpleToken, Expression> IntegerExpression =
            Integer.Select(x => (Expression) new IntegerExpression(x));

        public static readonly TokenListParser<SimpleToken, Expression> DoubleExpression =
            Double.Select(x => (Expression)new DoubleExpression(x));

        public static readonly TokenListParser<SimpleToken, Expression> IdentifierExpression =
            Identifier.Select(x => (Expression) new IdentifierExpression(x));

        public static readonly TokenListParser<SimpleToken, Expression> BooleanValueExpression =
            Token.EqualTo(SimpleToken.True).Value((Expression)new BooleanValueExpression(true))
                .Or(Token.EqualTo(SimpleToken.False).Value((Expression)new BooleanValueExpression(false)));

        private static readonly TokenListParser<SimpleToken, Expression[]> Parameters = Parse.Ref(() => Expression)
            .ManyDelimitedBy(Token.EqualTo(SimpleToken.Comma))
            .Between(SimpleToken.OpenParen, SimpleToken.CloseParen)
            .Select(objects => objects);

        public static readonly TokenListParser<SimpleToken, Expression> CallExpression = from funcName in Identifier
            from parameters in Parameters
            select (Expression) new CallExpression(funcName, parameters);

        private static readonly TokenListParser<SimpleToken, Block>
            Else = from keyword in Token.EqualTo(SimpleToken.Else)
                from block in Block
                select block;

        private static readonly TokenListParser<SimpleToken, Expression> Condition =
            Parse.Ref(() => Expression)
                .Between(Token.EqualTo(SimpleToken.OpenParen), Token.EqualTo(SimpleToken.CloseParen));

        public static readonly TokenListParser<SimpleToken, Statement> IfStatement =
            from keyword in Token.EqualTo(SimpleToken.If)
            from cond in Condition
            from ifStatements in Block
            from elseStatement in Else.OptionalOrDefault()
            select (Statement) new IfStatement(cond, ifStatements, elseStatement.SomeNotNull());

        public static readonly TokenListParser<SimpleToken, Statement>
            CallSentence = from expression in Parse.Ref(() => CallExpression)
                select (Statement) new CallStatement((CallExpression) expression);

        public static readonly TokenListParser<SimpleToken, Statement> AssignmentSentence =
            from identifier in Identifier
            from eq in Token.EqualTo(SimpleToken.Equal)
            from expr in Expression
            select (Statement) new AssignmentStatement(identifier, expr);


        private static readonly TokenListParser<SimpleToken, Expression> Item = CallExpression.Try()
            .Or(IntegerExpression)
            .Or(DoubleExpression)
            .Or(TextExpression)
            .Or(IdentifierExpression)
            .Or(BooleanValueExpression);

        private static readonly TokenListParser<SimpleToken, Expression> Factor =
            Parse.Ref(() => Expression).BetweenParenthesis()
                .Or(Item);

        private static readonly TokenListParser<SimpleToken, Expression> Operand =
            (from op in Not
                from factor in Factor
                select MakeUnary(op, factor)).Or(Factor).Named("expression");

        private static readonly TokenListParser<SimpleToken, Expression> InnerTerm = Operand;

        private static readonly TokenListParser<SimpleToken, Expression> Term = Parse.Chain(Multiply.Or(Divide), InnerTerm, MakeBinary);

        private static readonly TokenListParser<SimpleToken, Expression> Comparand = Parse.Chain(Add.Or(Subtract), Term, MakeBinary);

        private static readonly TokenListParser<SimpleToken, Expression> Comparison = Parse.Chain(Lte.Or(Neq).Or(Lt).Or(Gte.Or(Gt)).Or(Eq), Comparand, MakeBinary);

        private static readonly TokenListParser<SimpleToken, Expression> Conjunction = Parse.Chain(And, Comparison, MakeBinary);

        private static readonly TokenListParser<SimpleToken, Expression> Disjunction = Parse.Chain(Or, Conjunction, MakeBinary);

        public static readonly TokenListParser<SimpleToken, Expression> Expression = Disjunction;

        private static readonly TokenListParser<SimpleToken, Statement> EchoStatement = Token.EqualTo(SimpleToken.Echo)
            .Apply(ExtraParsers.SpanBetween("'", "'"))
            .Select(span => (Statement) new EchoStatement(span.ToStringValue()));

        public static readonly TokenListParser<SimpleToken, Statement> SingleSentence =
            from s in AssignmentSentence.Try().Or(CallSentence).Try()
            from semicolon in Token.EqualTo(SimpleToken.Semicolon)
            select s;

        public static readonly TokenListParser<SimpleToken, Statement> Sentence = IfStatement.Try().Or(SingleSentence);

        public static readonly TokenListParser<SimpleToken, Statement> Statement = Sentence.Try().Or(EchoStatement);

        public static readonly TokenListParser<SimpleToken, Block> Block =
            from statements in Statement.Many()
                .Between(Token.EqualTo(SimpleToken.OpenBrace), Token.EqualTo(SimpleToken.CloseBrace))
            select new Block(statements);

        public static TokenListParser<SimpleToken, Procedure> Function => 
            from i in Identifier
            from block in Block
            select new Procedure(i, block);

        public static TokenListParser<SimpleToken, IridioSyntax> Parser =>
            (from functions in Function.Many()
                select new IridioSyntax(functions))
            .AtEnd();

        private static Expression MakeBinary(BinaryOperator binaryOperatorName, Expression leftOperand, Expression rightOperand)
        {
            return new BinaryExpression(binaryOperatorName, leftOperand, rightOperand);
        }

        private static Expression MakeUnary(UnaryOperator binaryOperatorName, Expression factor)
        {
            return new UnaryExpression(binaryOperatorName, factor);
        }
    }
}