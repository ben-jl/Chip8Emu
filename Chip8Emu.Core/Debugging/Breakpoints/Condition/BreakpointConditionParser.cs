namespace Chip8Emu.Core.Debugging.Breakpoints.Condition
{
    internal static class BreakpointConditionParser
    {
        public static ConditionExpressionNode Parse(IReadOnlyList<BreakpointConditionToken> tokens)
        {
            var state = new ParserState(tokens);
            var expression = ParseOr(state);
            state.Expect(BreakpointConditionTokenType.End, "Unexpected trailing tokens in condition expression.");
            return expression;
        }

        private static ConditionExpressionNode ParseOr(ParserState state)
        {
            var left = ParseAnd(state);
            while (state.Match(BreakpointConditionTokenType.Or))
            {
                var right = ParseAnd(state);
                left = new LogicalNode("||", left, right);
            }

            return left;
        }

        private static ConditionExpressionNode ParseAnd(ParserState state)
        {
            var left = ParseComparison(state);
            while (state.Match(BreakpointConditionTokenType.And))
            {
                var right = ParseComparison(state);
                left = new LogicalNode("&&", left, right);
            }

            return left;
        }

        private static ConditionExpressionNode ParseComparison(ParserState state)
        {
            if (state.Match(BreakpointConditionTokenType.LeftParen))
            {
                var nested = ParseOr(state);
                state.Expect(BreakpointConditionTokenType.RightParen, "Expected ')' in condition expression.");
                return nested;
            }

            var leftValue = ParseValue(state);
            var opToken = state.Current;
            if (!IsComparison(opToken.Type))
            {
                throw new InvalidOperationException($"Expected comparison operator after '{leftValue.RawText}'.");
            }

            state.Advance();
            var rightValue = ParseValue(state);
            return new ComparisonNode(opToken.Lexeme, leftValue, rightValue);
        }

        private static ValueNode ParseValue(ParserState state)
        {
            var current = state.Current;
            if (current.Type == BreakpointConditionTokenType.Identifier)
            {
                state.Advance();
                return new IdentifierNode(current.Lexeme);
            }

            if (current.Type == BreakpointConditionTokenType.Number)
            {
                state.Advance();
                return new NumberNode(current.Lexeme);
            }

            throw new InvalidOperationException($"Expected identifier or number but found '{current.Lexeme}'.");
        }

        private static bool IsComparison(BreakpointConditionTokenType type)
        {
            return type == BreakpointConditionTokenType.Equals ||
                   type == BreakpointConditionTokenType.NotEquals ||
                   type == BreakpointConditionTokenType.LessThan ||
                   type == BreakpointConditionTokenType.LessThanOrEqual ||
                   type == BreakpointConditionTokenType.GreaterThan ||
                   type == BreakpointConditionTokenType.GreaterThanOrEqual;
        }

        internal abstract record ConditionExpressionNode;
        internal abstract record ValueNode(string RawText) : ConditionExpressionNode;
        internal sealed record IdentifierNode(string Identifier) : ValueNode(Identifier);
        internal sealed record NumberNode(string NumberText) : ValueNode(NumberText);
        internal sealed record ComparisonNode(string Operator, ValueNode Left, ValueNode Right) : ConditionExpressionNode;
        internal sealed record LogicalNode(string Operator, ConditionExpressionNode Left, ConditionExpressionNode Right) : ConditionExpressionNode;

        private sealed class ParserState
        {
            private readonly IReadOnlyList<BreakpointConditionToken> _tokens;
            private int _index;

            public ParserState(IReadOnlyList<BreakpointConditionToken> tokens)
            {
                _tokens = tokens;
                _index = 0;
            }

            public BreakpointConditionToken Current => _tokens[Math.Min(_index, _tokens.Count - 1)];

            public bool Match(BreakpointConditionTokenType type)
            {
                if (Current.Type != type)
                {
                    return false;
                }

                _index++;
                return true;
            }

            public void Expect(BreakpointConditionTokenType type, string message)
            {
                if (!Match(type))
                {
                    throw new InvalidOperationException(message);
                }
            }

            public void Advance()
            {
                if (_index < _tokens.Count)
                {
                    _index++;
                }
            }
        }
    }
}
