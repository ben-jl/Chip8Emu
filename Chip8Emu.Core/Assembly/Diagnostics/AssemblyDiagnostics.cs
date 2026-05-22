namespace Chip8Emu.Core.Assembly.Diagnostics
{
    public sealed class AssemblyDiagnostics
    {
        private readonly List<AssemblyError> _errors = new();

        public void ReportError(string code, string message, int line, int column)
        {
            _errors.Add(new AssemblyError(code, message, line, column));
        }

        public IReadOnlyList<AssemblyError> Errors => _errors;
        public bool HasErrors => _errors.Count > 0;
    }
}
