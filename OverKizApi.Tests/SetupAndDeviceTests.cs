// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace OverKizApi.Tests;

[TestFixture]
public sealed class SetupAndDeviceTests
	{
	[Test]
	public async Task Setup_CachesAndRefreshesBothPublicCollections ()
		{
		using var api = new ScriptedApi ();
		Assert.That (api.Client.Setup, Is.Null);
		Assert.That (api.Client.Devices, Is.Empty);
		Assert.That (api.Client.Gateways, Is.Empty);
		api.Expect ("GET", "setup", """{"id":"setup-1","devices":[{"deviceURL":"io://test/one","label":"One"}],"gateways":[{"gatewayId":"hub-1"}]}""");
		Setup first = await api.Client.GetSetup ();
		Assert.That (await api.Client.GetSetup (), Is.SameAs (first));
		Assert.That (api.RequestCount, Is.EqualTo (1));
		Assert.That (api.Client.Devices.Single ().Label, Is.EqualTo ("One"));
		Assert.That (api.Client.Gateways.Single ().Id, Is.EqualTo ("hub-1"));
		api.Expect ("GET", "setup", """{"id":"setup-2","devices":[],"gateways":[]}""");
		Setup second = await api.Client.GetSetup (true);
		Assert.That (second.Id, Is.EqualTo ("setup-2"));
		Assert.That (api.Client.Setup, Is.SameAs (second));
		Assert.That (api.Client.Devices, Is.Empty);
		Assert.That (api.Client.Gateways, Is.Empty);
		api.Complete ();
		}

	[Test]
	public async Task FailedRefresh_PreservesLastSuccessfulSetup ()
		{
		using var api = new ScriptedApi ();
		api.Expect ("GET", "setup", "{\"id\":\"retained\"}");
		Setup first = await api.Client.GetSetup ();
		api.Expect ("GET", "setup", "null");
		await Assert.ThatAsync (() => api.Client.GetSetup (true), Throws.TypeOf<OverkizException> ());
		Assert.That (api.Client.Setup, Is.SameAs (first));
		api.Complete ();
		}

	[Test]
	public async Task GetDevice_EncodesEntireDeviceUrlAsOneSegment ()
		{
		using var api = new ScriptedApi ();
		api.Expect ("GET", "setup/devices/" + ScriptedApi.EncodedDeviceUrl,
			 """{"deviceURL":"io://test-gateway/shutter#2","label":"Shutter","available":true,"type":"actuator"}""");
		Device device = await api.Client.GetDevice (ScriptedApi.DeviceUrl);
		Assert.That (device.Id, Is.EqualTo (ScriptedApi.DeviceUrl));
		Assert.That (device.Type, Is.EqualTo (ProductType.Actuator));
		Assert.That (device.Available, Is.True);
		api.Complete ();
		}

	[TestCase ("[{\"name\":\"core:ClosureState\",\"type\":1,\"value\":45}]")]
	[TestCase ("{\"states\":[{\"name\":\"core:ClosureState\",\"type\":1,\"value\":45}]}")]
	[TestCase (" {\"deviceStates\":[{\"name\":\"core:ClosureState\",\"type\":1,\"value\":45}]}")]
	[TestCase ("{\"values\":[{\"name\":\"core:ClosureState\",\"type\":1,\"value\":45}]}")]
	public async Task States_ReadsSupportedResponseShapes (string response)
		{
		using var api = new ScriptedApi ();
		api.Expect ("GET", "setup/devices/" + ScriptedApi.EncodedDeviceUrl + "/states", response);
		State state = (await api.Client.GetDeviceStates (ScriptedApi.DeviceUrl)).Single ();
		Assert.That (state.Name, Is.EqualTo ("core:ClosureState"));
		Assert.That (state.ValueAsInt, Is.EqualTo (45));
		api.Complete ();
		}

	[TestCase ("[]")]
	[TestCase ("{}")]
	[TestCase ("null")]
	[TestCase ("{\"states\":null}")]
	public async Task States_EmptyResponsesReturnEmptyCollection (string response)
		{
		using var api = new ScriptedApi ();
		api.Expect ("GET", "setup/devices/" + ScriptedApi.EncodedDeviceUrl + "/states", response);
		Assert.That (await api.Client.GetDeviceStates (ScriptedApi.DeviceUrl), Is.Empty);
		api.Complete ();
		}

	[TestCase ("true", typeof (JsonException))]
	[TestCase ("not-json", typeof (JsonException))]
	[TestCase ("null", typeof (OverkizException))]
	public async Task InvalidDeviceResponse_IsNotAcceptedAsADevice (string response, Type exception)
		{
		using var api = new ScriptedApi ();
		api.Expect ("GET", "setup/devices/" + ScriptedApi.EncodedDeviceUrl, response);
		await Assert.ThatAsync (() => api.Client.GetDevice (ScriptedApi.DeviceUrl), Throws.TypeOf (exception));
		api.Complete ();
		}

	[Test]
	public async Task Places_DeserializeNestedHierarchy ()
		{
		using var api = new ScriptedApi ();
		api.Expect ("GET", "setup/places", """{"oid":"house","label":"Home","subPlaces":[{"oid":"room","label":"Room"}]}""");
		Place place = await api.Client.GetPlaces ();
		Assert.That (place.Id, Is.EqualTo ("house"));
		Assert.That (place.SubPlaces.Single ().Label, Is.EqualTo ("Room"));
		api.Complete ();
		}

	[Test]
	public async Task Options_DeserializeListItemAndParameter ()
		{
		using var api = new ScriptedApi ();
		const string option = "{\"optionId\":\"FEATURE\",\"parameters\":[{\"name\":\"limit\",\"value\":\"10\"}]}";
		api.Expect ("GET", "setup/options", "[" + option + "]");
		Assert.That ((await api.Client.GetSetupOptions ()).Single ().OptionId, Is.EqualTo ("FEATURE"));
		api.Expect ("GET", "setup/options/FEATURE", option);
		Assert.That ((await api.Client.GetSetupOption ("FEATURE"))!.Parameters!.Single ().Value, Is.EqualTo ("10"));
		api.Expect ("GET", "setup/options/FEATURE/limit", "{\"name\":\"limit\",\"value\":\"10\"}");
		Assert.That ((await api.Client.GetSetupOptionParameter ("FEATURE", "limit"))!.Name, Is.EqualTo ("limit"));
		api.Complete ();
		}

	[TestCase ("")]
	[TestCase (" ")]
	[TestCase ("null")]
	public async Task MissingOptions_AreNull (string response)
		{
		using var api = new ScriptedApi ();
		api.Expect ("GET", "setup/options/FEATURE", response);
		Assert.That (await api.Client.GetSetupOption ("FEATURE"), Is.Null);
		api.Expect ("GET", "setup/options/FEATURE/limit", response);
		Assert.That (await api.Client.GetSetupOptionParameter ("FEATURE", "limit"), Is.Null);
		api.Complete ();
		}
	}