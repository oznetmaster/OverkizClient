// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

#pragma warning disable CA1510 // Explicit null checks support .NET Framework 4.7.2.

namespace OverKizApi.Models;

public sealed partial class State
	{
	/// <summary>Returns an object state as CLR dictionary values, or null when no value is present.</summary>
	/// <exception cref="InvalidCastException">The state is not an object.</exception>
	[JsonIgnore]
	public IReadOnlyDictionary<string, object?>? ValueAsDict => Type == DataType.None || Value is null ? null
		: Type == DataType.JsonObject && Value is IReadOnlyDictionary<string, object?> dictionary ? dictionary
		: throw new InvalidCastException ($"{Name} is not an object");

	/// <summary>Returns an array state as CLR list values, or null when no value is present.</summary>
	/// <exception cref="InvalidCastException">The state is not an array.</exception>
	[JsonIgnore]
	public IReadOnlyList<object?>? ValueAsList => Type == DataType.None || Value is null ? null
		: Type == DataType.JsonArray && Value is IReadOnlyList<object?> list ? list
		: throw new InvalidCastException ($"{Name} is not an array");
	}

public sealed partial class States
	{
	/// <summary>Returns a named integer value, or null if missing or null. A wrong type throws InvalidCastException.</summary>
	public int? GetValueAsInt (string name) => WithValue (name)?.ValueAsInt;
	/// <summary>Returns a named floating-point value, promoting integers; null if missing or null. A wrong type throws InvalidCastException.</summary>
	public double? GetValueAsFloat (string name) => WithValue (name)?.ValueAsFloat;
	/// <summary>Returns a named boolean value, or null if missing or null. A wrong type throws InvalidCastException.</summary>
	public bool? GetValueAsBool (string name) => WithValue (name)?.ValueAsBool;
	/// <summary>Returns a named string value, or null if missing or null. A wrong type throws InvalidCastException.</summary>
	public string? GetValueAsStr (string name) => WithValue (name)?.ValueAsStr;
	/// <summary>Returns a named CLR dictionary, or null if missing or null. A wrong type throws InvalidCastException.</summary>
	public IReadOnlyDictionary<string, object?>? GetValueAsDict (string name) => WithValue (name)?.ValueAsDict;
	/// <summary>Returns a named CLR list, or null if missing or null. A wrong type throws InvalidCastException.</summary>
	public IReadOnlyList<object?>? GetValueAsList (string name) => WithValue (name)?.ValueAsList;

	/// <summary>Returns the first non-null value in name order as an integer. A wrong type throws; later names are not tried.</summary>
	public int? FirstValueAsInt (IEnumerable<string> names) => FirstWithValue (names)?.ValueAsInt;
	/// <summary>Returns the first non-null value in name order as a float, promoting integers. A wrong type throws.</summary>
	public double? FirstValueAsFloat (IEnumerable<string> names) => FirstWithValue (names)?.ValueAsFloat;
	/// <summary>Returns the first non-null value in name order as a boolean. A wrong type throws.</summary>
	public bool? FirstValueAsBool (IEnumerable<string> names) => FirstWithValue (names)?.ValueAsBool;
	/// <summary>Returns the first non-null value in name order as a string. A wrong type throws.</summary>
	public string? FirstValueAsStr (IEnumerable<string> names) => FirstWithValue (names)?.ValueAsStr;
	/// <summary>Returns the first non-null value in name order as a CLR dictionary. A wrong type throws.</summary>
	public IReadOnlyDictionary<string, object?>? FirstValueAsDict (IEnumerable<string> names) => FirstWithValue (names)?.ValueAsDict;
	/// <summary>Returns the first non-null value in name order as a CLR list. A wrong type throws.</summary>
	public IReadOnlyList<object?>? FirstValueAsList (IEnumerable<string> names) => FirstWithValue (names)?.ValueAsList;

	private State? WithValue (string name)
		{
		if (name is null) throw new ArgumentNullException (nameof (name));
		var state = this[name];
		return state?.Value is null ? null : state;
		}

	private State? FirstWithValue (IEnumerable<string> names)
		{
		if (names is null) throw new ArgumentNullException (nameof (names));
		foreach (var name in names)
			{
			var state = WithValue (name);
			if (state is not null) return state;
			}
		return null;
		}
	}