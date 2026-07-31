// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace OverKizApi.Enums;

/// <summary>Lifecycle states of an execution as reported by the Overkiz API.</summary>
public enum ExecutionState
	{
	/// <summary>The execution has been accepted but has not yet started.</summary>
	NotStarted,
	/// <summary>The execution is currently being processed by the gateway.</summary>
	InProgress,
	/// <summary>The execution completed successfully.</summary>
	Completed,
	/// <summary>The execution terminated with an error.</summary>
	Failed,
	/// <summary>The execution was explicitly cancelled by the user or the API.</summary>
	Cancelled,
	/// <summary>The execution is waiting in the gateway queue.</summary>
	Queued,
	/// <summary>The execution state could not be determined.</summary>
	Unknown,
	}

/// <summary>Indicates how an execution was triggered.</summary>
public enum ExecutionType
	{
	/// <summary>Execution was triggered immediately (no delay).</summary>
	Immediate,
	/// <summary>Execution was scheduled for a specific future time.</summary>
	Delayed,
	/// <summary>Execution was triggered at sunrise.</summary>
	Sunrise,
	/// <summary>Execution was triggered at sunset.</summary>
	Sunset,
	/// <summary>Execution type could not be determined.</summary>
	Unknown,
	}

/// <summary>Further classifies the origin of an execution.</summary>
public enum ExecutionSubType
	{
	/// <summary>Execution was triggered by an internal gateway rule or automation.</summary>
	Internal,
	/// <summary>Execution was triggered by an external application or API call.</summary>
	External,
	/// <summary>Execution was triggered as part of a named scenario.</summary>
	Scenario,
	/// <summary>Execution sub-type could not be determined.</summary>
	Unknown,
	}

/// <summary>Failure reason codes reported on a failed execution.</summary>
public enum FailureType
	{
	/// <summary>No failure occurred.</summary>
	NoFailure,
	/// <summary>The target device or object does not exist.</summary>
	UnknownObject,
	/// <summary>The command contained a syntax error.</summary>
	CommandSyntaxError,
	/// <summary>The HTTP method used is not allowed for this endpoint.</summary>
	MethodNotAllowed,
	/// <summary>The command is not allowed on this device in its current state.</summary>
	CommandNotAllowed,
	/// <summary>The command name was not included in the request.</summary>
	MissingCommandName,
	/// <summary>The command was not found on the target device.</summary>
	CommandNotFound,
	/// <summary>The requested operation is not supported by the gateway or device.</summary>
	UnsupportedOperation,
	/// <summary>The command has been disabled.</summary>
	DisabledCommand,
	/// <summary>The SSM (Scenario State Machine) token required for this command is missing.</summary>
	MissingSsmToken,
	/// <summary>Failure reason could not be determined.</summary>
	Unknown,
	}
