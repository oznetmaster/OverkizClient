// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// Portions derived from python-overkiz-api © 2020 Mick Vleeshouwer — MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace OverKizApi;

/// <summary>
/// A <see cref="JsonConverterFactory"/> that deserialises enum values tolerantly:
/// any unrecognised string silently maps to the member named <c>Unknown</c> (value 0)
/// instead of throwing a <see cref="JsonException"/>.
/// </summary>
internal sealed class TolerantEnumConverterFactory : JsonConverterFactory
	{
	public override bool CanConvert (Type typeToConvert)
		=> typeToConvert.IsEnum;   // non-nullable only; runtime handles Nullable<T> wrapping

	public override JsonConverter? CreateConverter (Type typeToConvert, JsonSerializerOptions options)
		{
		Type converterType = typeof (TolerantEnumConverter<>).MakeGenericType (typeToConvert);
		return (JsonConverter?) Activator.CreateInstance (converterType);
		}
	}

internal sealed class TolerantEnumConverter<T> : JsonConverter<T> where T : struct, Enum
	{
	public override T Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
		if (reader.TokenType == JsonTokenType.String)
			{
			string? raw = reader.GetString ();
			if (Enum.TryParse<T> (raw, ignoreCase: true, out T result))
				return result;

			// Fall back to Unknown (0) if defined, otherwise default
			return Enum.IsDefined (typeof (T), "Unknown")
				? (T) Enum.ToObject (typeof (T), 0)
				: default;
			}

		return reader.TokenType == JsonTokenType.Number ? (T) Enum.ToObject (typeof (T), reader.GetInt32 ()) : default;
		}

	public override void Write (Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		=> writer.WriteStringValue (value.ToString ());
	}
