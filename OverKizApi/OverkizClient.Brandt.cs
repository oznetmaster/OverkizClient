// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using OverKizApi.Enums;
using OverKizApi.Exceptions;
using OverKizApi.Models;

namespace OverKizApi;

public sealed partial class OverkizClient
	{
	private const string BRANDT_MIDDLEWARE = "https://www.smartcontrol-app.com/api/v1/";

	private async Task<bool> BrandtLoginCore (bool registerEventListener)
		{
		// The same cookie-enabled transport must carry the middleware session into the JWT exchange.
		// A caller-supplied HttpClient must support cookies and be dedicated to this client.
		_http.DefaultRequestHeaders.Authorization = null;
		_ = _http.DefaultRequestHeaders.Remove (OverkizConst.REXEL_GATEWAY_HEADER);
		var payload = new BrandtSessionRequest
			{
			Client = new BrandtCredentials
				{
				Email = Username,
				Password = Password,
				Partner = Server == OverkizConst.SupportedServers[Enums.Server.SauterSmartControl]
					? "sauter-electromenager" : "brandt-electromenager"
				}
			};
		using (HttpResponseMessage login = await _http.PostAsJsonAsync (BRANDT_MIDDLEWARE + "sessions.json", payload, _jsonOptions, _operations.StopToken).ConfigureAwait (false))
			{
			if ((int)login.StatusCode >= 500)
				throw new BrandtServiceException ($"Smart Control session service failed with HTTP {(int)login.StatusCode}.");
			if (!login.IsSuccessStatusCode)
				throw new BrandtBadCredentialsException ($"Smart Control session login was rejected with HTTP {(int)login.StatusCode}.");
			}

		using HttpResponseMessage response = await _http.GetAsync (BRANDT_MIDDLEWARE + "profile/jwt.json", _operations.StopToken).ConfigureAwait (false);
		if (!response.IsSuccessStatusCode)
			throw new BrandtServiceException ($"Smart Control JWT exchange failed with HTTP {(int)response.StatusCode}.");
		BrandtJwtResponse? token;
		try
			{
			token = await response.Content.ReadFromJsonAsync<BrandtJwtResponse> (_jsonOptions).ConfigureAwait (false);
			}
		catch (JsonException)
			{
			throw new BrandtServiceException ("Smart Control JWT response was malformed.");
			}

		if (string.IsNullOrWhiteSpace (token?.Client?.Jwt))
			throw new BrandtServiceException ("Smart Control JWT response contained no token.");
		// Middleware JWT login establishes the cloud cookie session, not a bearer-token session.
		_operations.Commit (() =>
			{
				_accessToken = null;
				_refreshToken = null;
				_expiresAt = null;
			});
		LoginResponse? result = await PostAsync<LoginResponse> ("login", new LoginRequest { Jwt = token!.Client!.Jwt }, refreshToken: false).ConfigureAwait (false);
		if (result?.Success is not true)
			return false;
		InvalidateSessionState ();
		if (registerEventListener)
			await RegisterEventListenerCore ().ConfigureAwait (false);
		return true;
		}

	private sealed class BrandtSessionRequest
		{
		[JsonPropertyName ("client")]
		public BrandtCredentials? Client
			{
			get; init;
			}
		}
	private sealed class BrandtCredentials
		{
		[JsonPropertyName ("email")]
		public string? Email
			{
			get; init;
			}
		[JsonPropertyName ("password")]
		public string? Password
			{
			get; init;
			}
		[JsonPropertyName ("partner")]
		public string? Partner
			{
			get; init;
			}
		}
	private sealed class BrandtJwtResponse
		{
		[JsonPropertyName ("client")]
		public BrandtJwtToken? Client
			{
			get; init;
			}
		}
	private sealed class BrandtJwtToken
		{
		[JsonPropertyName ("jwt")]
		public string? Jwt
			{
			get; init;
			}
		}
	}