// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace OverKizApi.Tests;

[TestFixture]
public sealed class ModelTests
	{
	[TestCase ("io://gateway/device", Protocol.Io, "gateway", "device", null)]
	[TestCase ("RTS://gateway/device#3", Protocol.Rts, "gateway", "device", 3)]
	[TestCase ("hlrrwifi://gateway/device#0", Protocol.HlrrWifi, "gateway", "device", 0)]
	[TestCase ("future://gateway/device", Protocol.Unknown, "gateway", "device", null)]
	[TestCase ("io://gateway/device#not-a-number", Protocol.Io, "gateway", "device", null)]
	public void DeviceUrl_ParsesProtocolsAndSubsystems (string url, Protocol protocol, string gateway, string address, int? subsystem)
		{
		var device = new Device { DeviceUrl = url };
		Assert.That (device.Id, Is.EqualTo (url));
		Assert.That (device.Protocol, Is.EqualTo (protocol));
		Assert.That (device.GatewayId, Is.EqualTo (gateway));
		Assert.That (device.DeviceAddress, Is.EqualTo (address));
		Assert.That (device.SubsystemId, Is.EqualTo (subsystem));
		Assert.That (device.IsSubDevice, Is.EqualTo (subsystem.HasValue));
		}

	[TestCase (null)]
	[TestCase ("")]
	[TestCase (" ")]
	[TestCase ("invalid")]
	[TestCase ("://gateway/device")]
	[TestCase ("io:///device")]
	[TestCase ("io://gateway/")]
	public void InvalidDeviceUrl_HasNoParsedComponents (string? url)
		{
		var device = new Device { DeviceUrl = url };
		Assert.That (device.Protocol, Is.Null);
		Assert.That (device.GatewayId, Is.Null);
		Assert.That (device.DeviceAddress, Is.Null);
		Assert.That (device.IsSubDevice, Is.False);
		}

	[TestCase ("15", GatewayType.Tahoma)]
	[TestCase ("\"15\"", GatewayType.Tahoma)]
	[TestCase ("\"tahoma\"", GatewayType.Tahoma)]
	[TestCase ("0", GatewayType.VirtualKizbox)]
	[TestCase ("99999", GatewayType.Unknown)]
	[TestCase ("\"future-hardware\"", GatewayType.Unknown)]
	[TestCase ("null", null)]
	[TestCase ("\"\"", null)]
	public async Task GatewayType_AcceptsNumericNamedAndFutureValues (string value, GatewayType? expected)
		{
		using var api = new ScriptedApi ();
		api.Expect ("GET", "setup/gateways", "[{\"gatewayId\":\"gateway\",\"type\":" + value + "}]");
		Assert.That ((await api.Client.GetGateways ()).Single ().Type, Is.EqualTo (expected));
		api.Complete ();
		}

	[TestCase ("0", null)]
	[TestCase ("\"0\"", null)]
	[TestCase ("null", null)]
	[TestCase ("\" \"", null)]
	[TestCase ("99999", GatewaySubType.Unknown)]
	[TestCase ("\"future-hardware\"", GatewaySubType.Unknown)]
	public async Task GatewaySubtype_ZeroMeansUnspecified (string value, GatewaySubType? expected)
		{
		using var api = new ScriptedApi ();
		api.Expect ("GET", "setup/gateways", "[{\"subType\":" + value + "}]");
		Assert.That ((await api.Client.GetGateways ()).Single ().SubType, Is.EqualTo (expected));
		api.Complete ();
		}

	[TestCase ("true")]
	[TestCase ("[]")]
	[TestCase ("{}")]
	public async Task InvalidGatewayTypeTokens_AreRejected (string value)
		{
		using var api = new ScriptedApi ();
		api.Expect ("GET", "setup/gateways", "[{\"type\":" + value + "}]");
		await Assert.ThatAsync (() => api.Client.GetGateways (), Throws.TypeOf<JsonException> ());
		api.Complete ();
		}

	[TestCase ("Completed", ExecutionState.Completed)]
	[TestCase ("completed", ExecutionState.Completed)]
	[TestCase ("future-state", ExecutionState.Unknown)]
	public async Task UnknownExecutionState_DoesNotBecomeNotStarted (string value, ExecutionState expected)
		{
		using var api = new ScriptedApi ();
		api.Expect ("GET", "history/executions", "[{\"state\":\"" + value + "\"}]");
		Assert.That ((await api.Client.GetExecutionHistory ()).Single ().State, Is.EqualTo (expected));
		api.Complete ();
		}

	[TestCase ("42")]
	[TestCase ("\"42\"")]
	public void IntegerState_ReadsJsonValue (string value)
		{
		State state = JsonSerializer.Deserialize<State> ("{\"name\":\"integer\",\"type\":1,\"value\":" + value + "}")!;
		Assert.That (state.ValueAsInt, Is.EqualTo (42));
		Assert.That (state.ValueAsFloat, Is.EqualTo (42d));
		}

	[TestCase ("21.5")]
	[TestCase ("\"21.5\"")]
	public void FloatState_ReadsJsonUsingInvariantCulture (string value)
		{
		var original = System.Globalization.CultureInfo.CurrentCulture;
		try
			{
			System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo ("fr-FR");
			State state = JsonSerializer.Deserialize<State> ("{\"type\":2,\"value\":" + value + "}")!;
			Assert.That (state.ValueAsFloat, Is.EqualTo (21.5d));
			}
		finally { System.Globalization.CultureInfo.CurrentCulture = original; }
		}

	[TestCase ("true", true)]
	[TestCase ("false", false)]
	[TestCase ("\"true\"", true)]
	public void BooleanState_ReadsJsonValue (string value, bool expected)
		{
		State state = JsonSerializer.Deserialize<State> ("{\"type\":6,\"value\":" + value + "}")!;
		Assert.That (state.ValueAsBool, Is.EqualTo (expected));
		}

	[Test]
	public void StateAccessors_AlsoSupportDirectClrValues ()
		{
		Assert.That (new State { Type = DataType.Integer, Value = 42 }.ValueAsInt, Is.EqualTo (42));
		Assert.That (new State { Type = DataType.Float, Value = 21.5d }.ValueAsFloat, Is.EqualTo (21.5d));
		Assert.That (new State { Type = DataType.Boolean, Value = true }.ValueAsBool, Is.True);
		Assert.That (new State { Type = DataType.String, Value = "open" }.ValueAsStr, Is.EqualTo ("open"));
		}

	[Test]
	public void NoneState_HasNullTypedValues ()
		{
		var state = new State { Type = DataType.None };
		Assert.That (state.ValueAsInt, Is.Null);
		Assert.That (state.ValueAsFloat, Is.Null);
		Assert.That (state.ValueAsBool, Is.Null);
		Assert.That (state.ValueAsStr, Is.Null);
		}

	[Test]
	public void TypedAccessors_RejectWrongDiscriminator ()
		{
		var state = new State { Name = "text", Type = DataType.String, Value = "42" };
		Assert.That (() => state.ValueAsInt, Throws.TypeOf<InvalidCastException> ());
		Assert.That (() => state.ValueAsFloat, Throws.TypeOf<InvalidCastException> ());
		Assert.That (() => state.ValueAsBool, Throws.TypeOf<InvalidCastException> ());
		Assert.That (() => new State { Type = DataType.Integer, Value = 42 }.ValueAsStr, Throws.TypeOf<InvalidCastException> ());
		}

	[Test]
	public void StatesCollection_RoundTripsAndKeepsLookupAndOrder ()
		{
		States states = JsonSerializer.Deserialize<States> ("""[{"name":"first","type":3,"value":"open"},{"name":"second","type":1,"value":42}]""")!;
		Assert.That (states.Select (s => s.Name), Is.EqualTo (new[] { "first", "second" }));
		Assert.That (states.Contains ("first"), Is.True);
		Assert.That (states["first"]!.ValueAsStr, Is.EqualTo ("open"));
		Assert.That (states["missing"], Is.Null);
		Assert.That (states.Contains ("FIRST"), Is.False);
		States roundTrip = JsonSerializer.Deserialize<States> (JsonSerializer.Serialize (states))!;
		Assert.That (roundTrip["second"]!.ValueAsInt, Is.EqualTo (42));
		Assert.That (new States (), Is.Empty);
		}

	[Test]
	public void DictionaryHelpers_HandlePresentMissingAndNullValues ()
		{
		var values = new Dictionary<string, object> { ["text"] = "value", ["number"] = 42, ["null"] = null! };
		Assert.That (values.TryGetNonNull ("text", out object? present), Is.True);
		Assert.That (present, Is.EqualTo ("value"));
		Assert.That (values.TryGetNonNull ("null", out object? absent), Is.False);
		Assert.That (absent, Is.Null);
		Assert.That (values.GetStringOr ("number"), Is.EqualTo ("42"));
		Assert.That (values.GetStringOr ("missing"), Is.EqualTo ("Unknown"));
		Assert.That (values.GetStringOr ("null", "fallback"), Is.EqualTo ("fallback"));
		Assert.That (values.GetNullableStringOr ("missing"), Is.Null);
		}
	}