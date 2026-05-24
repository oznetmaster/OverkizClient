// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// Portions derived from python-overkiz-api © 2020 Mick Vleeshouwer — MIT License.

namespace OverKizApi.Enums;

/// <summary>Describes the role of a gateway within its installation.</summary>
public enum GatewayMode
	{
	/// <summary>The gateway operates independently with no peer gateways.</summary>
	Standalone,
	/// <summary>The gateway is the primary coordinator in a multi-gateway setup.</summary>
	Master,
	/// <summary>The gateway is subordinate to a master gateway.</summary>
	Slave,
	/// <summary>Gateway mode could not be determined.</summary>
	Unknown,
	}

/// <summary>Identifies the hardware model / product family of a gateway.</summary>
public enum GatewaySubType
	{
	/// <summary>Hardware model is not known.</summary>
	Unknown,
	/// <summary>TaHoma Switch (first generation).</summary>
	TahomaSwitch,
	/// <summary>TaHoma Box v1.</summary>
	TahomaBoxV1,
	/// <summary>TaHoma Classic v2.</summary>
	TahomaClassicV2,
	/// <summary>TaHoma Beecon.</summary>
	TahomaBeecon,
	/// <summary>Cozytouch first-generation gateway.</summary>
	Cozytouch,
	/// <summary>Cozytouch v2 gateway.</summary>
	CozytouchV2,
	/// <summary>Hitachi Hi Kumo gateway.</summary>
	HiKumo,
	/// <summary>Nexity Eugénie gateway.</summary>
	Nexity,
	/// <summary>Rexel Energeasy Connect gateway.</summary>
	Rexel,
	/// <summary>Somfy gateway (generic).</summary>
	Somfy,
	/// <summary>Any other unclassified hardware model.</summary>
	Other,
	}

/// <summary>Tracks the firmware/software update lifecycle of a gateway.</summary>
public enum GatewayUpdateStatus
	{
	/// <summary>No update is pending; firmware is current.</summary>
	NotUpdate,
	/// <summary>An update has been detected and is pending download.</summary>
	Pending,
	/// <summary>The update package is downloaded and ready to be applied.</summary>
	ReadyToUpdate,
	/// <summary>The gateway is currently applying the update.</summary>
	InProgress,
	/// <summary>The update was applied successfully.</summary>
	Updated,
	/// <summary>The update attempt failed.</summary>
	Failed,
	/// <summary>Update status could not be determined.</summary>
	Unknown,
	}

/// <summary>Indicates whether the gateway is reachable by the Overkiz cloud.</summary>
public enum GatewayConnectivityState
	{
	/// <summary>The gateway is online and connected to the cloud.</summary>
	Connected,
	/// <summary>The gateway cannot be reached by the cloud.</summary>
	Disconnected,
	/// <summary>Connectivity state could not be determined.</summary>
	Unknown,
	}
