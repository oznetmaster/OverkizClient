// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace OverKizApi.Tests;

[TestFixture]
public sealed class AuditRegressionTests
{
    private const string AtlanticToken = "https://apis.groupe-atlantic.com/token";
    private const string AtlanticJwt = "https://apis.groupe-atlantic.com/magellan/accounts/jwt";

    [Test]
    public void Gateway_WireNamesShouldBeControlledByAttributes()
    {
        Gateway gateway = JsonSerializer.Deserialize<Gateway>("""{"gatewayId":"test-gateway","alive":true,"type":15}""")!;
        Assert.That(gateway.GatewayId, Is.EqualTo("test-gateway"));
        Assert.That(gateway.Alive, Is.True);
        Assert.That(gateway.Type, Is.EqualTo(GatewayType.Tahoma));
    }

    [Test]
    public void DeviceSerialization_ShouldExcludeComputedProperties()
    {
        string json = JsonSerializer.Serialize(new Device { DeviceUrl = "io://gateway/device#2" });
        using var document = JsonDocument.Parse(json);
        string[] derived = { "id", "protocol", "gatewayId", "deviceAddress", "subsystemId", "isSubDevice", "uiClass", "widget" };
        Assert.That(document.RootElement.EnumerateObject().Select(p => p.Name).Intersect(derived, StringComparer.OrdinalIgnoreCase), Is.Empty);
    }

    [TestCase("null", HttpStatusCode.OK)]
    [TestCase("{}", HttpStatusCode.OK)]
    [TestCase("[]", HttpStatusCode.OK)]
    [TestCase("true", HttpStatusCode.OK)]
    [TestCase("42", HttpStatusCode.OK)]
    [TestCase("\" \"", HttpStatusCode.OK)]
    [TestCase("\"\"", HttpStatusCode.OK)]
    [TestCase("", HttpStatusCode.OK)]
    [TestCase("not-json", HttpStatusCode.OK)]
    [TestCase("{\"error\":\"unauthorized\"}", HttpStatusCode.Unauthorized)]
    [TestCase("<html>Service unavailable</html>", HttpStatusCode.ServiceUnavailable)]
    public async Task CozytouchJwt_ShouldRejectMissingTokenAndHttpErrors(string body, HttpStatusCode status)
    {
        using var api = new ScriptedApi(OverkizConst.SupportedServers[Server.AtlanticCozytouch]);
        api.Expect("POST", AtlanticToken, """{"access_token":"test-token","token_type":"Bearer"}""");
        api.Expect("GET", AtlanticJwt, body, status);
        await Assert.ThatAsync(() => api.Client.CozytouchLogin(), Throws.TypeOf<CozyTouchServiceException>());
        api.Complete();
    }

    [Test]
    public async Task CozytouchJwt_ShouldDecodeJsonStringEscapes()
    {
        using var api = new ScriptedApi(OverkizConst.SupportedServers[Server.AtlanticCozytouch]);
        api.Expect("POST", AtlanticToken, """{"access_token":"test-token","token_type":"Bearer"}""");
        api.Expect("GET", AtlanticJwt, "\"synthetic\\u002djwt\"");
        Assert.That(await api.Client.CozytouchLogin(), Is.EqualTo("synthetic-jwt"));
        api.Complete();
    }

    [TestCase("header.payload.signature", true)]
    [TestCase(" null ", false)]
    [TestCase("<html>Error</html>", false)]
    [TestCase("header..signature", false)]
    public async Task CozytouchJwt_ValidatesLegacyTextResponses(string body, bool valid)
    {
        using var api = new ScriptedApi(OverkizConst.SupportedServers[Server.AtlanticCozytouch]);
        api.Expect("POST", AtlanticToken, """{"access_token":"test-token","token_type":"Bearer"}""");
        api.Expect("GET", AtlanticJwt, body, mediaType: "text/plain");
        if (valid)
            Assert.That(await api.Client.CozytouchLogin(), Is.EqualTo(body));
        else
            await Assert.ThatAsync(() => api.Client.CozytouchLogin(), Throws.TypeOf<CozyTouchServiceException>());
        api.Complete();
    }

