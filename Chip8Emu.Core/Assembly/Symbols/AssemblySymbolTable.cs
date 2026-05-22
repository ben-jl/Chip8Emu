namespace Chip8Emu.Core.Assembly.Symbols
{
    public sealed class AssemblySymbolTable
    {
        private readonly Dictionary<string, Symbol> _symbols = new(StringComparer.OrdinalIgnoreCase);

        public void DefineLabel(string name, int address, int line)
        {
            _symbols[name] = new Symbol(name, SymbolKind.Label, address, line);
        }

        public void DefineConstant(string name, int value, int line)
        {
            _symbols[name] = new Symbol(name, SymbolKind.Constant, value, line);
        }

        public bool TryResolve(string name, out Symbol? symbol)
        {
            return _symbols.TryGetValue(name, out symbol);
        }

        public bool IsDefined(string name)
        {
            return _symbols.ContainsKey(name);
        }

        public IReadOnlyList<Symbol> AllSymbols => _symbols.Values.ToList();
    }
}
