// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics.CodeAnalysis;
using OverKizApi.Enums;

namespace OverKizApi.Models;

/// <summary>
/// Describes a named Overkiz-compatible cloud server endpoint.
/// Instances are stored in <see cref="OverkizConst.SupportedServers"/> and passed to
/// <see cref="OverkizClient"/> on construction.
/// </summary>
public sealed class OverkizServer
	{
	/// <summary>Human-readable display name of the server / brand (e.g. "Somfy (Europe)").</summary>
	/// <value>Human-readable display name of the server / brand (e.g. "Somfy (Europe)").</value>
	[JsonPropertyName ("name")]
	public required string Name { get; init; }
	/// <summary>Base URL of the Overkiz <c>enduserAPI</c> for this server.</summary>
	/// <value>Base URL of the Overkiz <c>enduserAPI</c> for this server.</value>
	[JsonPropertyName ("endpoint")]
	public required string Endpoint { get; init; }
	/// <summary>Name of the hardware manufacturer or reseller associated with this server.</summary>
	/// <value>Name of the hardware manufacturer or reseller associated with this server.</value>
	[JsonPropertyName ("manufacturer")]
	public required string Manufacturer { get; init; }
	/// <summary>Optional URL of the end-user configuration portal for this server.</summary>
	/// <value>Optional URL of the end-user configuration portal for this server.</value>
	[JsonPropertyName ("configurationUrl")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ConfigurationUrl { get; init; }
	/// <summary>Whether requests to this server must be scoped to a selected gateway via a custom header.</summary>
	/// <value>Whether requests to this server must be scoped to a selected gateway via a custom header.</value>
	[JsonPropertyName ("requiresGatewaySelection")]
	public bool RequiresGatewaySelection { get; init; }
	}

/// <summary>Represents a Rexel gateway candidate discovered from the end-user directory API.</summary>
public sealed class GatewayCandidate
	{
	/// <summary>Rexel gateway identifier required in the <c>gatewayId</c> header.</summary>
	/// <value>Rexel gateway identifier required in the <c>gatewayId</c> header.</value>
	[JsonPropertyName ("gatewayId")]
	public required string GatewayId { get; init; }
	/// <summary>Rexel home identifier that owns the gateway.</summary>
	/// <value>Rexel home identifier that owns the gateway.</value>
	[JsonPropertyName ("homeId")]
	public required string HomeId { get; init; }
	/// <summary>Optional human-readable home label.</summary>
	/// <value>Optional human-readable home label.</value>
	[JsonPropertyName ("label")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Label { get; init; }
	/// <summary>Optional external identifier associated with the gateway; this is the Overkiz serial used in URL paths for Rexel cloud endpoints.</summary>
	/// <value>Optional external identifier associated with the gateway; this is the Overkiz serial used in URL paths for Rexel cloud endpoints.</value>
	[JsonPropertyName ("externalId")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ExternalId { get; init; }
	}

internal sealed class RexelHomeDirectoryEntry
	{
	[JsonPropertyName ("id")]
	public required string Id { get; init; }

	[JsonPropertyName ("label")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Label { get; init; }
	}

internal sealed class RexelGatewayDirectoryEntry
	{
	[JsonPropertyName ("gatewayId")]
	public required string GatewayId { get; init; }

	[JsonPropertyName ("externalId")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ExternalId { get; init; }
	}

internal sealed class GatewayTypeJsonConverter : JsonConverter<GatewayType?>
	{
	public override GatewayType? Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
		if (reader.TokenType == JsonTokenType.Null)
			return null;

		if (reader.TokenType == JsonTokenType.Number)
			return ReadNumericValue (reader.GetInt32 ());

		if (reader.TokenType == JsonTokenType.String)
			{
			string? raw = reader.GetString ();
			if (string.IsNullOrWhiteSpace (raw))
				return null;

			if (int.TryParse (raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numericValue))
				return ReadNumericValue (numericValue);

			return Enum.TryParse<GatewayType> (raw, ignoreCase: true, out GatewayType parsedValue)
				? parsedValue
				: GatewayType.Unknown;
			}

		throw new JsonException ($"Unsupported token {reader.TokenType} for gateway type.");
		}

	public override void Write (Utf8JsonWriter writer, GatewayType? value, JsonSerializerOptions options)
		{
		if (value is null)
			writer.WriteNullValue ();
		else
			writer.WriteNumberValue ((int) value.Value);
		}

	private static GatewayType ReadNumericValue (int rawValue)
		=> Enum.IsDefined (typeof (GatewayType), rawValue)
			? (GatewayType) rawValue
			: GatewayType.Unknown;
	}

internal sealed class GatewaySubTypeJsonConverter : JsonConverter<GatewaySubType?>
	{
	public override GatewaySubType? Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
		if (reader.TokenType == JsonTokenType.Null)
			return null;

		if (reader.TokenType == JsonTokenType.Number)
			return ReadNumericValue (reader.GetInt32 ());

		if (reader.TokenType == JsonTokenType.String)
			{
			string? raw = reader.GetString ();
			if (string.IsNullOrWhiteSpace (raw))
				return null;

			if (int.TryParse (raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numericValue))
				return ReadNumericValue (numericValue);

			return Enum.TryParse<GatewaySubType> (raw, ignoreCase: true, out GatewaySubType parsedValue)
				? parsedValue
				: GatewaySubType.Unknown;
			}

		throw new JsonException ($"Unsupported token {reader.TokenType} for gateway sub-type.");
		}

	public override void Write (Utf8JsonWriter writer, GatewaySubType? value, JsonSerializerOptions options)
		{
		if (value is null)
			writer.WriteNullValue ();
		else
			writer.WriteNumberValue ((int) value.Value);
		}

	private static GatewaySubType? ReadNumericValue (int rawValue)
		=> rawValue == 0
			? null
			: Enum.IsDefined (typeof (GatewaySubType), rawValue)
				? (GatewaySubType) rawValue
				: GatewaySubType.Unknown;
	}

/// <summary>
/// A command to be sent to a device as part of an execution action.
/// Commands are defined in the device's <see cref="Definition.Commands"/> list.
/// </summary>
public sealed class Command
	{
	/// <summary>Name of the command (e.g. <c>"open"</c>, <c>"setClosure"</c>).</summary>
	/// <value>Name of the command (e.g. <c>"open"</c>, <c>"setClosure"</c>).</value>
	[JsonPropertyName ("name")]
	public required string Name { get; init; }

	/// <summary>
	/// Ordered list of parameter values to pass to the command.
	/// May be <see langword="null"/> for zero-parameter commands.
	/// </summary>
	/// <value>
	/// Ordered list of parameter values to pass to the command.
	/// May be <see langword="null"/> for zero-parameter commands.
	/// </value>
	[JsonPropertyName ("parameters")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	[JsonConverter (typeof (DeviceValuesConverter))]
	public IReadOnlyList<object?>? Parameters { get; init; }
	}

/// <summary>
/// Discriminates the CLR type of a <see cref="State"/> value as reported by the Overkiz API.
/// </summary>
[SuppressMessage ("Naming", "CA1720:Identifier contains type name",
	Justification = "Member names mirror the Overkiz protocol's own type discriminator values and must be preserved for clarity.")]
[JsonConverter (typeof (TolerantEnumConverterFactory))]
public enum DataType
	{
	/// <summary>No value is present.</summary>
	None = 0,
	/// <summary>Value is a 32-bit integer.</summary>
	Integer = 1,
	/// <summary>Value is a double-precision floating-point number.</summary>
	Float = 2,
	/// <summary>Value is a UTF-8 string.</summary>
	String = 3,
	/// <summary>Value is an opaque binary blob (Base64-encoded in JSON).</summary>
	Blob = 4,
	/// <summary>Value is a boolean (<c>true</c> or <c>false</c>).</summary>
	Boolean = 6,
	/// <summary>Value is a JSON array serialised as a string.</summary>
	JsonArray = 10,
	/// <summary>Value is a JSON object serialised as a string.</summary>
	JsonObject = 11,
	/// <summary>Value is a date/time represented as a Unix epoch millisecond timestamp.</summary>
	Date = 13,
	}

/// <summary>
/// A named state value reported by a device (e.g. <c>"core:ClosureState"</c> = <c>50</c>).
/// Use <see cref="Type"/> to determine the appropriate typed accessor.
/// </summary>
public sealed partial class State
	{
	/// <summary>Qualified state name (e.g. <c>"core:ClosureState"</c>).</summary>
	/// <value>Qualified state name (e.g. <c>"core:ClosureState"</c>).</value>
	[JsonPropertyName ("name")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Name { get; init; }

	/// <summary>Discriminator that identifies the CLR type of <see cref="Value"/>.</summary>
	/// <value>Discriminator that identifies the CLR type of <see cref="Value"/>.</value>
	[JsonPropertyName ("type")]
	public DataType Type { get; init; }

	/// <summary>Vendor-defined value represented by CLR primitives, lists or dictionaries. Use the typed accessors where possible.</summary>
	/// <value>Vendor-defined value represented by CLR primitives, lists or dictionaries. Use the typed accessors where possible.</value>
	[JsonPropertyName ("value")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	[JsonConverter (typeof (DeviceValueConverter))]
	public object? Value { get; init; }

	/// <summary>
	/// Returns the value as <see cref="int"/>, or <see langword="null"/> when <see cref="Type"/> is <see cref="DataType.None"/>.
	/// </summary>
	/// <exception cref="InvalidCastException">Thrown when <see cref="Type"/> is not <see cref="DataType.Integer"/>.</exception>
	/// <value>Returns the value as <see cref="int"/>, or <see langword="null"/> when <see cref="Type"/> is <see cref="DataType.None"/>.</value>
	[JsonIgnore]
	public int? ValueAsInt => Type == DataType.None ? null
		: Type == DataType.Integer ? Convert.ToInt32 (Value, CultureInfo.InvariantCulture)
		: throw new InvalidCastException ($"{Name} is not an integer");

	/// <summary>
	/// Returns the value as <see cref="double"/>, or <see langword="null"/> when <see cref="Type"/> is <see cref="DataType.None"/>.
	/// Integer states are promoted to <see cref="double"/> automatically.
	/// </summary>
	/// <exception cref="InvalidCastException">Thrown when <see cref="Type"/> is neither <see cref="DataType.Float"/> nor <see cref="DataType.Integer"/>.</exception>
	/// <value>
	/// Returns the value as <see cref="double"/>, or <see langword="null"/> when <see cref="Type"/> is <see cref="DataType.None"/>.
	/// Integer states are promoted to <see cref="double"/> automatically.
	/// </value>
	[JsonIgnore]
	public double? ValueAsFloat => Type == DataType.None ? null
		: Type == DataType.Float ? Convert.ToDouble (Value, CultureInfo.InvariantCulture)
		: Type == DataType.Integer ? Convert.ToDouble (Value, CultureInfo.InvariantCulture)
		: throw new InvalidCastException ($"{Name} is not a float");

	/// <summary>
	/// Returns the value as <see cref="bool"/>, or <see langword="null"/> when <see cref="Type"/> is <see cref="DataType.None"/>.
	/// </summary>
	/// <exception cref="InvalidCastException">Thrown when <see cref="Type"/> is not <see cref="DataType.Boolean"/>.</exception>
	/// <value>Returns the value as <see cref="bool"/>, or <see langword="null"/> when <see cref="Type"/> is <see cref="DataType.None"/>.</value>
	[JsonIgnore]
	public bool? ValueAsBool => Type == DataType.None ? null
		: Type == DataType.Boolean ? Convert.ToBoolean (Value, CultureInfo.InvariantCulture)
		: throw new InvalidCastException ($"{Name} is not a boolean");

	/// <summary>
	/// Returns the value as <see cref="string"/>, or <see langword="null"/> when <see cref="Type"/> is <see cref="DataType.None"/>.
	/// </summary>
	/// <exception cref="InvalidCastException">Thrown when <see cref="Type"/> is not <see cref="DataType.String"/>.</exception>
	/// <value>Returns the value as <see cref="string"/>, or <see langword="null"/> when <see cref="Type"/> is <see cref="DataType.None"/>.</value>
	[JsonIgnore]
	public string? ValueAsStr => Type == DataType.None ? null
		: Type == DataType.String ? Value?.ToString ()
		: throw new InvalidCastException ($"{Name} is not a string");
	}

/// <summary>
/// An ordered, name-indexed collection of <see cref="State"/> objects for a device.
/// Implements <see cref="IEnumerable{T}"/> so it can be used directly in foreach loops.
/// </summary>
/// <remarks>Initialises the collection from an optional sequence of states.</remarks>
/// <param name="states">Initial states; pass <see langword="null"/> or omit for an empty collection.</param>
[JsonConverter (typeof (StatesJsonConverter))]
public sealed partial class States (IEnumerable<State>? states = null) : System.Collections.Generic.IEnumerable<State>
	{
	private readonly List<State> _states = states?.ToList () ?? [];

	/// <summary>Returns the <see cref="State"/> with the given qualified <paramref name="name"/>, or <see langword="null"/> if not found.</summary>
	/// <param name="name">Qualified state name (e.g. <c>"core:ClosureState"</c>).</param>
	/// <value>Returns the <see cref="State"/> with the given qualified <paramref name="name"/>, or <see langword="null"/> if not found.</value>
	public State? this [string name] => _states.FirstOrDefault (s => s.Name == name);

	/// <summary>Returns <see langword="true"/> if a state with the given qualified <paramref name="name"/> is present.</summary>
	/// <param name="name">Qualified state name to look up.</param>
	/// <returns>True if the named state exists; otherwise false.</returns>
	public bool Contains (string name) => this [name] is not null;

	/// <summary>Enumerates the states in collection order.</summary>
	/// <returns>An enumerator over the stored states.</returns>
	public System.Collections.Generic.IEnumerator<State> GetEnumerator () => _states.GetEnumerator ();

	/// <inheritdoc/>
	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator () => _states.GetEnumerator ();
	}

/// <summary>Deserialises a JSON array of <see cref="State"/> objects into a <see cref="States"/> collection.</summary>
internal sealed class StatesJsonConverter : JsonConverter<States>
	{
	public override States Read (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
		List<State>? list = JsonSerializer.Deserialize<List<State>> (ref reader, options);
		return new States (list);
		}

	public override void Write (Utf8JsonWriter writer, States value, JsonSerializerOptions options)
		=> JsonSerializer.Serialize (writer, value.ToList (), options);
	}

/// <summary>Geographical and address metadata associated with a <see cref="Setup"/>.</summary>
public sealed class Location
	{
	/// <summary>Unix epoch millisecond timestamp when the location was created.</summary>
	/// <value>Unix epoch millisecond timestamp when the location was created.</value>
	[JsonPropertyName ("creationTime")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	public long CreationTime { get; init; }
	/// <summary>Unix epoch millisecond timestamp of the last update, or <see langword="null"/> if never updated.</summary>
	/// <value>Unix epoch millisecond timestamp of the last update, or <see langword="null"/> if never updated.</value>
	[JsonPropertyName ("lastUpdateTime")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public long? LastUpdateTime { get; init; }
	/// <summary>City name.</summary>
	/// <value>City name.</value>
	[JsonPropertyName ("city")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? City { get; init; }
	/// <summary>Country name.</summary>
	/// <value>Country name.</value>
	[JsonPropertyName ("country")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Country { get; init; }
	/// <summary>Postal / ZIP code.</summary>
	/// <value>Postal / ZIP code.</value>
	[JsonPropertyName ("postalCode")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? PostalCode { get; init; }
	/// <summary>First line of the street address.</summary>
	/// <value>First line of the street address.</value>
	[JsonPropertyName ("addressLine1")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? AddressLine1 { get; init; }
	/// <summary>Second line of the street address (apartment, suite, etc.).</summary>
	/// <value>Second line of the street address (apartment, suite, etc.).</value>
	[JsonPropertyName ("addressLine2")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? AddressLine2 { get; init; }
	/// <summary>IANA time zone identifier (e.g. <c>"Europe/Paris"</c>).</summary>
	/// <value>IANA time zone identifier (e.g. <c>"Europe/Paris"</c>).</value>
	[JsonPropertyName ("timezone")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Timezone { get; init; }
	/// <summary>Longitude coordinate in decimal degrees.</summary>
	/// <value>Longitude coordinate in decimal degrees.</value>
	[JsonPropertyName ("longitude")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public double? Longitude { get; init; }
	/// <summary>Latitude coordinate in decimal degrees.</summary>
	/// <value>Latitude coordinate in decimal degrees.</value>
	[JsonPropertyName ("latitude")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public double? Latitude { get; init; }
	/// <summary>Twilight calculation mode (0 = civil, 1 = nautical, 2 = astronomical, 3 = custom city).</summary>
	/// <value>Twilight calculation mode (0 = civil, 1 = nautical, 2 = astronomical, 3 = custom city).</value>
	[JsonPropertyName ("twilightMode")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	public int TwilightMode { get; init; }
	/// <summary>Twilight angle used for custom-mode calculations.</summary>
	/// <value>Twilight angle used for custom-mode calculations.</value>
	[JsonPropertyName ("twilightAngle")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? TwilightAngle { get; init; }
	/// <summary>City used for twilight calculations when <see cref="TwilightMode"/> is 3.</summary>
	/// <value>City used for twilight calculations when <see cref="TwilightMode"/> is 3.</value>
	[JsonPropertyName ("twilightCity")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? TwilightCity { get; init; }
	/// <summary>Minutes after sunset at summer solstice (used for dusk offset calculations).</summary>
	/// <value>Minutes after sunset at summer solstice (used for dusk offset calculations).</value>
	[JsonPropertyName ("summerSolsticeDuskMinutes")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? SummerSolsticeDuskMinutes { get; init; }
	/// <summary>Minutes after sunset at winter solstice (used for dusk offset calculations).</summary>
	/// <value>Minutes after sunset at winter solstice (used for dusk offset calculations).</value>
	[JsonPropertyName ("winterSolsticeDuskMinutes")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? WinterSolsticeDuskMinutes { get; init; }
	/// <summary>Whether manual dawn/dusk offsets are enabled.</summary>
	/// <value>Whether manual dawn/dusk offsets are enabled.</value>
	[JsonPropertyName ("twilightOffsetEnabled")]
	public bool TwilightOffsetEnabled { get; init; }
	/// <summary>Manual dawn offset in minutes (positive = later, negative = earlier).</summary>
	/// <value>Manual dawn offset in minutes (positive = later, negative = earlier).</value>
	[JsonPropertyName ("dawnOffset")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	public int DawnOffset { get; init; }
	/// <summary>Manual dusk offset in minutes (positive = later, negative = earlier).</summary>
	/// <value>Manual dusk offset in minutes (positive = later, negative = earlier).</value>
	[JsonPropertyName ("duskOffset")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	public int DuskOffset { get; init; }
	}

/// <summary>Describes a single command that a device exposes in its capability definition.</summary>
public sealed class CommandDefinition
	{
	/// <summary>Name of the command (e.g. <c>"setClosure"</c>).</summary>
	/// <value>Name of the command (e.g. <c>"setClosure"</c>).</value>
	[JsonPropertyName ("commandName")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? CommandName { get; init; }
	/// <summary>Number of parameters the command accepts.</summary>
	/// <value>Number of parameters the command accepts.</value>
	[JsonPropertyName ("nParams")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	public int NParams { get; init; }
	}

/// <summary>Describes a state that a device can report, including its type and allowed values.</summary>
public sealed class StateDefinition
	{
	/// <summary>Qualified name of the state (e.g. <c>"core:ClosureState"</c>).</summary>
	/// <value>Qualified name of the state (e.g. <c>"core:ClosureState"</c>).</value>
	[JsonPropertyName ("qualifiedName")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? QualifiedName { get; init; }
	/// <summary>Data type name as returned by the API (e.g. <c>"Integer"</c>).</summary>
	/// <value>Data type name as returned by the API (e.g. <c>"Integer"</c>).</value>
	[JsonPropertyName ("type")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Type { get; init; }
	/// <summary>Allowed string values for enum-type states, or <see langword="null"/> for numeric/free-form states.</summary>
	/// <value>Allowed string values for enum-type states, or <see langword="null"/> for numeric/free-form states.</value>
	[JsonPropertyName ("values")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public IReadOnlyList<string>? Values { get; init; }
	}

/// <summary>
/// Full capability definition for a device, describing every command it accepts
/// and every state it can report.
/// </summary>
public sealed class Definition
	{
	/// <summary>All commands supported by the device.</summary>
	/// <value>All commands supported by the device.</value>
	[JsonPropertyName ("commands")]
	public IReadOnlyList<CommandDefinition> Commands { get; init; } = [];
	/// <summary>All states the device can report.</summary>
	/// <value>All states the device can report.</value>
	[JsonPropertyName ("states")]
	public IReadOnlyList<StateDefinition> States { get; init; } = [];
	/// <summary>UI widget name used by the Overkiz app to render the device.</summary>
	/// <value>UI widget name used by the Overkiz app to render the device.</value>
	[JsonPropertyName ("widgetName")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? WidgetName { get; init; }
	/// <summary>UI class name used by the Overkiz app to categorise the device.</summary>
	/// <value>UI class name used by the Overkiz app to categorise the device.</value>
	[JsonPropertyName ("uiClass")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? UiClass { get; init; }
	/// <summary>Qualified name of the device controllable type (e.g. <c>"io:RollerShutterGenericIOComponent"</c>).</summary>
	/// <value>Qualified name of the device controllable type (e.g. <c>"io:RollerShutterGenericIOComponent"</c>).</value>
	[JsonPropertyName ("qualifiedName")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? QualifiedName { get; init; }
	}

/// <summary>
/// Represents a physical or virtual device registered in a setup.
/// The <see cref="DeviceUrl"/> uniquely identifies the device across the entire API.
/// </summary>
public sealed partial class Device
	{
	private const string HITACHI_HLRR_WIFI_PREFIX = "hlrrwifi";

	/// <summary>
	/// Unique device URL in the format <c>protocol://gatewayId/deviceAddress[#subsystemId]</c>
	/// (e.g. <c>io://1234-5678-9012/12345678</c>).
	/// </summary>
	/// <value>
	/// Unique device URL in the format <c>protocol://gatewayId/deviceAddress[#subsystemId]</c>
	/// (e.g. <c>io://1234-5678-9012/12345678</c>).
	/// </value>
	[JsonPropertyName ("deviceURL")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? DeviceUrl { get; init; }
	/// <summary>Alias for <see cref="DeviceUrl"/>; provided for convenience.</summary>
	/// <value>Alias for <see cref="DeviceUrl"/>; provided for convenience.</value>
	[JsonIgnore]
	public string? Id => DeviceUrl;
	/// <summary>Device attributes (manufacturer-specific, immutable metadata states).</summary>
	/// <value>Device attributes (manufacturer-specific, immutable metadata states).</value>
	[JsonPropertyName ("attributes")]
	public States Attributes { get; init; } = new ();
	/// <summary>Whether the device is currently reachable by the gateway.</summary>
	/// <value>Whether the device is currently reachable by the gateway.</value>
	[JsonPropertyName ("available")]
	public bool Available { get; init; }
	/// <summary>Full capability definition (commands and states) for this device.</summary>
	/// <value>Full capability definition (commands and states) for this device.</value>
	[JsonPropertyName ("definition")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public Definition? Definition { get; init; }
	/// <summary>Whether the device is enabled in the user's setup.</summary>
	/// <value>Whether the device is enabled in the user's setup.</value>
	[JsonPropertyName ("enabled")]
	public bool Enabled { get; init; }
	/// <summary>User-visible label for the device.</summary>
	/// <value>User-visible label for the device.</value>
	[JsonPropertyName ("label")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Label { get; init; }
	/// <summary>Controllable type name (e.g. <c>"io:RollerShutterGenericIOComponent"</c>).</summary>
	/// <value>Controllable type name (e.g. <c>"io:RollerShutterGenericIOComponent"</c>).</value>
	[JsonPropertyName ("controllableName")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ControllableName { get; init; }
	/// <summary>Current live states reported by the device.</summary>
	/// <value>Current live states reported by the device.</value>
	[JsonPropertyName ("states")]
	public States States { get; init; } = new ();
	/// <summary>Manufacturer-defined data properties (opaque).</summary>
	/// <value>Manufacturer-defined data properties (opaque).</value>
	[JsonPropertyName ("dataProperties")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	[JsonConverter (typeof (DeviceValuesConverter))]
	public IReadOnlyList<object?>? DataProperties { get; init; }
	/// <summary>Whether the device is an actuator, a sensor, or unknown.</summary>
	/// <value>Whether the device is an actuator, a sensor, or unknown.</value>
	[JsonPropertyName ("type")]
	public ProductType Type { get; init; }
	/// <summary>OID of the place (room/floor) the device is assigned to.</summary>
	/// <value>OID of the place (room/floor) the device is assigned to.</value>
	[JsonPropertyName ("placeOid")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? PlaceOid { get; init; }

	// Parsed from device URL
	/// <summary>Communication protocol parsed from the device URL prefix (e.g. <see cref="Enums.Protocol.Io"/>).</summary>
	/// <value>Communication protocol parsed from the device URL prefix (e.g. <see cref="Enums.Protocol.Io"/>).</value>
	[JsonIgnore]
	public Protocol? Protocol => TryParseDeviceUrl (DeviceUrl, out ParsedDeviceUrl? parsed) && parsed is not null
				? parsed.Protocol
				: null;
	/// <summary>Gateway serial number parsed from the device URL.</summary>
	/// <value>Gateway serial number parsed from the device URL.</value>
	[JsonIgnore]
	public string? GatewayId => TryParseDeviceUrl (DeviceUrl, out ParsedDeviceUrl? parsed) && parsed is not null
				? parsed.GatewayId
				: null;
	/// <summary>Device address portion parsed from the device URL.</summary>
	/// <value>Device address portion parsed from the device URL.</value>
	[JsonIgnore]
	public string? DeviceAddress => TryParseDeviceUrl (DeviceUrl, out ParsedDeviceUrl? parsed) && parsed is not null
				? parsed.DeviceAddress
				: null;
	/// <summary>Sub-system index parsed from the device URL fragment, or <see langword="null"/> for top-level devices.</summary>
	/// <value>Sub-system index parsed from the device URL fragment, or <see langword="null"/> for top-level devices.</value>
	[JsonIgnore]
	public int? SubsystemId => TryParseDeviceUrl (DeviceUrl, out ParsedDeviceUrl? parsed) && parsed is not null
				? parsed.SubsystemId
				: null;
	/// <summary><see langword="true"/> if this device is a sub-device (has a <see cref="SubsystemId"/>).</summary>
	/// <value><see langword="true"/> if this device is a sub-device (has a <see cref="SubsystemId"/>).</value>
	[JsonIgnore]
	public bool IsSubDevice => SubsystemId.HasValue;

	/// <summary>UI class parsed from <see cref="Definition.UiClass"/>.</summary>
	/// <value>UI class parsed from <see cref="Definition.UiClass"/>.</value>
	[JsonIgnore]
	public UIClass? UiClass => Enum.TryParse<UIClass> (Definition?.UiClass, ignoreCase: true, out UIClass value) ? value : null;
	/// <summary>UI widget parsed from <see cref="Definition.WidgetName"/>.</summary>
	/// <value>UI widget parsed from <see cref="Definition.WidgetName"/>.</value>
	[JsonIgnore]
	public UIWidget? Widget => Enum.TryParse<UIWidget> (Definition?.WidgetName, ignoreCase: true, out UIWidget value) ? value : null;

	private static bool TryParseDeviceUrl (string? deviceUrl, out ParsedDeviceUrl? parsed)
		{
		parsed = null;
		if (deviceUrl is not string normalizedDeviceUrl || string.IsNullOrWhiteSpace (normalizedDeviceUrl))
			return false;

		int schemeIndex = normalizedDeviceUrl.IndexOf ("://", StringComparison.Ordinal);
		if (schemeIndex <= 0)
			return false;

		string protocolSegment = normalizedDeviceUrl[..schemeIndex];
		string remainder = normalizedDeviceUrl[(schemeIndex + 3)..];
		int slashIndex = remainder.IndexOf ('/');
		if (slashIndex <= 0 || slashIndex == remainder.Length - 1)
			return false;

		string gatewayId = remainder[..slashIndex];
		string deviceAddressSegment = remainder[(slashIndex + 1)..];
		string? deviceAddress = deviceAddressSegment;
		int? subsystemId = null;

		int hashIndex = deviceAddressSegment.IndexOf ('#');
		if (hashIndex >= 0)
			{
				deviceAddress = deviceAddressSegment[..hashIndex];
				string subsystemSegment = deviceAddressSegment[(hashIndex + 1)..];
				subsystemId = int.TryParse (subsystemSegment, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedSubsystemId)
					? parsedSubsystemId
					: null;
			}

		Protocol protocol = protocolSegment.Equals (HITACHI_HLRR_WIFI_PREFIX, StringComparison.OrdinalIgnoreCase)
			? Enums.Protocol.HlrrWifi
			: Enum.TryParse<Protocol> (protocolSegment, ignoreCase: true, out Protocol parsedProtocol)
				? parsedProtocol
				: Enums.Protocol.Unknown;

		parsed = new ParsedDeviceUrl
			{
			Protocol = protocol,
			GatewayId = gatewayId,
			DeviceAddress = deviceAddress,
			SubsystemId = subsystemId,
			};
		return parsed is not null;
		}

	private sealed class ParsedDeviceUrl
		{
		[JsonPropertyName ("protocol")]
		public required Protocol Protocol { get; init; }
		[JsonPropertyName ("gatewayId")]
		public required string GatewayId { get; init; }
		[JsonPropertyName ("deviceAddress")]
		[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string? DeviceAddress { get; init; }
		[JsonPropertyName ("subsystemId")]
		[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
		[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
		public int? SubsystemId { get; init; }
		}
	}

/// <summary>Real-time connectivity information for a <see cref="Gateway"/>.</summary>
public sealed class Connectivity
	{
	/// <summary>Connectivity status string as returned by the API (e.g. <c>"ok"</c>).</summary>
	/// <value>Connectivity status string as returned by the API (e.g. <c>"ok"</c>).</value>
	[JsonPropertyName ("status")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Status { get; init; }
	/// <summary>Protocol version reported by the gateway firmware, or <see langword="null"/> if unavailable.</summary>
	/// <value>Protocol version reported by the gateway firmware, or <see langword="null"/> if unavailable.</value>
	[JsonPropertyName ("protocolVersion")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ProtocolVersion { get; init; }
	}

/// <summary>Information about a third-party partner integration registered on a gateway.</summary>
public sealed class Partner
	{
	/// <summary>ISO 8601 timestamp when the partner integration was activated.</summary>
	/// <value>ISO 8601 timestamp when the partner integration was activated.</value>
	[JsonPropertyName ("activationTime")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ActivationTime { get; init; }
	/// <summary>Display name of the partner (e.g. <c>"Amazon Alexa"</c>).</summary>
	/// <value>Display name of the partner (e.g. <c>"Amazon Alexa"</c>).</value>
	[JsonPropertyName ("name")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Name { get; init; }
	/// <summary>Unique identifier of the partner integration.</summary>
	/// <value>Unique identifier of the partner integration.</value>
	[JsonPropertyName ("id")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Id { get; init; }
	/// <summary>Current integration status (e.g. <c>"enabled"</c>).</summary>
	/// <value>Current integration status (e.g. <c>"enabled"</c>).</value>
	[JsonPropertyName ("status")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Status { get; init; }
	}

/// <summary>
/// Represents a physical Overkiz-compatible gateway (hub) registered in a setup.
/// A setup may have more than one gateway.
/// </summary>
public sealed class Gateway
	{
	/// <summary>Unique gateway serial number (e.g. <c>"1234-5678-9012"</c>).</summary>
	/// <value>Unique gateway serial number (e.g. <c>"1234-5678-9012"</c>).</value>
	[JsonPropertyName ("gatewayId")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? GatewayId { get; init; }
	/// <summary>Alias for <see cref="GatewayId"/>; provided for convenience.</summary>
	/// <value>Alias for <see cref="GatewayId"/>; provided for convenience.</value>
	[JsonIgnore]
	public string? Id => GatewayId;
	/// <summary>Comma-separated list of functional capability flags reported by the gateway.</summary>
	/// <value>Comma-separated list of functional capability flags reported by the gateway.</value>
	[JsonPropertyName ("functions")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Functions { get; init; }
	/// <summary>Whether the gateway is currently sending heartbeat signals to the cloud.</summary>
	/// <value>Whether the gateway is currently sending heartbeat signals to the cloud.</value>
	[JsonPropertyName ("alive")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? Alive { get; init; }
	/// <summary>Gateway operating mode string (see <see cref="GatewayMode"/>).</summary>
	/// <value>Gateway operating mode string (see <see cref="GatewayMode"/>).</value>
	[JsonPropertyName ("mode")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Mode { get; init; }
	/// <summary>OID of the place (room/floor) the gateway is assigned to.</summary>
	/// <value>OID of the place (room/floor) the gateway is assigned to.</value>
	[JsonPropertyName ("placeOid")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? PlaceOid { get; init; }
	/// <summary>Whether the gateway's real-time clock is synchronised.</summary>
	/// <value>Whether the gateway's real-time clock is synchronised.</value>
	[JsonPropertyName ("timeReliable")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? TimeReliable { get; init; }
	/// <summary>Live connectivity state of the gateway.</summary>
	/// <value>Live connectivity state of the gateway.</value>
	[JsonPropertyName ("connectivity")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public Connectivity? Connectivity { get; init; }
	/// <summary>Whether the gateway firmware is up to date.</summary>
	/// <value>Whether the gateway firmware is up to date.</value>
	[JsonPropertyName ("upToDate")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? UpToDate { get; init; }
	/// <summary>Current firmware update lifecycle state.</summary>
	/// <value>Current firmware update lifecycle state.</value>
	[JsonPropertyName ("updateStatus")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public GatewayUpdateStatus? UpdateStatus { get; init; }
	/// <summary>Whether a device synchronisation cycle is in progress.</summary>
	/// <value>Whether a device synchronisation cycle is in progress.</value>
	[JsonPropertyName ("syncInProgress")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? SyncInProgress { get; init; }
	/// <summary>Third-party partner integrations activated on this gateway.</summary>
	/// <value>Third-party partner integrations activated on this gateway.</value>
	[JsonPropertyName ("partners")]
	public IReadOnlyList<Partner> Partners { get; init; } = [];
	/// <summary>Hardware product family of the gateway.</summary>
	/// <value>Hardware product family of the gateway.</value>
	[JsonConverter (typeof (GatewayTypeJsonConverter))]
	[JsonPropertyName ("type")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public GatewayType? Type { get; init; }
	/// <summary>Hardware model / product family of the gateway.</summary>
	/// <value>Hardware model / product family of the gateway.</value>
	[JsonConverter (typeof (GatewaySubTypeJsonConverter))]
	[JsonPropertyName ("subType")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public GatewaySubType? SubType { get; init; }
	}

/// <summary>A feature flag or subscription option active on a setup.</summary>
public sealed class Feature
	{
	/// <summary>Internal feature identifier name.</summary>
	/// <value>Internal feature identifier name.</value>
	[JsonPropertyName ("name")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Name { get; init; }
	/// <summary>Source that activated this feature (e.g. reseller or app bundle identifier).</summary>
	/// <value>Source that activated this feature (e.g. reseller or app bundle identifier).</value>
	[JsonPropertyName ("source")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Source { get; init; }
	}

/// <summary>References a device that belongs to a <see cref="Zone"/>.</summary>
public sealed class ZoneItem
	{
	/// <summary>Type of item (e.g. <c>"DEVICE"</c>).</summary>
	/// <value>Type of item (e.g. <c>"DEVICE"</c>).</value>
	[JsonPropertyName ("itemType")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ItemType { get; init; }
	/// <summary>Opaque OID of the referenced device.</summary>
	/// <value>Opaque OID of the referenced device.</value>
	[JsonPropertyName ("deviceOid")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? DeviceOid { get; init; }
	/// <summary>URL of the referenced device; matches <see cref="Device.DeviceUrl"/>.</summary>
	/// <value>URL of the referenced device; matches <see cref="Device.DeviceUrl"/>.</value>
	[JsonPropertyName ("deviceURL")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? DeviceUrl { get; init; }
	}

/// <summary>A logical zone.</summary>
public sealed class Zone
	{
	/// <summary>Unix epoch millisecond timestamp when the zone was created.</summary>
	/// <value>Unix epoch millisecond timestamp when the zone was created.</value>
	[JsonPropertyName ("creationTime")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	public long CreationTime { get; init; }
	/// <summary>Unix epoch millisecond timestamp of the most recent update.</summary>
	/// <value>Unix epoch millisecond timestamp of the most recent update.</value>
	[JsonPropertyName ("lastUpdateTime")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	public long LastUpdateTime { get; init; }
	/// <summary>User-visible label for the zone.</summary>
	/// <value>User-visible label for the zone.</value>
	[JsonPropertyName ("label")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Label { get; init; }
	/// <summary>Zone type discriminator (opaque integer).</summary>
	/// <value>Zone type discriminator (opaque integer).</value>
	[JsonPropertyName ("type")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	public int Type { get; init; }
	/// <summary>Devices belonging to this zone.</summary>
	/// <value>Devices belonging to this zone.</value>
	[JsonPropertyName ("items")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public IReadOnlyList<ZoneItem>? Items { get; init; }
	/// <summary>External system OID, if this zone is synchronised with a third-party integration.</summary>
	/// <value>External system OID, if this zone is synchronised with a third-party integration.</value>
	[JsonPropertyName ("externalOid")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ExternalOid { get; init; }
	/// <summary>Arbitrary metadata string attached to this zone.</summary>
	/// <value>Arbitrary metadata string attached to this zone.</value>
	[JsonPropertyName ("metadata")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Metadata { get; init; }
	/// <summary>Unique OID of this zone.</summary>
	/// <value>Unique OID of this zone.</value>
	[JsonPropertyName ("oid")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Oid { get; init; }
	}

/// <summary>A place in the home hierarchy (house, floor, or room).</summary>
public sealed class Place
	{
	/// <summary>Unix epoch millisecond timestamp when the place was created.</summary>
	/// <value>Unix epoch millisecond timestamp when the place was created.</value>
	[JsonPropertyName ("creationTime")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	public long CreationTime { get; init; }
	/// <summary>Unix epoch millisecond timestamp of the most recent update, or <see langword="null"/> if never updated.</summary>
	/// <value>Unix epoch millisecond timestamp of the most recent update, or <see langword="null"/> if never updated.</value>
	[JsonPropertyName ("lastUpdateTime")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public long? LastUpdateTime { get; init; }
	/// <summary>User-visible label for the place (e.g. <c>"Living Room"</c>).</summary>
	/// <value>User-visible label for the place (e.g. <c>"Living Room"</c>).</value>
	[JsonPropertyName ("label")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Label { get; init; }
	/// <summary>Place type: 0 = house, 1 = floor, 2 = room.</summary>
	/// <value>Place type: 0 = house, 1 = floor, 2 = room.</value>
	[JsonPropertyName ("type")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	public int Type { get; init; }
	/// <summary>Unique OID of this place.</summary>
	/// <value>Unique OID of this place.</value>
	[JsonPropertyName ("oid")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Oid { get; init; }
	/// <summary>Alias for <see cref="Oid"/>; provided for convenience.</summary>
	/// <value>Alias for <see cref="Oid"/>; provided for convenience.</value>
	[JsonIgnore]
	public string? Id => Oid;
	/// <summary>Child places (floors within a house, rooms within a floor).</summary>
	/// <value>Child places (floors within a house, rooms within a floor).</value>
	[JsonPropertyName ("subPlaces")]
	public IReadOnlyList<Place> SubPlaces { get; init; } = [];
	}

/// <summary>
/// The complete home setup as returned by the <c>setup</c> endpoint.
/// Contains all gateways, devices, zones, places and location metadata.
/// </summary>
public sealed class Setup
	{
	/// <summary>Unique identifier of the setup.</summary>
	/// <value>Unique identifier of the setup.</value>
	[JsonPropertyName ("id")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Id { get; init; }
	/// <summary>Unix epoch millisecond timestamp when the setup was created.</summary>
	/// <value>Unix epoch millisecond timestamp when the setup was created.</value>
	[JsonPropertyName ("creationTime")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	public long CreationTime { get; init; }
	/// <summary>Unix epoch millisecond timestamp of the most recent change, or <see langword="null"/> if unchanged.</summary>
	/// <value>Unix epoch millisecond timestamp of the most recent change, or <see langword="null"/> if unchanged.</value>
	[JsonPropertyName ("lastUpdateTime")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public long? LastUpdateTime { get; init; }
	/// <summary>Geographical location and twilight settings for the home.</summary>
	/// <value>Geographical location and twilight settings for the home.</value>
	[JsonPropertyName ("location")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public Location? Location { get; init; }
	/// <summary>All gateways (hubs) registered in this setup.</summary>
	/// <value>All gateways (hubs) registered in this setup.</value>
	[JsonPropertyName ("gateways")]
	public IReadOnlyList<Gateway> Gateways { get; init; } = [];
	/// <summary>All devices registered across all gateways in this setup.</summary>
	/// <value>All devices registered across all gateways in this setup.</value>
	[JsonPropertyName ("devices")]
	public IReadOnlyList<Device> Devices { get; init; } = [];
	/// <summary>Logical zones grouping devices, or <see langword="null"/> if the server does not return zones.</summary>
	/// <value>Logical zones grouping devices, or <see langword="null"/> if the server does not return zones.</value>
	[JsonPropertyName ("zones")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public IReadOnlyList<Zone>? Zones { get; init; }
	/// <summary>Reseller delegation type string, or <see langword="null"/> if not applicable.</summary>
	/// <value>Reseller delegation type string, or <see langword="null"/> if not applicable.</value>
	[JsonPropertyName ("resellerDelegationType")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ResellerDelegationType { get; init; }
	/// <summary>Opaque OID of the setup.</summary>
	/// <value>Opaque OID of the setup.</value>
	[JsonPropertyName ("oid")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Oid { get; init; }
	/// <summary>Root of the place hierarchy (house → floors → rooms).</summary>
	/// <value>Root of the place hierarchy (house → floors → rooms).</value>
	[JsonPropertyName ("rootPlace")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public Place? RootPlace { get; init; }
	/// <summary>Feature flags / subscriptions active on this setup.</summary>
	/// <value>Feature flags / subscriptions active on this setup.</value>
	[JsonPropertyName ("features")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public IReadOnlyList<Feature>? Features { get; init; }
	}

/// <summary>
/// A set of commands targeting a single device, used as a unit within an execution request.
/// One <see cref="Action"/> maps directly to one device URL.
/// </summary>
public sealed class Action
	{
	/// <summary>URL of the device these commands are addressed to.</summary>
	/// <value>URL of the device these commands are addressed to.</value>
	[JsonPropertyName ("deviceURL")]
	public required string DeviceUrl { get; init; }
	/// <summary>Ordered list of commands.</summary>
	/// <value>Ordered list of commands.</value>
	[JsonPropertyName ("commands")]
	public IReadOnlyList<Command> Commands { get; init; } = [];
	}

/// <summary>
/// A named scenario (action group) stored in the setup that can be triggered by name or OID.
/// Scenarios can include optional notification settings and scheduled triggers.
/// </summary>
public sealed class Scenario
	{
	/// <summary>Unique OID of this scenario.</summary>
	/// <value>Unique OID of this scenario.</value>
	[JsonPropertyName ("oid")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Oid { get; init; }
	/// <summary>Alias for <see cref="Oid"/>; provided for convenience.</summary>
	/// <value>Alias for <see cref="Oid"/>; provided for convenience.</value>
	[JsonIgnore]
	public string? Id => Oid;
	/// <summary>Unix epoch millisecond timestamp when the scenario was created.</summary>
	/// <value>Unix epoch millisecond timestamp when the scenario was created.</value>
	[JsonPropertyName ("creationTime")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	public long CreationTime { get; init; }
	/// <summary>Unix epoch millisecond timestamp of the most recent update, or <see langword="null"/> if never updated.</summary>
	/// <value>Unix epoch millisecond timestamp of the most recent update, or <see langword="null"/> if never updated.</value>
	[JsonPropertyName ("lastUpdateTime")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public long? LastUpdateTime { get; init; }
	/// <summary>User-visible label for the scenario.</summary>
	/// <value>User-visible label for the scenario.</value>
	[JsonPropertyName ("label")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Label { get; init; }
	/// <summary>Arbitrary metadata string attached to this scenario.</summary>
	/// <value>Arbitrary metadata string attached to this scenario.</value>
	[JsonPropertyName ("metadata")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Metadata { get; init; }
	/// <summary>Whether this scenario appears as a shortcut in the Overkiz app.</summary>
	/// <value>Whether this scenario appears as a shortcut in the Overkiz app.</value>
	[JsonPropertyName ("shortcut")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public bool? Shortcut { get; init; }
	/// <summary>Bitmask controlling which notification types fire when this scenario runs.</summary>
	/// <value>Bitmask controlling which notification types fire when this scenario runs.</value>
	[JsonPropertyName ("notificationTypeMask")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? NotificationTypeMask { get; init; }
	/// <summary>Condition expression evaluated before sending a notification.</summary>
	/// <value>Condition expression evaluated before sending a notification.</value>
	[JsonPropertyName ("notificationCondition")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? NotificationCondition { get; init; }
	/// <summary>Body text of the push notification sent when this scenario executes.</summary>
	/// <value>Body text of the push notification sent when this scenario executes.</value>
	[JsonPropertyName ("notificationText")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? NotificationText { get; init; }
	/// <summary>Title of the push notification sent when this scenario executes.</summary>
	/// <value>Title of the push notification sent when this scenario executes.</value>
	[JsonPropertyName ("notificationTitle")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? NotificationTitle { get; init; }
	/// <summary>The actions (device command sets) that make up this scenario.</summary>
	/// <value>The actions (device command sets) that make up this scenario.</value>
	[JsonPropertyName ("actions")]
	public IReadOnlyList<Action> Actions { get; init; } = [];
	}

/// <summary>
/// A snapshot of a single device state as embedded within an <see cref="EventObject"/> payload.
/// Semantically identical to <see cref="State"/> but typed separately by the API.
/// </summary>
public sealed class EventState
	{
	/// <summary>Qualified state name (e.g. <c>"core:ClosureState"</c>).</summary>
	/// <value>Qualified state name (e.g. <c>"core:ClosureState"</c>).</value>
	[JsonPropertyName ("name")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Name { get; init; }
	/// <summary>Discriminator identifying the CLR type of <see cref="Value"/>.</summary>
	/// <value>Discriminator identifying the CLR type of <see cref="Value"/>.</value>
	[JsonPropertyName ("type")]
	public DataType Type { get; init; }
	/// <summary>Vendor-defined value represented by CLR primitives, lists or dictionaries.</summary>
	/// <value>Vendor-defined value represented by CLR primitives, lists or dictionaries.</value>
	[JsonPropertyName ("value")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	[JsonConverter (typeof (DeviceValueConverter))]
	public object? Value { get; init; }
	}

/// <summary>
/// An event pushed to a registered event listener by the Overkiz gateway or cloud.
/// The meaningful fields depend on the event type — check <c>Name</c> (mapped from <see cref="EventName"/>)
/// before reading type-specific fields.
/// </summary>
public sealed class EventObject
	{
	/// <summary>Unix epoch millisecond timestamp when the event occurred, or <see langword="null"/> if not provided.</summary>
	/// <value>Unix epoch millisecond timestamp when the event occurred, or <see langword="null"/> if not provided.</value>
	[JsonPropertyName ("timestamp")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public long? Timestamp { get; init; }
	/// <summary>Serial number of the gateway that generated the event.</summary>
	/// <value>Serial number of the gateway that generated the event.</value>
	[JsonPropertyName ("gatewayId")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? GatewayId { get; init; }
	/// <summary>Execution ID associated with this event (present on execution state-change events).</summary>
	/// <value>Execution ID associated with this event (present on execution state-change events).</value>
	[JsonPropertyName ("execId")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ExecId { get; init; }
	/// <summary>Device URL of the device that triggered the event (present on device events).</summary>
	/// <value>Device URL of the device that triggered the event (present on device events).</value>
	[JsonPropertyName ("deviceURL")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? DeviceUrl { get; init; }
	/// <summary>Changed state snapshots included in a <c>DeviceStateChanged</c> event.</summary>
	/// <value>Changed state snapshots included in a <c>DeviceStateChanged</c> event.</value>
	[JsonPropertyName ("deviceStates")]
	public IReadOnlyList<EventState> DeviceStates { get; init; } = [];
	/// <summary>Previous execution state (present on <c>ExecutionStateChanged</c> events).</summary>
	/// <value>Previous execution state (present on <c>ExecutionStateChanged</c> events).</value>
	[JsonPropertyName ("oldState")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ExecutionState? OldState { get; init; }
	/// <summary>New execution state (present on <c>ExecutionStateChanged</c> events).</summary>
	/// <value>New execution state (present on <c>ExecutionStateChanged</c> events).</value>
	[JsonPropertyName ("newState")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ExecutionState? NewState { get; init; }
	/// <summary>OID of the setup this event relates to.</summary>
	/// <value>OID of the setup this event relates to.</value>
	[JsonPropertyName ("setupoid")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Setupoid { get; init; }
	/// <summary>Owner key of the principal that triggered the execution.</summary>
	/// <value>Owner key of the principal that triggered the execution.</value>
	[JsonPropertyName ("ownerKey")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? OwnerKey { get; init; }
	/// <summary>Raw event type integer as returned by the API.</summary>
	/// <value>Raw event type integer as returned by the API.</value>
	[JsonPropertyName ("type")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? Type { get; init; }
	/// <summary>Raw event sub-type integer as returned by the API.</summary>
	/// <value>Raw event sub-type integer as returned by the API.</value>
	[JsonPropertyName ("subType")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? SubType { get; init; }
	/// <summary>Estimated seconds until the next scheduled state transition.</summary>
	/// <value>Estimated seconds until the next scheduled state transition.</value>
	[JsonPropertyName ("timeToNextState")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? TimeToNextState { get; init; }
	/// <summary>Vendor-defined failed-command details as CLR primitives, lists or dictionaries; the structure varies by firmware.</summary>
	/// <value>Vendor-defined failed-command details as CLR primitives, lists or dictionaries; the structure varies by firmware.</value>
	[JsonPropertyName ("failedCommands")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	[JsonConverter (typeof (DeviceValueConverter))]
	public object? FailedCommands { get; init; }
	/// <summary>Failure type string as returned by the API (use <see cref="FailureTypeCode"/> for the parsed enum).</summary>
	/// <value>Failure type string as returned by the API (use <see cref="FailureTypeCode"/> for the parsed enum).</value>
	[JsonPropertyName ("failureType")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? FailureType { get; init; }
	/// <summary>OID of the condition group that triggered this event.</summary>
	/// <value>OID of the condition group that triggered this event.</value>
	[JsonPropertyName ("conditionGroupoid")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ConditionGroupoid { get; init; }
	/// <summary>OID of the place associated with this event.</summary>
	/// <value>OID of the place associated with this event.</value>
	[JsonPropertyName ("placeOid")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? PlaceOid { get; init; }
	/// <summary>Human-readable label associated with the event (e.g. scenario name).</summary>
	/// <value>Human-readable label associated with the event (e.g. scenario name).</value>
	[JsonPropertyName ("label")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Label { get; init; }
	/// <summary>Arbitrary metadata string attached to the event.</summary>
	/// <value>Arbitrary metadata string attached to the event.</value>
	[JsonPropertyName ("metadata")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Metadata { get; init; }
	/// <summary>Camera identifier for camera-related events.</summary>
	/// <value>Camera identifier for camera-related events.</value>
	[JsonPropertyName ("cameraId")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? CameraId { get; init; }
	/// <summary>Number of raw device records deleted during a synchronisation event.</summary>
	/// <value>Number of raw device records deleted during a synchronisation event.</value>
	[JsonPropertyName ("deletedRawDevicesCount")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? DeletedRawDevicesCount { get; init; }
	/// <summary>Protocol type string for protocol-synchronisation events.</summary>
	/// <value>Protocol type string for protocol-synchronisation events.</value>
	[JsonPropertyName ("protocolType")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ProtocolType { get; init; }
	/// <summary>Event name string as returned by the API (see <see cref="EventName"/> for known values).</summary>
	/// <value>Event name string as returned by the API (see <see cref="EventName"/> for known values).</value>
	[JsonPropertyName ("name")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Name { get; init; }
	/// <summary>Parsed failure type code for failed-execution events.</summary>
	/// <value>Parsed failure type code for failed-execution events.</value>
	[JsonPropertyName ("failureTypeCode")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public FailureType? FailureTypeCode { get; init; }
	}

/// <summary>
/// Represents an execution that is currently active (in-progress or queued) on a gateway.
/// Returned by <c>exec/current</c>. For completed executions see <see cref="HistoryExecution"/>.
/// </summary>
public sealed class Execution
	{
	/// <summary>Unique execution ID (also called <c>execId</c> in the API).</summary>
	/// <value>Unique execution ID (also called <c>execId</c> in the API).</value>
	[JsonPropertyName ("id")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Id { get; init; }
	/// <summary>Human-readable description or label for the execution.</summary>
	/// <value>Human-readable description or label for the execution.</value>
	[JsonPropertyName ("description")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Description { get; init; }
	/// <summary>Owner key identifying who triggered the execution.</summary>
	/// <value>Owner key identifying who triggered the execution.</value>
	[JsonPropertyName ("owner")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Owner { get; init; }
	/// <summary>Current state string of the execution (see <see cref="ExecutionState"/>).</summary>
	/// <value>Current state string of the execution (see <see cref="ExecutionState"/>).</value>
	[JsonPropertyName ("state")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? State { get; init; }
	/// <summary>Device actions associated with this execution.</summary>
	/// <value>Device actions associated with this execution.</value>
	[JsonPropertyName ("actionGroup")]
	public IReadOnlyList<Action> ActionGroup { get; init; } = [];
	}

/// <summary>A single command record within a <see cref="HistoryExecution"/>.</summary>
public sealed class HistoryExecutionCommand
	{
	/// <summary>URL of the device this command was addressed to.</summary>
	/// <value>URL of the device this command was addressed to.</value>
	[JsonPropertyName ("deviceURL")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? DeviceUrl { get; init; }
	/// <summary>Name of the command that was executed.</summary>
	/// <value>Name of the command that was executed.</value>
	[JsonPropertyName ("command")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Command { get; init; }
	/// <summary>Zero-based rank of this command within the execution action group.</summary>
	/// <value>Zero-based rank of this command within the execution action group.</value>
	[JsonPropertyName ("rank")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	public int Rank { get; init; }
	/// <summary>Whether this command was dynamically injected (not from a stored scenario).</summary>
	/// <value>Whether this command was dynamically injected (not from a stored scenario).</value>
	[JsonPropertyName ("dynamic")]
	public bool Dynamic { get; init; }
	/// <summary>Terminal state reached by this specific command.</summary>
	/// <value>Terminal state reached by this specific command.</value>
	[JsonPropertyName ("state")]
	public ExecutionState State { get; init; }
	/// <summary>Failure type string for this command (see <see cref="FailureType"/>).</summary>
	/// <value>Failure type string for this command (see <see cref="FailureType"/>).</value>
	[JsonPropertyName ("failureType")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? FailureType { get; init; }
	/// <summary>Parameter values that were passed to the command.</summary>
	/// <value>Parameter values that were passed to the command.</value>
	[JsonPropertyName ("parameters")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	[JsonConverter (typeof (DeviceValuesConverter))]
	public IReadOnlyList<object?>? Parameters { get; init; }
	}

/// <summary>A completed execution record as stored in the gateway history log.</summary>
public sealed class HistoryExecution
	{
	/// <summary>Unique execution ID.</summary>
	/// <value>Unique execution ID.</value>
	[JsonPropertyName ("id")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Id { get; init; }
	/// <summary>Unix epoch millisecond timestamp of when the execution was registered.</summary>
	/// <value>Unix epoch millisecond timestamp of when the execution was registered.</value>
	[JsonPropertyName ("eventTime")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	public long EventTime { get; init; }
	/// <summary>Owner key identifying who triggered the execution.</summary>
	/// <value>Owner key identifying who triggered the execution.</value>
	[JsonPropertyName ("owner")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Owner { get; init; }
	/// <summary>Source that submitted the execution (e.g. <c>"APP"</c>, <c>"SCENARIO"</c>).</summary>
	/// <value>Source that submitted the execution (e.g. <c>"APP"</c>, <c>"SCENARIO"</c>).</value>
	[JsonPropertyName ("source")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Source { get; init; }
	/// <summary>Unix epoch millisecond timestamp when the execution completed, or <see langword="null"/> if still active.</summary>
	/// <value>Unix epoch millisecond timestamp when the execution completed, or <see langword="null"/> if still active.</value>
	[JsonPropertyName ("endTime")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public long? EndTime { get; init; }
	/// <summary>Unix epoch millisecond timestamp when the gateway actually started executing, or <see langword="null"/> if it was queued.</summary>
	/// <value>Unix epoch millisecond timestamp when the gateway actually started executing, or <see langword="null"/> if it was queued.</value>
	[JsonPropertyName ("effectiveStartTime")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public long? EffectiveStartTime { get; init; }
	/// <summary>Total duration of the execution in milliseconds.</summary>
	/// <value>Total duration of the execution in milliseconds.</value>
	[JsonPropertyName ("duration")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	public long Duration { get; init; }
	/// <summary>Optional user-visible label (e.g. the scenario name that triggered the execution).</summary>
	/// <value>Optional user-visible label (e.g. the scenario name that triggered the execution).</value>
	[JsonPropertyName ("label")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Label { get; init; }
	/// <summary>Execution type string (see <see cref="ExecutionType"/>).</summary>
	/// <value>Execution type string (see <see cref="ExecutionType"/>).</value>
	[JsonPropertyName ("type")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Type { get; init; }
	/// <summary>Terminal state of the execution.</summary>
	/// <value>Terminal state of the execution.</value>
	[JsonPropertyName ("state")]
	public ExecutionState State { get; init; }
	/// <summary>Overall failure type string for the execution (see <see cref="FailureType"/>).</summary>
	/// <value>Overall failure type string for the execution (see <see cref="FailureType"/>).</value>
	[JsonPropertyName ("failureType")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? FailureType { get; init; }
	/// <summary>Per-command outcome records for this execution.</summary>
	/// <value>Per-command outcome records for this execution.</value>
	[JsonPropertyName ("commands")]
	public IReadOnlyList<HistoryExecutionCommand> Commands { get; init; } = [];
	/// <summary>How the execution was triggered (immediate, delayed, sunrise, or sunset).</summary>
	/// <value>How the execution was triggered (immediate, delayed, sunrise, or sunset).</value>
	[JsonPropertyName ("executionType")]
	public ExecutionType ExecutionType { get; init; }
	/// <summary>Further classification of the execution origin (internal, external, or scenario).</summary>
	/// <value>Further classification of the execution origin (internal, external, or scenario).</value>
	[JsonPropertyName ("executionSubType")]
	public ExecutionSubType ExecutionSubType { get; init; }
	}

/// <summary>
/// Descriptor for a local gateway API token generated via
/// <see cref="OverkizClient.GenerateLocalToken"/> and activated with
/// <see cref="OverkizClient.ActivateLocalToken"/>.
/// </summary>
public sealed class LocalToken
	{
	/// <summary>User-assigned label for this token.</summary>
	/// <value>User-assigned label for this token.</value>
	[JsonPropertyName ("label")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Label { get; init; }
	/// <summary>Serial number of the gateway this token is bound to.</summary>
	/// <value>Serial number of the gateway this token is bound to.</value>
	[JsonPropertyName ("gatewayId")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? GatewayId { get; init; }
	/// <summary>Unix epoch millisecond timestamp recorded when the gateway was created.</summary>
	/// <value>Unix epoch millisecond timestamp recorded when the gateway was created.</value>
	[JsonPropertyName ("gatewayCreationTime")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	public long GatewayCreationTime { get; init; }
	/// <summary>UUID that uniquely identifies this token; required when deleting via <see cref="OverkizClient.DeleteLocalToken"/>.</summary>
	/// <value>UUID that uniquely identifies this token; required when deleting via <see cref="OverkizClient.DeleteLocalToken"/>.</value>
	[JsonPropertyName ("uuid")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Uuid { get; init; }
	/// <summary>Access scope granted by this token (e.g. <c>"devmode"</c>).</summary>
	/// <value>Access scope granted by this token (e.g. <c>"devmode"</c>).</value>
	[JsonPropertyName ("scope")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Scope { get; init; }
	/// <summary>Unix epoch millisecond timestamp when this token expires, or <see langword="null"/> if it does not expire.</summary>
	/// <value>Unix epoch millisecond timestamp when this token expires, or <see langword="null"/> if it does not expire.</value>
	[JsonPropertyName ("expirationTime")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public long? ExpirationTime { get; init; }
	}

/// <summary>Developer-mode status for a gateway, as returned by <c>setup/gateways/{gatewayId}/developerMode</c>.</summary>
public sealed class DeveloperMode
	{
	/// <summary><see langword="true"/> when developer mode is active for the gateway.</summary>
	/// <value><see langword="true"/> when developer mode is active for the gateway.</value>
	[JsonPropertyName ("active")]
	public bool Active { get; init; }
	}

/// <summary>A key/value configuration parameter belonging to a setup <see cref="OptionObject"/>.</summary>
public sealed class OptionParameter
	{
	/// <summary>Parameter name key.</summary>
	/// <value>Parameter name key.</value>
	[JsonPropertyName ("name")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Name { get; init; }
	/// <summary>Parameter value.</summary>
	/// <value>Parameter value.</value>
	[JsonPropertyName ("value")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Value { get; init; }
	}

/// <summary>
/// A subscribed option (add-on feature or service) active on a setup,
/// as returned by <c>setup/options</c>.
/// </summary>
public sealed class OptionObject
	{
	/// <summary>Unix epoch millisecond timestamp when the option subscription started.</summary>
	/// <value>Unix epoch millisecond timestamp when the option subscription started.</value>
	[JsonPropertyName ("creationTime")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	public long CreationTime { get; init; }
	/// <summary>Unix epoch millisecond timestamp of the most recent update to this option.</summary>
	/// <value>Unix epoch millisecond timestamp of the most recent update to this option.</value>
	[JsonPropertyName ("lastUpdateTime")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	public long LastUpdateTime { get; init; }
	/// <summary>Unique identifier of the option type (e.g. <c>"ADVANCED_SCENARIOS"</c>).</summary>
	/// <value>Unique identifier of the option type (e.g. <c>"ADVANCED_SCENARIOS"</c>).</value>
	[JsonPropertyName ("optionId")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? OptionId { get; init; }
	/// <summary>Unix epoch millisecond timestamp of the subscription start date.</summary>
	/// <value>Unix epoch millisecond timestamp of the subscription start date.</value>
	[JsonPropertyName ("startDate")]
	[JsonNumberHandling (JsonNumberHandling.AllowReadingFromString)]
	public long StartDate { get; init; }
	/// <summary>Configuration parameters for this option, or <see langword="null"/> if none are defined.</summary>
	/// <value>Configuration parameters for this option, or <see langword="null"/> if none are defined.</value>
	[JsonPropertyName ("parameters")]
	[JsonIgnore (Condition = JsonIgnoreCondition.WhenWritingNull)]
	public IReadOnlyList<OptionParameter>? Parameters { get; init; }
	}