    [TestCase("true")]
    [TestCase("false")]
    [TestCase("null")]
    [TestCase("{}")]
    [TestCase("[]")]
    [TestCase("1.5")]
    public async Task ExecutionState_ShouldRejectWrongJsonTokenTypes(string value)
    {
        using var api = new ScriptedApi();
        api.Expect("GET", "history/executions", "[{\"state\":" + value + "}]");
        await Assert.ThatAsync(() => api.Client.GetExecutionHistory(), Throws.TypeOf<JsonException>());
        api.Complete();
    }

    [Test]
    public void OptionalEventStates_PreserveNullAndUnknownNames()
    {
        EventObject change = JsonSerializer.Deserialize<EventObject>("""{"oldState":null,"newState":"future-value","timestamp":"123"}""")!;
        Assert.That(change.OldState, Is.Null);
        Assert.That(change.NewState, Is.EqualTo(ExecutionState.Unknown));
        Assert.That(change.Timestamp, Is.EqualTo(123));
    }

    [TestCase("true")]
    [TestCase("false")]
    [TestCase("{}")]
    public void OptionalEventStates_RejectWrongTokenTypes(string value)
    {
        Assert.That(() => JsonSerializer.Deserialize<EventObject>("{\"newState\":" + value + "}"), Throws.TypeOf<JsonException>());
    }

    [Test]
    public void Setup_RoundTripsNestedModelsWithDefaultOptions()
    {
        const string json = """{"creationTime":"123","location":{"city":"Test","latitude":"12.5"},"gateways":[{"gatewayId":"hub","updateStatus":"future"}],"devices":[{"deviceURL":"io://hub/device","type":"actuator","definition":{"commands":[{"commandName":"stop","nParams":"0"}]},"states":[{"name":"closure","type":1,"value":45}]}],"rootPlace":{"oid":"root","subPlaces":[{"oid":"room"}]}}""";
        Setup setup = JsonSerializer.Deserialize<Setup>(json)!;
        Assert.That(setup.CreationTime, Is.EqualTo(123));
        Assert.That(setup.Location!.Latitude, Is.EqualTo(12.5));
        Assert.That(setup.Gateways.Single().UpdateStatus, Is.EqualTo(GatewayUpdateStatus.Unknown));
        Assert.That(setup.Devices.Single().Definition!.Commands.Single().CommandName, Is.EqualTo("stop"));
        Assert.That(setup.Devices.Single().States["closure"]!.ValueAsInt, Is.EqualTo(45));
        Setup roundTrip = JsonSerializer.Deserialize<Setup>(JsonSerializer.Serialize(setup))!;
        Assert.That(roundTrip.Devices.Single().Type, Is.EqualTo(ProductType.Actuator));
        Assert.That(roundTrip.RootPlace!.SubPlaces.Single().Oid, Is.EqualTo("room"));
    }

    [Test]
    public void DeviceValues_AreClrValuesAndRoundTripNestedVendorData()
    {
        State state = JsonSerializer.Deserialize<State>("""{"name":"vendor","type":11,"value":{"items":[45,21.5,true,null,"open",{"nested":false}]}}""")!;
        var values = (IDictionary<string, object?>)state.Value!;
        var items = (IReadOnlyList<object?>)values["items"]!;
        Assert.That(items[0], Is.TypeOf<long>().And.EqualTo(45));
        Assert.That(items[1], Is.TypeOf<decimal>().And.EqualTo(21.5m));
        Assert.That(items[2], Is.True);
        Assert.That(items[3], Is.Null);
        Assert.That(items[4], Is.EqualTo("open"));
        Assert.That(((IDictionary<string, object?>)items[5]!)["nested"], Is.False);
        State roundTrip = JsonSerializer.Deserialize<State>(JsonSerializer.Serialize(state))!;
        Assert.That(roundTrip.Value, Is.InstanceOf<IDictionary<string, object?>>());
    }

