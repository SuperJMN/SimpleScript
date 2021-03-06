namespace Iridio.Parsing.Model
{
    public abstract class Statement : ISyntax
    {
        public abstract void Accept(IExpressionVisitor visitor);
    }
}