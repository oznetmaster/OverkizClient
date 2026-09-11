// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace OverKizApi.Tests;

[TestFixture]
public sealed class ExecutionAndConfigurationTests
	{
	[Test]
	public async Task DeviceAction_PreservesCommandOrderAndParameterTypes ()
		{
		using var api = new ScriptedApi ();
		api.Expect ("POST", "exec/apply", "{\"execId\":\"execution-1\"}", inspect: (request, body) =>
		{
			Assert.That (request.Content!.Headers.ContentType!.MediaType, Is.EqualTo ("application/json"));
			JsonElement json = ScriptedApi.Json (body);
			Assert.That (json.GetProperty ("label").GetString (), Is.EqualTo ("Test action"));
			JsonElement action = json.GetProperty ("actions")[0];
			Assert.That (action.GetProperty ("deviceURL").GetString (), Is.EqualTo (ScriptedApi.DeviceUrl));
			JsonElement commands = action.GetProperty ("commands");
			Assert.That (commands.GetArrayLength (), Is.EqualTo (2));
			Assert.That (commands[0].GetProperty ("name").GetString (), Is.EqualTo ("setClosure"));
			Assert.That (commands[0].GetProperty ("parameters")[0].GetInt32 (), Is.EqualTo (45));
			Assert.That (commands[1].GetProperty ("name").GetString (), Is.EqualTo ("stop"));
			Assert.That (commands[1].TryGetProperty ("parameters", out _), Is.False);
		});
		string result = await api.Client.ExecuteDeviceAction (ScriptedApi.DeviceUrl,
			 new[] { new Command { Name = "setClosure", Parameters = new object?[] { 45 } }, new Command { Name = "stop" } }, "Test action");
		Assert.That (result, Is.EqualTo ("execution-1"));
		api.Complete ();
		}

	[Test]
	public async Task ScenarioExecutionAndCancellation_UseExpectedEndpoints ()
		{
		using var api = new ScriptedApi ();
		api.Expect ("POST", "exec/scenario-1", "{\"execId\":\"execution-1\"}");
		Assert.That (await api.Client.ExecuteScenario ("scenario-1"), Is.EqualTo ("execution-1"));
		api.Expect ("POST", "exec/schedule/scenario-1/1700000000123", "{\"triggerId\":\"trigger-1\"}");
		Assert.That (await api.Client.ExecuteScheduledScenario ("scenario-1", 1700000000123), Is.EqualTo ("trigger-1"));
		api.Expect ("DELETE", "exec/current/setup/execution-1", "", HttpStatusCode.NoContent);
		await api.Client.CancelExecution ("execution-1");
		api.Complete ();
		}

	[TestCase ("action", "exec/apply")]
	[TestCase ("scenario", "exec/scenario")]
	[TestCase ("schedule", "exec/schedule/scenario/123")]
	[TestCase ("generate", "config/gateway/local/tokens/generate")]
	[TestCase ("activate", "config/gateway/local/tokens")]
	public async Task MissingResponseIdentifiers_ThrowLibraryException (string operation, string path)
		{
		using var api = new ScriptedApi ();
		api.Expect (operation == "generate" ? "GET" : "POST", path, "{}");
		Func<Task> call = operation switch
			{
				"action" => async () => { await api.Client.ExecuteDeviceAction (ScriptedApi.DeviceUrl, new[] { new Command { Name = "stop" } }); }
				,
				"scenario" => async () => { await api.Client.ExecuteScenario ("scenario"); }
				,
				"schedule" => async () => { await api.Client.ExecuteScheduledScenario ("scenario", 123); }
				,
				"generate" => async () => { await api.Client.GenerateLocalToken ("gateway"); }
				,
				_ => async () => { await api.Client.ActivateLocalToken ("gateway", "token", "label"); }
				};
		await Assert.ThatAsync (call, Throws.TypeOf<OverkizException> ());
		api.Complete ();
		}

	[TestCase ("[]")]
	[TestCase ("null")]
	public async Task EmptyListEndpoints_ReturnEmptyCollections (string response)
		{
		using var api = new ScriptedApi ();
		api.Expect ("GET", "setup/gateways", response);
		Assert.That (await api.Client.GetGateways (), Is.Empty);
		api.Expect ("GET", "setup/devices", response);
		Assert.That (await api.Client.GetDevices (), Is.Empty);
		api.Expect ("GET", "exec/current", response);
		Assert.That (await api.Client.GetCurrentExecutions (), Is.Empty);
		api.Expect ("GET", "history/executions", response);
		Assert.That (await api.Client.GetExecutionHistory (), Is.Empty);
		api.Expect ("GET", "actionGroups", response);
		Assert.That (await api.Client.GetScenarios (), Is.Empty);
		api.Expect ("GET", "setup/options", response);
		Assert.That (await api.Client.GetSetupOptions (), Is.Empty);
		api.Complete ();
		}

	[Test]
	public async Task ExecutionLists_DeserializeHistoryAndScenarioCommands ()
		{
		using var api = new ScriptedApi ();
		api.Expect ("GET", "exec/current", "[{\"id\":\"running\",\"state\":\"IN_PROGRESS\"}]");
		Assert.That ((await api.Client.GetCurrentExecutions ()).Single ().Id, Is.EqualTo ("running"));
		api.Expect ("GET", "history/executions", """[{"id":"finished","state":"Completed","commands":[{"deviceURL":"io://test/device","command":"stop","rank":1}]}]""");
		HistoryExecution history = (await api.Client.GetExecutionHistory ()).Single ();
		Assert.That (history.State, Is.EqualTo (ExecutionState.Completed));
		Assert.That (history.Commands.Single ().DeviceUrl, Is.EqualTo ("io://test/device"));
		api.Expect ("GET", "actionGroups", """[{"oid":"scene","actions":[{"deviceURL":"io://test/device","commands":[{"name":"stop"}]}]}]""");
		Assert.That ((await api.Client.GetScenarios ()).Single ().Actions.Single ().Commands.Single ().Name, Is.EqualTo ("stop"));
		api.Complete ();
		}

	[Test]
	public async Task LocalTokenLifecycle_EncodesIdentifiersAndPreservesPayload ()
		{
		using var api = new ScriptedApi ();
		const string gateway = "hub/with space";
		const string prefix = "config/hub%2Fwith%20space/local/tokens";
		api.Expect ("GET", prefix + "/generate", "{\"token\":\"generated-token\"}");
		string token = await api.Client.GenerateLocalToken (gateway);
		Assert.That (token, Is.EqualTo ("generated-token"));
		api.Expect ("POST", prefix, "{\"requestId\":\"request-1\"}", inspect: (_, body) =>
		{
			JsonElement json = ScriptedApi.Json (body);
			Assert.That (json.GetProperty ("token").GetString (), Is.EqualTo (token));
			Assert.That (json.GetProperty ("label").GetString (), Is.EqualTo ("Test token"));
			Assert.That (json.GetProperty ("scope").GetString (), Is.EqualTo ("devmode"));
		});
		Assert.That (await api.Client.ActivateLocalToken (gateway, token, "Test token"), Is.EqualTo ("request-1"));
		api.Expect ("GET", prefix + "/scope%2Fone", "[{\"uuid\":\"id/one\",\"scope\":\"scope/one\",\"gatewayCreationTime\":\"123\"}]");
		LocalToken entry = (await api.Client.GetLocalTokens (gateway, "scope/one")).Single ();
		Assert.That (entry.GatewayCreationTime, Is.EqualTo (123));
		api.Expect ("DELETE", prefix + "/id%2Fone", "", HttpStatusCode.NoContent);
		Assert.That (await api.Client.DeleteLocalToken (gateway, entry.Uuid!), Is.True);
		api.Complete ();
		}

	[Test]
	public async Task DeveloperMode_ActivatesReadsAndDeactivates ()
		{
		using var api = new ScriptedApi ();
		const string endpoint = "setup/gateways/hub%2Fone/developerMode";
		api.Expect ("POST", endpoint, "", HttpStatusCode.NoContent);
		await api.Client.ActivateDeveloperMode ("hub/one");
		api.Expect ("GET", endpoint, "{\"active\":true}");
		Assert.That ((await api.Client.GetDeveloperMode ("hub/one")).Active, Is.True);
		api.Expect ("DELETE", endpoint, "", HttpStatusCode.NoContent);
		await api.Client.DeactivateDeveloperMode ("hub/one");
		api.Complete ();
		}

	[TestCase ("")]
	[TestCase (" ")]
	public async Task OpenPairing_EmptyBodyIsNull (string response)
		{
		using var api = new ScriptedApi ();
		api.Expect ("POST", "config/hub%2Fone/local/openPairing", response);
		Assert.That (await api.Client.OpenLocalPairing ("hub/one"), Is.Null);
		api.Complete ();
		}

	[Test]
	public async Task OpenPairing_ReturnedJsonRemainsUsableAfterResponseDisposal ()
		{
		using var api = new ScriptedApi ();
		api.Expect ("POST", "config/hub/local/openPairing", "{\"duration\":180}");
		JsonElement? result = await api.Client.OpenLocalPairing ("hub");
		Assert.That (result!.Value.GetProperty ("duration").GetInt32 (), Is.EqualTo (180));
		api.Complete ();
		}

	[Test]
	public async Task RefreshDeviceStates_AcceptsNoContentResponse ()
		{
		using var api = new ScriptedApi ();
		api.Expect ("POST", "setup/devices/states/refresh", "", HttpStatusCode.NoContent);
		await api.Client.RefreshAllDeviceStates ();
		api.Complete ();
		}
	}