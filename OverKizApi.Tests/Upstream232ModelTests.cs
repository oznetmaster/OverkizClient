// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace OverKizApi.Tests;

[TestFixture]
public sealed class Upstream232ModelTests
	{
	[TestCase ("core:ActionEventState")]
	[TestCase ("core:CommissioningWindowCloseTimestampState")]
	[TestCase ("core:CommissioningWindowOpenState")]
	[TestCase ("core:CredentialsState")]
	[TestCase ("core:ElectricityEnergyProductionState")]
	[TestCase ("core:FirmwareRevisionState")]
	[TestCase ("core:IntrusionEventState")]
	[TestCase ("core:SmokeEventState")]
	[TestCase ("core:SubsetId")]
	[TestCase ("core:SupportedCycleStages")]
	[TestCase ("core:SupportedIntensities")]
	[TestCase ("core:SupportedModesByCategory")]
	[TestCase ("core:SupportedRecipes")]
	[TestCase ("core:SupportedRecipesByCategory")]
	[TestCase ("core:TargetTimeLowerBound")]
	[TestCase ("core:TargetTimeUpperBound")]
	[TestCase ("core:TargetWeightLowerBound")]
	[TestCase ("core:TargetWeightUpperBound")]
	[TestCase ("io:BatteryLevelState")]
	[TestCase ("io:LuminanceState")]
	[TestCase ("io:SunEnergyState")]
	[TestCase ("ogcp:ActionEventState")]
	[TestCase ("ogcp:CommissioningWindowCloseTimestampState")]
	[TestCase ("ogcp:CommissioningWindowOpenState")]
	[TestCase ("ogcp:CredentialsState")]
	[TestCase ("ogcp:ElectricityEnergyProductionState")]
	[TestCase ("ogcp:IntrusionEventState")]
	[TestCase ("ogcp:SmokeEventState")]
	[TestCase ("ogp:ActionEventState")]
	[TestCase ("ogp:CommissioningWindowCloseTimestampState")]
	[TestCase ("ogp:CommissioningWindowOpenState")]
	[TestCase ("ogp:CredentialsState")]
	[TestCase ("ogp:ElectricityEnergyProductionState")]
	[TestCase ("ogp:IntrusionEventState")]
	[TestCase ("ogp:SmokeEventState")]
	public void AddedWireNames_RoundTripWithoutAnEnumCatalog (string name)
		{
		string json = "[{\"name\":\"" + name + "\",\"type\":1,\"value\":0}]";
		States states = JsonSerializer.Deserialize<States> (json)!;
		Assert.That (states[name]!.ValueAsInt, Is.Zero);
		States restored = JsonSerializer.Deserialize<States> (JsonSerializer.Serialize (states))!;
		Assert.That (restored[name]!.Name, Is.EqualTo (name));
		Assert.That (restored[name]!.ValueAsInt, Is.Zero);
		}

	[TestCase ("DynamicAudioPlayer", UIWidget.DynamicAudioPlayer)]
	[TestCase ("DynamicAudioplayer", UIWidget.DynamicAudioPlayer)]
	[TestCase ("DynamicElectricalSensor", UIWidget.DynamicElectricalSensor)]
	public void AddedWidgets_AreRecognized (string name, UIWidget expected)
		{
		Device device = JsonSerializer.Deserialize<Device> ("{\"definition\":{\"widgetName\":\"" + name + "\"}}")!;
		Assert.That (device.Widget, Is.EqualTo (expected));
		}

	[Test]
	public void TokenRefreshEvent_RetainsItsWireName ()
		{
		const string name = "OGCPTokensRefreshCompletedEvent";
		EventObject value = JsonSerializer.Deserialize<EventObject> ("{\"name\":\"" + name + "\"}")!;
		Assert.That (value.Name, Is.EqualTo (name));
		Assert.That (Enum.TryParse<EventName> (name[..^"Event".Length], true, out EventName parsed), Is.True);
		Assert.That (parsed, Is.EqualTo (EventName.OgcpTokensRefreshCompleted));
		Assert.That (JsonSerializer.Deserialize<EventObject> (JsonSerializer.Serialize (value))!.Name, Is.EqualTo (name));
		}
	}