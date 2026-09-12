// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using OverKizApi.TestSupport;

namespace OverKizApi.Tests;

/// <summary>Opt-in checks against a local gateway using the console's existing token.</summary>
[TestFixture]
[Category ("Live")]
[NonParallelizable]
public sealed class LiveLocalApiTests
	{
	private LiveTestSettings _settings = null!;
	private HttpClient? _http;
	private OverkizClient _client = null!;

	[OneTimeSetUp]
	public void RequireExplicitOptIn () => _settings = LiveTestSupport.LoadForRun ();

	[SetUp]
	public void ConnectUsingSavedLocalToken ()
		{
		_http = new HttpClient (OverkizConst.CreateLocalHttpClientHandler ())
			{ Timeout = TimeSpan.FromSeconds (_settings.TimeoutSeconds) };
		_client = new OverkizClient (string.Empty, string.Empty, OverkizConst.LocalServer (_settings.GatewayHost), _settings.Token, _http);
		}

	[TearDown]
	public async Task ReleaseClient ()
		{
		try
			{
			if (_client is not null)
				await _client.DisposeAsync ();
			}
		finally
			{
			_http?.Dispose ();
			_http = null;
			_client = null!;
			}
		}

	[Test]
	public async Task SavedToken_AuthenticatesWithoutCreatingAnotherToken ()
		{
		Assert.That (await _client.Login (false), Is.True);
		Assert.That (_client.ApiType, Is.EqualTo (APIType.Local));
		Assert.That (_client.EventListenerId, Is.Null);
		}

	[Test]
	public async Task Gateways_ReturnIdentifiableModels ()
		{
		IReadOnlyList<Gateway> gateways = await _client.GetGateways ();
		Assert.That (gateways, Is.Not.Empty);
		Assert.That (gateways.All (gateway => !string.IsNullOrWhiteSpace (gateway.GatewayId)), Is.True);
		}

	[Test]
	public async Task Setup_DeserializesGatewayAndDeviceCollections ()
		{
		Setup setup = await _client.GetSetup (refresh: true);
		Assert.That (setup.Gateways, Is.Not.Empty);
		Assert.That (setup.Devices, Is.Not.Null);
		}

	[Test]
	public async Task Devices_HaveUniqueUsableUrls ()
		{
		IReadOnlyList<Device> devices = await _client.GetDevices ();
		Assert.That (devices.All (device => !string.IsNullOrWhiteSpace (device.DeviceUrl)), Is.True);
		Assert.That (devices.Select (device => device.DeviceUrl).Distinct ().Count (), Is.EqualTo (devices.Count));
		}

	[Test]
	public async Task DeviceAndStates_CanBeReadWithoutCommands ()
		{
		Device? listed = (await _client.GetDevices ()).OrderBy (device => device.DeviceUrl, StringComparer.Ordinal).FirstOrDefault ();
		if (listed is null)
			Assert.Ignore ("The local gateway has no devices to query.");
		Assert.That (listed!.DeviceUrl, Is.Not.Null.And.Not.Empty);
		Device device = await _client.GetDevice (listed.DeviceUrl!);
		Assert.That (device.DeviceUrl, Is.EqualTo (listed.DeviceUrl));
		IReadOnlyList<State> states = await _client.GetDeviceStates (listed.DeviceUrl!);
		Assert.That (states.All (state => !string.IsNullOrWhiteSpace (state.Name)), Is.True);
		}

	[Test]
	public async Task EventListener_RegistersFetchesAndUnregisters ()
		{
		await _client.RegisterEventListener ();
		try
			{
			Assert.That (_client.EventListenerId, Is.Not.Null.And.Not.Empty);
			IReadOnlyList<EventObject> events = await _client.FetchEvents ();
			Assert.That (events, Is.Not.Null);
			}
		finally
			{
			await _client.UnregisterEventListener ();
			}
		Assert.That (_client.EventListenerId, Is.Null);
		}
	}