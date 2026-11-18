using System.Diagnostics.CodeAnalysis;

namespace ConsumerLib
{
    public class FixtureType
    {
        [MemberNotNullWhen (true, nameof (Value))]
        public bool HasValue => !string.IsNullOrEmpty (Value);

        public string Value { get; set; } = string.Empty;
    }
}