    [Test]
    public async Task CurrentExecutions_ExposeTypedActionsAndClrCommandParameters()
    {
        using var api = new ScriptedApi();
        api.Expect("GET", "exec/current", """[{"id":"running","actionGroup":[{"deviceURL":"io://hub/device","commands":[{"name":"setClosure","parameters":[45,true,{"vendor":"option"}]}]}]}]""");
        var action = (await api.Client.GetCurrentExecutions()).Single().ActionGroup.Single();
        Assert.That(action.DeviceUrl, Is.EqualTo("io://hub/device"));
        var command = action.Commands.Single();
        Assert.That(command.Name, Is.EqualTo("setClosure"));
        Assert.That(command.Parameters![0], Is.TypeOf<long>().And.EqualTo(45));
        Assert.That(command.Parameters[1], Is.True);
        Assert.That(command.Parameters[2], Is.InstanceOf<IDictionary<string, object?>>());
        api.Complete();
    }

    [TestCase("true")]
    [TestCase("42")]
    [TestCase("\"states\"")]
    [TestCase("{\"states\":true}")]
    public async Task DeviceStates_RejectInvalidUnionShapes(string response)
    {
        using var api = new ScriptedApi();
        api.Expect("GET", "setup/devices/" + ScriptedApi.EncodedDeviceUrl + "/states", response);
        await Assert.ThatAsync(() => api.Client.GetDeviceStates(ScriptedApi.DeviceUrl), Throws.TypeOf<JsonException>());
        api.Complete();
    }

    [Test]
    public async Task Pairing_PropagatesHttpErrors()
    {
        using var api = new ScriptedApi();
        api.Expect("POST", "config/hub/local/openPairing", """{"error":"Not authenticated"}""", HttpStatusCode.Unauthorized);
        await Assert.ThatAsync(() => api.Client.OpenLocalPairing("hub"), Throws.TypeOf<NotAuthenticatedException>());
        api.Complete();
    }

    [Test]
    public void EventsAndDeviceMetadata_ExposeClrValuesWithoutJsonDomObjects()
    {
        EventObject change = JsonSerializer.Deserialize<EventObject>("""{"deviceStates":[{"name":"moving","type":3,"value":"moving"}],"failedCommands":[{"reason":"offline","code":7}]}""")!;
        Assert.That(change.DeviceStates.Single().Value, Is.TypeOf<string>().And.EqualTo("moving"));
        var failures = (IReadOnlyList<object?>)change.FailedCommands!;
        Assert.That(((IDictionary<string, object?>)failures.Single()!)["code"], Is.TypeOf<long>().And.EqualTo(7));
        Device device = JsonSerializer.Deserialize<Device>("""{"dataProperties":[{"revision":2},false]}""")!;
        Assert.That(((IDictionary<string, object?>)device.DataProperties![0]!)["revision"], Is.TypeOf<long>().And.EqualTo(2));
        Assert.That(device.DataProperties[1], Is.False);
    }

    [Test]
    public void CommandParameters_RoundTripNullsAndNestedClrValues()
    {
        var command = new Command
        {
            Name = "vendorCommand",
            Parameters = new object?[] { null, new Dictionary<string, object?> { ["values"] = new object?[] { 1, true, "test" } } }
        };
        Command roundTrip = JsonSerializer.Deserialize<Command>(JsonSerializer.Serialize(command))!;
        Assert.That(roundTrip.Parameters![0], Is.Null);
        var nested = (IDictionary<string, object?>)roundTrip.Parameters[1]!;
        var values = (IReadOnlyList<object?>)nested["values"]!;
        Assert.That(values, Is.EqualTo(new object?[] { 1L, true, "test" }));
    }

    [Test]
    public void Aliases_AreNotSerializedAsExtraWireFields()
    {
        foreach (object model in new object[] { new Gateway { GatewayId = "hub" }, new Place { Oid = "room" }, new Scenario { Oid = "scene" } })
        {
            using var json = JsonDocument.Parse(JsonSerializer.Serialize(model, model.GetType()));
            Assert.That(json.RootElement.EnumerateObject().Select(p => p.Name), Does.Not.Contain("id"));
            Assert.That(json.RootElement.EnumerateObject().Select(p => p.Name), Does.Not.Contain("Id"));
        }
    }
}
