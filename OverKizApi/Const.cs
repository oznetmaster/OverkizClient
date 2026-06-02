// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// Portions derived from python-overkiz-api © 2020 Mick Vleeshouwer — MIT License.

using System.Net.Http;
using OverKizApi.Enums;
using OverKizApi.Models;

namespace OverKizApi;

/// <summary>
/// Compile-time constants and server-endpoint descriptors used throughout the library.
/// </summary>
/// <remarks>
/// OAuth client IDs and secrets for Somfy and CozyTouch are intentionally public —
/// they are embedded in the official mobile apps and are required for the standard
/// OAuth 2.0 Resource Owner Password Credentials flow.
/// </remarks>
public static class OverkizConst
	{
	// --- CozyTouch (Atlantic) ---

	/// <summary>Base URL of the Atlantic/CozyTouch identity API.</summary>
	public const string COZYTOUCH_ATLANTIC_API = "https://apis.groupe-atlantic.com";

	/// <summary>
	/// Base64-encoded <c>clientId:clientSecret</c> credential used as the HTTP Basic
	/// Authorization header when exchanging credentials for an Atlantic OAuth token.
	/// </summary>
	public const string COZYTOUCH_CLIENT_ID = "Q3RfMUpWeVRtSUxYOEllZkE3YVVOQmpGblpVYToyRWNORHpfZHkzNDJVSnFvMlo3cFNKTnZVdjBh";

	// --- Nexity ---

	/// <summary>Base URL of the Nexity cloud API.</summary>
	public const string NEXITY_API = "https://api.egn.prd.aws-nexity.fr";

	/// <summary>AWS Cognito app client ID for the Nexity user pool.</summary>
	public const string NEXITY_COGNITO_CLIENT_ID = "3mca95jd5ase5lfde65rerovok";

	/// <summary>AWS Cognito user pool ID used for Nexity SRP authentication.</summary>
	public const string NEXITY_COGNITO_USER_POOL = "eu-west-1_wj277ucoI";

	/// <summary>AWS region that hosts the Nexity Cognito user pool.</summary>
	public const string NEXITY_COGNITO_REGION = "eu-west-1";

	// --- Somfy ---

	/// <summary>Base URL of the Somfy identity / OAuth 2.0 server.</summary>
	public const string SOMFY_API = "https://accounts.somfy.com";

	/// <summary>
	/// Somfy OAuth 2.0 client ID embedded in the official Somfy TaHoma mobile app.
	/// This value is public by design.
	/// </summary>
	public const string SOMFY_CLIENT_ID = "0d8e920c-1478-11e7-a377-02dd59bd3041_1ewvaqmclfogo4kcsoo0c8k4kso884owg08sg8c40sk4go4ksg";

	/// <summary>
	/// Somfy OAuth 2.0 client secret embedded in the official Somfy TaHoma mobile app.
	/// This value is public by design (required for ROPC flow).
	/// </summary>
	public const string SOMFY_CLIENT_SECRET = "12k73w1n540g8o4cokg0cw84cog840k84cwggscwg884004kgk";

	// --- Rexel ---

	/// <summary>Base URL of the Rexel end-user directory API used to enumerate homes and gateways.</summary>
	public const string REXEL_ENDUSER_API = "https://econnect-api.rexelservices.fr/api/enduser";

	/// <summary>Base URL of the Rexel Overkiz-proxy backend used for gateway-scoped device requests.</summary>
	public const string REXEL_BACKEND_API = REXEL_ENDUSER_API + "/overkiz/";

	/// <summary>Name of the HTTP header required by Rexel to scope requests to a selected gateway.</summary>
	public const string REXEL_GATEWAY_HEADER = "gatewayId";

	// --- Local API ---

	/// <summary>
	/// HTTPS port used by the Overkiz local developer-mode API.
	/// </summary>
	public const int LOCAL_API_PORT = 8443;

	/// <summary>
	/// HTTP path prefix appended to the gateway's local IP address to reach the
	/// developer-mode local API (e.g. <c>https://&lt;gateway-ip&gt;:8443/enduser-mobile-web/1/enduserAPI/</c>).
	/// </summary>
	public const string LOCAL_API_PATH = "/enduser-mobile-web/1/enduserAPI/";

	/// <summary>
	/// Builds the full local API endpoint URL for the given gateway IP or hostname.
	/// </summary>
	public static string LocalEndpoint (string gatewayIp)
		=> $"https://{gatewayIp}:{LOCAL_API_PORT}{LOCAL_API_PATH}";

	/// <summary>
	/// Creates an <see cref="OverkizServer"/> descriptor for a local gateway connection.
	/// </summary>
	public static OverkizServer LocalServer (string gatewayIp)
		=> new ()
			{ Name = $"Local gateway ({gatewayIp})", Endpoint = LocalEndpoint (gatewayIp), Manufacturer = "Local" };

	/// <summary>
	/// Creates an <see cref="HttpClientHandler"/> suitable for connecting to a local Overkiz gateway.
	/// The handler bypasses TLS certificate validation because local gateways use a self-signed certificate.
	/// The caller is responsible for disposing both the handler and the <see cref="HttpClient"/> built from it.
	/// </summary>
	public static HttpClientHandler CreateLocalHttpClientHandler ()
		=> new ()
			{ ServerCertificateCustomValidationCallback = (_, _, _, _) => true };

	/// <summary>
	/// The subset of <see cref="Server"/> values whose gateways support the local developer-mode API.
	/// When the selected server is in this list, a local bearer token can be used instead of
	/// cloud credentials to communicate directly with the gateway on the LAN.
	/// </summary>
	public static readonly IReadOnlyList<Server> ServersWithLocalApi =
		[Server.SomfyEurope, Server.SomfyOceania, Server.SomfyAmerica];

	/// <summary>
	/// Maps every known <see cref="Server"/> key to its <see cref="OverkizServer"/> endpoint descriptor.
	/// Pass the value obtained from this dictionary directly to the <see cref="OverkizClient"/> constructor.
	/// </summary>
	public static readonly IReadOnlyDictionary<Server, OverkizServer> SupportedServers =
		new Dictionary<Server, OverkizServer>
		{
			[Server.AtlanticCozytouch] = new OverkizServer
				{
				Name = "Atlantic Cozytouch",
				Endpoint = "https://ha110-1.overkiz.com/enduser-mobile-web/enduserAPI/",
				Manufacturer = "Atlantic",
				},
			[Server.Brandt] = new OverkizServer
				{
				Name = "Brandt Smart Control",
				Endpoint = "https://ha3-1.overkiz.com/enduser-mobile-web/enduserAPI/",
				Manufacturer = "Brandt",
				},
			[Server.Flexom] = new OverkizServer
				{
				Name = "Flexom",
				Endpoint = "https://ha108-1.overkiz.com/enduser-mobile-web/enduserAPI/",
				Manufacturer = "Bouygues",
				},
			[Server.HexaomHexaconnect] = new OverkizServer
				{
				Name = "Hexaom HexaConnect",
				Endpoint = "https://ha5-1.overkiz.com/enduser-mobile-web/enduserAPI/",
				Manufacturer = "Hexaom",
				},
			[Server.HiKumoAsia] = new OverkizServer
				{
				Name = "Hitachi Hi Kumo (Asia)",
				Endpoint = "https://ha203-1.overkiz.com/enduser-mobile-web/enduserAPI/",
				Manufacturer = "Hitachi",
				},
			[Server.HiKumoEurope] = new OverkizServer
				{
				Name = "Hitachi Hi Kumo (Europe)",
				Endpoint = "https://ha117-1.overkiz.com/enduser-mobile-web/enduserAPI/",
				Manufacturer = "Hitachi",
				},
			[Server.HiKumoOceania] = new OverkizServer
				{
				Name = "Hitachi Hi Kumo (Oceania)",
				Endpoint = "https://ha203-1.overkiz.com/enduser-mobile-web/enduserAPI/",
				Manufacturer = "Hitachi",
				},
			[Server.Nexity] = new OverkizServer
				{
				Name = "Nexity Eugénie",
				Endpoint = "https://ha106-1.overkiz.com/enduser-mobile-web/enduserAPI/",
				Manufacturer = "Nexity",
				},
			[Server.Rexel] = new OverkizServer
				{
				Name = "Rexel Energeasy Connect",
				Endpoint = REXEL_BACKEND_API,
				Manufacturer = "Rexel",
				ConfigurationUrl = "https://utilisateur.energeasyconnect.com/user/#/zone/equipements",
				RequiresGatewaySelection = true,
				},
			[Server.SauterCozytouch] = new OverkizServer
				{
				Name = "Sauter Cozytouch",
				Endpoint = "https://ha110-1.overkiz.com/enduser-mobile-web/enduserAPI/",
				Manufacturer = "Sauter",
				},
			[Server.SimuLivein2] = new OverkizServer
				{
				Name = "SIMU (LiveIn2)",
				Endpoint = "https://ha101-1.overkiz.com/enduser-mobile-web/enduserAPI/",
				Manufacturer = "Somfy",
				},
			[Server.SomfyEurope] = new OverkizServer
				{
				Name = "Somfy (Europe)",
				Endpoint = "https://ha101-1.overkiz.com/enduser-mobile-web/enduserAPI/",
				Manufacturer = "Somfy",
				},
			[Server.SomfyAmerica] = new OverkizServer
				{
				Name = "Somfy (North America)",
				Endpoint = "https://ha401-1.overkiz.com/enduser-mobile-web/enduserAPI/",
				Manufacturer = "Somfy",
				},
			[Server.SomfyOceania] = new OverkizServer
				{
				Name = "Somfy (Oceania)",
				Endpoint = "https://ha201-1.overkiz.com/enduser-mobile-web/enduserAPI/",
				Manufacturer = "Somfy",
				},
			[Server.ThermorCozytouch] = new OverkizServer
				{
				Name = "Thermor Cozytouch",
				Endpoint = "https://ha110-1.overkiz.com/enduser-mobile-web/enduserAPI/",
				Manufacturer = "Thermor",
				},
			[Server.Ubiwizz] = new OverkizServer
				{
				Name = "Ubiwizz",
				Endpoint = "https://ha129-1.overkiz.com/enduser-mobile-web/enduserAPI/",
				Manufacturer = "Decelect",
				},
		};
	}
