// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

using OverKizApi.TestSupport;

namespace OverKizApi.Tests;

[TestFixture]
public sealed class LiveTestConfigurationTests
	{
	[TestCase (false, "", false)]
	[TestCase (true, "", true)]
	[TestCase (false, "true", true)]
	[TestCase (true, "false", false)]
	public void RunOverride_TakesPrecedenceWithoutChangingSavedFlag (bool saved, string parameter, bool expected)
		{
		var settings = new LiveTestSettings { Enabled = saved };
		Assert.That (LiveTestSupport.IsEnabled (settings, parameter), Is.EqualTo (expected));
		Assert.That (settings.Enabled, Is.EqualTo (saved));
		}

	[Test]
	public void MissingSettings_DefaultToDisabled () => Assert.That (LiveTestSupport.IsEnabled (null, ""), Is.False);

	[Test]
	public void InvalidOverride_FailsClosed () => Assert.That (() => LiveTestSupport.IsEnabled (new LiveTestSettings { Enabled = true }, "yes"), Throws.TypeOf<InvalidDataException> ());

	[Test]
	public void SuppliedInputDirectory_DoesNotFallBackToDesktopCredentials () =>
		Assert.That (LiveTestSupport.SettingsPath ("inputs"), Is.EqualTo (Path.Combine ("inputs", "LiveTestSettings.json")));

	[Test]
	public void ConsoleCredentialUpdates_PreserveLocalFlagAndTimeout ()
		{
		string directory = Path.Combine (Path.GetTempPath (), "OverkizSettingsTests", Guid.NewGuid ().ToString ("N"));
		string path = Path.Combine (directory, "LiveTestSettings.json");
		try
			{
			LiveTestSettingsStore.SaveLocalCredentials ("gateway.example.invalid", "synthetic-token", path);
			LiveTestSettings initial = LiveTestSettingsStore.Load (path)!;
			Assert.That (initial.Enabled, Is.False);
			Assert.That (initial.Token, Is.EqualTo ("synthetic-token"));
			File.WriteAllText (path, "{\"enabled\":true,\"timeoutSeconds\":30}");
			LiveTestSettingsStore.SaveLocalCredentials ("new.example.invalid", "updated-synthetic-token", path);
			LiveTestSettings updated = LiveTestSettingsStore.Load (path)!;
			Assert.That (updated.Enabled, Is.True);
			Assert.That (updated.TimeoutSeconds, Is.EqualTo (30));
			Assert.That (updated.GatewayHost, Is.EqualTo ("new.example.invalid"));
			Assert.That (updated.Token, Is.EqualTo ("updated-synthetic-token"));
			}
		finally
			{
			if (File.Exists (path))
				File.Delete (path);
			if (Directory.Exists (directory))
				Directory.Delete (directory);
			}
		}
	}