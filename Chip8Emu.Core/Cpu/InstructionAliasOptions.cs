using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Chip8Emu.Core.Cpu
{
    public sealed record InstructionAliasOptions
    {
        public static InstructionAliasOptions Empty { get; } = new();

        public IReadOnlyDictionary<InstructionVariantKey, string> VariantAliases { get; }

        public InstructionAliasOptions() : this(new Dictionary<InstructionVariantKey, string>())
        {
        }

        public InstructionAliasOptions(IReadOnlyDictionary<InstructionVariantKey, string> variantAliases)
        {
            ArgumentNullException.ThrowIfNull(variantAliases);
            VariantAliases = new ReadOnlyDictionary<InstructionVariantKey, string>(
                new Dictionary<InstructionVariantKey, string>(variantAliases));
        }

        public string ResolveMnemonic(InstructionDefinition definition)
        {
            ArgumentNullException.ThrowIfNull(definition);

            var key = new InstructionVariantKey(definition.Mask, definition.Pattern);
            if (VariantAliases.TryGetValue(key, out var alias) && !string.IsNullOrWhiteSpace(alias))
            {
                return alias;
            }

            return definition.Mnemonic;
        }
    }
}
