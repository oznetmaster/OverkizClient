// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace OverKizApi.Exceptions;

/// <summary>
/// Base class for all exceptions thrown by the Overkiz API client library.
/// Catch this type to handle any API-related error in a single handler.
/// </summary>
public class OverkizException : Exception
	{
	/// <summary>Initialises a new instance with no message.</summary>
	public OverkizException () { }
	/// <summary>Initialises a new instance with the specified error <paramref name="message"/>.</summary>
	/// <param name="message">Human-readable description of the error.</param>
	public OverkizException (string message) : base (message) { }
	/// <summary>Initialises a new instance with a <paramref name="message"/> and an <paramref name="inner"/> cause.</summary>
	/// <param name="message">Human-readable description of the error.</param>
	/// <param name="inner">The exception that caused this exception.</param>
	public OverkizException (string message, Exception inner) : base (message, inner) { }
	}

/// <summary>
/// Raised for structured Overkiz API error responses (HTTP 4xx/5xx with a JSON error body).
/// The <see cref="Exception.Message"/> property contains the <c>error</c> field from the response.
/// </summary>
public class BaseOverkizException : OverkizException
	{
	/// <summary>Initialises a new instance with no message.</summary>
	public BaseOverkizException () { }
	/// <summary>Initialises a new instance with the specified error <paramref name="message"/>.</summary>
	/// <param name="message">The <c>error</c> string from the API response body.</param>
	public BaseOverkizException (string message) : base (message) { }
	/// <summary>Initialises a new instance with a <paramref name="message"/> and an <paramref name="inner"/> cause.</summary>
	/// <param name="message">The <c>error</c> string from the API response body.</param>
	/// <param name="inner">The exception that caused this exception.</param>
	public BaseOverkizException (string message, Exception inner) : base (message, inner) { }
	}

/// <summary>
/// Raised when the username or password supplied to the API is incorrect.
/// Check credentials and retry; do not loop automatically.
/// </summary>
public class BadCredentialsException : BaseOverkizException
	{
	/// <summary>Initialises a new instance with no message.</summary>
	public BadCredentialsException () { }
	/// <summary>Initialises a new instance with the specified error <paramref name="message"/>.</summary>
	/// <param name="message">The API error message.</param>
	public BadCredentialsException (string message) : base (message) { }
	}

/// <summary>
/// Raised when a command sent to a device is not recognised by the gateway or device.
/// Verify the command name and parameters against the device's <see cref="OverKizApi.Models.Definition"/>.
/// </summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The API error message, which typically includes the unknown command name.</param>
public class InvalidCommandException (string message) : BaseOverkizException(message)
	{
	}

/// <summary>
/// Raised when an execution is submitted for a device that already has a pending action.
/// Wait for the existing action to complete or cancel it before retrying.
/// </summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The API error message.</param>
public class DuplicateActionOnDeviceException (string message) : BaseOverkizException(message)
	{
	}

/// <summary>
/// Raised when an action group setup cannot be resolved for the target gateway.
/// This may indicate a configuration issue on the gateway side.
/// </summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The API error message.</param>
public class ActionGroupSetupNotFoundException (string message) : BaseOverkizException(message)
	{
	}

/// <summary>Raised when the requested resource (device, scenario, token, etc.) does not exist.</summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The API error message.</param>
public class NoSuchResourceException (string message) : BaseOverkizException(message)
	{
	}

/// <summary>
/// Raised when the authenticated user does not have permission to access the requested resource.
/// </summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The API error message.</param>
public class ResourceAccessDeniedException (string message) : BaseOverkizException(message)
	{
	}

/// <summary>
/// Raised when the API returns "Not authenticated".
/// The session cookie or bearer token has expired; call
/// <see cref="OverKizApi.OverkizClient.Login"/> again to obtain a new session.
/// </summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The API error message.</param>
public class NotAuthenticatedException (string message) : ResourceAccessDeniedException(message)
	{
	}

/// <summary>
/// Raised when the gateway reports that its execution queue is saturated.
/// Reduce the rate of execution submissions and retry with back-off.
/// </summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The API error message.</param>
public class TooManyExecutionsException (string message) : BaseOverkizException (message)
	{
	}

/// <summary>
/// Raised when the gateway's execution queue is completely full and cannot accept new requests.
/// </summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The API error message, which typically includes the gateway ID.</param>
public class ExecutionQueueFullException (string message) : BaseOverkizException (message)
	{
	}

/// <summary>
/// Raised when the HTTP 429 Too Many Requests status is returned by the API.
/// Back off and retry after a delay.
/// </summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The API error message.</param>
public class TooManyRequestsException (string message) : BaseOverkizException (message)
	{
	}

/// <summary>
/// Raised when the server rejects the request because too many calls are in-flight simultaneously.
/// Reduce concurrency and retry.
/// </summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The API error message.</param>
public class TooManyConcurrentRequestsException (string message) : BaseOverkizException (message)
	{
	}

/// <summary>
/// Raised when the Overkiz server returns HTTP 503 Service Unavailable.
/// Retry after a delay; the service may be temporarily overloaded.
/// </summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The API error message.</param>
public class ServiceUnavailableException (string message) : BaseOverkizException (message)
	{
	}

/// <summary>
/// Raised when the server returns a 503 response that indicates a scheduled maintenance window.
/// Inherits from <see cref="ServiceUnavailableException"/>.
/// </summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The API error message.</param>
public class MaintenanceException (string message) : ServiceUnavailableException (message)
	{
	}

