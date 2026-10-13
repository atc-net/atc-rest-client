#if !NET
// Internal copy of the type the compiler asks for when a property has an init accessor; it exists in .NET 5+ but
// not in netstandard2.0.
#pragma warning disable S2094 // Classes should not be empty
namespace System.Runtime.CompilerServices;

internal static class IsExternalInit
{
}
#endif