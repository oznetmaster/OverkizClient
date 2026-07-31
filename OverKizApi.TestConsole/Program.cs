// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using OverKizApi;
using OverKizApi.Enums;
using OverKizApi.Exceptions;
using OverKizApi.Models;

namespace OverKizApi.TestConsole;

internal static class Program
	{
	private static async Task Main ()
		{
		Console.OutputEncoding = System.Text.Encoding.UTF8;
		Console.WriteLine ("=== OverKiz API Test Console ===");
		Console.WriteLine ();

		// ── Credentials ──────────────────────────────────────────────────
		Console.Write ("Connection mode:");
		Console.WriteLine ();

		Dictionary<int, Server> serverChoices = new ()
			{
			[1] = Server.SomfyEurope,
			[2] = Server.AtlanticCozytouch,
			[3] = Server.Nexity,
			[4] = Server.SomfyAmerica,
			[5] = Server.SomfyOceania,
			[6] = Server.Flexom,
			[7] = Server.Rexel,
			};

		foreach ((int key, Server value) in serverChoices)
			Console.WriteLine ($"  {key}. {value} (cloud)");
		Console.WriteLine ("  L. Local gateway (direct LAN connection)");

		Console.Write ("> ");
		string modeInput = Console.ReadLine ()?.Trim () ?? string.Empty;

		bool isLocalMode = modeInput.Equals ("L", StringComparison.OrdinalIgnoreCase);
		Server selectedServer = Server.SomfyEurope; // default; unused in local mode

		if (!isLocalMode)
			{
			if (!int.TryParse (modeInput, out int serverChoice) || !serverChoices.TryGetValue (serverChoice, out selectedServer))
				{
				Console.WriteLine ("Invalid selection. Defaulting to SomfyEurope.");
				selectedServer = Server.SomfyEurope;
				}
			}

		// ── Credentials / connection details ──────────────────────────────

		OverkizClient client;
		HttpClient? localHttpClient = null;
		bool isRexel = !isLocalMode && selectedServer == Server.Rexel;
		string cloudIdentity = string.Empty;

		if (isLocalMode)
			{
			// ── Local gateway mode ─────────────────────────────────────
			SavedCredential? savedLocal = CredentialStore.LoadLocal ();

			string gatewayIp;
			if (savedLocal is not null)
				{
				Console.Write ($"Gateway IP [{savedLocal.Username}]: ");
				string ipInput = Console.ReadLine ()?.Trim () ?? string.Empty;
				gatewayIp = string.IsNullOrEmpty (ipInput) ? savedLocal.Username : ipInput;
				}
			else
				{
				Console.Write ("Gateway IP (e.g. 192.168.1.xxx): ");
				gatewayIp = Console.ReadLine ()?.Trim () ?? string.Empty;
				}

			Console.Write (savedLocal is not null ? "Local token [saved] (press Enter to keep): " : "Local token: ");
			string typedToken = ReadPassword ();
			string localToken = (savedLocal is not null && string.IsNullOrEmpty (typedToken))
				? savedLocal.Password
				: typedToken;

			if (string.IsNullOrEmpty (gatewayIp) || string.IsNullOrEmpty (localToken))
				{
				Console.WriteLine ("Gateway IP and token are required.");
				return;
				}

			OverkizServer localServer = OverkizConst.LocalServer (gatewayIp);

			Console.WriteLine ();
			Console.WriteLine ($"Connecting to {localServer.Name}...");

			// Gateways use a self-signed TLS certificate on port 8443 — bypass certificate validation for the local connection.
			HttpClientHandler localHandler = OverkizConst.CreateLocalHttpClientHandler ();
			localHttpClient = new HttpClient (localHandler);

			client = new OverkizClient (string.Empty, string.Empty, localServer, token: localToken, httpClient: localHttpClient);

			Console.WriteLine ("Local connection established (no cloud login required).");
			CredentialStore.SaveLocal (new SavedCredential { Username = gatewayIp, Password = localToken });
			Console.WriteLine ("(Connection details saved for next run.)");
			Console.WriteLine ();
			}
		else
			{
			// ── Cloud mode ─────────────────────────────────────────────
			SavedCredential? saved = CredentialStore.Load (selectedServer);

			string username;
			if (saved is not null)
				{
				Console.Write (isRexel ? "Rexel access token [saved] (press Enter to keep): " : $"Username [{saved.Username}]: ");
				string input2 = (isRexel ? ReadPassword () : Console.ReadLine ())?.Trim () ?? string.Empty;
				username = string.IsNullOrEmpty (input2) ? saved.Username : input2;
				}
			else
				{
				Console.Write (isRexel ? "Rexel access token: " : "Username: ");
				username = (isRexel ? ReadPassword () : Console.ReadLine ())?.Trim () ?? string.Empty;
				}

			string password;
			if (isRexel)
				{
				password = saved?.Password ?? string.Empty;
				}
			else
				{
				Console.Write (saved is not null ? "Password [saved] (press Enter to keep): " : "Password: ");
				string typedPassword = ReadPassword ();
				password = saved is not null && string.IsNullOrEmpty (typedPassword)
					? saved.Password
					: typedPassword;
				}

			if (string.IsNullOrEmpty (username) || (!isRexel && string.IsNullOrEmpty (password)))
				{
				Console.WriteLine (isRexel ? "Rexel access token is required." : "Username and password are required.");
				return;
				}

			OverkizServer server = OverkizConst.SupportedServers[selectedServer];

			Console.WriteLine ();
			Console.WriteLine ($"Connecting to {server.Name} ({server.Endpoint})...");

			cloudIdentity = username;

			client = isRexel
				? new OverkizClient (string.Empty, string.Empty, server, token: username)
				: new OverkizClient (username, password, server);
			}

		await using (client)
			{
			try
				{
				if (!isLocalMode)
					{
					// ── Login (cloud only) ─────────────────────────────────────
					Console.WriteLine ("Logging in...");
					_ = await client.Login ();
					Console.WriteLine ("Login successful.");

					// Save credentials after successful login
					CredentialStore.Save (selectedServer, new SavedCredential
						{
						Username = isRexel ? cloudIdentity : client.Username,
						Password = isRexel ? "RexelBearerToken" : client.Password
						});
					Console.WriteLine ("(Credentials saved for next run.)");
					Console.WriteLine ();
					}

				// ── Main menu loop ─────────────────────────────────────────
				bool running = true;
				while (running)
					{
					Console.WriteLine ("Choose an action:");
					Console.WriteLine ("  1. Get setup summary");
					Console.WriteLine ("  2. List devices");
					Console.WriteLine ("  3. List gateways");
					Console.WriteLine ("  4. List scenarios");
					Console.WriteLine ("  5. Get device states");
					Console.WriteLine ("  6. Refresh all device states");
					Console.WriteLine ("  7. Get execution history");
					Console.WriteLine ("  8. Get current executions");
					Console.WriteLine ("  9. List local tokens");
					Console.WriteLine ("  R. Discover/select Rexel gateways");
					Console.WriteLine ("  A. Control device");
					Console.WriteLine ("  E. Watch events (live polling)");
					Console.WriteLine ("  0. Exit");
					Console.Write ("> ");

					string input = Console.ReadLine ()?.Trim () ?? string.Empty;
					Console.WriteLine ();

					switch (input)
						{
						case "1":
							await RunAction (() => ShowSetupSummary (client));
							break;
						case "2":
							await RunAction (() => ListDevices (client));
							break;
						case "3":
							await RunAction (() => ListGateways (client));
							break;
						case "4":
							await RunAction (() => ListScenarios (client));
							break;
						case "5":
							await RunAction (() => GetDeviceStates (client));
							break;
						case "6":
							await RunAction (() => RefreshStates (client));
							break;
						case "7":
							await RunAction (() => ShowExecutionHistory (client));
							break;
						case "8":
							await RunAction (() => ShowCurrentExecutions (client));
							break;
						case "9":
							await RunAction (() => ListLocalTokens (client));
							break;
						case "r":
						case "R":
							await RunAction (() => SelectRexelGateway (client));
							break;
						case "a":
						case "A":
							await RunAction (() => ControlDevice (client));
							break;
						case "e":
						case "E":
							await RunAction (() => WatchEvents (client));
							break;
						case "0":
							running = false;
							break;
						default:
							Console.WriteLine ("Unknown option.");
							break;
						}

					Console.WriteLine ();
					}
				}
			catch (BadCredentialsException ex)
				{
				Console.WriteLine ($"[AUTH ERROR] {ex.Message}");
				}
			catch (OverkizException ex)
				{
				Console.WriteLine ($"[API ERROR] {ex.GetType ().Name}: {ex.Message}");
				}
			catch (Exception ex)
				{
				Console.WriteLine ($"[ERROR] {ex.GetType ().Name}: {ex.Message}");
				}
			}

		localHttpClient?.Dispose ();

		Console.WriteLine ("Goodbye.");
		}

	// ── Menu actions ──────────────────────────────────────────────────────

	private static async Task RunAction (Func<Task> action)
		{
		try
			{
			await action ();
			}
		catch (OverkizException ex)
			{
			Console.WriteLine ($"[API ERROR] {ex.GetType ().Name}: {ex.Message}");
			}
		catch (Exception ex)
			{
			Console.WriteLine ($"[ERROR] {ex.GetType ().Name}: {ex.Message}");
			}
		}

	private static async Task ShowSetupSummary (OverkizClient client)
		{
		Setup setup = await client.GetSetup ();
		Console.WriteLine ($"Setup ID   : {setup.Id}");
		Console.WriteLine ($"Gateways   : {setup.Gateways.Count}");
		Console.WriteLine ($"Devices    : {setup.Devices.Count}");
		Console.WriteLine ($"Zones      : {setup.Zones?.Count ?? 0}");

		foreach (Gateway gw in setup.Gateways)
			Console.WriteLine ($"  GW {gw.GatewayId}  updateStatus={gw.UpdateStatus}");

		if (setup.Location is not null)
			Console.WriteLine ($"Location   : lat={setup.Location.Latitude}, lon={setup.Location.Longitude}, tz={setup.Location.Timezone}");
		}

	private static async Task ListDevices (OverkizClient client)
		{
		IReadOnlyList<Device> devices = await client.GetDevices ();
		if (devices.Count == 0)
			{
			Console.WriteLine ("No devices found.");
			return;
			}

		Console.WriteLine ($"{"Label",-40} {"Type",-10} {"Class",-30} {"URL"}");
		Console.WriteLine (new string ('-', 110));

		foreach (Device d in devices.OrderBy (d => d.Label))
			Console.WriteLine ($"{d.Label,-40} {d.Type,-10} {d.UiClass,-30} {d.DeviceUrl}");
		}

	private static async Task ListGateways (OverkizClient client)
		{
		IReadOnlyList<Gateway> gateways = await client.GetGateways ();
		if (gateways.Count == 0)
			{
			Console.WriteLine ("No gateways found.");
			return;
			}

		foreach (Gateway gw in gateways)
			{
			Console.WriteLine ($"ID           : {gw.GatewayId}");
			Console.WriteLine ($"SubType      : {gw.SubType}");
			Console.WriteLine ($"Status       : {gw.Connectivity?.Status}");
			Console.WriteLine ($"Alive        : {gw.Alive}");
			Console.WriteLine ($"UpToDate     : {gw.UpToDate}");
			Console.WriteLine ($"UpdateStatus : {gw.UpdateStatus}");
			Console.WriteLine ();
			}
		}

	private static async Task ListScenarios (OverkizClient client)
		{
		IReadOnlyList<Scenario> scenarios = await client.GetScenarios ();
		if (scenarios.Count == 0)
			{
			Console.WriteLine ("No scenarios found.");
			return;
			}

		Console.WriteLine ($"{"Label",-40} {"OID"}");
		Console.WriteLine (new string ('-', 80));

		foreach (Scenario s in scenarios.OrderBy (s => s.Label))
			Console.WriteLine ($"{s.Label,-40} {s.Oid}");
		}

	private static async Task GetDeviceStates (OverkizClient client)
		{
		Console.Write ("Device URL: ");
		string url = Console.ReadLine ()?.Trim () ?? string.Empty;

		if (string.IsNullOrEmpty (url))
			{
			Console.WriteLine ("No URL entered.");
			return;
			}

		IReadOnlyList<State> states = await client.GetDeviceStates (url);
		if (states.Count == 0)
			{
			Console.WriteLine ("No states returned.");
			return;
			}

		Console.WriteLine ($"{"State",-50} {"Type",-10} Value");
		Console.WriteLine (new string ('-', 90));

		foreach (State s in states.OrderBy (s => s.Name))
			Console.WriteLine ($"{s.Name,-50} {s.Type,-10} {s.Value}");
		}

	private static async Task RefreshStates (OverkizClient client)
		{
		await client.RefreshAllDeviceStates ();
		Console.WriteLine ("Refresh request sent.");
		}

	private static async Task ShowExecutionHistory (OverkizClient client)
		{
		IReadOnlyList<HistoryExecution> history = await client.GetExecutionHistory ();
		if (history.Count == 0)
			{
			Console.WriteLine ("No execution history.");
			return;
			}

		Console.WriteLine ($"{"Time",-22} {"State",-12} {"Source",-14} Label");
		Console.WriteLine (new string ('-', 90));

		foreach (HistoryExecution h in history.Take (20))
			{
			DateTimeOffset t = DateTimeOffset.FromUnixTimeMilliseconds (h.EventTime).ToLocalTime ();
			Console.WriteLine ($"{t:yyyy-MM-dd HH:mm:ss}  {h.State,-12} {h.Source,-14} {h.Label}");
			}
		}

	private static async Task ShowCurrentExecutions (OverkizClient client)
		{
		IReadOnlyList<Execution> execs = await client.GetCurrentExecutions ();
		if (execs.Count == 0)
			{
			Console.WriteLine ("No active executions.");
			return;
			}

		foreach (Execution e in execs)
			Console.WriteLine ($"  [{e.State}] {e.Description} (owner: {e.Owner}, id: {e.Id})");
		}

	private static async Task ControlDevice (OverkizClient client)
		{
		// Pick a device from the list
		IReadOnlyList<Device> devices = await client.GetDevices ();
		if (devices.Count == 0)
			{
			Console.WriteLine ("No devices found.");
			return;
			}

		Console.WriteLine ($"{"#",-4} {"Label",-40} {"URL"}");
		Console.WriteLine (new string ('-', 90));
		var actuators = devices.Where (d => d.Type == ProductType.Actuator).OrderBy (d => d.Label).ToList ();
		for (int i = 0; i < actuators.Count; i++)
			Console.WriteLine ($"{i,-4} {actuators[i].Label,-40} {actuators[i].DeviceUrl}");

		Console.Write ("Device #: ");
		if (!int.TryParse (Console.ReadLine ()?.Trim (), out int pick) || pick < 0 || pick >= actuators.Count)
			{
			Console.WriteLine ("Invalid selection.");
			return;
			}

		Device device = actuators[pick];

		// Show available commands
		IReadOnlyList<CommandDefinition> cmds = device.Definition?.Commands ?? [];
		if (cmds.Count == 0)
			{
			Console.WriteLine ("This device has no defined commands.");
			return;
			}

		Console.WriteLine ();
		Console.WriteLine ($"Commands for '{device.Label}':");
		for (int i = 0; i < cmds.Count; i++)
			Console.WriteLine ($"  {i,-4} {cmds[i].CommandName} ({cmds[i].NParams} param{(cmds[i].NParams == 1 ? "" : "s")})");

		Console.Write ("Command #: ");
		if (!int.TryParse (Console.ReadLine ()?.Trim (), out int cmdPick) || cmdPick < 0 || cmdPick >= cmds.Count)
			{
			Console.WriteLine ("Invalid selection.");
			return;
			}

		CommandDefinition chosen = cmds[cmdPick];
		List<object?> parameters = [];

		for (int i = 0; i < chosen.NParams; i++)
			{
			Console.Write ($"  Parameter {i + 1}: ");
			string raw = Console.ReadLine ()?.Trim () ?? string.Empty;
			// Try int, then double, then leave as string
			if (int.TryParse (raw, out int iv))
				{
				parameters.Add (iv);
				}
			else if (double.TryParse (raw, System.Globalization.NumberStyles.Any,
													 System.Globalization.CultureInfo.InvariantCulture, out double dv))
				{
				parameters.Add (dv);
				}
			else
				{
				parameters.Add (raw);
				}
			}

		Command command = new ()
			{
			Name = chosen.CommandName!,
			Parameters = parameters.Count > 0 ? (IReadOnlyList<object?>)parameters : null
			};

		Console.WriteLine ($"Sending '{chosen.CommandName}' to '{device.Label}'...");
		string execId = await client.ExecuteDeviceAction (device.DeviceUrl!, [command], $"Test: {chosen.CommandName}");
		Console.WriteLine ($"Execution started. execId={execId}");
		}

	private static async Task WatchEvents (OverkizClient client)
		{
		const int POLL_MS = 2_000;

		Console.WriteLine ("Registering event listener...");
		await client.RegisterEventListener ();
		Console.WriteLine ($"Listener ID : {client.EventListenerId}");
		Console.WriteLine ("Polling for events every 2 s — press any key to stop.");
		Console.WriteLine (new string ('-', 90));

		using var cts = new System.Threading.CancellationTokenSource ();

		// Background thread: wait for a keypress then cancel.
		_ = Task.Run (() =>
			{
				try
					{
					_ = Console.ReadKey (intercept: true);
					}
				catch
					{
					}

				cts.Cancel ();
			});

		try
			{
			while (!cts.Token.IsCancellationRequested)
				{
				await Task.Delay (POLL_MS, cts.Token);

				(IReadOnlyList<EventObject>? events, string? rawJson) = await client.FetchEventsRaw ();

				if (events.Count == 0 && rawJson.Length > 2)
					Console.WriteLine ($"[RAW] {rawJson}");

				foreach (EventObject ev in events)
					PrintEvent (ev);
				}
			}
		catch (OperationCanceledException)
			{
			}
		finally
			{
			Console.WriteLine (new string ('-', 90));
			Console.WriteLine ("Unregistering event listener...");
			try
				{
				await client.UnregisterEventListener ();
				}
			catch (Exception ex)
				{
				Console.WriteLine ($"[WARN] Unregister failed: {ex.Message}");
				}

			Console.WriteLine ("Done.");
			}
		}

	private static void PrintEvent (EventObject ev)
		{
		DateTimeOffset ts = ev.Timestamp.HasValue
			? DateTimeOffset.FromUnixTimeMilliseconds (ev.Timestamp.Value).ToLocalTime ()
			: DateTimeOffset.Now;

		string name = ev.Name ?? "(unknown)";
		string device = ev.DeviceUrl ?? ev.GatewayId ?? string.Empty;

		Console.Write ($"{ts:HH:mm:ss.fff}  {name,-35}");

		if (!string.IsNullOrEmpty (device))
			Console.Write ($"  {device}");

		// State changes — list each changed state on its own indented line.
		if (ev.DeviceStates.Count > 0)
			{
			Console.WriteLine ();
			foreach (EventState s in ev.DeviceStates)
				Console.WriteLine ($"              {s.Name,-45} = {s.Value}");
			}
		else if (!string.IsNullOrEmpty (ev.Label))
			{
			Console.WriteLine ($"  label='{ev.Label}'");
			}
		else
			{
			// Extra context for availability / execution events.
			var extras = new System.Text.StringBuilder ();
			if (ev.SubType.HasValue)
				{
				_ = extras.Append (string.Format (CultureInfo.CurrentCulture, "  subType={0}", ev.SubType));
				}

			if (ev.NewState.HasValue)
				{
				_ = extras.Append (string.Format (CultureInfo.CurrentCulture, "  newState={0}", ev.NewState));
				}

			if (ev.OldState.HasValue)
				{
				_ = extras.Append (string.Format (CultureInfo.CurrentCulture, "  oldState={0}", ev.OldState));
				}

			if (ev.ExecId != null)
				{
				_ = extras.Append (string.Format (CultureInfo.CurrentCulture, "  execId={0}", ev.ExecId));
				}

			Console.WriteLine (extras.ToString ());
			}
		}

	private static async Task ListLocalTokens (OverkizClient client)
		{
		if (client.Server == OverkizConst.SupportedServers[Server.Rexel])
			{
			Console.WriteLine ("Local tokens are not supported for Rexel cloud connections.");
			return;
			}

		Setup setup = await client.GetSetup ();
		if (setup.Gateways.Count == 0)
			{
			Console.WriteLine ("No gateways in setup.");
			return;
			}

		foreach (Gateway gw in setup.Gateways)
			{
			Console.WriteLine ($"Gateway: {gw.GatewayId}");
			IReadOnlyList<LocalToken> tokens = await client.GetLocalTokens (gw.GatewayId!);

			if (tokens.Count == 0)
				{
				Console.WriteLine ("  No local tokens.");
				continue;
				}

			foreach (LocalToken t in tokens)
				Console.WriteLine ($"  [{t.Scope}] {t.Label} — {t.Uuid}");
			}
		}

	private static async Task SelectRexelGateway (OverkizClient client)
		{
		if (client.Server != OverkizConst.SupportedServers[Server.Rexel])
			{
			Console.WriteLine ("Gateway selection is only used for Rexel connections.");
			return;
			}

		IReadOnlyList<GatewayCandidate> gateways = await client.DiscoverRexelGateways ();
		if (gateways.Count == 0)
			{
			Console.WriteLine ("No Rexel gateways were discovered for this token.");
			return;
			}

		for (int i = 0; i < gateways.Count; i++)
			{
			GatewayCandidate gateway = gateways[i];
			Console.WriteLine ($"{i + 1}. gatewayId={gateway.GatewayId}  homeId={gateway.HomeId}  label={gateway.Label ?? "(none)"}  externalId={gateway.ExternalId ?? "(none)"}");
			}

		if (gateways.Count == 1)
			{
			client.SelectRexelGateway (gateways[0].GatewayId);
			Console.WriteLine ($"Auto-selected sole Rexel gateway: {client.SelectedGatewayId}");
			return;
			}

		Console.Write ("Select Rexel gateway number: ");
		string input = Console.ReadLine ()?.Trim () ?? string.Empty;
		if (!int.TryParse (input, out int selectedIndex) || selectedIndex < 1 || selectedIndex > gateways.Count)
			{
			Console.WriteLine ("Invalid selection.");
			return;
			}

		client.SelectRexelGateway (gateways[selectedIndex - 1].GatewayId);
		Console.WriteLine ($"Selected Rexel gateway: {client.SelectedGatewayId}");
		}

	// ── Helpers ───────────────────────────────────────────────────────────

	/// <summary>Reads a password from the console without echoing characters.</summary>
	private static string ReadPassword ()
		{
		// When stdin is redirected (e.g. piped input) ReadKey throws; fall back to ReadLine.
		if (Console.IsInputRedirected)
			return Console.ReadLine ()?.Trim () ?? string.Empty;

		System.Text.StringBuilder sb = new ();
		ConsoleKeyInfo key;

		do
			{
			key = Console.ReadKey (intercept: true);

			if (key.Key == ConsoleKey.Backspace && sb.Length > 0)
				{
				_ = sb.Remove (sb.Length - 1, 1);
				Console.Write ("\b \b");
				}
			else if (key.Key is not ConsoleKey.Enter and not ConsoleKey.Backspace)
				{
				_ = sb.Append (key.KeyChar);
				Console.Write ('*');
				}
			}
		while (key.Key is not ConsoleKey.Enter);

		Console.WriteLine ();
		return sb.ToString ();
		}
	}

