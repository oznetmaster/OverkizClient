// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// Portions derived from python-overkiz-api © 2020 Mick Vleeshouwer — MIT License.

namespace OverKizApi.Enums;

/// <summary>
/// Maps to the <c>uiClass</c> field returned in a device definition.
/// The UI class broadly categorises what a device <em>is</em> (e.g. a shutter, a sensor, a lock)
/// and is used by applications to choose the correct control template.
/// </summary>
public enum UIClass
	{
	/// <summary>UI class could not be parsed or is not yet known to this library.</summary>
	Unknown,
	/// <summary>Roller shutter whose slats can be individually tilted as well as raised/lowered.</summary>
	AdjustableSlatsRollerShutter,
	/// <summary>Alarm panel or intrusion detection system.</summary>
	Alarm,
	/// <summary>AirPlay-enabled power socket (smart plug with AirPlay audio routing).</summary>
	AirPlaySocket,
	/// <summary>Air quality sensor (generic; see also more specific sensor classes).</summary>
	AirSensor,
	/// <summary>Barometric (atmospheric) pressure sensor.</summary>
	AtmosphericPressureSensor,
	/// <summary>Retractable fabric awning (horizontal sun shade).</summary>
	Awning,
	/// <summary>Balcony-mounted device (generic balcony classification).</summary>
	Balcony,
	/// <summary>Bike rack or bicycle storage device.</summary>
	Bike,
	/// <summary>Bioclimatic pergola (louvred roof pergola that adjusts to weather conditions).</summary>
	BioclimaticPergola,
	/// <summary>Security or surveillance camera.</summary>
	Camera,
	/// <summary>CO₂ (carbon dioxide) concentration sensor.</summary>
	CarbonDioxideSensor,
	/// <summary>CO (carbon monoxide) concentration sensor.</summary>
	CarbonMonoxideSensor,
	/// <summary>Smart circuit breaker or electrical panel switch.</summary>
	CircuitBreaker,
	/// <summary>Gate whose open/close behaviour can be configured.</summary>
	ConfigurableGate,
	/// <summary>Magnetic or reed-switch contact sensor (door/window open detection).</summary>
	ContactSensor,
	/// <summary>Sensor that accumulates total electrical power consumption over time.</summary>
	CumulatedElectricPowerSensor,
	/// <summary>Fabric curtain that draws to the side(s).</summary>
	Curtain,
	/// <summary>Exterior heating element with dimmer (variable heat output) control.</summary>
	DimmerExteriorHeating,
	/// <summary>Dimmable light fixture.</summary>
	DimmerLight,
	/// <summary>Motorised or smart door.</summary>
	Door,
	/// <summary>Electronic door lock (deadbolt or latch with remote control).</summary>
	DoorLock,
	/// <summary>Combined sensor that measures two different physical quantities.</summary>
	DualSensor,
	/// <summary>Real-time electricity consumption sensor (instant power draw).</summary>
	ElectricitySensor,
	/// <summary>Exterior blind (fabric or slat blind installed outside the window).</summary>
	ExteriorBlind,
	/// <summary>Exterior roller screen (insect screen or sun screen mounted outside).</summary>
	ExteriorScreen,
	/// <summary>Exterior Venetian blind (angled horizontal slats, mounted outside).</summary>
	ExteriorVenetianBlind,
	/// <summary>Ceiling or desk fan.</summary>
	Fan,
	/// <summary>Controller hub for one or more fans.</summary>
	FanController,
	/// <summary>Single-car or multi-car garage (non-motorised or generic).</summary>
	Garage,
	/// <summary>Motorised garage door.</summary>
	GarageDoor,
	/// <summary>Motorised entry gate (driveway or pedestrian).</summary>
	Gate,
	/// <summary>Generic device that does not map to a more specific UI class.</summary>
	Generic,
	/// <summary>Central heating system controller (boiler, heat pump, or underfloor heating).</summary>
	HeatingSystem,
	/// <summary>Hitachi air-to-air heat pump (requires Hitachi-specific commands).</summary>
	HitachiAirToAirHeatPump,
	/// <summary>Horizontal retractable awning (extends horizontally over a terrace or balcony).</summary>
	HorizontalAwning,
	/// <summary>Relative humidity sensor.</summary>
	HumiditySensor,
	/// <summary>Light fixture (on/off, no dimming).</summary>
	Light,
	/// <summary>Ambient light level sensor (lux).</summary>
	LightSensor,
	/// <summary>Generic lock (door, safe, or access-control lock).</summary>
	Lock,
	/// <summary>Passive infrared or microwave motion detector.</summary>
	MotionSensor,
	/// <summary>Network infrastructure component (switch, access point, etc.).</summary>
	NetworkComponent,
	/// <summary>Acoustic noise level sensor (dB).</summary>
	NoiseSensor,
	/// <summary>Occupancy sensor (presence detection).</summary>
	OccupancySensor,
	/// <summary>Simple on/off actuator (relay or smart plug without energy metering).</summary>
	OnOff,
	/// <summary>On/off light (non-dimmable smart bulb or switch).</summary>
	OnOffLight,
	/// <summary>Smart oven device.</summary>
	OvenOven,
	/// <summary>Pergola (open-structure canopy, fixed louvres).</summary>
	Pergola,
	/// <summary>Pergola fitted with a retractable horizontal awning.</summary>
	PergolaHorizontalAwning,
	/// <summary>Swimming pool controller (pumps, heating, chemistry dosing).</summary>
	Pool,
	/// <summary>Pool circulation or filter pump.</summary>
	PoolPump,
	/// <summary>Protocol gateway/bridge device (e.g. an io-homecontrol to Z-Wave bridge).</summary>
	ProtocolGateway,
	/// <summary>Generic pump actuator.</summary>
	Pump,
	/// <summary>Rain detector or precipitation sensor.</summary>
	RainSensor,
	/// <summary>Roller shutter (opaque panel that rolls up into a box).</summary>
	RollerShutter,
	/// <summary>Interior roller screen or sun screen.</summary>
	Screen,
	/// <summary>Shower-room extractor fan.</summary>
	ShowerRoomFan,
	/// <summary>Alarm siren (audible/visual alert actuator).</summary>
	Siren,
	/// <summary>Combined siren and sensor (e.g. siren with built-in motion detection).</summary>
	SirenSensor,
	/// <summary>Adjustable slat panel (louvred screen, not a full roller shutter).</summary>
	Slats,
	/// <summary>Optical or ionisation smoke detector.</summary>
	SmokeSensor,
	/// <summary>Alarm panel that is stateless (no persistent armed/disarmed state on the gateway).</summary>
	StatelessAlarm,
	/// <summary>Exterior heater that is stateless (no persistent on/off state on the gateway).</summary>
	StatelessExteriorHeating,
	/// <summary>Solar irradiance or sunshine duration sensor.</summary>
	SunSensor,
	/// <summary>Full swimming pool management system.</summary>
	SwimmingPool,
	/// <summary>Swinging shutter (bi-fold or hinged panel shutter).</summary>
	SwingingShutter,
	/// <summary>Ambient air temperature sensor.</summary>
	TemperatureSensor,
	/// <summary>Multi-zone thermostat controller.</summary>
	ThermostatZonesController,
	/// <summary>Window handle with three positions (closed, tilt, open).</summary>
	ThreeWayWindowHandle,
	/// <summary>Venetian blind that can only tilt its slats (no raise/lower travel).</summary>
	TiltOnlyVenetianBlind,
	/// <summary>Timed on/off actuator (relay that switches off automatically after a delay).</summary>
	TimedOnOff,
	/// <summary>Timed on/off light (switches off automatically after a configured duration).</summary>
	TimedOnOffLight,
	/// <summary>Universal sensor that can be configured to measure different quantities.</summary>
	UniversalSensor,
	/// <summary>Device whose type has not been classified by the gateway.</summary>
	Untyped,
	/// <summary>Bioclimatic pergola with up/down travel control.</summary>
	UpDownBioclimaticPergola,
	/// <summary>Cellular (honeycomb) screen with up/down travel.</summary>
	UpDownCellularScreen,
	/// <summary>Curtain with up/down travel (vertical draw curtain on a track).</summary>
	UpDownCurtain,
	/// <summary>Dual curtain (two panels) with up/down travel.</summary>
	UpDownDualCurtain,
	/// <summary>Exterior roller screen with up/down travel.</summary>
	UpDownExteriorScreen,
	/// <summary>Exterior Venetian blind with up/down travel and slat tilt.</summary>
	UpDownExteriorVenetianBlind,
	/// <summary>Motorised garage door with up/down travel (2-wire control).</summary>
	UpDownGarageDoor,
	/// <summary>Motorised garage door with up/down travel (4-wire terminal control).</summary>
	UpDownGarageDoor4T,
	/// <summary>Motorised garage door with up/down travel and a ventilation stop position.</summary>
	UpDownGarageDoorWithVentilationPosition,
	/// <summary>Horizontal awning with up/down travel axis.</summary>
	UpDownHorizontalAwning,
	/// <summary>Roller shutter with up/down travel.</summary>
	UpDownRollerShutter,
	/// <summary>Interior screen with up/down travel.</summary>
	UpDownScreen,
	/// <summary>Sheer (translucent) roller screen with up/down travel.</summary>
	UpDownSheerScreen,
	/// <summary>Swinging shutter with up/down travel.</summary>
	UpDownSwingingShutter,
	/// <summary>Venetian blind with up/down travel and slat tilt.</summary>
	UpDownVenetianBlind,
	/// <summary>Motorised window with up/down travel.</summary>
	UpDownWindow,
	/// <summary>Zebra (alternating sheer/opaque stripe) roller screen with up/down travel.</summary>
	UpDownZebraScreen,
	/// <summary>VOC (volatile organic compound) air quality sensor.</summary>
	VOCSensor,
	/// <summary>Thermostatic valve with a temperature interface (TRV or zone valve).</summary>
	ValveHeatingTemperatureInterface,
	/// <summary>Controlled ventilation inlet (fresh-air intake damper).</summary>
	VentilationInlet,
	/// <summary>Controlled ventilation outlet (exhaust-air damper or extractor).</summary>
	VentilationOutlet,
	/// <summary>Controlled ventilation transfer damper (between zones).</summary>
	VentilationTransfer,
	/// <summary>Water ingress / flooding detection sensor.</summary>
	WaterDetectionSensor,
	/// <summary>Multi-parameter weather forecast sensor (temperature, humidity, wind, rain).</summary>
	WeatherForecastSensor,
	/// <summary>Wi-Fi access point or repeater device.</summary>
	Wifi,
	/// <summary>Anemometer measuring wind speed only.</summary>
	WindSpeedSensor,
	/// <summary>Anemometer measuring both wind speed and direction.</summary>
	WindSpeedAndDirectionSensor,
	/// <summary>Motorised or smart window lock.</summary>
	WindowLock,
	/// <summary>Window sensor that also detects tilt (open vs. tilted vs. closed).</summary>
	WindowWithTiltSensor,
	/// <summary>Z-Wave Aeotec device configuration interface.</summary>
	ZWaveAeotecConfiguration,
	/// <summary>Generic Z-Wave device configuration interface.</summary>
	ZWaveConfiguration,
	/// <summary>Z-Wave Danfoss RS Link thermostatic head configuration interface.</summary>
	ZWaveDanfossRSLink,
	/// <summary>Z-Wave door lock parameter configuration interface.</summary>
	ZWaveDoorLockConfiguration,
	/// <summary>Z-Wave Fibaro roller shutter module configuration interface.</summary>
	ZWaveFibaroRollerShutterConfiguration,
	/// <summary>Z-Wave Heatit thermostat configuration interface.</summary>
	ZWaveHeatitThermostatConfiguration,
	/// <summary>Z-Wave NodOn device configuration interface.</summary>
	ZWaveNodonConfiguration,
	/// <summary>Z-Wave Qubino module configuration interface.</summary>
	ZWaveQubinoConfiguration,
	/// <summary>Z-Wave Schneider Electric device configuration interface.</summary>
	ZWaveSEDeviceConfiguration,
	/// <summary>Z-Wave transceiver (radio dongle or module acting as a Z-Wave controller).</summary>
	ZWaveTransceiver,
	/// <summary>Zigbee network coordinator or router device.</summary>
	ZigbeeNetwork,
	/// <summary>Zigbee protocol stack component (low-level Zigbee controller).</summary>
	ZigbeeStack,
	}

