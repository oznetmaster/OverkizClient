// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace OverKizApi;

/// <summary>
/// Nullable flow-friendly helpers for dictionaries.
/// </summary>
public static class DictionaryExtensions
	{
	/// <summary>
	/// TryGetValue that also guarantees the value is non-null when the method returns true.
	/// </summary>
	/// <typeparam name="TKey">Dictionary key type.</typeparam>
	/// <typeparam name="TValue">Dictionary value type (reference type).</typeparam>
	/// <param name="dict">Source dictionary.</param>
	/// <param name="key">Key to look up.</param>
	/// <param name="value">Non-null value when the method returns true.</param>
	/// <returns>True when the key exists and the associated value is not null; otherwise false.</returns>
	public static bool TryGetNonNull<TKey, TValue> (
		this IDictionary<TKey, TValue> dict,
		TKey key,
		[NotNullWhen (true)] out TValue? value)
		where TValue : class
		{
		if (dict is null)
			{
			value = null;
			return false;
			}

		if (dict.TryGetValue (key, out TValue? v) && v is not null)
			{
			value = v;
			return true;
			}

		value = null;
		return false;
		}

	/// <summary>
	/// Gets a string value for a key or a fallback when not present.
	/// Assumes JSON deserialization never stores null values in the dictionary.
	/// </summary>
	/// <param name="dict">Source dictionary.</param>
	/// <param name="key">Key to look up.</param>
	/// <param name="fallback">Fallback string when key is missing.</param>
	/// <returns>String value or fallback.</returns>
	public static string GetStringOr (this IDictionary<string, object> dict, string key, string fallback = "Unknown") =>
		dict.TryGetValue (key, out var v) ? (v?.ToString () ?? fallback) : fallback;

	/// <summary>
	/// Gets a nullable string value for a key or a nullable fallback when not present.
	/// </summary>
	/// <param name="dict">Source dictionary.</param>
	/// <param name="key">Key to look up.</param>
	/// <param name="fallback">Nullable fallback when key is missing.</param>
	/// <returns>String value or nullable fallback.</returns>
	public static string? GetNullableStringOr (this IDictionary<string, object> dict, string key, string? fallback = null) =>
		dict.TryGetValue (key, out var v) ? (v?.ToString () ?? fallback) : fallback;
	}

#if !NET5_0_OR_GREATER
/// <summary>
/// Provides <c>KeyValuePair&lt;TKey,TValue&gt;.Deconstruct</c> for targets below .NET 5
/// where the BCL does not include it, allowing <c>foreach (var (key, value) in dict)</c> syntax.
/// </summary>
public static class KeyValuePairExtensions
	{
	/// <summary>
	/// Deconstructs a <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> into its key and value components,
	/// enabling <c>foreach (var (key, value) in dictionary)</c> syntax on targets below .NET 5
	/// where the BCL does not provide this method natively.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	/// <param name="pair">The key/value pair to deconstruct.</param>
	/// <param name="key">Receives the <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}.Key"/>.</param>
	/// <param name="value">Receives the <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}.Value"/>.</param>
	public static void Deconstruct<TKey, TValue> (
		this System.Collections.Generic.KeyValuePair<TKey, TValue> pair,
		out TKey key,
		out TValue value)
		{
		key   = pair.Key;
		value = pair.Value;
		}
	}
#endif
