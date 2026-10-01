// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace OverKizApi.Tests;

[TestFixture]
public sealed class EventRecoveryTests
	{
	private const string TokenUrl = "https://accounts.somfy.com/oauth/oauth/v2/token/jwt";
	private const string Token = """{"access_token":"fresh","refresh_token":"refresh","expires_in":3600}""";

	[Test]
	public async Task AutomaticFetch_RegistersOnceAndReusesListener ()
		{
		using var api = new ScriptedApi ();
		api.Expect ("POST", "events/register", """{"id":"one"}""");
		api.Expect ("POST", "events/one/fetch", """[{"name":"GatewayUpdatedEvent"}]""");
		Assert.That ((await api.Client.FetchEvents (true)).Single ().Name, Is.EqualTo ("GatewayUpdatedEvent"));
		api.Expect ("POST", "events/one/fetch", "[]");
		Assert.That (await api.Client.FetchEvents (true), Is.Empty);
		api.Complete ();
		}

	[Test]
	public async Task FalseOverload_PreservesMissingListenerContract ()
		{
		using var api = new ScriptedApi ();
		await Assert.ThatAsync (() => api.Client.FetchEvents (false), Throws.TypeOf<NoRegisteredEventListenerException> ());
		Assert.That (api.RequestCount, Is.Zero);
		}

	[TestCase (true)]
	[TestCase (false)]
	public async Task FetchAfterExpiry_UsesFreshTokenAndNewListener (bool automatic)
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[Server.SomfyEurope]);
		await ExpiredLogin (api);
		api.Expect ("POST", TokenUrl, Token);
		api.Expect ("POST", "events/register", """{"id":"new"}""");
		api.Expect ("POST", "events/new/fetch", "[]", inspect: (request, _) => Assert.That (request.Headers.Authorization!.Parameter, Is.EqualTo ("fresh")));
		Assert.That (await (automatic ? api.Client.FetchEvents (true) : api.Client.FetchEvents ()), Is.Empty);
		api.Complete ();
		}

	[Test]
	public async Task RegistrationAfterExpiry_DoesNotRegisterTwice ()
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[Server.SomfyEurope]);
		await ExpiredLogin (api);
		api.Expect ("POST", TokenUrl, Token);
		api.Expect ("POST", "events/register", """{"id":"new"}""");
		await api.Client.RegisterEventListener ();
		Assert.That (api.Client.EventListenerId, Is.EqualTo ("new"));
		api.Complete ();
		}

	[TestCase (false)]
	[TestCase (true)]
	public async Task CleanupAfterExpiry_DoesNotCreateReplacement (bool dispose)
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[Server.SomfyEurope]);
		await ExpiredLogin (api);
		api.Expect ("POST", TokenUrl, Token);
		api.Expect ("POST", "events/old/unregister", "");
		if (dispose) await api.Client.DisposeAsync ();
		else await api.Client.UnregisterEventListener ();
		Assert.That (api.Client.EventListenerId, Is.Null);
		api.Complete ();
		}

	[TestCase ("Invalid event listener id old")]
	[TestCase ("No registered event listener")]
	public async Task ExplicitListenerRejection_RecoversOnce (string error)
		{
		using var api = new ScriptedApi ();
		await RegisterOld (api);
		api.Expect ("POST", "events/old/fetch", JsonSerializer.Serialize (new { error }), HttpStatusCode.BadRequest);
		api.Expect ("POST", "events/register", """{"id":"new"}""");
		api.Expect ("POST", "events/new/fetch", "[]");
		Assert.That (await api.Client.FetchEvents (true), Is.Empty);
		api.Complete ();
		}

	[Test]
	public async Task RepeatedListenerRejection_StopsAndClearsId ()
		{
		using var api = new ScriptedApi ();
		await RegisterOld (api);
		api.Expect ("POST", "events/old/fetch", """{"error":"Invalid event listener id old"}""", HttpStatusCode.BadRequest);
		api.Expect ("POST", "events/register", """{"id":"new"}""");
		api.Expect ("POST", "events/new/fetch", """{"error":"Invalid event listener id new"}""", HttpStatusCode.BadRequest);
		await Assert.ThatAsync (() => api.Client.FetchEvents (true), Throws.TypeOf<InvalidEventListenerIdException> ());
		Assert.That (api.Client.EventListenerId, Is.Null);
		api.Complete ();
		}

	[Test]
	public async Task StrictFetch_DoesNotRecoverRejectedListener ()
		{
		using var api = new ScriptedApi ();
		await RegisterOld (api);
		api.Expect ("POST", "events/old/fetch", """{"error":"Invalid event listener id old"}""", HttpStatusCode.BadRequest);
		await Assert.ThatAsync (() => api.Client.FetchEvents (), Throws.TypeOf<InvalidEventListenerIdException> ());
		Assert.That (api.Client.EventListenerId, Is.Null);
		api.Complete ();
		}

	[TestCase (true)]
	[TestCase (false)]
	public async Task TransportFailure_IsNeverRetried (bool timeout)
		{
		using var api = new ScriptedApi ();
		await RegisterOld (api);
		Exception failure = timeout ? new TaskCanceledException ("simulated timeout") : new HttpRequestException ("simulated disconnect");
		api.Expect ("POST", "events/old/fetch", inspect: (_, _) => throw failure);
		var caught = await Assert.CatchAsync (() => api.Client.FetchEvents (true));
		if (timeout) Assert.That (caught, Is.InstanceOf<OperationCanceledException> ());
		else Assert.That (caught, Is.SameAs (failure));
		Assert.That (api.Client.EventListenerId, Is.EqualTo ("old"));
		api.Complete ();
		}

	[TestCase ((HttpStatusCode) 429, typeof (TooManyRequestsException))]
	[TestCase (HttpStatusCode.ServiceUnavailable, typeof (ServiceUnavailableException))]
	public async Task ServiceFailures_AreNotRetried (HttpStatusCode status, Type error)
		{
		using var api = new ScriptedApi ();
		await RegisterOld (api);
		api.Expect ("POST", "events/old/fetch", "offline", status);
		await Assert.ThatAsync (() => api.Client.FetchEvents (true), Throws.TypeOf (error));
		api.Complete ();
		}

	[Test]
	public async Task FailedReplacement_ClearsIdAndNextPollCanRegister ()
		{
		using var api = new ScriptedApi ();
		await RegisterOld (api);
		api.Expect ("POST", "events/register", "offline", HttpStatusCode.ServiceUnavailable);
		await Assert.ThatAsync (() => api.Client.RegisterEventListener (), Throws.TypeOf<ServiceUnavailableException> ());
		Assert.That (api.Client.EventListenerId, Is.Null);
		api.Expect ("POST", "events/register", """{"id":"new"}""");
		api.Expect ("POST", "events/new/fetch", "[]");
		Assert.That (await api.Client.FetchEvents (true), Is.Empty);
		api.Complete ();
		}

	[Test]
	public async Task RefreshRegistrationFailure_DoesNotFetchStaleListener ()
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[Server.SomfyEurope]);
		await ExpiredLogin (api);
		api.Expect ("POST", TokenUrl, Token);
		api.Expect ("POST", "events/register", "offline", HttpStatusCode.ServiceUnavailable);
		await Assert.ThatAsync (() => api.Client.FetchEvents (true), Throws.TypeOf<ServiceUnavailableException> ());
		Assert.That (api.Client.EventListenerId, Is.Null);
		api.Expect ("POST", "events/register", """{"id":"new"}""");
		api.Expect ("POST", "events/new/fetch", "[]");
		await api.Client.FetchEvents (true);
		api.Complete ();
		}

	[TestCase (true)]
	[TestCase (false)]
	public async Task AuthenticationRejection_ReauthenticatesWithoutDuplicateRegistration (bool hadListener)
		{
		using var api = new ScriptedApi (token: null);
		if (hadListener) await RegisterOld (api);
		api.Expect ("POST", hadListener ? "events/old/fetch" : "events/register", """{"error":"Not authenticated"}""", HttpStatusCode.Unauthorized);
		api.Expect ("POST", "login", """{"success":true}""");
		api.Expect ("POST", "events/register", """{"id":"new"}""");
		api.Expect ("POST", "events/new/fetch", "[]");
		await api.Client.FetchEvents (true);
		api.Complete ();
		}

	[TestCase ("{\"success\":false}", HttpStatusCode.OK, typeof (NotAuthenticatedException))]
	[TestCase ("{\"error\":\"Bad credentials.\"}", HttpStatusCode.Unauthorized, typeof (BadCredentialsException))]
	public async Task FailedReauthentication_StopsWithoutRegistration (string response, HttpStatusCode status, Type error)
		{
		using var api = new ScriptedApi (token: null);
		await RegisterOld (api);
		api.Expect ("POST", "events/old/fetch", """{"error":"Not authenticated"}""", HttpStatusCode.Unauthorized);
		api.Expect ("POST", "login", response, status);
		await Assert.ThatAsync (() => api.Client.FetchEvents (true), Throws.TypeOf (error));
		Assert.That (api.Client.EventListenerId, Is.Null);
		api.Complete ();
		}

	[Test]
	public async Task LocalTokenRejection_DoesNotAttemptLogin ()
		{
		using var api = new ScriptedApi (OverkizConst.LocalServer ("gateway.example.invalid"));
		await RegisterOld (api);
		api.Expect ("POST", "events/old/fetch", """{"error":"Not authenticated"}""", HttpStatusCode.Unauthorized);
		await Assert.ThatAsync (() => api.Client.FetchEvents (true), Throws.TypeOf<NotAuthenticatedException> ());
		api.Complete ();
		}

	[Test]
	public async Task RexelTokenRejection_DoesNotAttemptPasswordLogin ()
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[Server.Rexel]);
		api.Client.SelectRexelGateway ("gateway");
		await RegisterOld (api);
		api.Expect ("POST", "events/old/fetch", """{"error":"Not authenticated"}""", HttpStatusCode.Unauthorized);
		await Assert.ThatAsync (() => api.Client.FetchEvents (true), Throws.TypeOf<NotAuthenticatedException> ());
		api.Complete ();
		}

	[Test]
	public async Task CloudReloginWithoutRegistration_ClearsPriorSessionState ()
		{
		using var api = new ScriptedApi (token: null);
		await RegisterOld (api);
		api.Expect ("GET", "setup", "{}");
		await api.Client.GetSetup ();
		api.Expect ("POST", "login", """{"success":true}""");
		Assert.That (await api.Client.Login (false), Is.True);
		Assert.That (api.Client.EventListenerId, Is.Null);
		Assert.That (api.Client.Setup, Is.Null);
		api.Complete ();
		}

	[Test]
	public async Task RexelSelection_ChangesClearScopeButSameGatewayPreservesIt ()
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[Server.Rexel]);
		api.Client.SelectRexelGateway ("a");
		api.Expect ("GET", "setup", """{"devices":[{"deviceURL":"io://a/device"}],"gateways":[{"gatewayId":"a"}]}""");
		var original = await api.Client.GetSetup ();
		await RegisterOld (api);
		api.Client.SelectRexelGateway ("a");
		Assert.That (api.Client.Setup, Is.SameAs (original));
		Assert.That (api.Client.EventListenerId, Is.EqualTo ("old"));
		api.Client.SelectRexelGateway ("b");
		Assert.That (api.Client.Setup, Is.Null);
		Assert.That (api.Client.Devices, Is.Empty);
		Assert.That (api.Client.Gateways, Is.Empty);
		Assert.That (api.Client.EventListenerId, Is.Null);
		api.Expect ("GET", "setup", "{}", inspect: (request, _) => Assert.That (request.Headers.GetValues (OverkizConst.REXEL_GATEWAY_HEADER).Single (), Is.EqualTo ("b")));
		Assert.That (await api.Client.GetSetup (), Is.Not.SameAs (original));
		api.Expect ("POST", "events/register", """{"id":"b-listener"}""");
		api.Expect ("POST", "events/b-listener/fetch", "[]");
		await api.Client.FetchEvents (true);
		api.Complete ();
		}

	[Test]
	public async Task ExplicitUnregister_LeavesStrictFetchDisabled ()
		{
		using var api = new ScriptedApi ();
		await RegisterOld (api);
		api.Expect ("POST", "events/old/unregister", "offline", HttpStatusCode.ServiceUnavailable);
		await Assert.ThatAsync (() => api.Client.UnregisterEventListener (), Throws.TypeOf<ServiceUnavailableException> ());
		await Assert.ThatAsync (() => api.Client.FetchEvents (), Throws.TypeOf<NoRegisteredEventListenerException> ());
		Assert.That (api.Client.EventListenerId, Is.Null);
		api.Complete ();
		}

	[TestCase ("Invalid event listener id old")]
	[TestCase ("Not authenticated")]
	public async Task RecoveryBudget_IsSharedAcrossListenerAndAuthenticationFailures (string firstError)
		{
		using var api = new ScriptedApi (token: null);
		await RegisterOld (api);
		api.Expect ("POST", "events/old/fetch", JsonSerializer.Serialize (new { error = firstError }), HttpStatusCode.Unauthorized);
		if (firstError == "Not authenticated") api.Expect ("POST", "login", "{\"success\":true}");
		api.Expect ("POST", "events/register", "{\"id\":\"new\"}");
		api.Expect ("POST", "events/new/fetch", "{\"error\":\"Not authenticated\"}", HttpStatusCode.Unauthorized);
		await Assert.ThatAsync (() => api.Client.FetchEvents (true), Throws.TypeOf<NotAuthenticatedException> ());
		Assert.That (api.Client.EventListenerId, Is.Null);
		api.Complete ();
		}

	[Test]
	public async Task RejectedTokenRefresh_DoesNotFetchOrRegister ()
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[Server.SomfyEurope]);
		await ExpiredLogin (api);
		api.Expect ("POST", TokenUrl, "{\"message\":\"error.invalid.grant\"}", HttpStatusCode.BadRequest);
		await Assert.ThatAsync (() => api.Client.FetchEvents (true), Throws.TypeOf<SomfyBadCredentialsException> ());
		api.Complete ();
		}

	[Test]
	public async Task MalformedFetchResponse_IsNotRetried ()
		{
		using var api = new ScriptedApi ();
		await RegisterOld (api);
		api.Expect ("POST", "events/old/fetch", "not-json");
		await Assert.ThatAsync (() => api.Client.FetchEvents (true), Throws.TypeOf<JsonException> ());
		api.Complete ();
		}

	private static async Task ExpiredLogin (ScriptedApi api)
		{
		api.Expect ("POST", TokenUrl, Token.Replace ("3600", "0"));
		api.Expect ("POST", "events/register", """{"id":"old"}""");
		await api.Client.Login ();
		}

	private static async Task RegisterOld (ScriptedApi api)
		{
		api.Expect ("POST", "events/register", """{"id":"old"}""");
		await api.Client.RegisterEventListener ();
		}
	}