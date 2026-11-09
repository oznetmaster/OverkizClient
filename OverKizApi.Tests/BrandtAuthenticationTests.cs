// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace OverKizApi.Tests;

[TestFixture]
public sealed class BrandtAuthenticationTests
	{
	private const string SESSIONS = "https://www.smartcontrol-app.com/api/v1/sessions.json";
	private const string JWT = "https://www.smartcontrol-app.com/api/v1/profile/jwt.json";

	[TestCase (Server.Brandt, "brandt-electromenager", true)]
	[TestCase (Server.Brandt, "brandt-electromenager", false)]
	[TestCase (Server.SauterSmartControl, "sauter-electromenager", true)]
	[TestCase (Server.SauterSmartControl, "sauter-electromenager", false)]
	public async Task Login_UsesPartnerCookieThenJwtOnly (Server server, string partner, bool register)
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[server]);
		api.Http.DefaultRequestHeaders.Authorization = new ("Bearer", "old-token");
		api.Expect ("POST", SESSIONS, "{}", inspect: (request, body) =>
			{
				JsonElement client = ScriptedApi.Json (body).GetProperty ("client");
				Assert.That (client.GetProperty ("email").GetString (), Is.EqualTo (api.Client.Username));
				Assert.That (client.GetProperty ("password").GetString (), Is.EqualTo (api.Client.Password));
				Assert.That (client.GetProperty ("partner").GetString (), Is.EqualTo (partner));
				Assert.That (request.Headers.Authorization, Is.Null);
			}, cookie: "_devise_session=fake-session; Path=/; Secure; HttpOnly");
		api.Expect ("GET", JWT, """{"client":{"jwt":"fake-jwt"}}""", inspect: (request, _) =>
			{
				Assert.That (request.Headers.GetValues ("Cookie"), Does.Contain ("_devise_session=fake-session"));
				Assert.That (request.Headers.Authorization, Is.Null);
			});
		api.Expect ("POST", "login", """{"success":true}""", inspect: (request, body) =>
			{
				Assert.That (request.Headers.Contains ("Cookie"), Is.False, "Middleware cookies must not reach Overkiz.");
				Assert.That (request.Headers.Authorization, Is.Null);
				JsonElement json = ScriptedApi.Json (body);
				Assert.That (json.EnumerateObject ().Select (p => p.Name), Is.EqualTo (["jwt"]));
				Assert.That (json.GetProperty ("jwt").GetString (), Is.EqualTo ("fake-jwt"));
			}, cookie: "JSESSIONID=fake-cloud-session; Path=/; Secure; HttpOnly");
		if (register)
			api.Expect ("POST", "events/register", """{"id":"brand-listener"}""");
		api.Expect ("GET", "setup/devices", "[]", inspect: (request, _) =>
			Assert.That (request.Headers.GetValues ("Cookie"), Does.Contain ("JSESSIONID=fake-cloud-session")));
		Assert.That (await api.Client.Login (register), Is.True);
		Assert.That (api.Client.EventListenerId, Is.EqualTo (register ? "brand-listener" : null));
		Assert.That (await api.Client.GetDevices (), Is.Empty);
		api.Complete ();
		}

	[TestCase (400, typeof (BrandtBadCredentialsException))]
	[TestCase (401, typeof (BrandtBadCredentialsException))]
	[TestCase (403, typeof (BrandtBadCredentialsException))]
	[TestCase (500, typeof (BrandtServiceException))]
	[TestCase (502, typeof (BrandtServiceException))]
	[TestCase (503, typeof (BrandtServiceException))]
	public async Task SessionFailure_DoesNotFetchJwtOrLeakResponse (int status, Type error)
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[Server.SauterSmartControl]);
		api.Expect ("POST", SESSIONS, "secret-response-never-log", (HttpStatusCode)status, mediaType: "text/html");
		Exception? exception = await Assert.ThrowsAsync (error, async () => await api.Client.Login ());
		Assert.That (exception!.Message, Does.Not.Contain ("secret-response").And.Not.Contain (api.Client.Password));
		Assert.That (api.Client.EventListenerId, Is.Null);
		api.Complete ();
		}

	[TestCase ("{}", 200)]
	[TestCase ("null", 200)]
	[TestCase ("{\"client\":{\"jwt\":\" \"}}", 200)]
	[TestCase ("{\"client\":{\"jwt\":123}}", 200)]
	[TestCase ("not-json", 200)]
	[TestCase ("sensitive-html", 401)]
	[TestCase ("sensitive-html", 500)]
	public async Task JwtFailure_DoesNotAttemptCloudLogin (string response, int status)
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[Server.Brandt]);
		api.Expect ("POST", SESSIONS);
		api.Expect ("GET", JWT, response, (HttpStatusCode)status);
		await Assert.ThatAsync (() => api.Client.Login (), Throws.TypeOf<BrandtServiceException> ());
		Assert.That (api.Client.EventListenerId, Is.Null);
		api.Complete ();
		}

	[TestCase (Server.Brandt)]
	[TestCase (Server.SauterSmartControl)]
	public async Task RejectedCloudLogin_DoesNotRegister (Server server)
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[server]);
		api.Expect ("POST", SESSIONS);
		api.Expect ("GET", JWT, """{"client":{"jwt":"fake-jwt"}}""");
		api.Expect ("POST", "login", """{"success":false}""");
		Assert.That (await api.Client.Login (), Is.False);
		Assert.That (api.Client.EventListenerId, Is.Null);
		api.Complete ();
		}

	[Test]
	public void ServerAndEventValues_AreAdditive ()
		{
		Assert.That ((int)Server.Ubiwizz, Is.EqualTo (16));
		Assert.That ((int)Server.SauterSmartControl, Is.EqualTo (17));
		Assert.That ((int)EventName.ScenarioExecutionStateChanged, Is.EqualTo (17));
		Assert.That ((int)EventName.OgcpTokensRefreshCompleted, Is.EqualTo (18));
		Assert.That (OverkizConst.SupportedServers[Server.SauterSmartControl].Endpoint,
			Is.EqualTo (OverkizConst.SupportedServers[Server.Brandt].Endpoint));
		Assert.That (OverkizConst.SupportedServers[Server.SauterCozytouch].Endpoint,
			Is.Not.EqualTo (OverkizConst.SupportedServers[Server.SauterSmartControl].Endpoint));
		}
	}