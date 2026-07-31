// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using OverKizApi.Enums;
using OverKizApi.Exceptions;
using OverKizApi.Models;

using Polly;
using Polly.Retry;

namespace OverKizApi;

/// <summary>
/// C# client for the Overkiz API, providing similar capabilities to the Python python-overkiz-api library.
/// Supports cloud (standard, Somfy OAuth, CozyTouch JWT, Nexity SSO) and local API modes.
/// </summary>
public sealed class OverkizClient : IAsyncDisposable
	{
	// ── Configuration ──────────────────────────────────────────────────────

	/// <summary>Account username or e-mail address used to authenticate with the Overkiz server.</summary>
	public string Username
		{
		get;
		}

	/// <summary>Account password used to authenticate with the Overkiz server.</summary>
	public string Password
		{
		get;
		}

	/// <summary>The target server this client is connected to.</summary>
	public OverkizServer Server
		{
		get;
		}

	/// <summary>Whether the client is operating against the cloud API or a local gateway API.</summary>
	public APIType ApiType
		{
		get;
		}

	// ── Cached state ───────────────────────────────────────────────────────

	/// <summary>
	/// The most recently retrieved <see cref="Models.Setup"/>, or <see langword="null"/> if
	/// <see cref="GetSetup"/> has not yet been called.
	/// </summary>
	public Setup? Setup
		{
		get; private set;
		}

	/// <summary>
	/// All devices from the most recently retrieved setup.
	/// Populated by <see cref="GetSetup"/>; empty until that method is called.
	/// </summary>
	public List<Device> Devices { get; private set; } = [];

	/// <summary>
	/// All gateways from the most recently retrieved setup.
	/// Populated by <see cref="GetSetup"/>; empty until that method is called.
	/// </summary>
	public List<Gateway> Gateways { get; private set; } = [];

	/// <summary>
	/// The event listener ID returned by the last successful <see cref="RegisterEventListener"/> call,
	/// or <see langword="null"/> if no listener is currently registered.
	/// </summary>
	public string? EventListenerId
		{
		get; private set;
		}

	/// <summary>The currently selected Rexel gateway ID, or <see langword="null"/> if none has been selected.</summary>
	public string? SelectedGatewayId { get; private set; }

	// ── Private fields ─────────────────────────────────────────────────────
	private readonly HttpClient _http;
	private readonly bool _ownsHttpClient;
	private string? _accessToken;
	private string? _refreshToken;
	private DateTime? _expiresAt;

	// ── Local-mode label-change detection ──────────────────────────────────
	// On local connections the gateway never emits DeviceUpdatedEvent for renames.
	// We diff device labels inside FetchEventsRaw and synthesize the event so callers
	// need no special handling.  Cloud connections skip this entirely.
	private readonly Dictionary<string, string> _labelSnapshot
		= new (StringComparer.OrdinalIgnoreCase);
	private DateTime _lastLabelCheck = DateTime.MinValue;
	private static readonly TimeSpan _labelPollInterval = TimeSpan.FromSeconds (30);

	private static readonly JsonSerializerOptions _jsonOptions = new ()
		{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		NumberHandling = JsonNumberHandling.AllowReadingFromString,
		Converters = { new TolerantEnumConverterFactory () },
		};

	// ── Polly retry pipelines ───────────────────────────────────────────────
	private ResiliencePipeline<HttpResponseMessage> BuildAuthRetry () =>
		new ResiliencePipelineBuilder<HttpResponseMessage> ()
			.AddRetry (new RetryStrategyOptions<HttpResponseMessage>
				{
				ShouldHandle = new PredicateBuilder<HttpResponseMessage> ()
					.Handle<NotAuthenticatedException> (),
				MaxRetryAttempts = 2,
				DelayGenerator = args => new ValueTask<TimeSpan?> (
					TimeSpan.FromSeconds (Math.Pow (2, args.AttemptNumber))),
				OnRetry = async args =>
					{
						_ = await Login (registerEventListener: false);
						if (EventListenerId is not null)
							await RegisterEventListener ();
					},
				})
			.Build ();

	// ── Constructor ────────────────────────────────────────────────────────

	/// <summary>
	/// Creates a new <see cref="OverkizClient"/>.
	/// </summary>
	/// <param name="username">Account username / email.</param>
	/// <param name="password">Account password.</param>
	/// <param name="server">Target <see cref="OverkizServer"/> (use <see cref="OverkizConst.SupportedServers"/>).</param>
	/// <param name="token">Pre-existing bearer token (optional; bypasses login).</param>
	/// <param name="httpClient">Optional externally-managed <see cref="HttpClient"/>. If null, one is created internally.</param>
	public OverkizClient (
		string username,
		string password,
		OverkizServer server,
		string? token = null,
		HttpClient? httpClient = null)
		{
		Username = username;
		Password = password;
		Server = server;
		_accessToken = token;
		_ownsHttpClient = httpClient is null;
		_http = httpClient ?? new HttpClient ();
		_http.BaseAddress = new Uri (server.Endpoint);

		ApiType = server.Endpoint.Contains (OverkizConst.LOCAL_API_PATH, StringComparison.Ordinal)
			? APIType.Local
			: APIType.Cloud;
		}

	// ── IAsyncDisposable ───────────────────────────────────────────────────

	/// <summary>
	/// Releases resources owned by this client.
	/// If an event listener is registered it is unregistered (best-effort) before disposal.
	/// If the <see cref="HttpClient"/> was created internally it is also disposed.
	/// </summary>
	public ValueTask DisposeAsync () => new(DisposeAsyncCore ());

	private async Task DisposeAsyncCore ()
		{
		if (EventListenerId is not null)
			{
			try
				{
				await UnregisterEventListener ();
				}
			catch { /* best-effort */ }
			}

		if (_ownsHttpClient)
			_http.Dispose ();
		}

	// ── Authentication ─────────────────────────────────────────────────────

	/// <summary>
	/// Authenticate and open an API session. Must be called before other operations unless a token was supplied.
	/// </summary>
	/// <param name="registerEventListener">Register an event listener after login (default true).</param>
	public async Task<bool> Login (bool registerEventListener = true)
		{
		// Local API – no username/password login endpoint
		if (ApiType == APIType.Local)
			{
			if (registerEventListener)
				await RegisterEventListener ();
			else
				_ = await GetGateways ();   // verify token is valid
			return true;
			}

		// Rexel uses externally-managed bearer tokens and gateway selection.
		if (Server == OverkizConst.SupportedServers [Enums.Server.Rexel])
			{
			if (string.IsNullOrWhiteSpace (_accessToken))
				throw new InvalidOperationException ("Rexel requires an externally managed bearer token. Supply it via the constructor token parameter.");

			IReadOnlyList<GatewayCandidate> gateways = await DiscoverRexelGateways ();
			if (gateways.Count == 1)
				SelectRexelGateway (gateways [0].GatewayId);

			if (registerEventListener)
				await RegisterEventListener ();
			else
				_ = await GetGateways ();

			return true;
			}

		// Somfy TaHoma (Europe) uses OAuth
		if (Server == OverkizConst.SupportedServers [Enums.Server.SomfyEurope])
			{
			_ = await SomfyTahomaGetAccessToken ();
			if (registerEventListener)
				await RegisterEventListener ();
			return true;
			}

		// CozyTouch servers use a JWT
		if (Server == OverkizConst.SupportedServers [Enums.Server.AtlanticCozytouch] ||
			Server == OverkizConst.SupportedServers [Enums.Server.ThermorCozytouch] ||
			Server == OverkizConst.SupportedServers [Enums.Server.SauterCozytouch])
			{
			var jwt = await CozytouchLogin ();
			Dictionary<string, object?> response = await PostAsync ("login", new Dictionary<string, string> { ["jwt"] = jwt });
			var success = response.TryGetValue ("success", out var s) && s is true;
			if (success && registerEventListener)
				await RegisterEventListener ();
			return success;
			}

		// Nexity uses SSO token
		if (Server == OverkizConst.SupportedServers [Enums.Server.Nexity])
			{
			var ssoToken = await NexityLogin ();
			var userId = Username.Replace ("@", "_-_");
			var payload = new Dictionary<string, string>
				{
				["userId"] = userId,
				["userPassword"] = Password,
				["ssoToken"] = ssoToken,
				};
			Dictionary<string, object?> response = await PostAsync ("login", payload);
			var success = response.TryGetValue ("success", out var s) && s is true;
			if (success && registerEventListener)
				await RegisterEventListener ();
			return success;
			}

		// Standard username + password
			{
			var payload = new Dictionary<string, string>
				{
				["userId"] = Username,
				["userPassword"] = Password,
				};
			Dictionary<string, object?> response = await PostAsync ("login", payload);
			var success = response.TryGetValue ("success", out var s) && s is true;
			if (success && registerEventListener)
				await RegisterEventListener ();
			return success;
			}
		}

	/// <summary>Authenticate via Somfy identity and acquire an access token.</summary>
	/// <returns>The raw access token string stored in <see cref="_accessToken"/>.</returns>
	/// <exception cref="SomfyBadCredentialsException">Thrown when the supplied credentials are rejected by the Somfy OAuth server.</exception>
	/// <exception cref="SomfyServiceException">Thrown when the Somfy token endpoint returns an unexpected response.</exception>
	public async Task<string> SomfyTahomaGetAccessToken ()
		{
		var form = new FormUrlEncodedContent ([
			new ("grant_type", "password"),
			new ("username", Username),
			new ("password", Password),
			new ("client_id", OverkizConst.SOMFY_CLIENT_ID),
			new ("client_secret", OverkizConst.SOMFY_CLIENT_SECRET),
			]);

		using HttpResponseMessage resp = await _http.PostAsync (
			new Uri (OverkizConst.SOMFY_API + "/oauth/oauth/v2/token/jwt"), form);

		Dictionary<string, JsonElement> token = await resp.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>> (_jsonOptions)
			?? throw new SomfyServiceException ("Empty response from Somfy token endpoint.");

		if (token.TryGetValue ("message", out JsonElement msg) && msg.GetString () == "error.invalid.grant")
			throw new SomfyBadCredentialsException (msg.GetString ()!);

		if (!token.TryGetValue ("access_token", out JsonElement value))
			throw new SomfyServiceException ("No Somfy access token provided.");

		_accessToken = value.GetString ()!;
		_refreshToken = token["refresh_token"].GetString ();
		_expiresAt = DateTime.Now.AddSeconds (token["expires_in"].GetInt32 () - 5);
		return _accessToken;
		}

	/// <summary>
	/// Refreshes the Somfy access token using the stored refresh token.
	/// This is called automatically by <see cref="RefreshTokenIfExpired"/> when the token is near expiry;
	/// you do not normally need to call it manually.
	/// </summary>
	/// <remarks>No-op if the current server is not <see cref="OverkizConst.SupportedServers"/>[<see cref="Server.SomfyEurope"/>].</remarks>
	/// <exception cref="InvalidOperationException">Thrown when no refresh token is available (i.e. <see cref="Login"/> was not called first).</exception>
	/// <exception cref="SomfyBadCredentialsException">Thrown when the refresh token has been revoked.</exception>
	/// <exception cref="SomfyServiceException">Thrown when the Somfy token endpoint returns an unexpected response.</exception>
	public async Task RefreshToken ()
		{
		if (Server != OverkizConst.SupportedServers[Enums.Server.SomfyEurope])
			return;

		if (_refreshToken is null)
			throw new InvalidOperationException ("No refresh token available. Call Login first.");

		var form = new FormUrlEncodedContent ([
			new ("grant_type", "refresh_token"),
			new ("refresh_token", _refreshToken),
			new ("client_id", OverkizConst.SOMFY_CLIENT_ID),
			new ("client_secret", OverkizConst.SOMFY_CLIENT_SECRET),
			]);

		using HttpResponseMessage resp = await _http.PostAsync (
			new Uri (OverkizConst.SOMFY_API + "/oauth/oauth/v2/token/jwt"), form);

		Dictionary<string, JsonElement> token = await resp.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>> (_jsonOptions)
			?? throw new SomfyServiceException ("Empty response from Somfy refresh endpoint.");

		if (token.TryGetValue ("message", out JsonElement msg) && msg.GetString () == "error.invalid.grant")
			throw new SomfyBadCredentialsException (msg.GetString ()!);

		if (!token.TryGetValue ("access_token", out JsonElement value))
			throw new SomfyServiceException ("No Somfy access token provided.");

		_accessToken = value.GetString ()!;
		_refreshToken = token["refresh_token"].GetString ();
		_expiresAt = DateTime.Now.AddSeconds (token["expires_in"].GetInt32 () - 5);
		}

	/// <summary>
	/// Authenticates against the Atlantic/CozyTouch OAuth endpoint and returns
	/// the JWT token required for the subsequent Overkiz <c>login</c> call.
	/// </summary>
	/// <returns>The JWT string to pass to the Overkiz <c>login</c> endpoint.</returns>
	/// <exception cref="CozyTouchBadCredentialsException">Thrown when the Atlantic OAuth server rejects the credentials.</exception>
	/// <exception cref="CozyTouchServiceException">Thrown when the Atlantic token or JWT endpoint returns an unexpected response.</exception>
	public async Task<string> CozytouchLogin ()
		{
		// Step 1: get OAuth2 token from Atlantic API
		var form = new FormUrlEncodedContent ([
			new ("grant_type", "password"),
			new ("username", "GA-PRIVATEPERSON/" + Username),
			new ("password", Password),
			]);
		form.Headers.ContentType = new MediaTypeHeaderValue ("application/x-www-form-urlencoded");

		using HttpResponseMessage tokenResp = await _http.PostAsync (
			new Uri (OverkizConst.COZYTOUCH_ATLANTIC_API + "/token"),
			new HttpRequestMessage (HttpMethod.Post, OverkizConst.COZYTOUCH_ATLANTIC_API + "/token")
				{
				Content = form,
				Headers = { Authorization = new AuthenticationHeaderValue ("Basic", OverkizConst.COZYTOUCH_CLIENT_ID) },
				}.Content);

		Dictionary<string, JsonElement> tokenJson = await tokenResp.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>> (_jsonOptions)
			?? throw new CozyTouchServiceException ("Empty response from CozyTouch token endpoint.");

		if (tokenJson.TryGetValue ("error", out JsonElement err) && err.GetString () == "invalid_grant")
			{
			throw new CozyTouchBadCredentialsException (
				tokenJson.TryGetValue ("error_description", out JsonElement desc) ? desc.GetString ()! : "Invalid grant");
			}

		if (!tokenJson.TryGetValue ("token_type", out _))
			throw new CozyTouchServiceException ("No CozyTouch token provided.");

		var accessToken = tokenJson["access_token"].GetString ()!;

		// Step 2: exchange for JWT
		using var req = new HttpRequestMessage (HttpMethod.Get, OverkizConst.COZYTOUCH_ATLANTIC_API + "/magellan/accounts/jwt");
		req.Headers.Authorization = new AuthenticationHeaderValue ("Bearer", accessToken);
		using HttpResponseMessage jwtResp = await _http.SendAsync (req);
		string jwtRaw = await jwtResp.Content.ReadAsStringAsync ();
		string jwt = jwtRaw.Trim ().Trim ('"');

		return jwt.Length == 0 ? throw new CozyTouchServiceException ("No JWT token provided.") : jwt;
		}

	/// <summary>
	/// Authenticates against the Nexity AWS Cognito SRP endpoint and returns the SSO token
	/// required for the subsequent Overkiz <c>login</c> call.
	/// </summary>
	/// <remarks>
	/// This method is a stub. Nexity authentication requires the AWS Cognito SRP protocol
	/// which depends on <c>AWSSDK.CognitoIdentityProvider</c>. Add that package and override
	/// (or replace) this method with a real SRP implementation.
	/// </remarks>
	/// <returns>The Nexity SSO token string.</returns>
	/// <exception cref="NotSupportedException">Always thrown by the default stub implementation.</exception>
	public Task<string> NexityLogin () =>
		// Nexity uses AWS Cognito (SRP) — a full implementation requires AWSSDK.CognitoIdentityProvider.
		// This stub throws to indicate the dependency is not bundled.
		throw new NotSupportedException (
			"Nexity authentication requires AWS Cognito SRP. " +
			"Add AWSSDK.CognitoIdentityProvider and implement SRP authentication, " +
			"then override this method.");

	// ── Event Listener ─────────────────────────────────────────────────────

	/// <summary>Register an event listener to receive device state changes.</summary>
	/// <remarks>The assigned listener ID is stored in <see cref="EventListenerId"/> after this call completes.</remarks>
	/// <exception cref="OverkizException">Thrown when the server does not return a valid listener ID.</exception>
	public async Task RegisterEventListener ()
		{
		Dictionary<string, object?> response = await PostAsync ("events/register", new
			{
			});
		EventListenerId = response["id"]?.ToString () ?? throw new OverkizException ("No event listener ID returned.");
		}

	/// <summary>
	/// Fetches all queued events from the registered event listener and clears the server-side queue.
	/// Call this method repeatedly (polling) to receive device state change notifications.
	/// </summary>
	/// <returns>A list of <see cref="EventObject"/> objects; may be empty if no events are pending.</returns>
	/// <exception cref="NoRegisteredEventListenerException">Thrown when no event listener is registered. Call <see cref="RegisterEventListener"/> first.</exception>
	public async Task<IReadOnlyList<EventObject>> FetchEvents ()
		{
		(IReadOnlyList<EventObject>? events, string _) = await FetchEventsRaw ();
		return events;
		}

	/// <summary>
	/// Like <see cref="FetchEvents"/> but also returns the raw JSON response string for diagnostics.
	/// </summary>
	public async Task<(IReadOnlyList<EventObject> Events, string RawJson)> FetchEventsRaw ()
		{
		if (EventListenerId is null)
			throw new NoRegisteredEventListenerException ("No event listener registered. Call RegisterEventListener first.");

		var response = await PostRawAsync ($"events/{EventListenerId}/fetch");
		List<EventObject> events = JsonSerializer.Deserialize<List<EventObject>> (response, _jsonOptions) ?? [];

		// Local-only: synthesize DeviceUpdatedEvent for renames not signalled by the gateway.
		if (ApiType == APIType.Local && DateTime.UtcNow - _lastLabelCheck >= _labelPollInterval)
			{
			_lastLabelCheck = DateTime.UtcNow;
			try
				{
				IReadOnlyList<Device> devices = await GetDevices ();
				bool snapshotWasEmpty = _labelSnapshot.Count == 0;

				foreach (Device d in devices)
					{
					if (d.DeviceUrl == null || d.Label == null)
						continue;

					var newLabel = d.Label.Trim ();

					if (_labelSnapshot.TryGetValue (d.DeviceUrl, out string? oldLabel))
						{
						if (!string.Equals (oldLabel, newLabel, StringComparison.Ordinal))
							{
							_labelSnapshot[d.DeviceUrl] = newLabel;
							// Only emit the event once we had a prior snapshot to compare against.
							if (!snapshotWasEmpty)
								{
								events.Add (new EventObject
									{
									Name      = "DeviceUpdatedEvent",
									DeviceUrl = d.DeviceUrl,
									Label     = newLabel,
									Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds (),
									});
								}
							}
						}
					else
						{
						_labelSnapshot[d.DeviceUrl] = newLabel;
						}
					}
				}
			catch
				{
				// Label poll is best-effort; don't surface errors to the caller.
				}
			}

		return (events, response);
		}

	/// <summary>
	/// Unregisters the event listener and clears <see cref="EventListenerId"/>.
	/// Safe to call when no listener is registered (no-op).
	/// </summary>
	public async Task UnregisterEventListener ()
		{
		if (EventListenerId is null)
			return;

		_ = await PostRawAsync ($"events/{EventListenerId}/unregister");
		EventListenerId = null;
		}

	// ── Setup ──────────────────────────────────────────────────────────────

	// ── Setup ──────────────────────────────────────────────────────────────

	/// <summary>
	/// Retrieves the complete home setup including all gateways, devices, zones, places and location.
	/// The result is cached; pass <paramref name="refresh"/> as <see langword="true"/> to force a fresh API call.
	/// </summary>
	/// <param name="refresh">When <see langword="true"/> the cached result is discarded and a new API call is made.</param>
	/// <returns>The <see cref="Models.Setup"/> for the authenticated account.</returns>
	/// <exception cref="OverkizException">Thrown when the response cannot be deserialised.</exception>
	public async Task<Setup> GetSetup (bool refresh = false)
		{
		if (Setup is not null && !refresh)
			return Setup;

		await RefreshTokenIfExpired ();
		var raw = await GetRawAsync ("setup");
		Setup setup = JsonSerializer.Deserialize<Setup> (raw, _jsonOptions)
			?? throw new OverkizException ("Failed to deserialize Setup.");

		Setup = setup;
		Gateways = [.. setup.Gateways];
		Devices = [.. setup.Devices];
		return setup;
		}

	/// <summary>Returns all gateways registered in the setup.</summary>
	/// <returns>A read-only list of <see cref="Gateway"/> objects.</returns>
	public async Task<IReadOnlyList<Gateway>> GetGateways ()
		{
		await RefreshTokenIfExpired ();
		var raw = await GetRawAsync ("setup/gateways");
		return JsonSerializer.Deserialize<List<Gateway>> (raw, _jsonOptions) ?? [];
		}

	/// <summary>Discovers Rexel homes and gateways available to the current bearer token.</summary>
	/// <returns>A read-only list of gateway candidates.</returns>
	/// <exception cref="UnsupportedOperationException">Thrown when the current server is not Rexel.</exception>
	/// <exception cref="InvalidOperationException">Thrown when no bearer token is available.</exception>
	public async Task<IReadOnlyList<GatewayCandidate>> DiscoverRexelGateways ()
		{
		if (Server != OverkizConst.SupportedServers[Enums.Server.Rexel])
			throw new UnsupportedOperationException ("Gateway discovery is only available for the Rexel server.");
		if (string.IsNullOrWhiteSpace (_accessToken))
			throw new InvalidOperationException ("Rexel gateway discovery requires a bearer token.");

		var homesJson = await GetAbsoluteRawAsync ($"{OverkizConst.REXEL_ENDUSER_API}/homes", includeGatewayHeader: false);
		List<RexelHomeDirectoryEntry> homes = JsonSerializer.Deserialize<List<RexelHomeDirectoryEntry>> (homesJson, _jsonOptions) ?? [];
		var candidates = new List<GatewayCandidate> ();

		foreach (RexelHomeDirectoryEntry home in homes)
			{
			string gatewaysJson = await GetAbsoluteRawAsync ($"{OverkizConst.REXEL_ENDUSER_API}/overkizgateways?homeId={Uri.EscapeDataString (home.Id)}", includeGatewayHeader: false);
			List<RexelGatewayDirectoryEntry> gateways = JsonSerializer.Deserialize<List<RexelGatewayDirectoryEntry>> (gatewaysJson, _jsonOptions) ?? [];

			foreach (RexelGatewayDirectoryEntry gateway in gateways)
				{
				candidates.Add (new GatewayCandidate
					{
					GatewayId = gateway.GatewayId,
					HomeId = home.Id,
					Label = home.Label,
					ExternalId = gateway.ExternalId,
					});
				}
			}

		return candidates;
		}

	/// <summary>Selects the Rexel gateway to scope subsequent requests to.</summary>
	/// <param name="gatewayId">Gateway identifier returned by <see cref="DiscoverRexelGateways"/>.</param>
	/// <exception cref="UnsupportedOperationException">Thrown when the current server is not Rexel.</exception>
	public void SelectRexelGateway (string gatewayId)
		{
		if (Server != OverkizConst.SupportedServers[Enums.Server.Rexel])
			throw new UnsupportedOperationException ("Gateway selection is only available for the Rexel server.");

		SelectedGatewayId = string.IsNullOrWhiteSpace (gatewayId)
			? throw new ArgumentException ("Gateway ID is required.", nameof (gatewayId))
			: gatewayId;
		}

	/// <summary>Returns all devices registered across all gateways in the setup.</summary>
	/// <returns>A read-only list of <see cref="Device"/> objects.</returns>
	public async Task<IReadOnlyList<Device>> GetDevices ()
		{
		await RefreshTokenIfExpired ();
		var raw = await GetRawAsync ("setup/devices");
		return JsonSerializer.Deserialize<List<Device>> (raw, _jsonOptions) ?? [];
		}

	/// <summary>Returns a single device identified by its URL.</summary>
	/// <param name="deviceUrl">The device URL (e.g. <c>io://1234-5678-9012/12345678</c>).</param>
	/// <returns>The matching <see cref="Device"/>.</returns>
	/// <exception cref="OverkizException">Thrown when the response cannot be deserialised.</exception>
	public async Task<Device> GetDevice (string deviceUrl)
		{
		await RefreshTokenIfExpired ();
		var encoded = Uri.EscapeDataString (deviceUrl);
		var raw = await GetRawAsync ($"setup/devices/{encoded}");
		return JsonSerializer.Deserialize<Device> (raw, _jsonOptions)
			?? throw new OverkizException ($"Failed to deserialize Device for {deviceUrl}.");
		}

	/// <summary>Returns the current live states for a device.</summary>
	/// <param name="deviceUrl">The device URL to query.</param>
	/// <returns>A read-only list of <see cref="State"/> objects.</returns>
	public async Task<IReadOnlyList<State>> GetDeviceStates (string deviceUrl)
		{
		await RefreshTokenIfExpired ();
		var encoded = Uri.EscapeDataString (deviceUrl);
		var raw = await GetRawAsync ($"setup/devices/{encoded}/states");
		raw = raw.TrimStart ();
		if (raw.Length > 0 && raw [0] == '{')
			{
			using var doc = JsonDocument.Parse (raw);
			// Empty object {} means no states; try common wrapper keys otherwise
			foreach (string key in new [] { "states", "deviceStates", "values" })
				{
				if (doc.RootElement.TryGetProperty (key, out JsonElement arr))
					return JsonSerializer.Deserialize<List<State>> (arr.GetRawText (), _jsonOptions) ?? [];
				}

			return [];
			}

		return JsonSerializer.Deserialize<List<State>> (raw, _jsonOptions) ?? [];
		}

	/// <summary>
	/// Asks the gateway to refresh all device states from the physical devices.
	/// Updated states will be pushed through the event listener.
	/// </summary>
	public async Task RefreshAllDeviceStates ()
		{
		await RefreshTokenIfExpired ();
		_ = await PostAsync ("setup/devices/states/refresh");
		}

	// ── Execution ──────────────────────────────────────────────────────────

	/// <summary>Sends a set of commands to a device as a single execution action.</summary>
	/// <param name="deviceUrl">URL of the target device.</param>
	/// <param name="commands">One or more <see cref="Command"/> objects to execute.</param>
	/// <param name="label">Optional human-readable label recorded in the execution history.</param>
	/// <returns>The execution ID (<c>execId</c>) assigned by the server.</returns>
	/// <exception cref="OverkizException">Thrown when the server does not return an execution ID.</exception>
	public async Task<string> ExecuteDeviceAction (string deviceUrl, IEnumerable<Command> commands, string label = "Execute")
		{
		await RefreshTokenIfExpired ();
		var payload = new
			{
			label,
			actions = new[] { new { deviceURL = deviceUrl, commands } },
			};
		Dictionary<string, object?> response = await PostAsync ("exec/apply", payload);
		return response.TryGetValue ("execId", out var id) && id is not null
			? id.ToString ()!
			: throw new OverkizException ("No execId returned.");
		}

	/// <summary>Cancels a running or queued execution.</summary>
	/// <param name="execId">The execution ID returned by <see cref="ExecuteDeviceAction"/> or <see cref="ExecuteScenario"/>.</param>
	public async Task CancelExecution (string execId)
		{
		await RefreshTokenIfExpired ();
		await DeleteAsync ($"exec/current/setup/{execId}");
		}

	/// <summary>Returns all executions that are currently active (in-progress or queued) on the gateway.</summary>
	/// <returns>A read-only list of <see cref="Execution"/> objects; empty if nothing is running.</returns>
	public async Task<IReadOnlyList<Execution>> GetCurrentExecutions ()
		{
		await RefreshTokenIfExpired ();
		var raw = await GetRawAsync ("exec/current");
		return JsonSerializer.Deserialize<List<Execution>> (raw, _jsonOptions) ?? [];
		}

	/// <summary>Returns the execution history log stored on the gateway.</summary>
	/// <returns>A read-only list of <see cref="HistoryExecution"/> records, most-recent first.</returns>
	public async Task<IReadOnlyList<HistoryExecution>> GetExecutionHistory ()
		{
		await RefreshTokenIfExpired ();
		var raw = await GetRawAsync ("history/executions");
		return JsonSerializer.Deserialize<List<HistoryExecution>> (raw, _jsonOptions) ?? [];
		}

	// ── Scenarios ──────────────────────────────────────────────────────────

	/// <summary>Returns all named scenarios (action groups) stored in the setup.</summary>
	/// <returns>A read-only list of <see cref="Scenario"/> objects.</returns>
	public async Task<IReadOnlyList<Scenario>> GetScenarios ()
		{
		await RefreshTokenIfExpired ();
		var raw = await GetRawAsync ("actionGroups");
		return JsonSerializer.Deserialize<List<Scenario>> (raw, _jsonOptions) ?? [];
		}

	/// <summary>Triggers immediate execution of a stored scenario.</summary>
	/// <param name="oid">The OID of the scenario to execute (see <see cref="Scenario.Oid"/>).</param>
	/// <returns>The execution ID assigned by the server.</returns>
	/// <exception cref="OverkizException">Thrown when the server does not return an execution ID.</exception>
	public async Task<string> ExecuteScenario (string oid)
		{
		await RefreshTokenIfExpired ();
		Dictionary<string, object?> response = await PostAsync ($"exec/{oid}", new
			{
			});
		return response["execId"]?.ToString () ?? throw new OverkizException ("No execId returned.");
		}

	/// <summary>Schedules a scenario to execute at a specific point in time.</summary>
	/// <param name="oid">The OID of the scenario to schedule.</param>
	/// <param name="timestamp">Unix epoch millisecond timestamp at which to trigger the scenario.</param>
	/// <returns>The trigger ID assigned by the server.</returns>
	/// <exception cref="OverkizException">Thrown when the server does not return a trigger ID.</exception>
	public async Task<string> ExecuteScheduledScenario (string oid, long timestamp)
		{
		await RefreshTokenIfExpired ();
		Dictionary<string, object?> response = await PostAsync ($"exec/schedule/{oid}/{timestamp}", new
			{
			});
		return response["triggerId"]?.ToString () ?? throw new OverkizException ("No triggerId returned.");
		}

	// ── Places ─────────────────────────────────────────────────────────────

	/// <summary>Returns the root of the place hierarchy (house → floors → rooms).</summary>
	/// <returns>The root <see cref="Place"/> containing all sub-places.</returns>
	/// <exception cref="OverkizException">Thrown when the response cannot be deserialised.</exception>
	public async Task<Place> GetPlaces ()
		{
		await RefreshTokenIfExpired ();
		var raw = await GetRawAsync ("setup/places");
		return JsonSerializer.Deserialize<Place> (raw, _jsonOptions)
			?? throw new OverkizException ("Failed to deserialize Place.");
		}

	// ── Local tokens ───────────────────────────────────────────────────────

	/// <summary>
	/// Generates a new local API token for the specified gateway.
	/// The token must then be activated with <see cref="ActivateLocalToken"/> before it can be used.
	/// </summary>
	/// <param name="gatewayId">Serial number of the gateway to generate a token for. For Rexel cloud endpoints, use <see cref="GatewayCandidate.ExternalId"/> rather than the header-scoping <see cref="GatewayCandidate.GatewayId"/>.</param>
	/// <returns>The raw token string to pass to <see cref="ActivateLocalToken"/>.</returns>
	/// <exception cref="OverkizException">Thrown when the server does not return a token.</exception>
	public async Task<string> GenerateLocalToken (string gatewayId)
		{
		await RefreshTokenIfExpired ();
		string encodedGatewayId = Uri.EscapeDataString (gatewayId);
		Dictionary<string, object?> response = await GetAsync ($"config/{encodedGatewayId}/local/tokens/generate");
		return response["token"]?.ToString () ?? throw new OverkizException ("No token returned.");
		}

	/// <summary>
	/// Registers a generated local API token on the gateway so that it can be used for local API calls.
	/// </summary>
	/// <param name="gatewayId">Serial number of the gateway to register the token on. For Rexel cloud endpoints, use <see cref="GatewayCandidate.ExternalId"/>.</param>
	/// <param name="token">The raw token string returned by <see cref="GenerateLocalToken"/>.</param>
	/// <param name="label">A human-readable label to identify this token in the token list.</param>
	/// <param name="scope">Access scope to grant (default <c>"devmode"</c>).</param>
	/// <returns>The request ID of the activation request.</returns>
	/// <exception cref="OverkizException">Thrown when the server does not return a request ID.</exception>
	public async Task<string> ActivateLocalToken (string gatewayId, string token, string label, string scope = "devmode")
		{
		await RefreshTokenIfExpired ();
		string encodedGatewayId = Uri.EscapeDataString (gatewayId);
		Dictionary<string, object?> response = await PostAsync (
			$"config/{encodedGatewayId}/local/tokens",
			new
				{
				label,
				token,
				scope
				});
		return response["requestId"]?.ToString () ?? throw new OverkizException ("No requestId returned.");
		}

	/// <summary>Returns all active local API tokens for a gateway filtered by scope.</summary>
	/// <param name="gatewayId">Serial number of the gateway to query. For Rexel cloud endpoints, use <see cref="GatewayCandidate.ExternalId"/>.</param>
	/// <param name="scope">Token scope to filter by (default <c>"devmode"</c>).</param>
	/// <returns>A read-only list of <see cref="LocalToken"/> descriptors.</returns>
	public async Task<IReadOnlyList<LocalToken>> GetLocalTokens (string gatewayId, string scope = "devmode")
		{
		await RefreshTokenIfExpired ();
		string encodedGatewayId = Uri.EscapeDataString (gatewayId);
		string encodedScope = Uri.EscapeDataString (scope);
		var raw = await GetRawAsync ($"config/{encodedGatewayId}/local/tokens/{encodedScope}");
		return JsonSerializer.Deserialize<List<LocalToken>> (raw, _jsonOptions) ?? [];
		}

	/// <summary>Revokes and deletes a local API token.</summary>
	/// <param name="gatewayId">Serial number of the gateway the token belongs to. For Rexel cloud endpoints, use <see cref="GatewayCandidate.ExternalId"/>.</param>
	/// <param name="uuid">UUID of the token to delete (see <see cref="LocalToken.Uuid"/>).</param>
	/// <returns><see langword="true"/> on success.</returns>
	public async Task<bool> DeleteLocalToken (string gatewayId, string uuid)
		{
		await RefreshTokenIfExpired ();
		string encodedGatewayId = Uri.EscapeDataString (gatewayId);
		string encodedUuid = Uri.EscapeDataString (uuid);
		await DeleteAsync ($"config/{encodedGatewayId}/local/tokens/{encodedUuid}");
		return true;
		}

	/// <summary>
	/// Opens the local pairing window on a gateway for approximately 180 seconds.
	/// During this period, additional local tokens may be registered directly on the gateway.
	/// </summary>
	/// <param name="gatewayId">Serial number of the gateway. For Rexel cloud endpoints, use <see cref="GatewayCandidate.ExternalId"/>.</param>
	/// <returns>The raw JSON response payload, or <see langword="null"/> if the server returns no body.</returns>
	public async Task<JsonElement?> OpenLocalPairing (string gatewayId)
		{
		await RefreshTokenIfExpired ();
		string encodedGatewayId = Uri.EscapeDataString (gatewayId);
		string raw = await PostRawAsync ($"config/{encodedGatewayId}/local/openPairing");
		if (string.IsNullOrWhiteSpace (raw))
			return null;

		using JsonDocument document = JsonDocument.Parse (raw);
		return document.RootElement.Clone ();
		}

	/// <summary>
	/// Activates developer mode for a gateway.
	/// This is required on supported gateways before local developer-mode tokens can be used.
	/// </summary>
	/// <param name="gatewayId">Serial number of the gateway. For Rexel cloud endpoints, use <see cref="GatewayCandidate.ExternalId"/>.</param>
	public async Task ActivateDeveloperMode (string gatewayId)
		{
		await RefreshTokenIfExpired ();
		string encodedGatewayId = Uri.EscapeDataString (gatewayId);
		_ = await PostAsync ($"setup/gateways/{encodedGatewayId}/developerMode");
		}

	/// <summary>Returns the current developer-mode status for a gateway.</summary>
	/// <param name="gatewayId">Serial number of the gateway. For Rexel cloud endpoints, use <see cref="GatewayCandidate.ExternalId"/>.</param>
	/// <returns>A <see cref="DeveloperMode"/> object describing whether developer mode is active.</returns>
	public async Task<DeveloperMode> GetDeveloperMode (string gatewayId)
		{
		await RefreshTokenIfExpired ();
		string encodedGatewayId = Uri.EscapeDataString (gatewayId);
		string raw = await GetRawAsync ($"setup/gateways/{encodedGatewayId}/developerMode");
		return JsonSerializer.Deserialize<DeveloperMode> (raw, _jsonOptions)
			?? throw new OverkizException ($"Failed to deserialize developer mode status for gateway {gatewayId}.");
		}

	/// <summary>
	/// Deactivates developer mode for a gateway.
	/// Some gateways reject this endpoint even when they support reading developer-mode state.
	/// </summary>
	/// <param name="gatewayId">Serial number of the gateway. For Rexel cloud endpoints, use <see cref="GatewayCandidate.ExternalId"/>.</param>
	public async Task DeactivateDeveloperMode (string gatewayId)
		{
		await RefreshTokenIfExpired ();
		string encodedGatewayId = Uri.EscapeDataString (gatewayId);
		await DeleteAsync ($"setup/gateways/{encodedGatewayId}/developerMode");
		}

	// ── Setup options ──────────────────────────────────────────────────────

	/// <summary>Returns all option subscriptions active on the authenticated setup.</summary>
	/// <returns>A read-only list of <see cref="OptionObject"/> objects.</returns>
	public async Task<IReadOnlyList<OptionObject>> GetSetupOptions ()
		{
		await RefreshTokenIfExpired ();
		var raw = await GetRawAsync ("setup/options");
		return JsonSerializer.Deserialize<List<OptionObject>> (raw, _jsonOptions) ?? [];
		}

	/// <summary>Returns a specific option subscription by option ID.</summary>
	/// <param name="option">Option ID to look up (e.g. <c>"ADVANCED_SCENARIOS"</c>).</param>
	/// <returns>The <see cref="OptionObject"/>, or <see langword="null"/> if the option is not subscribed.</returns>
	public async Task<OptionObject?> GetSetupOption (string option)
		{
		await RefreshTokenIfExpired ();
		var raw = await GetRawAsync ($"setup/options/{option}");
		return string.IsNullOrWhiteSpace (raw) ? null : JsonSerializer.Deserialize<OptionObject> (raw, _jsonOptions);
		}

	/// <summary>Returns a single configuration parameter of a setup option.</summary>
	/// <param name="option">Option ID (e.g. <c>"ADVANCED_SCENARIOS"</c>).</param>
	/// <param name="parameter">Parameter name to retrieve.</param>
	/// <returns>The <see cref="OptionParameter"/>, or <see langword="null"/> if not found.</returns>
	public async Task<OptionParameter?> GetSetupOptionParameter (string option, string parameter)
		{
		await RefreshTokenIfExpired ();
		var raw = await GetRawAsync ($"setup/options/{option}/{parameter}");
		return string.IsNullOrWhiteSpace (raw) ? null : JsonSerializer.Deserialize<OptionParameter> (raw, _jsonOptions);
		}

	// ── HTTP helpers ───────────────────────────────────────────────────────

	private void ApplyRequestHeaders (bool includeGatewayHeader = true)
		{
		_http.DefaultRequestHeaders.Authorization = _accessToken is not null
			? new AuthenticationHeaderValue ("Bearer", _accessToken)
			: null;

		_ = _http.DefaultRequestHeaders.Remove (OverkizConst.REXEL_GATEWAY_HEADER);
		if (includeGatewayHeader && Server.RequiresGatewaySelection)
			{
			if (string.IsNullOrWhiteSpace (SelectedGatewayId))
				throw new NoGatewaySelectedException ("Multiple Rexel gateways available; call DiscoverRexelGateways and SelectRexelGateway before making requests.");

			_http.DefaultRequestHeaders.Add (OverkizConst.REXEL_GATEWAY_HEADER, SelectedGatewayId);
			}
		}

	private async Task<string> GetRawAsync (string path)
		{
		await RefreshTokenIfExpired ();
		ApplyRequestHeaders ();
		using HttpResponseMessage resp = await _http.GetAsync (path);
		var body = await resp.Content.ReadAsStringAsync ();
		await ThrowIfOverkizError (resp, body);
		return body;
		}

	private async Task<string> GetAbsoluteRawAsync (string absoluteUri, bool includeGatewayHeader)
		{
		await RefreshTokenIfExpired ();
		ApplyRequestHeaders (includeGatewayHeader);
		using HttpResponseMessage resp = await _http.GetAsync (new Uri (absoluteUri));
		var body = await resp.Content.ReadAsStringAsync ();
		await ThrowIfOverkizError (resp, body);
		return body;
		}

	private async Task<Dictionary<string, object?>> GetAsync (string path)
		{
		var raw = await GetRawAsync (path);
		return JsonSerializer.Deserialize<Dictionary<string, object?>> (raw, _jsonOptions) ?? [];
		}

	private async Task<string> PostRawAsync (string path, object? payload = null)
		{
		await RefreshTokenIfExpired ();
		ApplyRequestHeaders ();
		HttpContent content = payload is null
			? new ByteArrayContent ([])
			: (HttpContent)JsonContent.Create (payload, options: _jsonOptions);
		using HttpResponseMessage resp = await _http.PostAsync (path, content);
		var body = await resp.Content.ReadAsStringAsync ();
		await ThrowIfOverkizError (resp, body);
		return body;
		}

	private async Task<Dictionary<string, object?>> PostAsync (string path, object? payload = null)
		{
		var raw = await PostRawAsync (path, payload);
		return string.IsNullOrWhiteSpace (raw)
			? []
			: JsonSerializer.Deserialize<Dictionary<string, object?>> (raw, _jsonOptions) ?? [];
		}

	private async Task DeleteAsync (string path)
		{
		await RefreshTokenIfExpired ();
		ApplyRequestHeaders ();
		using HttpResponseMessage resp = await _http.DeleteAsync (path);
		var body = await resp.Content.ReadAsStringAsync ();
		await ThrowIfOverkizError (resp, body);
		}

	private async Task RefreshTokenIfExpired ()
		{
		if (_expiresAt is not null && _refreshToken is not null && _expiresAt <= DateTime.Now)
			{
			await RefreshToken ();
			if (EventListenerId is not null)
				await RegisterEventListener ();
			}
		}

	// ── Error mapping ──────────────────────────────────────────────────────

	private static Task ThrowIfOverkizError (HttpResponseMessage response, string body)
		{
		if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
			{
			if (body.Contains ("maintenance", StringComparison.OrdinalIgnoreCase))
				throw new MaintenanceException ("Service under maintenance.");
			throw new ServiceUnavailableException ("Service unavailable.");
			}

		if ((int)response.StatusCode == 429)
			throw new TooManyRequestsException ("Too many requests.");

		if (response.IsSuccessStatusCode)
			return Task.CompletedTask;

		Dictionary<string, JsonElement>? result = null;
		try
			{
			result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>> (body, _jsonOptions);
			}
		catch (JsonException) { }

		if (result is null)
			{
			_ = response.EnsureSuccessStatusCode ();
			return Task.CompletedTask;
			}

		var message = result.TryGetValue ("error", out JsonElement e) ? e.GetString () ?? string.Empty : string.Empty;

		if (message == "Bad credentials.")
			throw new BadCredentialsException (message);
		if (message == "Your account has been temporarily locked.")
			throw new TooManyAttemptsBannedException (message);
		if (message == "Not authenticated")
			throw new NotAuthenticatedException (message);
		if (message == "An API key is required to access this setup")
			throw new MissingAPIKeyException (message);
		if (message == "Missing authorization token")
			throw new MissingAuthorizationTokenException (message);
		if (message == "Server busy, please try again later. (Too many executions)")
			throw new TooManyExecutionsException (message);
		if (message.Contains ("No such command", StringComparison.Ordinal))
			throw new InvalidCommandException (message);
		if (message.Contains ("Invalid event listener id", StringComparison.Ordinal))
			throw new InvalidEventListenerIdException (message);
		if (message == "No registered event listener")
			throw new NoRegisteredEventListenerException (message);
		if (message.Contains ("No such user account", StringComparison.Ordinal))
			throw new UnknownUserException (message);
		if (message == "No such resource")
			throw new NoSuchResourceException (message);
		if (message == "too many concurrent requests")
			throw new TooManyConcurrentRequestsException (message);
		if (message.Contains ("Execution queue is full on gateway", StringComparison.Ordinal))
			throw new ExecutionQueueFullException (message);
		if (message == "Cannot use JSESSIONID and bearer token in same request")
			throw new SessionAndBearerInSameRequestException (message);
		if (message == "Too many attempts with an invalid token, temporarily banned")
			throw new TooManyAttemptsBannedException (message);
		if (message.Contains ("Invalid token : ", StringComparison.Ordinal))
			throw new InvalidTokenException (message);
		if (message.Contains ("Not such token with UUID: ", StringComparison.Ordinal))
			throw new NotSuchTokenException (message);
		if (message.Contains ("Unknown user :", StringComparison.Ordinal))
			throw new UnknownUserException (message);
		if (message == "Unknown object")
			throw new UnknownObjectException (message);
		if (message.Contains ("Access denied to gateway", StringComparison.Ordinal))
			throw new AccessDeniedToGatewayException (message);
		if (message == "Your setup cannot be accessed through this application")
			throw new ApplicationNotAllowedException (message);
		if (message.Contains ("not supported", StringComparison.OrdinalIgnoreCase))
			throw new UnsupportedOperationException (message);
		if (message.Contains ("Another action already exists for device", StringComparison.Ordinal))
			throw new DuplicateActionOnDeviceException (message);
		if (message.Contains ("No action group setup found", StringComparison.Ordinal))
			throw new ActionGroupSetupNotFoundException (message);

		throw new OverkizException (message.Length > 0 ? message : body);
		}
	}
