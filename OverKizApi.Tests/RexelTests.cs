// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace OverKizApi.Tests;

[TestFixture]
public sealed class RexelTests
	{
	private const string Directory = "https://econnect-api.rexelservices.fr/api/enduser/";

	[Test]
	public async Task Discovery_CombinesHomesAndKeepsExternalIdSeparateFromGatewayHeader ()
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[Server.Rexel]);
		api.Client.SelectRexelGateway ("old-gateway");
		api.Expect ("GET", Directory + "homes", "[{\"id\":\"home/one\",\"label\":\"Home One\"},{\"id\":\"home-two\"}]", inspect: CheckDirectoryHeaders);
		api.Expect ("GET", Directory + "overkizgateways?homeId=home%2Fone", "[{\"gatewayId\":\"selected-gateway\",\"externalId\":\"external-serial\"}]", inspect: CheckDirectoryHeaders);
		api.Expect ("GET", Directory + "overkizgateways?homeId=home-two", "[]", inspect: CheckDirectoryHeaders);
		GatewayCandidate candidate = (await api.Client.DiscoverRexelGateways ()).Single ();
		Assert.That (candidate.HomeId, Is.EqualTo ("home/one"));
		Assert.That (candidate.Label, Is.EqualTo ("Home One"));
		Assert.That (candidate.ExternalId, Is.EqualTo ("external-serial"));
		api.Client.SelectRexelGateway (candidate.GatewayId);
		api.Expect ("GET", "setup/gateways", "[]", inspect: (request, _) =>
			 Assert.That (request.Headers.GetValues ("gatewayId"), Is.EqualTo (new[] { "selected-gateway" })));
		await api.Client.GetGateways ();
		api.Expect ("GET", "config/external-serial/local/tokens/generate", "{\"token\":\"local-token\"}");
		Assert.That (await api.Client.GenerateLocalToken (candidate.ExternalId!), Is.EqualTo ("local-token"));
		api.Complete ();
		}

	[Test]
	public async Task SingleGatewayLogin_AutomaticallySelectsGateway ()
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[Server.Rexel]);
		api.Expect ("GET", Directory + "homes", "[{\"id\":\"home\"}]");
		api.Expect ("GET", Directory + "overkizgateways?homeId=home", "[{\"gatewayId\":\"gateway\"}]");
		api.Expect ("POST", "events/register", "{\"id\":\"listener\"}", inspect: (request, _) =>
			 Assert.That (request.Headers.GetValues ("gatewayId").Single (), Is.EqualTo ("gateway")));
		Assert.That (await api.Client.Login (), Is.True);
		Assert.That (api.Client.SelectedGatewayId, Is.EqualTo ("gateway"));
		api.Complete ();
		}

	[Test]
	public async Task MultipleGateways_RequireExplicitSelection ()
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[Server.Rexel]);
		api.Expect ("GET", Directory + "homes", "[{\"id\":\"home\"}]");
		api.Expect ("GET", Directory + "overkizgateways?homeId=home", "[{\"gatewayId\":\"one\"},{\"gatewayId\":\"two\"}]");
		await Assert.ThatAsync (() => api.Client.Login (false), Throws.TypeOf<NoGatewaySelectedException> ());
		Assert.That (api.Client.SelectedGatewayId, Is.Null);
		api.Complete ();
		}

	[TestCase (null)]
	[TestCase ("")]
	[TestCase (" ")]
	public async Task MissingToken_IsRejectedBeforeNetwork (string? token)
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[Server.Rexel], token);
		await Assert.ThatAsync (() => api.Client.Login (), Throws.TypeOf<InvalidOperationException> ());
		await Assert.ThatAsync (() => api.Client.DiscoverRexelGateways (), Throws.TypeOf<InvalidOperationException> ());
		Assert.That (api.RequestCount, Is.Zero);
		}

	[TestCase ("")]
	[TestCase (" ")]
	public void EmptySelection_IsRejected (string gateway)
		{
		using var api = new ScriptedApi (OverkizConst.SupportedServers[Server.Rexel]);
		Assert.That (() => api.Client.SelectRexelGateway (gateway), Throws.TypeOf<ArgumentException> ());
		}

	[Test]
	public async Task GatewayDirectoryMethods_AreRestrictedToRexel ()
		{
		using var api = new ScriptedApi ();
		await Assert.ThatAsync (() => api.Client.DiscoverRexelGateways (), Throws.TypeOf<UnsupportedOperationException> ());
		Assert.That (() => api.Client.SelectRexelGateway ("gateway"), Throws.TypeOf<UnsupportedOperationException> ());
		Assert.That (api.RequestCount, Is.Zero);
		}

	private static void CheckDirectoryHeaders (HttpRequestMessage request, string body)
		{
		Assert.That (request.Headers.Contains ("gatewayId"), Is.False);
		Assert.That (request.Headers.Authorization!.ToString (), Is.EqualTo ("Bearer synthetic-token"));
		}
	}