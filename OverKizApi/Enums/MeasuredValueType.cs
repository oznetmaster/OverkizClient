// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace OverKizApi.Enums;

/// <summary>
/// Identifies the physical quantity and unit of a numeric sensor state value.
/// Used alongside the raw state value to interpret measurements correctly.
/// </summary>
public enum MeasuredValueType
	{
	/// <summary>Unit is not specified or not recognised.</summary>
	Unknown,
	/// <summary>A dimensionless absolute numeric value.</summary>
	AbsoluteValue,
	/// <summary>Angular position measured in degrees (°).</summary>
	AngleInDegrees,
	/// <summary>Rotational speed measured in degrees per second (°/s).</summary>
	AngularSpeedInDegreesPerSecond,
	/// <summary>Electrical energy measured in kilowatt-hours (kWh).</summary>
	ElectricalEnergyInKWh,
	/// <summary>Electrical energy measured in watt-hours (Wh).</summary>
	ElectricalEnergyInWh,
	/// <summary>Electrical power measured in kilowatts (kW).</summary>
	ElectricalPowerInKW,
	/// <summary>Electrical power measured in watts (W).</summary>
	ElectricalPowerInW,
	/// <summary>Electric current measured in amperes (A).</summary>
	ElectricCurrentInAmpere,
	/// <summary>Electric current measured in milliamperes (mA).</summary>
	ElectricCurrentInMilliAmpere,
	/// <summary>Energy measured in calories (cal).</summary>
	EnergyInCal,
	/// <summary>Energy measured in kilocalories (kcal).</summary>
	EnergyInKCal,
	/// <summary>Fluid flow rate measured in litres per second (L/s).</summary>
	FlowInLitrePerSecond,
	/// <summary>Fluid flow rate measured in cubic metres per hour (m³/h).</summary>
	FlowInMeterCubePerHour,
	/// <summary>Fluid flow rate measured in cubic metres per second (m³/s).</summary>
	FlowInMeterCubePerSecond,
	/// <summary>Fossil-fuel energy measured in watt-hours (Wh).</summary>
	FossilEnergyInWh,
	/// <summary>Rate of change measured in percentage points per second (%/s).</summary>
	GradientInPercentagePerSecond,
	/// <summary>Distance or displacement measured in metres (m).</summary>
	LengthInMeter,
	/// <summary>Linear velocity measured in metres per second (m/s).</summary>
	LinearSpeedInMeterPerSecond,
	/// <summary>Illuminance measured in lux (lx).</summary>
	LuminanceInLux,
	/// <summary>Concentration measured in parts per billion (ppb).</summary>
	PartsPerBillion,
	/// <summary>Concentration measured in parts per million (ppm).</summary>
	PartsPerMillion,
	/// <summary>Concentration measured in parts per quadrillion (ppq).</summary>
	PartsPerQuadrillion,
	/// <summary>Concentration measured in parts per trillion (ppt).</summary>
	PartsPerTrillion,
	/// <summary>Irradiance measured in watts per square metre (W/m²).</summary>
	PowerPerSquareMeter,
	/// <summary>Atmospheric pressure measured in hectopascals (hPa).</summary>
	PressureInHpa,
	/// <summary>Atmospheric pressure measured in millibars (mbar).</summary>
	PressureInMilliBar,
	/// <summary>A relative value expressed as a percentage (%).</summary>
	RelativeValueInPercentage,
	/// <summary>Temperature measured in degrees Celsius (°C).</summary>
	TemperatureInCelcius,
	/// <summary>Temperature measured in kelvin (K).</summary>
	TemperatureInKelvin,
	/// <summary>Duration measured in seconds (s).</summary>
	TimeInSecond,
	/// <summary>A component of a vector (dimensionless coordinate).</summary>
	VectorCoordinate,
	/// <summary>Electrical potential measured in millivolts (mV).</summary>
	VoltageInMilliVolt,
	/// <summary>Electrical potential measured in volts (V).</summary>
	VoltageInVolt,
	/// <summary>Volume measured in cubic metres (m³).</summary>
	VolumeInCubicMeter,
	/// <summary>Volume measured in US gallons (gal).</summary>
	VolumeInGallon,
	/// <summary>Volume measured in litres (L).</summary>
	VolumeInLiter,
	}
