// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace OverKizApi.Tests;

[TestFixture]
public sealed class AuthenticationTests
	{
	private const string SomfyToken = "https://accounts.somfy.com/oauth/oauth/v2/token/jwt";
	private const string AtlanticToken = "https://apis.groupe-atlantic.com/token";
	private const string AtlanticJwt = "https://apis.groupe-atlantic.com/magellan/accounts/jwt";
	private const string TokenResponse = """{"access_token":"new-token","refresh_token":"refresh-token","expires_in":3600}""";

	[TestCase (true)]
	[TestCase (false)]
	public async Task StandardLogin_UsesCredentialsAndHonorsListenerOption (bool register)
		{
		using var api = new ScriptedApi (token: null);
		api.Expect ("POST", "login", """{"success":true}""", inspect: (request, body) =>
		{
			JsonElement json = ScriptedApi.Json (body);
			Assert.That (json.GetProperty ("userId").GetString (), Is.EqualTo ("tester@example.invalid"));
			Assert.That (json.GetProperty ("userPassword").GetString (), Is.EqualTo ("synthetic-password"));
			Assert.That (request.Headers.Authorization, Is.Null);
		});
		if (register)
			api.Expect ("POST", "events/register", """{"id":"listener-1"}""");
		Assert.That (await api.Client.Login (register), Is.True);
		Assert.That (api.Client.EventListenerId, Is.EqualTo (register ? "listener-1" : null));
		api.Complete ();
		}

	[TestCase ("{\"success\":false}")]
	[TestCase ("{}")]
	[TestCase ("{\"success\":null}")]
	public async Task StandardLogin_UnsuccessfulResponseDoesNotRegister (string response)
		{
		using var api = new ScriptedApi (token: null);
		api.Expect ("POST", "login", response);
		Assert.That (await api.Client.Login (), Is.False);
		Assert.That (api.Client.EventListenerId, Is.Null);
		api.Complete ();
		}

	[TestCase (true)]
	[TestCase (false)]
	public async Task LocalLogin_VerifiesBearerWithoutSendingCredentials (bool register)
		{
		using var api = new ScriptedApi (OverkizConst.LocalServer ("gateway.example.invalid"));
		api.Expect (register ? "POST" : "GET", register ? "events/register" : "setup/gateways",
			 register ? "{\"id\":\"local-listener\"}" : "[]", inspect: (request, body) =>
			 {
				 Assert.That (request.Headers.Authorization!.ToString (), Is.EqualTo ("Bearer synthetic-token"));
				 Assert.That (body, Does.Not.Contain ("synthetic-password"));
			 });
		Assert.That (await api.Client.Login (register), Is.True);
		Assert.That (api.Client.ApiType, Is.EqualTo (APIType.Local));
		api.Complete ();
		}

	[Test]
	public async Task SomfyLogin_ExchangesFormAndUsesTokenOnApiCalls ()
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[Server.SomfyEurope], token: null);
		api.Expect ("POST", SomfyToken, TokenResponse, inspect: (request, body) =>
		{
			var form = ScriptedApi.Form (body);
			Assert.That (request.Content!.Headers.ContentType!.MediaType, Is.EqualTo ("application/x-www-form-urlencoded"));
			Assert.That (form["grant_type"], Is.EqualTo ("password"));
			Assert.That (form["username"], Is.EqualTo (api.Client.Username));
			Assert.That (form["password"], Is.EqualTo (api.Client.Password));
			Assert.That (form["client_id"], Is.EqualTo (OverkizConst.SOMFY_CLIENT_ID));
		});
		api.Expect ("POST", "events/register", "{\"id\":\"listener\"}", inspect: (request, _) =>
			 Assert.That (request.Headers.Authorization!.Parameter, Is.EqualTo ("new-token")));
		Assert.That (await api.Client.Login (), Is.True);
		api.Complete ();
		}

	[TestCase ("{\"message\":\"error.invalid.grant\"}", typeof (SomfyBadCredentialsException))]
	[TestCase ("{}", typeof (SomfyServiceException))]
	[TestCase ("null", typeof (SomfyServiceException))]
	public async Task SomfyLogin_MapsTokenFailures (string response, Type exception)
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[Server.SomfyEurope]);
		api.Expect ("POST", SomfyToken, response, HttpStatusCode.BadRequest);
		await Assert.ThatAsync (() => api.Client.SomfyTahomaGetAccessToken (), Throws.TypeOf (exception));
		api.Complete ();
		}

	[Test]
	public async Task ExpiredToken_RefreshesBeforeRequestAndReRegistersListener ()
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[Server.SomfyEurope]);
		api.Expect ("POST", "events/register", "{\"id\":\"old-listener\"}");
		await api.Client.RegisterEventListener ();
		api.Expect ("POST", SomfyToken, TokenResponse.Replace ("3600", "0"));
		await api.Client.SomfyTahomaGetAccessToken ();
		api.Expect ("POST", SomfyToken, TokenResponse.Replace ("new-token", "refreshed-token"), inspect: (_, body) =>
		{
			var form = ScriptedApi.Form (body);
			Assert.That (form["grant_type"], Is.EqualTo ("refresh_token"));
			Assert.That (form["refresh_token"], Is.EqualTo ("refresh-token"));
		});
		api.Expect ("POST", "events/register", "{\"id\":\"new-listener\"}");
		api.Expect ("GET", "setup/devices", "[]", inspect: (request, _) =>
			 Assert.That (request.Headers.Authorization!.Parameter, Is.EqualTo ("refreshed-token")));
		await api.Client.GetDevices ();
		Assert.That (api.Client.EventListenerId, Is.EqualTo ("new-listener"));
		api.Complete ();
		}

	[Test]
	public async Task RefreshWithoutInitialToken_HasClearFailure ()
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[Server.SomfyEurope]);
		await Assert.ThatAsync (() => api.Client.RefreshToken (), Throws.TypeOf<InvalidOperationException> ());
		Assert.That (api.RequestCount, Is.Zero);
		}

	[Test]
	public async Task RefreshOnOtherServers_IsNoOp ()
		{
		using var api = new ScriptedApi ();
		await api.Client.RefreshToken ();
		Assert.That (api.RequestCount, Is.Zero);
		}

	[TestCase (Server.AtlanticCozytouch)]
	[TestCase (Server.ThermorCozytouch)]
	[TestCase (Server.SauterCozytouch)]
	public async Task CozytouchLogin_SendsBasicCredentialsThenJwt (Server server)
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[server], token: null);
		api.Expect ("POST", AtlanticToken, "{\"access_token\":\"atlantic-token\",\"token_type\":\"Bearer\"}", inspect: (request, body) =>
		{
			Assert.That (request.Headers.Authorization?.Scheme, Is.EqualTo ("Basic"));
			Assert.That (request.Headers.Authorization?.Parameter, Is.EqualTo (OverkizConst.COZYTOUCH_CLIENT_ID));
			Assert.That (ScriptedApi.Form (body)["username"], Is.EqualTo ("GA-PRIVATEPERSON/tester@example.invalid"));
		});
		api.Expect ("GET", AtlanticJwt, " \"synthetic-jwt\" ", inspect: (request, _) =>
			 Assert.That (request.Headers.Authorization!.ToString (), Is.EqualTo ("Bearer atlantic-token")));
		api.Expect ("POST", "login", "{\"success\":true}", inspect: (_, body) =>
			 Assert.That (ScriptedApi.Json (body).GetProperty ("jwt").GetString (), Is.EqualTo ("synthetic-jwt")));
		Assert.That (await api.Client.Login (false), Is.True);
		api.Complete ();
		}

	[TestCase ("{\"error\":\"invalid_grant\",\"error_description\":\"Rejected\"}", typeof (CozyTouchBadCredentialsException))]
	[TestCase ("{}", typeof (CozyTouchServiceException))]
	[TestCase ("null", typeof (CozyTouchServiceException))]
	public async Task CozytouchLogin_MapsTokenFailures (string response, Type exception)
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[Server.AtlanticCozytouch]);
		api.Expect ("POST", AtlanticToken, response, HttpStatusCode.BadRequest);
		await Assert.ThatAsync (() => api.Client.CozytouchLogin (), Throws.TypeOf (exception));
		api.Complete ();
		}

	[Test]
	public async Task NexityLogin_ReportsUnsupportedAuthenticationWithoutNetwork ()
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[Server.Nexity]);
		await Assert.ThatAsync (() => api.Client.Login (), Throws.TypeOf<NotSupportedException> ());
		Assert.That (api.RequestCount, Is.Zero);
		}
	}