// ReSharper disable CheckNamespace
namespace System.Runtime.CompilerServices
// ReSharper restore CheckNamespace
{
    /// <summary>
    /// Attribute required for .NET Micro Framework to recognize extension methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class ExtensionAttribute : Attribute { }
}
