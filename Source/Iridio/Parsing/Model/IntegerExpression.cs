namespace Iridio.Parsing.Model
{
    public class IntegerExpression : Expression
    {
        public IntegerExpression(in int n)
        {
            Value = n;
        }

        public int Value { get; }

        public override string ToString()
        {
            return $"{Value}";
        }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}