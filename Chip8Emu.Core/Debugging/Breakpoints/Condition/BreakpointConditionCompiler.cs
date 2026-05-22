using System.Globalization;
using static Chip8Emu.Core.Debugging.Breakpoints.Condition.BreakpointConditionParser;

namespace Chip8Emu.Core.Debugging.Breakpoints.Condition
{
    internal static class BreakpointConditionCompiler
    {
        public static Func<BreakpointEvaluationContext, bool> Compile(string expression)
        {
            var tokens = BreakpointConditionLexer.Tokenize(expression);
            var ast = BreakpointConditionParser.Parse(tokens);
            return CompileBoolean(ast);
        }

        private static Func<BreakpointEvaluationContext, bool> CompileBoolean(ConditionExpressionNode node)
        {
            return node switch
            {
                ComparisonNode comparison => CompileComparison(comparison),
                LogicalNode logical => CompileLogical(logical),
                _ => throw new InvalidOperationException("Invalid condition expression root.")
            };
        }

        private static Func<BreakpointEvaluationContext, bool> CompileLogical(LogicalNode logical)
        {
            var left = CompileBoolean(logical.Left);
            var right = CompileBoolean(logical.Right);

            return logical.Operator switch
            {
                "&&" => context => left(context) && right(context),
                "||" => context => left(context) || right(context),
                _ => throw new InvalidOperationException($"Unsupported logical operator '{logical.Operator}'.")
            };
        }

        private static Func<BreakpointEvaluationContext, bool> CompileComparison(ComparisonNode comparison)
        {
            var left = CompileValue(comparison.Left);
            var right = CompileValue(comparison.Right);

            return comparison.Operator switch
            {
                "==" => context => left(context) == right(context),
                "!=" => context => left(context) != right(context),
                "<" => context => left(context) < right(context),
                "<=" => context => left(context) <= right(context),
                ">" => context => left(context) > right(context),
                ">=" => context => left(context) >= right(context),
                _ => throw new InvalidOperationException($"Unsupported comparison operator '{comparison.Operator}'.")
            };
        }

        private static Func<BreakpointEvaluationContext, int> CompileValue(ValueNode node)
        {
            return node switch
            {
                IdentifierNode identifier => context => BreakpointRegisterResolver.ResolveRegisterValue(context, identifier.Identifier),
                NumberNode number => _ => ParseNumber(number.NumberText),
                _ => throw new InvalidOperationException("Unsupported value expression.")
            };
        }

        private static int ParseNumber(string text)
        {
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return int.Parse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }
    }
}