/// <summary>Raised when the API requires an API key that was not provided.</summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The API error message.</param>
public class MissingAPIKeyException (string message) : BaseOverkizException (message)
	{
	}

/// <summary>
/// Raised when the request is missing the required authorization token header.
/// Ensure a valid bearer token or session cookie is attached to the request.
/// </summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The API error message.</param>
public class MissingAuthorizationTokenException (string message) : ResourceAccessDeniedException (message)
	{
	}

/// <summary>
/// Raised when the event listener ID supplied to a fetch or unregister call is not recognised by the server.
/// Re-register by calling <see cref="OverKizApi.OverkizClient.RegisterEventListener"/>.
/// </summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The API error message.</param>
public class InvalidEventListenerIdException (string message) : BaseOverkizException (message)
	{
	}

/// <summary>
/// Raised when a fetch or unregister call is made but no event listener has been registered for this session.
/// Call <see cref="OverKizApi.OverkizClient.RegisterEventListener"/> first.
/// </summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The API error message.</param>
public class NoRegisteredEventListenerException (string message) : BaseOverkizException (message)
	{
	}

/// <summary>
/// Raised when a request attempts to use both a JSESSIONID cookie and a Bearer token simultaneously,
/// which is not supported by the API.
/// </summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The API error message.</param>
public class SessionAndBearerInSameRequestException (string message) : BaseOverkizException (message)
	{
	}

/// <summary>
/// Raised when the account is temporarily banned after too many failed authentication attempts.
/// Wait before retrying; do not loop automatically.
/// </summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The API error message.</param>
public class TooManyAttemptsBannedException (string message) : BaseOverkizException (message)
	{
	}

/// <summary>Raised when a bearer token supplied to the API is malformed or has been revoked.</summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The API error message, which typically includes the offending token prefix.</param>
public class InvalidTokenException (string message) : BaseOverkizException (message)
	{
	}

/// <summary>Raised when a Rexel request requires a selected gateway but no gateway has been chosen.</summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The API error message.</param>
public class NoGatewaySelectedException (string message) : BaseOverkizException (message)
	{
	}

/// <summary>Raised when an operation is not supported by the current server, gateway, or endpoint.</summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The API error message.</param>
public class UnsupportedOperationException (string message) : BaseOverkizException (message)
	{
	}

/// <summary>Raised when no local token exists for the specified UUID.</summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The API error message, which typically includes the UUID.</param>
public class NotSuchTokenException (string message) : BaseOverkizException (message)
	{
	}

/// <summary>Raised when the specified user account does not exist in the Overkiz system.</summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The API error message.</param>
public class UnknownUserException (string message) : BaseOverkizException (message)
	{
	}

/// <summary>Raised when the API cannot find the requested object (device, gateway, etc.).</summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The API error message.</param>
public class UnknownObjectException (string message) : BaseOverkizException (message)
	{
	}

/// <summary>
/// Raised when the authenticated user does not have permission to control the target gateway.
/// This may occur when the gateway belongs to a different account.
/// </summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The API error message.</param>
public class AccessDeniedToGatewayException (string message) : ResourceAccessDeniedException (message)
	{
	}

/// <summary>
/// Raised when the client application is not permitted to access this setup.
/// This typically occurs when the setup's reseller has restricted API access to specific apps.
/// </summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The API error message.</param>
public class ApplicationNotAllowedException (string message) : ResourceAccessDeniedException (message)
	{
	}

// --- Nexity ---

/// <summary>
/// Raised when the credentials supplied to Nexity's AWS Cognito authentication endpoint are invalid.
/// </summary>
public class NexityBadCredentialsException : BadCredentialsException
	{
	/// <summary>Initialises a new instance with no message.</summary>
	public NexityBadCredentialsException () { }
	/// <summary>Initialises a new instance with the specified error <paramref name="message"/>.</summary>
	/// <param name="message">The Cognito error message.</param>
	public NexityBadCredentialsException (string message) : base (message) { }
	}

/// <summary>Raised when an unexpected error occurs while communicating with the Nexity identity API.</summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">Description of the Nexity service error.</param>
public class NexityServiceException (string message) : BaseOverkizException (message)
	{
	}

// --- CozyTouch ---

/// <summary>
/// Raised when the credentials supplied to the Atlantic CozyTouch OAuth endpoint are invalid.
/// </summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The CozyTouch OAuth error description.</param>
public class CozyTouchBadCredentialsException (string message) : BadCredentialsException (message)
	{
	}

/// <summary>Raised when an unexpected error occurs while communicating with the CozyTouch identity API.</summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">Description of the CozyTouch service error.</param>
public class CozyTouchServiceException (string message) : BaseOverkizException (message)
	{
	}

// --- Somfy ---

/// <summary>
/// Raised when the credentials supplied to the Somfy OAuth 2.0 token endpoint are invalid.
/// </summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">The Somfy OAuth error message.</param>
public class SomfyBadCredentialsException (string message) : BadCredentialsException (message)
	{
	}

/// <summary>Raised when an unexpected error occurs while communicating with the Somfy identity API.</summary>
/// <remarks>Initialises a new instance with the specified error <paramref name="message"/>.</remarks>
/// <param name="message">Description of the Somfy service error.</param>
public class SomfyServiceException (string message) : BaseOverkizException (message)
	{
	}
