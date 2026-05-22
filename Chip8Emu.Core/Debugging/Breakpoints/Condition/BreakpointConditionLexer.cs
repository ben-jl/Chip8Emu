namespace Chip8Emu.Core.Debugging.Breakpoints.Condition
{
    internal static class BreakpointConditionLexer
    {
        public static IReadOnlyList<BreakpointConditionToken> Tokenize(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                throw new ArgumentException("Condition expression is required.", nameof(expression));
            }

            var tokens = new List<BreakpointConditionToken>();
            var i = 0;
            while (i < expression.Length)
            {
                var c = expression[i];
                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                if (char.IsLetter(c))
                {
                    var start = i;
                    i++;
                    while (i < expression.Length && (char.IsLetterOrDigit(expression[i]) || expression[i] == '_'))
                    {
                        i++;
                    }

                    tokens.Add(new BreakpointConditionToken(
                        BreakpointConditionTokenType.Identifier,
                        expression[start..i]));
                    continue;
                }

                if (char.IsDigit(c))
                {
                    var start = i;
                    i++;
                    if (c == '0' && i < expression.Length && (expression[i] == 'x' || expression[i] == 'X'))
                    {
                        i++;
                        while (i < expression.Length && IsHex(expression[i]))
                        {
                            i++;
                        }
                    }
                    else
                    {
                        while (i < expression.Length && char.IsDigit(expression[i]))
                        {
                            i++;
                        }
                    }

                    tokens.Add(new BreakpointConditionToken(
                        BreakpointConditionTokenType.Number,
                        expression[start..i]));
                    continue;
                }

                if (TryConsumeTwoCharOperator(expression, ref i, out var twoCharToken))
                {
                    tokens.Add(twoCharToken);
                    continue;
                }

                var token = c switch
                {
                    '<' => new BreakpointConditionToken(BreakpointConditionTokenType.LessThan, "<"),
                    '>' => new BreakpointConditionToken(BreakpointConditionTokenType.GreaterThan, ">"),
                    '(' => new BreakpointConditionToken(BreakpointConditionTokenType.LeftParen, "("),
                    ')' => new BreakpointConditionToken(BreakpointConditionTokenType.RightParen, ")"),
                    _ => throw new InvalidOperationException($"Unexpected token '{c}' in condition expression.")
                };

                tokens.Add(token);
                i++;
            }

            tokens.Add(new BreakpointConditionToken(BreakpointConditionTokenType.End, string.Empty));
            return tokens;
        }

        private static bool TryConsumeTwoCharOperator(string expression, ref int index, out BreakpointConditionToken token)
        {
            token = default;
            if (index + 1 >= expression.Length)
            {
                return false;
            }

            var pair = expression[index..(index + 2)];
            var matched = true;
            token = pair switch
            {
                "==" => new BreakpointConditionToken(BreakpointConditionTokenType.Equals, pair),
                "!=" => new BreakpointConditionToken(BreakpointConditionTokenType.NotEquals, pair),
                "<=" => new BreakpointConditionToken(BreakpointConditionTokenType.LessThanOrEqual, pair),
                ">=" => new BreakpointConditionToken(BreakpointConditionTokenType.GreaterThanOrEqual, pair),
                "&&" => new BreakpointConditionToken(BreakpointConditionTokenType.And, pair),
                "||" => new BreakpointConditionToken(BreakpointConditionTokenType.Or, pair),
                _ => MarkUnmatched(ref matched)
            };

            if (!matched)
            {
                return false;
            }

            index += 2;
            return true;
        }

        private static bool IsHex(char c)
        {
            return (c >= '0' && c <= '9') ||
                   (c >= 'a' && c <= 'f') ||
                   (c >= 'A' && c <= 'F');
        }

        private static BreakpointConditionToken MarkUnmatched(ref bool matched)
        {
            matched = false;
            return default;
        }
    }
}
