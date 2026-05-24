// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// Portions derived from python-overkiz-api © 2020 Mick Vleeshouwer — MIT License.

namespace OverKizApi.Enums;

/// <summary>
/// Identifies the radio/wired communication protocol used by a device.
/// The value is parsed from the device URL prefix (e.g. <c>io://</c>, <c>rts://</c>).
/// </summary>
public enum Protocol
	{
	/// <summary>Protocol is not recognised.</summary>
	Unknown,
	/// <summary>August smart lock protocol.</summary>
	August,
	/// <summary>IP camera protocol.</summary>
	Camera,
	/// <summary>Eliot IoT protocol (Somfy cloud-to-cloud).</summary>
	Eliot,
	/// <summary>EnOcean self-powered wireless protocol.</summary>
	Enocean,
	/// <summary>Hitachi Hi-Lumo Wi-Fi protocol.</summary>
	HlrrWifi,
	/// <summary>Apple HomeKit integration.</summary>
	Homekit,
	/// <summary>Philips Hue integration.</summary>
	Hue,
	/// <summary>Internal virtual device (not a real physical protocol).</summary>
	Internal,
	/// <summary>Somfy io-homecontrol® 868 MHz bidirectional protocol.</summary>
	Io,
	/// <summary>JSW protocol.</summary>
	Jsw,
	/// <summary>Modbus wired serial protocol.</summary>
	Modbus,
	/// <summary>Modbus Link variant.</summary>
	Modbuslink,
	/// <summary>MyFox security protocol.</summary>
	Myfox,
	/// <summary>Netatmo cloud-to-cloud integration.</summary>
	Netatmo,
	/// <summary>Open Gateway Communication Protocol.</summary>
	Ogcp,
	/// <summary>Open Gateway Protocol.</summary>
	Ogp,
	/// <summary>OpenDoors access-control protocol.</summary>
	Opendoors,
	/// <summary>Overkiz Virtual Protocol (cloud-side virtual devices).</summary>
	Ovp,
	/// <summary>Profalux 868 MHz protocol.</summary>
	Profalux868,
	/// <summary>RAMSES II bi-directional heating protocol (Honeywell/Evohome).</summary>
	Ramses,
	/// <summary>RTD protocol.</summary>
	Rtd,
	/// <summary>RTDS protocol.</summary>
	Rtds,
	/// <summary>RTN protocol.</summary>
	Rtn,
	/// <summary>Somfy RTS (Radio Technology Somfy) unidirectional 433 MHz protocol.</summary>
	Rts,
	/// <summary>Somfy Thermostat protocol.</summary>
	SomfyThermostat,
	/// <summary>UPnP control protocol.</summary>
	UpnpControl,
	/// <summary>Verisure security system integration.</summary>
	Verisure,
	/// <summary>Schneider Wiser heating/energy protocol.</summary>
	Wiser,
	/// <summary>Zigbee IEEE 802.15.4 mesh protocol.</summary>
	Zigbee,
	/// <summary>Z-Wave mesh wireless protocol.</summary>
	Zwave,
	}