/// <summary>
/// Maps to the <c>widgetName</c> field returned in a device definition.
/// The widget name selects the specific UI control rendered by the Overkiz app for a device.
/// Where a widget name matches a <see cref="UIClass"/> name the semantics are identical;
/// not every <see cref="UIClass"/> value has a corresponding widget.
/// </summary>
public enum UIWidget
	{
	/// <summary>Widget name could not be parsed or is not yet known to this library.</summary>
	Unknown,
	/// <summary>Widget for a roller shutter with individually tiltable slats.</summary>
	AdjustableSlatsRollerShutter,
	/// <summary>Widget for an alarm panel or intrusion detection system.</summary>
	Alarm,
	/// <summary>Widget for an AirPlay-enabled smart socket.</summary>
	AirPlaySocket,
	/// <summary>Widget for a barometric pressure sensor.</summary>
	AtmosphericPressureSensor,
	/// <summary>Widget for a retractable fabric awning.</summary>
	Awning,
	/// <summary>Widget for a security or surveillance camera.</summary>
	Camera,
	/// <summary>Widget for a CO₂ concentration sensor.</summary>
	CarbonDioxideSensor,
	/// <summary>Widget for a CO concentration sensor.</summary>
	CarbonMonoxideSensor,
	/// <summary>Widget for a smart circuit breaker.</summary>
	CircuitBreaker,
	/// <summary>Widget for a configurable entry gate.</summary>
	ConfigurableGate,
	/// <summary>Widget for a magnetic contact / door-window open sensor.</summary>
	ContactSensor,
	/// <summary>Widget for a fabric curtain that draws to the side(s).</summary>
	Curtain,
	/// <summary>Widget for an exterior heater with variable (dimmer) control.</summary>
	DimmerExteriorHeating,
	/// <summary>Widget for a dimmable light.</summary>
	DimmerLight,
	/// <summary>Widget for a motorised or smart door.</summary>
	Door,
	/// <summary>Widget for an electronic door lock.</summary>
	DoorLock,
	/// <summary>Widget for a real-time electricity consumption sensor.</summary>
	ElectricitySensor,
	/// <summary>Widget for an exterior fabric or slat blind.</summary>
	ExteriorBlind,
	/// <summary>Widget for an exterior roller screen.</summary>
	ExteriorScreen,
	/// <summary>Widget for an exterior Venetian blind.</summary>
	ExteriorVenetianBlind,
	/// <summary>Widget for a ceiling or desk fan.</summary>
	Fan,
	/// <summary>Widget for a fan controller hub.</summary>
	FanController,
	/// <summary>Widget for a motorised garage door.</summary>
	GarageDoor,
	/// <summary>Widget for a motorised entry gate.</summary>
	Gate,
	/// <summary>Widget for a generic device with no specific template.</summary>
	Generic,
	/// <summary>Widget for a heating system controller.</summary>
	HeatingSystem,
	/// <summary>Widget for a Hitachi air-to-air heat pump.</summary>
	HitachiAirToAirHeatPump,
	/// <summary>Widget for a horizontal retractable awning.</summary>
	HorizontalAwning,
	/// <summary>Widget for a relative humidity sensor.</summary>
	HumiditySensor,
	/// <summary>Widget for an on/off light fixture.</summary>
	Light,
	/// <summary>Widget for an ambient light level sensor.</summary>
	LightSensor,
	/// <summary>Widget for a generic lock.</summary>
	Lock,
	/// <summary>Widget for a motion / PIR sensor.</summary>
	MotionSensor,
	/// <summary>Widget for an acoustic noise level sensor.</summary>
	NoiseSensor,
	/// <summary>Widget for an occupancy / presence sensor.</summary>
	OccupancySensor,
	/// <summary>Widget for a simple on/off relay or smart plug.</summary>
	OnOff,
	/// <summary>Widget for a non-dimmable on/off light.</summary>
	OnOffLight,
	/// <summary>Widget for a fixed-louvre pergola.</summary>
	Pergola,
	/// <summary>Widget for a swimming pool controller.</summary>
	Pool,
	/// <summary>Widget for a pool circulation pump.</summary>
	PoolPump,
	/// <summary>Widget for a protocol gateway/bridge device.</summary>
	ProtocolGateway,
	/// <summary>Widget for a generic pump actuator.</summary>
	Pump,
	/// <summary>Widget for a rain / precipitation sensor.</summary>
	RainSensor,
	/// <summary>Widget for a roller shutter.</summary>
	RollerShutter,
	/// <summary>Widget for an interior roller screen.</summary>
	Screen,
	/// <summary>Widget for an alarm siren.</summary>
	Siren,
	/// <summary>Widget for a combined siren and sensor.</summary>
	SirenSensor,
	/// <summary>Widget for a smoke detector.</summary>
	SmokeSensor,
	/// <summary>Widget for a stateless alarm panel.</summary>
	StatelessAlarm,
	/// <summary>Widget for a stateless exterior heater.</summary>
	StatelessExteriorHeating,
	/// <summary>Widget for a full swimming pool management system.</summary>
	SwimmingPool,
	/// <summary>Widget for a hinged or bi-fold swinging shutter.</summary>
	SwingingShutter,
	/// <summary>Widget for an ambient temperature sensor.</summary>
	TemperatureSensor,
	/// <summary>Widget for a multi-zone thermostat controller.</summary>
	ThermostatZonesController,
	/// <summary>Widget for a Venetian blind that can only tilt (no travel).</summary>
	TiltOnlyVenetianBlind,
	/// <summary>Widget for a timed on/off relay.</summary>
	TimedOnOff,
	/// <summary>Widget for a timed on/off light.</summary>
	TimedOnOffLight,
	/// <summary>Widget for a configurable universal sensor.</summary>
	UniversalSensor,
	/// <summary>Widget for an unclassified device.</summary>
	Untyped,
	/// <summary>Widget for a roller shutter with up/down travel.</summary>
	UpDownRollerShutter,
	/// <summary>Widget for an interior screen with up/down travel.</summary>
	UpDownScreen,
	/// <summary>Widget for a Venetian blind with up/down travel and slat tilt.</summary>
	UpDownVenetianBlind,
	/// <summary>Widget for a motorised window with up/down travel.</summary>
	UpDownWindow,
	/// <summary>Widget for a thermostatic valve with temperature interface.</summary>
	ValveHeatingTemperatureInterface,
	/// <summary>Widget for a controlled fresh-air ventilation inlet.</summary>
	VentilationInlet,
	/// <summary>Widget for a controlled exhaust-air ventilation outlet.</summary>
	VentilationOutlet,
	/// <summary>Widget for a controlled inter-zone ventilation transfer damper.</summary>
	VentilationTransfer,
	/// <summary>Widget for a water ingress / flood detection sensor.</summary>
	WaterDetectionSensor,
	/// <summary>Widget for a multi-parameter weather forecast sensor.</summary>
	WeatherForecastSensor,
	/// <summary>Widget for a Wi-Fi access point or repeater.</summary>
	Wifi,
	/// <summary>Widget for an anemometer measuring wind speed only.</summary>
	WindSpeedSensor,
	/// <summary>Widget for an anemometer measuring wind speed and direction.</summary>
	WindSpeedAndDirectionSensor,
	/// <summary>Widget for a motorised or smart window lock.</summary>
	WindowLock,
	/// <summary>Widget for a window sensor that detects tilt as well as open/closed state.</summary>
	WindowWithTiltSensor,
	/// <summary>Widget for a Z-Wave transceiver module.</summary>
	ZWaveTransceiver,
	/// <summary>Widget for a Zigbee network coordinator or router.</summary>
	ZigbeeNetwork,
	/// <summary>Widget for a Zigbee protocol stack component.</summary>
	ZigbeeStack,
	}
