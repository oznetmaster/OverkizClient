// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace OverKizApi.Tests;

[TestFixture]
public sealed class EventAndLifecycleTests
	{
	[Test]
	public async Task EventLifecycle_PreservesWireFieldsAndRawJson ()
		{
		using var api = new ScriptedApi ();
		api.Expect ("POST", "events/register", "{\"id\":\"listener\"}");
		await api.Client.RegisterEventListener ();
		const string response = """[{"name":"DeviceStateChangedEvent","deviceURL":"io://hub/device","timestamp":"1700000000123","deviceStates":[{"name":"core:ClosureState","type":1,"value":45}],"newState":"Completed"}]""";
		api.Expect ("POST", "events/listener/fetch", response);
		var (events, raw) = await api.Client.FetchEventsRaw ();
		Assert.That (raw, Is.EqualTo (response));
		EventObject change = events.Single ();
		Assert.That (change.DeviceUrl, Is.EqualTo ("io://hub/device"));
		Assert.That (change.Timestamp, Is.EqualTo (1700000000123));
		Assert.That (change.DeviceStates.Single ().Name, Is.EqualTo ("core:ClosureState"));
		Assert.That (change.NewState, Is.EqualTo (ExecutionState.Completed));
		api.Expect ("POST", "events/listener/unregister", "", HttpStatusCode.NoContent);
		await api.Client.UnregisterEventListener ();
		await api.Client.UnregisterEventListener ();
		Assert.That (api.Client.EventListenerId, Is.Null);
		api.Complete ();
		}

	[Test]
	public async Task FetchWithoutListener_FailsWithoutRequest ()
		{
		using var api = new ScriptedApi ();
		await Assert.ThatAsync (() => api.Client.FetchEvents (), Throws.TypeOf<NoRegisteredEventListenerException> ());
		Assert.That (api.RequestCount, Is.Zero);
		}

	[TestCase ("{}")]
	[TestCase ("{\"id\":null}")]
	public async Task RegistrationWithoutId_ThrowsDocumentedException (string response)
		{
		using var api = new ScriptedApi ();
		api.Expect ("POST", "events/register", response);
		await Assert.ThatAsync (() => api.Client.RegisterEventListener (), Throws.TypeOf<OverkizException> ());
		Assert.That (api.Client.EventListenerId, Is.Null);
		api.Complete ();
		}

	[Test]
	public async Task Disposal_UnregistersButLeavesExternalHttpClientUsable ()
		{
		using var api = new ScriptedApi ();
		api.Expect ("POST", "events/register", "{\"id\":\"listener\"}");
		await api.Client.RegisterEventListener ();
		api.Expect ("POST", "events/listener/unregister", "");
		await api.Client.DisposeAsync ();
		await api.Client.DisposeAsync ();
		Assert.That (api.HandlerDisposed, Is.False);
		api.Expect ("GET", "still-usable", "{}");
		using HttpResponseMessage response = await api.Http.GetAsync ("still-usable");
		Assert.That (response.IsSuccessStatusCode, Is.True);
		api.Complete ();
		}

	[Test]
	public async Task Disposal_ToleratesListenerCleanupFailure ()
		{
		using var api = new ScriptedApi ();
		api.Expect ("POST", "events/register", "{\"id\":\"listener\"}");
		await api.Client.RegisterEventListener ();
		api.Expect ("POST", "events/listener/unregister", "offline", HttpStatusCode.ServiceUnavailable);
		await api.Client.DisposeAsync ();
		Assert.That (api.HandlerDisposed, Is.False);
		api.Complete ();
		}

	[Test]
	public async Task LocalEventPolling_TakesInitialLabelSnapshotWithoutSpuriousRename ()
		{
		using var api = new ScriptedApi (OverkizConst.LocalServer ("gateway.example.invalid"));
		api.Expect ("POST", "events/register", "{\"id\":\"listener\"}");
		await api.Client.RegisterEventListener ();
		api.Expect ("POST", "events/listener/fetch", "[]");
		api.Expect ("GET", "setup/devices", "[{\"deviceURL\":\"io://hub/device\",\"label\":\" Room \"}]");
		Assert.That (await api.Client.FetchEvents (), Is.Empty);
		// The next poll happens immediately, so label discovery must not repeat within 30 seconds.
		api.Expect ("POST", "events/listener/fetch", "[]");
		Assert.That (await api.Client.FetchEvents (), Is.Empty);
		api.Complete ();
		}

	[Test]
	public async Task LocalLabelLookupFailure_DoesNotLoseActualEvents ()
		{
		using var api = new ScriptedApi (OverkizConst.LocalServer ("gateway.example.invalid"));
		api.Expect ("POST", "events/register", "{\"id\":\"listener\"}");
		await api.Client.RegisterEventListener ();
		api.Expect ("POST", "events/listener/fetch", "[{\"name\":\"GatewayUpdatedEvent\"}]");
		api.Expect ("GET", "setup/devices", "offline", HttpStatusCode.ServiceUnavailable);
		Assert.That ((await api.Client.FetchEvents ()).Single ().Name, Is.EqualTo ("GatewayUpdatedEvent"));
		api.Complete ();
		}
	}