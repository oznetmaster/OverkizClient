// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace OverKizApi.Tests;

[TestFixture]
public sealed class ResponseModelTests
	{
	private const string SomfyToken = "https://accounts.somfy.com/oauth/oauth/v2/token/jwt";
	private const string AtlanticToken = "https://apis.groupe-atlantic.com/token";

	private static IEnumerable<TestCaseData> IdentifierResponses ()
		{
		(string Operation, string Method, string Path, string Property)[] endpoints =
			[
			("listener", "POST", "events/register", "id"),
			("action", "POST", "exec/apply", "execId"),
			("scenario", "POST", "exec/scenario", "execId"),
			("schedule", "POST", "exec/schedule/scenario/123", "triggerId"),
			("generate", "GET", "config/gateway/local/tokens/generate", "token"),
			("activate", "POST", "config/gateway/local/tokens", "requestId")
			];
		foreach (var endpoint in endpoints)
			{
			(string Name, string Body, Type? Exception)[] responses =
				[
				("ValidWithExtraFields", "{\"" + endpoint.Property + "\":\"accepted-id\",\"extra\":{\"values\":[true,17]}}", null),
				("MissingField", "{}", typeof (OverkizException)),
				("NullField", "{\"" + endpoint.Property + "\":null}", typeof (OverkizException)),
				("BlankField", "{\"" + endpoint.Property + "\":\" \"}", typeof (OverkizException)),
				("NullBody", "null", typeof (OverkizException)),
				("EmptyBody", "", typeof (OverkizException)),
				("WrongFieldType", "{\"" + endpoint.Property + "\":123}", typeof (JsonException))
				];
			foreach (var response in responses)
				yield return new TestCaseData (endpoint.Operation, endpoint.Method, endpoint.Path, response.Body, response.Exception)
					.SetName ($"IdentifierResponse_{endpoint.Operation}_{response.Name}");
			}
		}

	[TestCaseSource (nameof (IdentifierResponses))]
	public async Task IdentifierResponses_ValidateKnownFieldsAndIgnoreAdditions (string operation, string method, string path, string response, Type? exception)
		{
		using var api = new ScriptedApi ();
		api.Expect (method, path, response);
		if (exception is null)
			Assert.That (await InvokeIdentifierOperation (api.Client, operation), Is.EqualTo ("accepted-id"));
		else
			await Assert.ThatAsync (() => InvokeIdentifierOperation (api.Client, operation), Throws.TypeOf (exception));
		api.Complete ();
		}

	private static async Task<string> InvokeIdentifierOperation (OverkizClient client, string operation)
		{
		if (operation == "listener")
			{
			await client.RegisterEventListener ();
			return client.EventListenerId!;
			}
		return operation switch
			{
			"action" => await client.ExecuteDeviceAction (ScriptedApi.DeviceUrl, [new Command { Name = "stop" }]),
			"scenario" => await client.ExecuteScenario ("scenario"),
			"schedule" => await client.ExecuteScheduledScenario ("scenario", 123),
			"generate" => await client.GenerateLocalToken ("gateway"),
			"activate" => await client.ActivateLocalToken ("gateway", "token", "label"),
			_ => throw new ArgumentOutOfRangeException (nameof (operation))
			};
		}

	[TestCase ("{\"success\":true,\"extra\":{\"gateway\":1}}", true)]
	[TestCase ("null", false)]
	[TestCase ("", false)]
	public async Task Login_HandlesOptionalBodyAndUnknownFields (string response, bool expected)
		{
		using var api = new ScriptedApi (token: null);
		api.Expect ("POST", "login", response);
		Assert.That (await api.Client.Login (false), Is.EqualTo (expected));
		api.Complete ();
		}

	[TestCase ("{\"success\":\"true\"}")]
	[TestCase ("{\"success\":1}")]
	public async Task Login_RejectsNonBooleanSuccess (string response)
		{
		using var api = new ScriptedApi (token: null);
		api.Expect ("POST", "login", response);
		await Assert.ThatAsync (() => api.Client.Login (), Throws.TypeOf<JsonException> ());
		Assert.That (api.Client.EventListenerId, Is.Null);
		api.Complete ();
		}

	[TestCase ("{\"access_token\":null,\"expires_in\":3600}", typeof (SomfyServiceException))]
	[TestCase ("{\"access_token\":\" \" ,\"expires_in\":3600}", typeof (SomfyServiceException))]
	[TestCase ("{\"access_token\":\"token\"}", typeof (SomfyServiceException))]
	[TestCase ("{\"access_token\":\"token\",\"expires_in\":null}", typeof (SomfyServiceException))]
	[TestCase ("{\"access_token\":\"token\",\"expires_in\":{}}", typeof (JsonException))]
	[TestCase ("{\"access_token\":123,\"expires_in\":3600}", typeof (JsonException))]
	public async Task SomfyToken_RejectsMissingAndMistypedRequiredFields (string response, Type exception)
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[Server.SomfyEurope]);
		api.Expect ("POST", SomfyToken, response);
		await Assert.ThatAsync (() => api.Client.SomfyTahomaGetAccessToken (), Throws.TypeOf (exception));
		api.Complete ();
		}

	[TestCase ("{\"access_token\":\"token\",\"expires_in\":3600,\"extra\":[]}")]
	[TestCase ("{\"access_token\":\"token\",\"expires_in\":\"3600\",\"refresh_token\":null}")]
	public async Task SomfyToken_AcceptsOptionalRefreshTokenAndNumericExpiry (string response)
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[Server.SomfyEurope]);
		api.Expect ("POST", SomfyToken, response);
		Assert.That (await api.Client.SomfyTahomaGetAccessToken (), Is.EqualTo ("token"));
		api.Expect ("GET", "setup/devices", "[]", inspect: (request, _) =>
			Assert.That (request.Headers.Authorization!.Parameter, Is.EqualTo ("token")));
		await api.Client.GetDevices ();
		api.Complete ();
		}

	[TestCase ("{\"access_token\":\"bad-token\"}", typeof (SomfyServiceException))]
	[TestCase ("{\"message\":\"error.invalid.grant\"}", typeof (SomfyBadCredentialsException))]
	public async Task RejectedSomfyRefresh_PreservesCurrentAccessToken (string response, Type exception)
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[Server.SomfyEurope]);
		api.Expect ("POST", SomfyToken, "{\"access_token\":\"old-token\",\"refresh_token\":\"refresh\",\"expires_in\":3600}");
		await api.Client.SomfyTahomaGetAccessToken ();
		api.Expect ("POST", SomfyToken, response);
		await Assert.ThatAsync (() => api.Client.RefreshToken (), Throws.TypeOf (exception));
		api.Expect ("GET", "setup/devices", "[]", inspect: (request, _) =>
			Assert.That (request.Headers.Authorization!.Parameter, Is.EqualTo ("old-token")));
		await api.Client.GetDevices ();
		api.Complete ();
		}

	[TestCase ("{\"token_type\":\"Bearer\"}", typeof (CozyTouchServiceException))]
	[TestCase ("{\"access_token\":null,\"token_type\":\"Bearer\"}", typeof (CozyTouchServiceException))]
	[TestCase ("{\"access_token\":\"token\",\"token_type\":null}", typeof (CozyTouchServiceException))]
	[TestCase ("{\"access_token\":\"token\",\"token_type\":[]}", typeof (JsonException))]
	[TestCase ("{\"error\":\"invalid_grant\"}", typeof (CozyTouchBadCredentialsException))]
	public async Task CozyTouchToken_ValidatesFieldsBeforeJwtExchange (string response, Type exception)
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[Server.AtlanticCozytouch]);
		api.Expect ("POST", AtlanticToken, response);
		await Assert.ThatAsync (() => api.Client.CozytouchLogin (), Throws.TypeOf (exception));
		api.Complete ();
		}

	[TestCase ("{}", null)]
	[TestCase ("{\"states\":null,\"deviceStates\":null,\"values\":null}", null)]
	[TestCase ("{\"states\":null,\"deviceStates\":[{\"name\":\"state\"}]}", "state")]
	[TestCase ("{\"deviceStates\":null,\"values\":[{\"name\":\"value\"}],\"extra\":17}", "value")]
	[TestCase ("{\"states\":[{\"name\":\"primary\"}],\"values\":[{\"name\":\"secondary\"}]}", "primary")]
	public async Task DeviceStateWrappers_UseOptionalFieldsWithDefinedPrecedence (string response, string? expectedName)
		{
		using var api = new ScriptedApi ();
		api.Expect ("GET", "setup/devices/" + Uri.EscapeDataString (ScriptedApi.DeviceUrl) + "/states", response);
		IReadOnlyList<State> states = await api.Client.GetDeviceStates (ScriptedApi.DeviceUrl);
		if (expectedName is null)
			Assert.That (states, Is.Empty);
		else
			Assert.That (states.Single ().Name, Is.EqualTo (expectedName));
		api.Complete ();
		}

	[TestCase ("{\"error\":null}", typeof (OverkizException))]
	[TestCase ("{\"error\":123}", typeof (HttpRequestException))]
	[TestCase ("{\"error\":{\"message\":\"unexpected shape\"}}", typeof (HttpRequestException))]
	[TestCase ("{\"error\":\"Not authenticated\",\"extra\":[]}", typeof (NotAuthenticatedException))]
	public async Task ErrorEnvelope_UsesKnownMessageOrHttpFallback (string response, Type exception)
		{
		using var api = new ScriptedApi ();
		api.Expect ("GET", "setup/devices", response, HttpStatusCode.BadRequest);
		await Assert.ThatAsync (() => api.Client.GetDevices (), Throws.TypeOf (exception));
		api.Complete ();
		}
	}