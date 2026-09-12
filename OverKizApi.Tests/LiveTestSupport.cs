// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

using OverKizApi.TestSupport;

namespace OverKizApi.Tests;

internal static class LiveTestSupport
	{
	internal static LiveTestSettings LoadForRun ()
		{
		string path = SettingsPath (TestContext.Parameters.Get ("TestDataDirectory", ""));
		string enableOverride = TestContext.Parameters.Get ("EnableLiveTests", "");
		// An explicit false prevents even reading credentials for this run.
		if (enableOverride.Equals ("false", StringComparison.OrdinalIgnoreCase))
			Assert.Ignore ("Local API live tests are disabled for this run.");

		LiveTestSettings? settings = LiveTestSettingsStore.Load (path);
		if (!IsEnabled (settings, enableOverride))
			Assert.Ignore ("Local API live tests are disabled. Set enabled=true in the shared LiveTestSettings.json or explicitly supply EnableLiveTests=true.");
		if (settings is null || string.IsNullOrWhiteSpace (settings.GatewayHost) || string.IsNullOrWhiteSpace (settings.Token))
			throw new InvalidDataException ("Live tests require the console's saved local gateway and token in LiveTestSettings.json. Open the console in local mode or supply that file through TestDataDirectory.");
		if (settings.TimeoutSeconds < 1 || settings.TimeoutSeconds > 120)
			throw new InvalidDataException ("Live test timeoutSeconds must be between 1 and 120.");
		return settings;
		}

	internal static string SettingsPath (string? directory) => string.IsNullOrWhiteSpace (directory)
		? LiveTestSettingsStore.DefaultPath : Path.Combine (directory!, "LiveTestSettings.json");

	internal static bool IsEnabled (LiveTestSettings? settings, string? enableOverride)
		{
		if (string.IsNullOrWhiteSpace (enableOverride))
			return settings?.Enabled is true;
		if (!bool.TryParse (enableOverride, out bool enabled))
			throw new InvalidDataException ("EnableLiveTests must be true or false.");
		return enabled;
		}
	}