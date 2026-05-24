// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// Portions derived from python-overkiz-api © 2020 Mick Vleeshouwer — MIT License.

namespace OverKizApi.Enums;

/// <summary>
/// Identifies a specific named Overkiz-based cloud server.
/// Use these keys to look up endpoint details in <see cref="OverkizConst.SupportedServers"/>.
/// </summary>
public enum Server
	{
	/// <summary>Atlantic Cozytouch (France) — uses CozyTouch JWT authentication.</summary>
	AtlanticCozytouch,
	/// <summary>Brandt Smart Control — standard username/password login.</summary>
	Brandt,
	/// <summary>Flexom by Bouygues — standard username/password login.</summary>
	Flexom,
	/// <summary>Hexaom HexaConnect — standard username/password login.</summary>
	HexaomHexaconnect,
	/// <summary>Hitachi Hi Kumo (Asia region) — standard username/password login.</summary>
	HiKumoAsia,
	/// <summary>Hitachi Hi Kumo (Europe region) — standard username/password login.</summary>
	HiKumoEurope,
	/// <summary>Hitachi Hi Kumo (Oceania region) — standard username/password login.</summary>
	HiKumoOceania,
	/// <summary>Nexity Eugénie — uses AWS Cognito SRP authentication.</summary>
	Nexity,
	/// <summary>Rexel Energeasy Connect — standard username/password login.</summary>
	Rexel,
	/// <summary>Sauter Cozytouch — uses CozyTouch JWT authentication.</summary>
	SauterCozytouch,
	/// <summary>SIMU LiveIn2 — standard username/password login.</summary>
	SimuLivein2,
	/// <summary>Somfy developer mode (local gateway) — uses local bearer token.</summary>
	SomfyDeveloperMode,
	/// <summary>Somfy TaHoma (Europe) — uses Somfy OAuth 2.0 authentication; supports local API.</summary>
	SomfyEurope,
	/// <summary>Somfy TaHoma (North America) — uses Somfy OAuth 2.0 authentication; supports local API.</summary>
	SomfyAmerica,
	/// <summary>Somfy TaHoma (Oceania) — uses Somfy OAuth 2.0 authentication; supports local API.</summary>
	SomfyOceania,
	/// <summary>Thermor Cozytouch — uses CozyTouch JWT authentication.</summary>
	ThermorCozytouch,
	/// <summary>Ubiwizz by Decelect — standard username/password login.</summary>
	Ubiwizz,
	}
