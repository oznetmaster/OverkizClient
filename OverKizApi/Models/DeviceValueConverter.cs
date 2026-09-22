// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace OverKizApi.Models;

// Vendor-defined values have no fixed schema. Materialize ordinary CLR values instead
// of leaking a serializer-specific DOM through State.Value or command parameters.
internal sealed class DeviceValueConverter : JsonConverter<object>
	{
	public override object? Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
		switch (reader.TokenType)
			{
			case JsonTokenType.Null: return null;
			case JsonTokenType.True: return true;
			case JsonTokenType.False: return false;
			case JsonTokenType.String: return reader.GetString ();
			case JsonTokenType.Number:
				if (reader.TryGetInt64 (out long integer)) return integer;
				if (reader.TryGetDecimal (out decimal number)) return number;
				return reader.GetDouble ();
			case JsonTokenType.StartArray:
				return JsonSerializer.Deserialize<List<object?>> (ref reader, ValueOptions);
			case JsonTokenType.StartObject:
				return JsonSerializer.Deserialize<Dictionary<string, object?>> (ref reader, ValueOptions);
			default: throw new JsonException ("Unsupported device value.");
			}
		}

	public override void Write (Utf8JsonWriter writer, object value, JsonSerializerOptions options)
		{
		if (value.GetType () == typeof (object))
			{
			writer.WriteStartObject ();
			writer.WriteEndObject ();
			}
		else
			JsonSerializer.Serialize (writer, value, value.GetType (), options);
		}

	internal static readonly JsonSerializerOptions ValueOptions = new ()
		{
		Converters = { new DeviceValueConverter () }
		};
	}

internal sealed class DeviceValuesConverter : JsonConverter<IReadOnlyList<object?>>
	{
	public override IReadOnlyList<object?>? Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		=> JsonSerializer.Deserialize<List<object?>> (ref reader, DeviceValueConverter.ValueOptions);

	public override void Write (Utf8JsonWriter writer, IReadOnlyList<object?> value, JsonSerializerOptions options)
		=> JsonSerializer.Serialize (writer, value.ToList (), options);
	}
