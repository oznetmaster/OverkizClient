// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace OverKizApi.Tests;

[TestFixture]
public sealed class ErrorHandlingTests
	{
	[TestCase ("Bad credentials.", typeof (BadCredentialsException))]
	[TestCase ("Your account has been temporarily locked.", typeof (TooManyAttemptsBannedException))]
	[TestCase ("Not authenticated", typeof (NotAuthenticatedException))]
	[TestCase ("An API key is required to access this setup", typeof (MissingAPIKeyException))]
	[TestCase ("Missing authorization token", typeof (MissingAuthorizationTokenException))]
	[TestCase ("Server busy, please try again later. (Too many executions)", typeof (TooManyExecutionsException))]
	[TestCase ("No such command: fly", typeof (InvalidCommandException))]
	[TestCase ("Invalid event listener id: missing", typeof (InvalidEventListenerIdException))]
	[TestCase ("No registered event listener", typeof (NoRegisteredEventListenerException))]
	[TestCase ("No such user account: test", typeof (UnknownUserException))]
	[TestCase ("No such resource", typeof (NoSuchResourceException))]
	[TestCase ("too many concurrent requests", typeof (TooManyConcurrentRequestsException))]
	[TestCase ("Execution queue is full on gateway test", typeof (ExecutionQueueFullException))]
	[TestCase ("Cannot use JSESSIONID and bearer token in same request", typeof (SessionAndBearerInSameRequestException))]
	[TestCase ("Too many attempts with an invalid token, temporarily banned", typeof (TooManyAttemptsBannedException))]
	[TestCase ("Invalid token : test", typeof (InvalidTokenException))]
	[TestCase ("Not such token with UUID: test", typeof (NotSuchTokenException))]
	[TestCase ("Unknown user : test", typeof (UnknownUserException))]
	[TestCase ("Unknown object", typeof (UnknownObjectException))]
	[TestCase ("Access denied to gateway test", typeof (AccessDeniedToGatewayException))]
	[TestCase ("Your setup cannot be accessed through this application", typeof (ApplicationNotAllowedException))]
	[TestCase ("Operation NOT SUPPORTED", typeof (UnsupportedOperationException))]
	[TestCase ("Another action already exists for device test", typeof (DuplicateActionOnDeviceException))]
	[TestCase ("No action group setup found: test", typeof (ActionGroupSetupNotFoundException))]
	[TestCase ("An unfamiliar server error", typeof (OverkizException))]
	public async Task StructuredErrors_PreserveExceptionTypeAndMessage (string message, Type exception)
		{
		using var api = new ScriptedApi ();
		api.Expect ("GET", "setup/devices", JsonSerializer.Serialize (new
			{
			error = message
			}), HttpStatusCode.BadRequest);
		await Assert.ThatAsync (() => api.Client.GetDevices (), Throws.TypeOf (exception).With.Message.EqualTo (message));
		api.Complete ();
		}

	[TestCase (503, "Maintenance window", typeof (MaintenanceException))]
	[TestCase (503, "unavailable", typeof (ServiceUnavailableException))]
	[TestCase (429, "<html>rate limited</html>", typeof (TooManyRequestsException))]
	[TestCase (500, "<html>server error</html>", typeof (HttpRequestException))]
	[TestCase (400, "null", typeof (HttpRequestException))]
	[TestCase (400, "{}", typeof (OverkizException))]
	public async Task StatusErrors_HandleNonJsonAndMissingErrorFields (int status, string body, Type exception)
		{
		using var api = new ScriptedApi ();
		api.Expect ("GET", "setup/devices", body, (HttpStatusCode)status);
		await Assert.ThatAsync (() => api.Client.GetDevices (), Throws.TypeOf (exception));
		api.Complete ();
		}

	[TestCase ("POST")]
	[TestCase ("DELETE")]
	public async Task MutatingRequests_UseTheSameErrorMapping (string method)
		{
		using var api = new ScriptedApi ();
		api.Expect (method, method == "POST" ? "setup/devices/states/refresh" : "exec/current/setup/execution",
			 "{\"error\":\"No such resource\"}", HttpStatusCode.NotFound);
		await Assert.ThatAsync (() => method == "POST" ? api.Client.RefreshAllDeviceStates () : api.Client.CancelExecution ("execution"),
			 Throws.TypeOf<NoSuchResourceException> ());
		api.Complete ();
		}
	}