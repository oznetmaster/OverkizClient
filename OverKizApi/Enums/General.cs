// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// Portions derived from python-overkiz-api © 2020 Mick Vleeshouwer — MIT License.

namespace OverKizApi.Enums;

/// <summary>Classifies a device as an actuator, a sensor, or unknown.</summary>
public enum ProductType
	{
	/// <summary>Device type is not known.</summary>
	Unknown = 0,
	/// <summary>The device can receive commands and perform physical actions (e.g. a roller shutter motor).</summary>
	Actuator = 1,
	/// <summary>The device reports measured state values (e.g. a temperature sensor).</summary>
	Sensor = 2,
	}

/// <summary>
/// Identifies the type of an event returned by the event listener endpoint
/// (<c>events/{listenerId}/fetch</c>).
/// </summary>
public enum EventName
	{
	/// <summary>Event type is not recognised.</summary>
	Unknown,
	/// <summary>A device's availability (reachability) changed.</summary>
	DeviceAvailabilityChanged,
	/// <summary>A new device was added to the setup.</summary>
	DeviceCreated,
	/// <summary>A device was removed from the setup.</summary>
	DeviceDeleted,
	/// <summary>A device's associated gateway was updated.</summary>
	DeviceGatewayUpdated,
	/// <summary>One or more states on a device changed value.</summary>
	DeviceStateChanged,
	/// <summary>A device's metadata (label, place, etc.) was updated.</summary>
	DeviceUpdated,
	/// <summary>A new execution was registered with the gateway.</summary>
	ExecutionRegistered,
	/// <summary>An execution transitioned to a new state.</summary>
	ExecutionStateChanged,
	/// <summary>A gateway's alive/heartbeat status changed.</summary>
	GatewayAliveChanged,
	/// <summary>A gateway firmware downgrade was detected.</summary>
	GatewayDowngradeChanged,
	/// <summary>A gateway completed a device synchronisation cycle.</summary>
	GatewaySynchronizationFinished,
	/// <summary>A gateway completed a device synchronisation cycle (alternate name used by some servers).</summary>
	GatewaySynchronizationEnded,
	/// <summary>A gateway started a device synchronisation cycle.</summary>
	GatewaySynchronizationStarted,
	/// <summary>A gateway's metadata was updated.</summary>
	GatewayUpdated,
	/// <summary>The cloud requested all devices to refresh their states.</summary>
	RefreshAllDevicesStates,
	/// <summary>A new scenario execution was registered.</summary>
	ScenarioExecutionRegistered,
	/// <summary>A scenario execution transitioned to a new state.</summary>
	ScenarioExecutionStateChanged,
	}
