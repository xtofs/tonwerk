namespace Tonwerk.Theory;


using System.Diagnostics.CodeAnalysis;


/// <summary>
/// Defines a contract for types that can be parsed from a character span.
/// Provides a composable parsing mechanism that returns both the parsed result and the remaining unparsed span.
/// </summary>
/// <typeparam name="TSelf"></typeparam>
public interface IPrefixParsable<TSelf> where TSelf : IPrefixParsable<TSelf>?
{
    static abstract bool TryParse(ReadOnlySpan<char> s, [MaybeNullWhen(returnValue: false)] out TSelf result, out ReadOnlySpan<char> remainder);
}





