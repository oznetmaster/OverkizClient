// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace OverKizApi.Models;

// Gateways return either an array or one of three attributed response envelopes.
[JsonConverter (typeof (DeviceStatesPayloadConverter))]
internal sealed class DeviceStatesPayload
	{
	public IReadOnlyList<State> States { get; init; } = [];
	}

internal sealed class DeviceStatesPayloadConverter : JsonConverter<DeviceStatesPayload>
	{
	public override DeviceStatesPayload Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
		if (reader.TokenType == JsonTokenType.StartArray)
			return new DeviceStatesPayload { States = JsonSerializer.Deserialize<List<State>> (ref reader, options) ?? [] };
		if (reader.TokenType == JsonTokenType.StartObject)
			{
			DeviceStatesResponse response = JsonSerializer.Deserialize<DeviceStatesResponse> (ref reader, options)!;
			return new DeviceStatesPayload { States = response.States ?? response.DeviceStates ?? response.Values ?? [] };
			}
		throw new JsonException ("Device states must be an array or a response envelope.");
		}

	public override void Write (Utf8JsonWriter writer, DeviceStatesPayload value, JsonSerializerOptions options)
		=> JsonSerializer.Serialize (writer, value.States, options);
	}
