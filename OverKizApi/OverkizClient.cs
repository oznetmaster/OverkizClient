// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using OverKizApi.Concurrency;
using OverKizApi.Enums;
using OverKizApi.Exceptions;
using OverKizApi.Models;

namespace OverKizApi;

/// <summary>
/// C# client for the Overkiz API, providing similar capabilities to the Python python-overkiz-api library.
/// Supports cloud (standard, Somfy OAuth, CozyTouch JWT, Nexity SSO) and local API modes.
/// </summary>
/// <remarks>Independent reads may overlap. Authentication, gateway and listener changes are coordinated with reads.
/// Disposal cancels pending work and prevents late publication. Callers must coordinate their own mutations
/// of returned models and dedicate any supplied HttpClient to this client.</remarks>
public sealed partial class OverkizClient : IAsyncDisposable
	{
	// ── Configuration ──────────────────────────────────────────────────────

	/// <summary>Account username or e-mail address used to authenticate with the Overkiz server.</summary>
	/// <value>Account username or e-mail address used to authenticate with the Overkiz server.</value>
	public string Username
		{
		get;
		}

	/// <summary>Account password used to authenticate with the Overkiz server.</summary>
	/// <value>Account password used to authenticate with the Overkiz server.</value>
	public string Password
		{
		get;
		}

	/// <summary>The target server this client is connected to.</summary>
	/// <value>The target server this client is connected to.</value>
	public OverkizServer Server
		{
		get;
		}

	/// <summary>Whether the client is operating against the cloud API or a local gateway API.</summary>
	/// <value>Whether the client is operating against the cloud API or a local gateway API.</value>
	public APIType ApiType
		{
		get;
		}

	// ── Cached state ───────────────────────────────────────────────────────

	/// <summary>
	/// The most recently retrieved <see cref="Models.Setup"/>, or <see langword="null"/> if
	/// <see cref="GetSetup"/> has not yet been called.
	/// </summary>
	/// <value>
	/// The most recently retrieved <see cref="Models.Setup"/>, or <see langword="null"/> if
	/// <see cref="GetSetup"/> has not yet been called.
	/// </value>
	public Setup? Setup
		{
		get => _operations.Observe (() => _setup);
		private set => _setup = value;
		}

	/// <summary>
	/// All devices from the most recently retrieved setup.
	/// Populated by <see cref="GetSetup"/>; empty until that method is called.
	/// </summary>
	/// <value>
	/// All devices from the most recently retrieved setup.
	/// Populated by <see cref="GetSetup"/>; empty until that method is called.
	/// </value>
	public List<Device> Devices
		{
		get => _operations.Observe (() => _devices); private set => _devices = value;
		}

	/// <summary>
	/// All gateways from the most recently retrieved setup.
	/// Populated by <see cref="GetSetup"/>; empty until that method is called.
	/// </summary>
	/// <value>
	/// All gateways from the most recently retrieved setup.
	/// Populated by <see cref="GetSetup"/>; empty until that method is called.
	/// </value>
	public List<Gateway> Gateways
		{
		get => _operations.Observe (() => _gateways); private set => _gateways = value;
		}

	/// <summary>
	/// The event listener ID returned by the last successful <see cref="RegisterEventListener"/> call,
	/// or <see langword="null"/> if no listener is currently registered.
	/// </summary>
	/// <value>
	/// The event listener ID returned by the last successful <see cref="RegisterEventListener"/> call,
	/// or <see langword="null"/> if no listener is currently registered.
	/// </value>
	public string? EventListenerId
		{
		get => _operations.Observe (() => _eventListenerId);
		private set => _operations.Commit (() => _eventListenerId = value);
		}

	/// <summary>The currently selected Rexel gateway ID, or <see langword="null"/> if none has been selected.</summary>
	/// <value>The currently selected Rexel gateway ID, or <see langword="null"/> if none has been selected.</value>
	public string? SelectedGatewayId
		{
		get => _operations.Observe (() => _selectedGatewayId);
		private set => _selectedGatewayId = value;
		}

	// ── Private fields ─────────────────────────────────────────────────────
	private readonly HttpClient _http;
	private readonly bool _ownsHttpClient;
	private string? _accessToken;
	private Setup? _setup;
	private List<Device> _devices = [];
	private List<Gateway> _gateways = [];
	private string? _eventListenerId, _selectedGatewayId;
	private string? _refreshToken;
	private DateTime? _expiresAt;
	private long _tokenGeneration;
	private readonly AsyncClientGate _operations = new (typeof (OverkizClient));
#pragma warning disable IDE0330 // Monitor semantics must match processor Mono.
	private readonly object _disposeSync = new ();
#pragma warning restore IDE0330
	private Task? _disposeTask;

	// ── Local-mode label-change detection ──────────────────────────────────
	// On local connections the gateway never emits DeviceUpdatedEvent for renames.
	// We diff device labels inside FetchEvents and synthesize the event so callers
	// need no special handling.  Cloud connections skip this entirely.
	private readonly Dictionary<string, string> _labelSnapshot
		= new (StringComparer.OrdinalIgnoreCase);
	private DateTime _lastLabelCheck = DateTime.MinValue;
	private static readonly TimeSpan _labelPollInterval = TimeSpan.FromSeconds (30);

	private static readonly JsonSerializerOptions _jsonOptions = new ()
		{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		};

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
		// Configure the dedicated transport before admitting any operations. Request
		// authorization and gateway selection are subsequently set on each message.
		_http.DefaultRequestHeaders.Authorization = null;
		_ = _http.DefaultRequestHeaders.Remove (OverkizConst.REXEL_GATEWAY_HEADER);

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
	/// <returns>A task that completes when listener cleanup and resource disposal finish.</returns>
	public ValueTask DisposeAsync ()
		{
		TaskCompletionSource<bool>? completion = null;
		Task task;
		lock (_disposeSync)
			{
			if (_disposeTask is null)
				{
				completion = new (TaskCreationOptions.RunContinuationsAsynchronously);
				_disposeTask = completion.Task;
				}
			task = _disposeTask;
			}
		if (completion is not null)
			_ = DisposeAsyncCore (completion);
		return new ValueTask (task);
		}

	private async Task DisposeAsyncCore (TaskCompletionSource<bool> completion)
		{
		try
			{
			string? listener = null;
			await _operations.Stop (() =>
				{
					listener = EventListenerId;
					ClearSessionState ();
				}).ConfigureAwait (false);
			if (listener is not null)
				{
				try
					{
					using var request = new HttpRequestMessage (HttpMethod.Post, $"events/{listener}/unregister");
					ApplyRequestHeaders (request);
					using HttpResponseMessage response = await _http.SendAsync (request).ConfigureAwait (false);
					}
				catch { /* Best-effort listener cleanup after admitted work drains. */ }
				}
			if (_ownsHttpClient)
				_http.Dispose ();
			_ = completion.TrySetResult (true);
			}
		catch (Exception error)
			{
			_ = completion.TrySetException (error);
			}
		}

	// ── Authentication ─────────────────────────────────────────────────────

	/// <summary>
	/// Authenticate and open an API session. Must be called before other operations unless a token was supplied.
	/// </summary>
	/// <param name="registerEventListener">Register an event listener after login (default true).</param>
	/// <returns>A task containing true if login succeeds; false if the server rejects the login.</returns>
	public Task<bool> Login (bool registerEventListener = true)
		=> RunWrite (() => LoginCore (registerEventListener), refresh: false);

	private async Task<bool> LoginCore (bool registerEventListener)
		{
		// Local API – no username/password login endpoint
		if (ApiType == APIType.Local)
			{
			if (registerEventListener)
				await RegisterEventListenerCore ().ConfigureAwait (false);
			else
				_ = await GetAsync<List<Gateway>> ("setup/gateways", refreshToken: false).ConfigureAwait (false);   // verify token is valid
			return true;
			}

		// Rexel uses externally-managed bearer tokens and gateway selection.
		if (Server == OverkizConst.SupportedServers[Enums.Server.Rexel])
			{
			if (string.IsNullOrWhiteSpace (_accessToken))
				throw new InvalidOperationException ("Rexel requires an externally managed bearer token. Supply it via the constructor token parameter.");

			IReadOnlyList<GatewayCandidate> gateways = await DiscoverRexelGatewaysCore (refreshToken: false).ConfigureAwait (false);
			if (gateways.Count == 1)
				SelectRexelGatewayCore (gateways[0].GatewayId);

			if (registerEventListener)
				await RegisterEventListenerCore ().ConfigureAwait (false);
			else
				_ = await GetAsync<List<Gateway>> ("setup/gateways", refreshToken: false).ConfigureAwait (false);

			return true;
			}

		// Somfy TaHoma (Europe) uses OAuth
		if (Server == OverkizConst.SupportedServers[Enums.Server.SomfyEurope])
			{
			_ = await SomfyTahomaGetAccessTokenCore ().ConfigureAwait (false);
			InvalidateSessionState ();
			if (registerEventListener)
				await RegisterEventListenerCore ().ConfigureAwait (false);
			return true;
			}

		// CozyTouch servers use a JWT
		if (Server == OverkizConst.SupportedServers[Enums.Server.AtlanticCozytouch] ||
			Server == OverkizConst.SupportedServers[Enums.Server.ThermorCozytouch] ||
			Server == OverkizConst.SupportedServers[Enums.Server.SauterCozytouch])
			{
			var jwt = await CozytouchLoginCore ().ConfigureAwait (false);
			LoginResponse? response = await PostAsync<LoginResponse> ("login", new LoginRequest { Jwt = jwt }, refreshToken: false).ConfigureAwait (false);
			var success = response?.Success is true;
			if (success)
				{
				InvalidateSessionState ();
				if (registerEventListener)
					await RegisterEventListenerCore ().ConfigureAwait (false);
				}
			return success;
			}

		// Nexity uses SSO token
		if (Server == OverkizConst.SupportedServers[Enums.Server.Nexity])
			{
			var ssoToken = await NexityLoginCore ().ConfigureAwait (false);
			var userId = Username.Replace ("@", "_-_");
			var payload = new LoginRequest
				{
				UserId = userId,
				UserPassword = Password,
				SsoToken = ssoToken,
				};
			LoginResponse? response = await PostAsync<LoginResponse> ("login", payload, refreshToken: false).ConfigureAwait (false);
			var success = response?.Success is true;
			if (success)
				{
				InvalidateSessionState ();
				if (registerEventListener)
					await RegisterEventListenerCore ().ConfigureAwait (false);
				}
			return success;
			}

		if (Server == OverkizConst.SupportedServers[Enums.Server.Brandt] ||
			Server == OverkizConst.SupportedServers[Enums.Server.SauterSmartControl])
			return await BrandtLoginCore (registerEventListener).ConfigureAwait (false);

		// Standard username + password
			{
			var payload = new LoginRequest
				{
				UserId = Username,
				UserPassword = Password,
				};
			LoginResponse? response = await PostAsync<LoginResponse> ("login", payload, refreshToken: false).ConfigureAwait (false);
			var success = response?.Success is true;
			if (success)
				{
				InvalidateSessionState ();
				if (registerEventListener)
					await RegisterEventListenerCore ().ConfigureAwait (false);
				}
			return success;
			}
		}

	/// <summary>Authenticate via Somfy identity and acquire an access token.</summary>
	/// <returns>The raw access token string stored in <see cref="_accessToken"/>.</returns>
	/// <exception cref="SomfyBadCredentialsException">Thrown when the supplied credentials are rejected by the Somfy OAuth server.</exception>
	/// <exception cref="SomfyServiceException">Thrown when the Somfy token endpoint returns an unexpected response.</exception>
	public Task<string> SomfyTahomaGetAccessToken ()
		=> RunWrite (SomfyTahomaGetAccessTokenCore, refresh: false);

	private async Task<string> SomfyTahomaGetAccessTokenCore ()
		{
		var form = new FormUrlEncodedContent ([
			new ("grant_type", "password"),
			new ("username", Username),
			new ("password", Password),
			new ("client_id", OverkizConst.SOMFY_CLIENT_ID),
			new ("client_secret", OverkizConst.SOMFY_CLIENT_SECRET),
			]);

		using HttpResponseMessage resp = await _http.PostAsync (
			new Uri (OverkizConst.SOMFY_API + "/oauth/oauth/v2/token/jwt"), form, _operations.StopToken).ConfigureAwait (false);

		SomfyTokenResponse token = await resp.Content.ReadFromJsonAsync<SomfyTokenResponse> (_jsonOptions).ConfigureAwait (false)
			?? throw new SomfyServiceException ("Empty response from Somfy token endpoint.");

		ApplySomfyTokenResponse (token);
		return _accessToken!;
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
	/// <returns>A task that completes when token refresh finishes, or immediately if refresh is not applicable.</returns>
	public Task RefreshToken ()
		{
		long observed = _operations.Observe (() => _tokenGeneration);
		return RunWrite (() => observed == _tokenGeneration ? RefreshTokenCore () : Task.CompletedTask, refresh: false);
		}

	private async Task RefreshTokenCore ()
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
			new Uri (OverkizConst.SOMFY_API + "/oauth/oauth/v2/token/jwt"), form, _operations.StopToken).ConfigureAwait (false);

		SomfyTokenResponse token = await resp.Content.ReadFromJsonAsync<SomfyTokenResponse> (_jsonOptions).ConfigureAwait (false)
			?? throw new SomfyServiceException ("Empty response from Somfy refresh endpoint.");

		ApplySomfyTokenResponse (token);
		}

	private void ApplySomfyTokenResponse (SomfyTokenResponse token)
		{
		if (token.Message == "error.invalid.grant")
			throw new SomfyBadCredentialsException (token.Message);
		if (string.IsNullOrWhiteSpace (token.AccessToken))
			throw new SomfyServiceException ("No Somfy access token provided.");
		if (token.ExpiresIn is null)
			throw new SomfyServiceException ("No Somfy token expiry provided.");

		_operations.Commit (() =>
			{
				_accessToken = token.AccessToken;
				_refreshToken = token.RefreshToken;
				_expiresAt = DateTime.Now.AddSeconds (token.ExpiresIn.Value - 5);
				_tokenGeneration++;
			});
		}

	/// <summary>
	/// Authenticates against the Atlantic/CozyTouch OAuth endpoint and returns
	/// the JWT token required for the subsequent Overkiz <c>login</c> call.
	/// </summary>
	/// <returns>The JWT string to pass to the Overkiz <c>login</c> endpoint.</returns>
	/// <exception cref="CozyTouchBadCredentialsException">Thrown when the Atlantic OAuth server rejects the credentials.</exception>
	/// <exception cref="CozyTouchServiceException">Thrown when the Atlantic token or JWT endpoint returns an unexpected response.</exception>
	public Task<string> CozytouchLogin () => RunWrite (CozytouchLoginCore, refresh: false);

	private async Task<string> CozytouchLoginCore ()
		{
		// Step 1: get OAuth2 token from Atlantic API
		var form = new FormUrlEncodedContent ([
			new ("grant_type", "password"),
			new ("username", "GA-PRIVATEPERSON/" + Username),
			new ("password", Password),
			]);
		form.Headers.ContentType = new MediaTypeHeaderValue ("application/x-www-form-urlencoded");

		using var tokenRequest = new HttpRequestMessage (HttpMethod.Post, OverkizConst.COZYTOUCH_ATLANTIC_API + "/token")
			{
			Content = form,
			Headers = { Authorization = new AuthenticationHeaderValue ("Basic", OverkizConst.COZYTOUCH_CLIENT_ID) },
			};
		using HttpResponseMessage tokenResp = await _http.SendAsync (tokenRequest, _operations.StopToken).ConfigureAwait (false);

		CozyTouchTokenResponse token = await tokenResp.Content.ReadFromJsonAsync<CozyTouchTokenResponse> (_jsonOptions).ConfigureAwait (false)
			?? throw new CozyTouchServiceException ("Empty response from CozyTouch token endpoint.");

		if (token.Error == "invalid_grant")
			throw new CozyTouchBadCredentialsException (token.ErrorDescription ?? "Invalid grant");

		if (string.IsNullOrWhiteSpace (token.TokenType) || string.IsNullOrWhiteSpace (token.AccessToken))
			throw new CozyTouchServiceException ("No CozyTouch token provided.");

		// Step 2: exchange for JWT
		using var req = new HttpRequestMessage (HttpMethod.Get, OverkizConst.COZYTOUCH_ATLANTIC_API + "/magellan/accounts/jwt");
		req.Headers.Authorization = new AuthenticationHeaderValue ("Bearer", token.AccessToken);
		using HttpResponseMessage jwtResp = await _http.SendAsync (req, _operations.StopToken).ConfigureAwait (false);
		if (!jwtResp.IsSuccessStatusCode)
			throw new CozyTouchServiceException ($"CozyTouch JWT exchange failed with HTTP {(int)jwtResp.StatusCode}.");

		string? jwt;
		try
			{
			if (string.Equals (jwtResp.Content.Headers.ContentType?.MediaType, "text/plain", StringComparison.OrdinalIgnoreCase))
				{
				// Legacy text responses must still be a compact JWT, not an error page or arbitrary text.
				jwt = (await jwtResp.Content.ReadAsStringAsync ().ConfigureAwait (false)).Trim ();
				if (!System.Text.RegularExpressions.Regex.IsMatch (jwt, @"\A[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\z"))
					throw new CozyTouchServiceException ("Invalid plain-text JWT response from CozyTouch.");
				}
			else
				jwt = await jwtResp.Content.ReadFromJsonAsync<string> (_jsonOptions).ConfigureAwait (false);
			}
		catch (JsonException)
			{
			throw new CozyTouchServiceException ("Invalid JSON JWT response from CozyTouch.");
			}

		return string.IsNullOrWhiteSpace (jwt) ? throw new CozyTouchServiceException ("No JWT token provided.") : jwt!;
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
	public Task<string> NexityLogin () => RunWrite (NexityLoginCore, refresh: false);

	private static Task<string> NexityLoginCore () =>
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
	/// <returns>A task that completes when the listener has been registered.</returns>
	public Task RegisterEventListener () => RunWrite (async () =>
		{
			await RefreshTokenIfExpiredCore (replaceListener: false).ConfigureAwait (false);
			await RegisterEventListenerCore ().ConfigureAwait (false);
		}, refresh: false);

	private async Task RegisterEventListenerCore ()
		{
		// The server can replace the listener even if its response is lost.
		EventListenerId = null;
		EventListenerResponse? response = await PostAsync<EventListenerResponse> ("events/register", new EmptyRequest (), refreshToken: false).ConfigureAwait (false);
		_operations.Commit (() => EventListenerId = GetRequiredResponseString (response?.Id, "No event listener ID returned."));
		}

	/// <summary>
	/// Fetches all queued events from the registered event listener and clears the server-side queue.
	/// Call this method repeatedly (polling) to receive device state change notifications.
	/// </summary>
	/// <returns>A list of <see cref="EventObject"/> objects; may be empty if no events are pending.</returns>
	/// <exception cref="NoRegisteredEventListenerException">Thrown when no event listener is registered. Call <see cref="RegisterEventListener"/> first.</exception>
	public Task<IReadOnlyList<EventObject>> FetchEvents () => FetchEvents (autoRegister: false);

	/// <summary>Fetches queued events, optionally registering or recovering the listener automatically.</summary>
	/// <param name="autoRegister">When true, registers a missing listener and allows one recovery after an explicit listener or session rejection.</param>
	/// <returns>The queued events; may be empty.</returns>
	/// <remarks>
	/// Authentication refresh completes before the listener URL is constructed. Recovery is bounded to one
	/// replacement per fetch. Timeouts and other transport failures are not retried because fetching consumes events.
	/// Reauthentication requires cloud credentials; rejected local or externally managed tokens are not retried.
	/// </remarks>
	/// <exception cref="NoRegisteredEventListenerException">No listener is registered and automatic registration is disabled, or the server rejects recovery.</exception>
	public Task<IReadOnlyList<EventObject>> FetchEvents (bool autoRegister) => RunWrite (() => FetchEventsCore (autoRegister), refresh: false);

	private async Task<IReadOnlyList<EventObject>> FetchEventsCore (bool autoRegister)
		{
		var response = await FetchEventResponseCore (autoRegister).ConfigureAwait (false);
		List<EventObject> events = JsonSerializer.Deserialize<List<EventObject>> (response, _jsonOptions) ?? [];

		// Local-only: synthesize DeviceUpdatedEvent for renames not signalled by the gateway.
		if (ApiType == APIType.Local && DateTime.UtcNow - _lastLabelCheck >= _labelPollInterval)
			{
			_operations.Commit (() => _lastLabelCheck = DateTime.UtcNow);
			try
				{
				IReadOnlyList<Device> devices = await GetDevicesCore ().ConfigureAwait (false);
				_operations.Commit (() =>
					{
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
											Name = "DeviceUpdatedEvent",
											DeviceUrl = d.DeviceUrl,
											Label = newLabel,
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
					});
				}
			catch
				{
				// Label poll is best-effort; don't surface errors to the caller.
				}
			}

		return events;
		}

	private async Task<string> FetchEventResponseCore (bool autoRegister)
		{
		if (EventListenerId is null && !autoRegister)
			throw new NoRegisteredEventListenerException ("No event listener registered. Call RegisterEventListener first.");

		await RefreshTokenIfExpiredCore (replaceListener: true).ConfigureAwait (false);
		for (var attempt = 0; ; attempt++)
			{
			var fetching = false;
			try
				{
				if (EventListenerId is null)
					await RegisterEventListenerCore ().ConfigureAwait (false);
				fetching = true;
				return await PostRawAsync ($"events/{EventListenerId}/fetch", refreshToken: false).ConfigureAwait (false);
				}
			catch (BaseOverkizException error) when ((fetching && IsListenerRejection (error)) || error is NotAuthenticatedException)
				{
				EventListenerId = null;
				if (!autoRegister || attempt != 0)
					throw;
				if (error is NotAuthenticatedException)
					{
					if (ApiType == APIType.Local || Server == OverkizConst.SupportedServers[Enums.Server.Rexel]
						|| string.IsNullOrWhiteSpace (Username) || string.IsNullOrWhiteSpace (Password))
						throw;
					if (!await LoginCore (registerEventListener: false).ConfigureAwait (false))
						throw;
					}
				}
			}
		}

	private static bool IsListenerRejection (BaseOverkizException error)
		=> error is InvalidEventListenerIdException or NoRegisteredEventListenerException;

	/// <summary>
	/// Unregisters the event listener and clears <see cref="EventListenerId"/>.
	/// Safe to call when no listener is registered (no-op).
	/// </summary>
	/// <returns>A task that completes when listener cleanup finishes.</returns>
	public Task UnregisterEventListener () => RunWrite (async () =>
		{
			var listenerId = EventListenerId;
			if (listenerId is null)
				return;
			try
				{
				await RefreshTokenIfExpiredCore (replaceListener: false).ConfigureAwait (false);
				_ = await PostRawAsync ($"events/{listenerId}/unregister", refreshToken: false).ConfigureAwait (false);
				}
			finally
				{
				EventListenerId = null;
				}
		}, refresh: false);

	// ── Setup ──────────────────────────────────────────────────────────────

	// ── Setup ──────────────────────────────────────────────────────────────

	/// <summary>
	/// Retrieves the complete home setup including all gateways, devices, zones, places and location.
	/// The result is cached; pass <paramref name="refresh"/> as <see langword="true"/> to force a fresh API call.
	/// </summary>
	/// <param name="refresh">When <see langword="true"/> the cached result is discarded and a new API call is made.</param>
	/// <returns>The <see cref="Models.Setup"/> for the authenticated account.</returns>
	/// <exception cref="OverkizException">Thrown when the response cannot be deserialised.</exception>
	public Task<Setup> GetSetup (bool refresh = false) => RunWrite (() => GetSetupCore (refresh), refresh: true);

	private async Task<Setup> GetSetupCore (bool refresh)
		{
		if (Setup is not null && !refresh)
			return Setup;

		await RefreshTokenIfExpired ().ConfigureAwait (false);
		var raw = await GetRawAsync ("setup").ConfigureAwait (false);
		Setup setup = JsonSerializer.Deserialize<Setup> (raw, _jsonOptions)
			?? throw new OverkizException ("Failed to deserialize Setup.");

		_operations.Commit (() =>
			{
				Setup = setup;
				Gateways = [.. setup.Gateways];
				Devices = [.. setup.Devices];
			});
		return setup;
		}

	/// <summary>Returns all gateways registered in the setup.</summary>
	/// <returns>A read-only list of <see cref="Gateway"/> objects.</returns>
	public Task<IReadOnlyList<Gateway>> GetGateways () => RunRead (GetGatewaysCore);

	private async Task<IReadOnlyList<Gateway>> GetGatewaysCore ()
		{
		await RefreshTokenIfExpired ().ConfigureAwait (false);
		var raw = await GetRawAsync ("setup/gateways").ConfigureAwait (false);
		return JsonSerializer.Deserialize<List<Gateway>> (raw, _jsonOptions) ?? [];
		}

	/// <summary>Discovers Rexel homes and gateways available to the current bearer token.</summary>
	/// <returns>A read-only list of gateway candidates.</returns>
	/// <exception cref="UnsupportedOperationException">Thrown when the current server is not Rexel.</exception>
	/// <exception cref="InvalidOperationException">Thrown when no bearer token is available.</exception>
	public Task<IReadOnlyList<GatewayCandidate>> DiscoverRexelGateways () => RunRead (() => DiscoverRexelGatewaysCore (refreshToken: true));

	private async Task<IReadOnlyList<GatewayCandidate>> DiscoverRexelGatewaysCore (bool refreshToken)
		{
		if (Server != OverkizConst.SupportedServers[Enums.Server.Rexel])
			throw new UnsupportedOperationException ("Gateway discovery is only available for the Rexel server.");
		if (string.IsNullOrWhiteSpace (_accessToken))
			throw new InvalidOperationException ("Rexel gateway discovery requires a bearer token.");

		var homesJson = await GetAbsoluteRawAsync ($"{OverkizConst.REXEL_ENDUSER_API}/homes", includeGatewayHeader: false, refreshToken: refreshToken).ConfigureAwait (false);
		List<RexelHomeDirectoryEntry> homes = JsonSerializer.Deserialize<List<RexelHomeDirectoryEntry>> (homesJson, _jsonOptions) ?? [];
		var candidates = new List<GatewayCandidate> ();

		foreach (RexelHomeDirectoryEntry home in homes)
			{
			string gatewaysJson = await GetAbsoluteRawAsync ($"{OverkizConst.REXEL_ENDUSER_API}/overkizgateways?homeId={Uri.EscapeDataString (home.Id)}", includeGatewayHeader: false, refreshToken: refreshToken).ConfigureAwait (false);
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
		using AsyncClientGate.Operation operation = _operations.EnterAsync (exclusive: true).ConfigureAwait (false).GetAwaiter ().GetResult ();
		SelectRexelGatewayCore (gatewayId);
		_operations.CheckRunning ();
		}

	private void SelectRexelGatewayCore (string gatewayId)
		{
		if (Server != OverkizConst.SupportedServers[Enums.Server.Rexel])
			throw new UnsupportedOperationException ("Gateway selection is only available for the Rexel server.");

		if (string.IsNullOrWhiteSpace (gatewayId))
			throw new ArgumentException ("Gateway ID is required.", nameof (gatewayId));
		if (SelectedGatewayId == gatewayId)
			return;
		_operations.Commit (() =>
			{
				SelectedGatewayId = gatewayId;
				ClearSessionState ();
			});
		}

	/// <summary>Returns all devices registered across all gateways in the setup.</summary>
	/// <returns>A read-only list of <see cref="Device"/> objects.</returns>
	public Task<IReadOnlyList<Device>> GetDevices () => RunRead (GetDevicesCore);

	private async Task<IReadOnlyList<Device>> GetDevicesCore ()
		{
		await RefreshTokenIfExpired ().ConfigureAwait (false);
		var raw = await GetRawAsync ("setup/devices").ConfigureAwait (false);
		return JsonSerializer.Deserialize<List<Device>> (raw, _jsonOptions) ?? [];
		}

	/// <summary>Returns a single device identified by its URL.</summary>
	/// <param name="deviceUrl">The device URL (e.g. <c>io://1234-5678-9012/12345678</c>).</param>
	/// <returns>The matching <see cref="Device"/>.</returns>
	/// <exception cref="OverkizException">Thrown when the response cannot be deserialised.</exception>
	public Task<Device> GetDevice (string deviceUrl) => RunRead (() => GetDeviceCore (deviceUrl));

	private async Task<Device> GetDeviceCore (string deviceUrl)
		{
		await RefreshTokenIfExpired ().ConfigureAwait (false);
		var encoded = Uri.EscapeDataString (deviceUrl);
		var raw = await GetRawAsync ($"setup/devices/{encoded}").ConfigureAwait (false);
		return JsonSerializer.Deserialize<Device> (raw, _jsonOptions)
			?? throw new OverkizException ($"Failed to deserialize Device for {deviceUrl}.");
		}

	/// <summary>Returns the current live states for a device.</summary>
	/// <param name="deviceUrl">The device URL to query.</param>
	/// <returns>A read-only list of <see cref="State"/> objects.</returns>
	public Task<IReadOnlyList<State>> GetDeviceStates (string deviceUrl) => RunRead (() => GetDeviceStatesCore (deviceUrl));

	private async Task<IReadOnlyList<State>> GetDeviceStatesCore (string deviceUrl)
		{
		await RefreshTokenIfExpired ().ConfigureAwait (false);
		var encoded = Uri.EscapeDataString (deviceUrl);
		DeviceStatesPayload? response = await GetAsync<DeviceStatesPayload> ($"setup/devices/{encoded}/states").ConfigureAwait (false);
		return response?.States ?? [];
		}

	/// <summary>
	/// Asks the gateway to refresh all device states from the physical devices.
	/// Updated states will be pushed through the event listener.
	/// </summary>
	/// <returns>A task that completes when the refresh request has been accepted; device state updates arrive separately.</returns>
	public Task RefreshAllDeviceStates () => RunWrite (RefreshAllDeviceStatesCore, refresh: true);

	private async Task RefreshAllDeviceStatesCore ()
		{
		await RefreshTokenIfExpired ().ConfigureAwait (false);
		_ = await PostRawAsync ("setup/devices/states/refresh").ConfigureAwait (false);
		}

	// ── Execution ──────────────────────────────────────────────────────────

	/// <summary>Sends a set of commands to a device as a single execution action.</summary>
	/// <param name="deviceUrl">URL of the target device.</param>
	/// <param name="commands">One or more <see cref="Command"/> objects to execute.</param>
	/// <param name="label">Optional human-readable label recorded in the execution history.</param>
	/// <returns>The execution ID (<c>execId</c>) assigned by the server.</returns>
	/// <exception cref="OverkizException">Thrown when the server does not return an execution ID.</exception>
	public Task<string> ExecuteDeviceAction (string deviceUrl, IEnumerable<Command> commands, string label = "Execute")
		=> RunWrite (() => ExecuteDeviceActionCore (deviceUrl, commands, label), refresh: true);

	private async Task<string> ExecuteDeviceActionCore (string deviceUrl, IEnumerable<Command> commands, string label)
		{
		await RefreshTokenIfExpired ().ConfigureAwait (false);
		var payload = new DeviceActionRequest
			{
			Label = label,
			Actions = [new Models.Action { DeviceUrl = deviceUrl, Commands = commands.ToList () }],
			};
		ExecutionResponse? response = await PostAsync<ExecutionResponse> ("exec/apply", payload).ConfigureAwait (false);
		return GetRequiredResponseString (response?.ExecId, "No execId returned.");
		}

	/// <summary>Cancels a running or queued execution.</summary>
	/// <param name="execId">The execution ID returned by <see cref="ExecuteDeviceAction"/> or <see cref="ExecuteScenario"/>.</param>
	/// <returns>A task that completes when the cancellation request finishes.</returns>
	public Task CancelExecution (string execId) => RunWrite (() => CancelExecutionCore (execId), refresh: true);

	private async Task CancelExecutionCore (string execId)
		{
		await RefreshTokenIfExpired ().ConfigureAwait (false);
		await DeleteAsync ($"exec/current/setup/{execId}").ConfigureAwait (false);
		}

	/// <summary>Returns all executions that are currently active (in-progress or queued) on the gateway.</summary>
	/// <returns>A read-only list of <see cref="Execution"/> objects; empty if nothing is running.</returns>
	public Task<IReadOnlyList<Execution>> GetCurrentExecutions () => RunRead (GetCurrentExecutionsCore);

	private async Task<IReadOnlyList<Execution>> GetCurrentExecutionsCore ()
		{
		await RefreshTokenIfExpired ().ConfigureAwait (false);
		var raw = await GetRawAsync ("exec/current").ConfigureAwait (false);
		return JsonSerializer.Deserialize<List<Execution>> (raw, _jsonOptions) ?? [];
		}

	/// <summary>Returns the execution history log stored on the gateway.</summary>
	/// <returns>A read-only list of <see cref="HistoryExecution"/> records, most-recent first.</returns>
	public Task<IReadOnlyList<HistoryExecution>> GetExecutionHistory () => RunRead (GetExecutionHistoryCore);

	private async Task<IReadOnlyList<HistoryExecution>> GetExecutionHistoryCore ()
		{
		await RefreshTokenIfExpired ().ConfigureAwait (false);
		var raw = await GetRawAsync ("history/executions").ConfigureAwait (false);
		return JsonSerializer.Deserialize<List<HistoryExecution>> (raw, _jsonOptions) ?? [];
		}

	// ── Scenarios ──────────────────────────────────────────────────────────

	/// <summary>Returns all named scenarios (action groups) stored in the setup.</summary>
	/// <returns>A read-only list of <see cref="Scenario"/> objects.</returns>
	public Task<IReadOnlyList<Scenario>> GetScenarios () => RunRead (GetScenariosCore);

	private async Task<IReadOnlyList<Scenario>> GetScenariosCore ()
		{
		await RefreshTokenIfExpired ().ConfigureAwait (false);
		var raw = await GetRawAsync ("actionGroups").ConfigureAwait (false);
		return JsonSerializer.Deserialize<List<Scenario>> (raw, _jsonOptions) ?? [];
		}

	/// <summary>Triggers immediate execution of a stored scenario.</summary>
	/// <param name="oid">The OID of the scenario to execute (see <see cref="Scenario.Oid"/>).</param>
	/// <returns>The execution ID assigned by the server.</returns>
	/// <exception cref="OverkizException">Thrown when the server does not return an execution ID.</exception>
	public Task<string> ExecuteScenario (string oid) => RunWrite (() => ExecuteScenarioCore (oid), refresh: true);

	private async Task<string> ExecuteScenarioCore (string oid)
		{
		await RefreshTokenIfExpired ().ConfigureAwait (false);
		ExecutionResponse? response = await PostAsync<ExecutionResponse> ($"exec/{oid}", new EmptyRequest ()).ConfigureAwait (false);
		return GetRequiredResponseString (response?.ExecId, "No execId returned.");
		}

	/// <summary>Schedules a scenario to execute at a specific point in time.</summary>
	/// <param name="oid">The OID of the scenario to schedule.</param>
	/// <param name="timestamp">Unix epoch millisecond timestamp at which to trigger the scenario.</param>
	/// <returns>The trigger ID assigned by the server.</returns>
	/// <exception cref="OverkizException">Thrown when the server does not return a trigger ID.</exception>
	public Task<string> ExecuteScheduledScenario (string oid, long timestamp) => RunWrite (() => ExecuteScheduledScenarioCore (oid, timestamp), refresh: true);

	private async Task<string> ExecuteScheduledScenarioCore (string oid, long timestamp)
		{
		await RefreshTokenIfExpired ().ConfigureAwait (false);
		ScheduledExecutionResponse? response = await PostAsync<ScheduledExecutionResponse> ($"exec/schedule/{oid}/{timestamp}", new EmptyRequest ()).ConfigureAwait (false);
		return GetRequiredResponseString (response?.TriggerId, "No triggerId returned.");
		}

	// ── Places ─────────────────────────────────────────────────────────────

	/// <summary>Returns the root of the place hierarchy (house → floors → rooms).</summary>
	/// <returns>The root <see cref="Place"/> containing all sub-places.</returns>
	/// <exception cref="OverkizException">Thrown when the response cannot be deserialised.</exception>
	public Task<Place> GetPlaces () => RunRead (GetPlacesCore);

	private async Task<Place> GetPlacesCore ()
		{
		await RefreshTokenIfExpired ().ConfigureAwait (false);
		var raw = await GetRawAsync ("setup/places").ConfigureAwait (false);
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
	public Task<string> GenerateLocalToken (string gatewayId) => RunWrite (() => GenerateLocalTokenCore (gatewayId), refresh: true);

	private async Task<string> GenerateLocalTokenCore (string gatewayId)
		{
		await RefreshTokenIfExpired ().ConfigureAwait (false);
		string encodedGatewayId = Uri.EscapeDataString (gatewayId);
		LocalTokenGenerationResponse? response = await GetAsync<LocalTokenGenerationResponse> ($"config/{encodedGatewayId}/local/tokens/generate").ConfigureAwait (false);
		return GetRequiredResponseString (response?.Token, "No token returned.");
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
	public Task<string> ActivateLocalToken (string gatewayId, string token, string label, string scope = "devmode")
		=> RunWrite (() => ActivateLocalTokenCore (gatewayId, token, label, scope), refresh: true);

	private async Task<string> ActivateLocalTokenCore (string gatewayId, string token, string label, string scope)
		{
		await RefreshTokenIfExpired ().ConfigureAwait (false);
		string encodedGatewayId = Uri.EscapeDataString (gatewayId);
		LocalTokenActivationResponse? response = await PostAsync<LocalTokenActivationResponse> (
			$"config/{encodedGatewayId}/local/tokens",
			new LocalTokenActivationRequest
				{
				Label = label,
				Token = token,
				Scope = scope
				}).ConfigureAwait (false);
		return GetRequiredResponseString (response?.RequestId, "No requestId returned.");
		}

	/// <summary>Returns all active local API tokens for a gateway filtered by scope.</summary>
	/// <param name="gatewayId">Serial number of the gateway to query. For Rexel cloud endpoints, use <see cref="GatewayCandidate.ExternalId"/>.</param>
	/// <param name="scope">Token scope to filter by (default <c>"devmode"</c>).</param>
	/// <returns>A read-only list of <see cref="LocalToken"/> descriptors.</returns>
	public Task<IReadOnlyList<LocalToken>> GetLocalTokens (string gatewayId, string scope = "devmode") => RunRead (() => GetLocalTokensCore (gatewayId, scope));

	private async Task<IReadOnlyList<LocalToken>> GetLocalTokensCore (string gatewayId, string scope)
		{
		await RefreshTokenIfExpired ().ConfigureAwait (false);
		string encodedGatewayId = Uri.EscapeDataString (gatewayId);
		string encodedScope = Uri.EscapeDataString (scope);
		var raw = await GetRawAsync ($"config/{encodedGatewayId}/local/tokens/{encodedScope}").ConfigureAwait (false);
		return JsonSerializer.Deserialize<List<LocalToken>> (raw, _jsonOptions) ?? [];
		}

	/// <summary>Revokes and deletes a local API token.</summary>
	/// <param name="gatewayId">Serial number of the gateway the token belongs to. For Rexel cloud endpoints, use <see cref="GatewayCandidate.ExternalId"/>.</param>
	/// <param name="uuid">UUID of the token to delete (see <see cref="LocalToken.Uuid"/>).</param>
	/// <returns><see langword="true"/> on success.</returns>
	public Task<bool> DeleteLocalToken (string gatewayId, string uuid) => RunWrite (() => DeleteLocalTokenCore (gatewayId, uuid), refresh: true);

	private async Task<bool> DeleteLocalTokenCore (string gatewayId, string uuid)
		{
		await RefreshTokenIfExpired ().ConfigureAwait (false);
		string encodedGatewayId = Uri.EscapeDataString (gatewayId);
		string encodedUuid = Uri.EscapeDataString (uuid);
		await DeleteAsync ($"config/{encodedGatewayId}/local/tokens/{encodedUuid}").ConfigureAwait (false);
		return true;
		}

	/// <summary>
	/// Opens the local pairing window on a gateway for approximately 180 seconds.
	/// During this period, additional local tokens may be registered directly on the gateway.
	/// </summary>
	/// <param name="gatewayId">Serial number of the gateway. For Rexel cloud endpoints, use <see cref="GatewayCandidate.ExternalId"/>.</param>
	/// <returns>A task that completes when the pairing request succeeds.</returns>
	public Task OpenLocalPairing (string gatewayId) => RunWrite (() => OpenLocalPairingCore (gatewayId), refresh: true);

	private async Task OpenLocalPairingCore (string gatewayId)
		{
		await RefreshTokenIfExpired ().ConfigureAwait (false);
		string encodedGatewayId = Uri.EscapeDataString (gatewayId);
		_ = await PostRawAsync ($"config/{encodedGatewayId}/local/openPairing").ConfigureAwait (false);
		}

	/// <summary>
	/// Activates developer mode for a gateway.
	/// This is required on supported gateways before local developer-mode tokens can be used.
	/// </summary>
	/// <param name="gatewayId">Serial number of the gateway. For Rexel cloud endpoints, use <see cref="GatewayCandidate.ExternalId"/>.</param>
	/// <returns>A task that completes when the activation request finishes.</returns>
	public Task ActivateDeveloperMode (string gatewayId) => RunWrite (() => ActivateDeveloperModeCore (gatewayId), refresh: true);

	private async Task ActivateDeveloperModeCore (string gatewayId)
		{
		await RefreshTokenIfExpired ().ConfigureAwait (false);
		string encodedGatewayId = Uri.EscapeDataString (gatewayId);
		_ = await PostRawAsync ($"setup/gateways/{encodedGatewayId}/developerMode").ConfigureAwait (false);
		}

	/// <summary>Returns the current developer-mode status for a gateway.</summary>
	/// <param name="gatewayId">Serial number of the gateway. For Rexel cloud endpoints, use <see cref="GatewayCandidate.ExternalId"/>.</param>
	/// <returns>A <see cref="DeveloperMode"/> object describing whether developer mode is active.</returns>
	public Task<DeveloperMode> GetDeveloperMode (string gatewayId) => RunRead (() => GetDeveloperModeCore (gatewayId));

	private async Task<DeveloperMode> GetDeveloperModeCore (string gatewayId)
		{
		await RefreshTokenIfExpired ().ConfigureAwait (false);
		string encodedGatewayId = Uri.EscapeDataString (gatewayId);
		string raw = await GetRawAsync ($"setup/gateways/{encodedGatewayId}/developerMode").ConfigureAwait (false);
		return JsonSerializer.Deserialize<DeveloperMode> (raw, _jsonOptions)
			?? throw new OverkizException ($"Failed to deserialize developer mode status for gateway {gatewayId}.");
		}

	/// <summary>
	/// Deactivates developer mode for a gateway.
	/// Some gateways reject this endpoint even when they support reading developer-mode state.
	/// </summary>
	/// <param name="gatewayId">Serial number of the gateway. For Rexel cloud endpoints, use <see cref="GatewayCandidate.ExternalId"/>.</param>
	/// <returns>A task that completes when the deactivation request finishes.</returns>
	public Task DeactivateDeveloperMode (string gatewayId) => RunWrite (() => DeactivateDeveloperModeCore (gatewayId), refresh: true);

	private async Task DeactivateDeveloperModeCore (string gatewayId)
		{
		await RefreshTokenIfExpired ().ConfigureAwait (false);
		string encodedGatewayId = Uri.EscapeDataString (gatewayId);
		await DeleteAsync ($"setup/gateways/{encodedGatewayId}/developerMode").ConfigureAwait (false);
		}

	// ── Setup options ──────────────────────────────────────────────────────

	/// <summary>Returns all option subscriptions active on the authenticated setup.</summary>
	/// <returns>A read-only list of <see cref="OptionObject"/> objects.</returns>
	public Task<IReadOnlyList<OptionObject>> GetSetupOptions () => RunRead (GetSetupOptionsCore);

	private async Task<IReadOnlyList<OptionObject>> GetSetupOptionsCore ()
		{
		await RefreshTokenIfExpired ().ConfigureAwait (false);
		var raw = await GetRawAsync ("setup/options").ConfigureAwait (false);
		return JsonSerializer.Deserialize<List<OptionObject>> (raw, _jsonOptions) ?? [];
		}

	/// <summary>Returns a specific option subscription by option ID.</summary>
	/// <param name="option">Option ID to look up (e.g. <c>"ADVANCED_SCENARIOS"</c>).</param>
	/// <returns>The <see cref="OptionObject"/>, or <see langword="null"/> if the option is not subscribed.</returns>
	public Task<OptionObject?> GetSetupOption (string option) => RunRead (() => GetSetupOptionCore (option));

	private async Task<OptionObject?> GetSetupOptionCore (string option)
		{
		await RefreshTokenIfExpired ().ConfigureAwait (false);
		var raw = await GetRawAsync ($"setup/options/{option}").ConfigureAwait (false);
		return string.IsNullOrWhiteSpace (raw) ? null : JsonSerializer.Deserialize<OptionObject> (raw, _jsonOptions);
		}

	/// <summary>Returns a single configuration parameter of a setup option.</summary>
	/// <param name="option">Option ID (e.g. <c>"ADVANCED_SCENARIOS"</c>).</param>
	/// <param name="parameter">Parameter name to retrieve.</param>
	/// <returns>The <see cref="OptionParameter"/>, or <see langword="null"/> if not found.</returns>
	public Task<OptionParameter?> GetSetupOptionParameter (string option, string parameter) => RunRead (() => GetSetupOptionParameterCore (option, parameter));

	private async Task<OptionParameter?> GetSetupOptionParameterCore (string option, string parameter)
		{
		await RefreshTokenIfExpired ().ConfigureAwait (false);
		var raw = await GetRawAsync ($"setup/options/{option}/{parameter}").ConfigureAwait (false);
		return string.IsNullOrWhiteSpace (raw) ? null : JsonSerializer.Deserialize<OptionParameter> (raw, _jsonOptions);
		}

	// ── HTTP helpers ───────────────────────────────────────────────────────

	private void ApplyRequestHeaders (HttpRequestMessage request, bool includeGatewayHeader = true)
		{
		request.Headers.Authorization = _accessToken is not null
			? new AuthenticationHeaderValue ("Bearer", _accessToken)
			: null;

		if (includeGatewayHeader && Server.RequiresGatewaySelection)
			{
			if (string.IsNullOrWhiteSpace (SelectedGatewayId))
				throw new NoGatewaySelectedException ("Multiple Rexel gateways available; call DiscoverRexelGateways and SelectRexelGateway before making requests.");

			request.Headers.Add (OverkizConst.REXEL_GATEWAY_HEADER, SelectedGatewayId);
			}
		}

	private async Task<string> GetRawAsync (string path, bool refreshToken = true)
		{
		if (refreshToken)
			await RefreshTokenIfExpired ().ConfigureAwait (false);
		using var request = new HttpRequestMessage (HttpMethod.Get, path);
		ApplyRequestHeaders (request);
		using HttpResponseMessage resp = await _http.SendAsync (request, _operations.StopToken).ConfigureAwait (false);
		var body = await resp.Content.ReadAsStringAsync ().ConfigureAwait (false);
		await ThrowIfOverkizError (resp, body).ConfigureAwait (false);
		return body;
		}

	private async Task<string> GetAbsoluteRawAsync (string absoluteUri, bool includeGatewayHeader, bool refreshToken = true)
		{
		if (refreshToken)
			await RefreshTokenIfExpired ().ConfigureAwait (false);
		using var request = new HttpRequestMessage (HttpMethod.Get, new Uri (absoluteUri));
		ApplyRequestHeaders (request, includeGatewayHeader);
		using HttpResponseMessage resp = await _http.SendAsync (request, _operations.StopToken).ConfigureAwait (false);
		var body = await resp.Content.ReadAsStringAsync ().ConfigureAwait (false);
		await ThrowIfOverkizError (resp, body).ConfigureAwait (false);
		return body;
		}

	private static string GetRequiredResponseString (string? value, string errorMessage)
		=> !string.IsNullOrWhiteSpace (value) ? value! : throw new OverkizException (errorMessage);

	private async Task<T?> GetAsync<T> (string path, bool refreshToken = true) where T : class
		{
		var raw = await GetRawAsync (path, refreshToken).ConfigureAwait (false);
		return string.IsNullOrWhiteSpace (raw) ? null : JsonSerializer.Deserialize<T> (raw, _jsonOptions);
		}

	private async Task<string> PostRawAsync (string path, object? payload = null, bool refreshToken = true)
		{
		if (refreshToken)
			await RefreshTokenIfExpired ().ConfigureAwait (false);
		using var request = new HttpRequestMessage (HttpMethod.Post, path);
		ApplyRequestHeaders (request);
		request.Content = payload is null
			? new ByteArrayContent ([])
			: (HttpContent)JsonContent.Create (payload, options: _jsonOptions);
		using HttpResponseMessage resp = await _http.SendAsync (request, _operations.StopToken).ConfigureAwait (false);
		var body = await resp.Content.ReadAsStringAsync ().ConfigureAwait (false);
		await ThrowIfOverkizError (resp, body).ConfigureAwait (false);
		return body;
		}

	private async Task<T?> PostAsync<T> (string path, object? payload = null, bool refreshToken = true) where T : class
		{
		var raw = await PostRawAsync (path, payload, refreshToken).ConfigureAwait (false);
		return string.IsNullOrWhiteSpace (raw) ? null : JsonSerializer.Deserialize<T> (raw, _jsonOptions);
		}

	private async Task DeleteAsync (string path)
		{
		await RefreshTokenIfExpired ().ConfigureAwait (false);
		using var request = new HttpRequestMessage (HttpMethod.Delete, path);
		ApplyRequestHeaders (request);
		using HttpResponseMessage resp = await _http.SendAsync (request, _operations.StopToken).ConfigureAwait (false);
		var body = await resp.Content.ReadAsStringAsync ().ConfigureAwait (false);
		await ThrowIfOverkizError (resp, body).ConfigureAwait (false);
		}

	// Public admission establishes a stable session before entering private operation cores.
	private static Task RefreshTokenIfExpired () => Task.CompletedTask;

	private async Task RefreshTokenIfExpiredCore (bool replaceListener)
		{
		if (_expiresAt is not null && _refreshToken is not null && _expiresAt <= DateTime.Now)
			{
			await RefreshTokenCore ().ConfigureAwait (false);
			if (replaceListener && EventListenerId is not null)
				await RegisterEventListenerCore ().ConfigureAwait (false);
			}
		}

	private void InvalidateSessionState () => _operations.Commit (ClearSessionState);

	private void ClearSessionState ()
		{
		_eventListenerId = null;
		Setup = null;
		Devices = [];
		Gateways = [];
		_labelSnapshot.Clear ();
		_lastLabelCheck = DateTime.MinValue;
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

		ApiErrorResponse? result = null;
		try
			{
			result = JsonSerializer.Deserialize<ApiErrorResponse> (body, _jsonOptions);
			}
		catch (JsonException) { }

		if (result is null)
			{
			_ = response.EnsureSuccessStatusCode ();
			return Task.CompletedTask;
			}

		var message = result.Error ?? string.Empty;

		if (message == "Too many requests, try again later")
			throw new TooManyRequestsException (message);
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
		if (message == "No registered event listener" || message == "\"No registered event listener.\"")
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