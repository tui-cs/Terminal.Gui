using System;

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage (AttributeTargets.Method | AttributeTargets.Property, Inherited = false)]
    public sealed class MemberNotNullWhenAttribute : Attribute
    {
        public MemberNotNullWhenAttribute (bool returnValue, params string [] members)
        {
            ReturnValue = returnValue;
            Members = members;
        }

        public string [] Members { get; }

        public bool ReturnValue { get; }
    }
}
