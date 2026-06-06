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
public enum GatewayType
	{
	/// <summary>Gateway type is not recognised.</summary>
	Unknown = -1,
	/// <summary>Virtual Kizbox gateway.</summary>
	VirtualKizbox = 0,
	/// <summary>Kizbox v1 gateway.</summary>
	KizboxV1 = 2,
	/// <summary>TaHoma gateway.</summary>
	Tahoma = 15,
	/// <summary>Energeasy Connect gateway.</summary>
	EnergeasyConnect = 19,
	/// <summary>Verisure alarm system gateway.</summary>
	VerisureAlarmSystem = 20,
	/// <summary>Kizbox Mini gateway.</summary>
	KizboxMini = 21,
	/// <summary>Hitachi Hi Kumo adapter gateway.</summary>
	HiKumoAdapter = 22,
	/// <summary>Kizbox v2 gateway.</summary>
	KizboxV2 = 24,
	/// <summary>MyFox alarm system gateway.</summary>
	MyfoxAlarmSystem = 25,
	/// <summary>Kizbox Mini VMBus gateway.</summary>
	KizboxMiniVmbus = 27,
	/// <summary>Kizbox Mini io gateway.</summary>
	KizboxMiniIo = 28,
	/// <summary>TaHoma v2 gateway.</summary>
	TahomaV2 = 29,
	/// <summary>Kizbox v2 3H gateway.</summary>
	KizboxV23H = 30,
	/// <summary>Kizbox v2 2H gateway.</summary>
	KizboxV22H = 31,
	/// <summary>Cozytouch gateway.</summary>
	Cozytouch = 32,
	/// <summary>Connexoon gateway.</summary>
	Connexoon = 34,
	/// <summary>JSW camera gateway.</summary>
	JswCamera = 35,
	/// <summary>TaHoma v2 RTS gateway.</summary>
	TahomaV2Rts = 41,
	/// <summary>Kizbox Mini Modbus gateway.</summary>
	KizboxMiniModbus = 42,
	/// <summary>Kizbox Mini OVP gateway.</summary>
	KizboxMiniOvp = 43,
	/// <summary>Hitachi Hi Box gateway.</summary>
	HiBox = 44,
	/// <summary>Hattara rail-DIN gateway.</summary>
	HattaraRailDin = 47,
	/// <summary>Energeasy Connect rail-DIN gateway.</summary>
	EnergeasyConnectRailDin = 48,
	/// <summary>Wizz Box gateway.</summary>
	WizzBox = 52,
	/// <summary>Connexoon RTS gateway.</summary>
	ConnexoonRts = 53,
	/// <summary>OpenDoors lock system gateway.</summary>
	OpendoorsLockSystem = 54,
	/// <summary>Connexoon RTS Japan gateway.</summary>
	ConnexoonRtsJapan = 56,
	/// <summary>Energeasy Connect v2 gateway.</summary>
	EnergeasyConnectV2 = 57,
	/// <summary>Home Protect System gateway.</summary>
	HomeProtectSystem = 58,
	/// <summary>Connexoon RTS Australia gateway.</summary>
	ConnexoonRtsAustralia = 62,
	/// <summary>Somfy thermostat system gateway.</summary>
	ThermostatSomfySystem = 63,
	/// <summary>Smartly Mini daughterboard Z-Wave gateway.</summary>
	SmartlyMiniDaughterboardZwave = 65,
	/// <summary>Smartly Minibox rail-DIN gateway.</summary>
	SmartlyMiniboxRaildin = 66,
	/// <summary>TaHoma Bee gateway.</summary>
	TahomaBee = 67,
	/// <summary>TaHoma rail-DIN gateway.</summary>
	TahomaRailDin = 72,
	/// <summary>Nexity rail-DIN gateway.</summary>
	NexityRailDin = 74,
	/// <summary>TaHoma Beecon gateway.</summary>
	TahomaBeecon = 75,
	/// <summary>Eliot gateway.</summary>
	Eliot = 77,
	/// <summary>Sauter Cozytouch gateway.</summary>
	CozytouchSauter = 83,
	/// <summary>Wiser gateway.</summary>
	Wiser = 88,
	/// <summary>Netatmo gateway.</summary>
	Netatmo = 92,
	/// <summary>TaHoma Switch gateway.</summary>
	TahomaSwitch = 98,
	/// <summary>Somfy Connectivity Kit gateway.</summary>
	SomfyConnectivityKit = 99,
	/// <summary>Cozytouch v2 gateway.</summary>
	CozytouchV2 = 105,
	/// <summary>TaHoma rail-DIN S gateway.</summary>
	TahomaRailDinS = 108,
	/// <summary>Nexity rail-DIN S gateway.</summary>
	NexityRailDinS = 109,
	/// <summary>HexaConnect gateway.</summary>
	Hexaconnect = 117,
	/// <summary>Daikin Onecta gateway.</summary>
	DaikinOnecta = 118,
	/// <summary>Energeasy Connect v3 gateway.</summary>
	EnergeasyConnectV3 = 120,
	/// <summary>TaHoma Switch US gateway.</summary>
	TahomaSwitchUs = 121,
	/// <summary>TaHoma Switch Oceania gateway.</summary>
	TahomaSwitchOc = 122,
	/// <summary>TaHoma Switch Australia gateway.</summary>
	TahomaSwitchAu = 123,
	/// <summary>Energeasy Connect v3 rail-DIN gateway.</summary>
	EnergeasyConnectV3RailDin = 125,
	/// <summary>TaHoma Switch CH gateway.</summary>
	TahomaSwitchCh = 126,
	/// <summary>TaHoma Switch SC gateway.</summary>
	TahomaSwitchSc = 128,
	}

/// <summary>Identifies the hardware sub-model / variant of a gateway.</summary>
public enum GatewaySubType
	{
	/// <summary>Gateway sub-type is not recognised.</summary>
	Unknown = -1,
	/// <summary>TaHoma Basic gateway.</summary>
	TahomaBasic = 1,
	/// <summary>TaHoma Basic Plus gateway.</summary>
	TahomaBasicPlus = 2,
	/// <summary>TaHoma Premium gateway.</summary>
	TahomaPremium = 3,
	/// <summary>Somfy Box gateway.</summary>
	SomfyBox = 4,
	/// <summary>Hitachi Box gateway.</summary>
	HitachiBox = 5,
	/// <summary>Mondial Box gateway.</summary>
	MondialBox = 6,
	/// <summary>Maroc Telecom Box gateway.</summary>
	MarocTelecomBox = 7,
	/// <summary>TaHoma Serenity gateway.</summary>
	TahomaSerenity = 8,
	/// <summary>TaHoma Verisure gateway.</summary>
	TahomaVerisure = 9,
	/// <summary>TaHoma Serenity Premium gateway.</summary>
	TahomaSerenityPremium = 10,
	/// <summary>TaHoma Monsieur Store gateway.</summary>
	TahomaMonsieurStore = 11,
	/// <summary>TaHoma Maison Avenir et Tradition gateway.</summary>
	TahomaMaisonAvenirEtTradition = 12,
	/// <summary>TaHoma short-channel gateway.</summary>
	TahomaShortChannel = 13,
	/// <summary>TaHoma Pro gateway.</summary>
	TahomaPro = 14,
	/// <summary>TaHoma Security short-channel gateway.</summary>
	TahomaSecurityShortChannel = 15,
	/// <summary>TaHoma Security Pro gateway.</summary>
	TahomaSecurityPro = 16,
	/// <summary>TaHoma Box C io gateway.</summary>
	TahomaBoxCIo = 17,
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
