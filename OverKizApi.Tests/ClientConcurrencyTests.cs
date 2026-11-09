// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using OverKizApi.Concurrency;

using static OverKizApi.Tests.ConcurrencyTestAwait;

namespace OverKizApi.Tests;

[TestFixture]
public sealed class ClientConcurrencyTests
	{
	[Test]
	public async Task Gate_FailingCancellationCallbackDoesNotPreventDrainOrCancelWaiters ()
		{
		using var gate = new AsyncClientGate (typeof (ClientConcurrencyTests));
		using AsyncClientGate.Operation operation = await gate.EnterAsync (exclusive: true);
		Task<AsyncClientGate.Operation> queued = gate.EnterAsync (exclusive: false);
		using CancellationTokenRegistration callback = operation.Token.Register (() => throw new InvalidOperationException ("Synthetic callback failure"));
		Task stopped = gate.Stop ();
		Assert.That (gate.CancellationFailure, Is.Not.Null);
		await Assert.ThatAsync (async () => await Ready (queued), Throws.InstanceOf<OperationCanceledException> ());
		operation.Dispose ();
		await Ready (stopped);
		}
	[Test]
	public async Task OverlappingExplicitRefreshes_RotateTokenOnlyOnce ()
		{
		using var handler = new RefreshHandler ();
		using var http = new HttpClient (handler);
		var client = new OverkizClient ("fake", "fake", OverkizConst.SupportedServers[Server.SomfyEurope], httpClient: http);
		Assert.That (await client.Login (false), Is.True);
		Task first = client.RefreshToken ();
		await Ready (handler.Entered.Task);
		Task second = client.RefreshToken ();
		try
			{
			Assert.That (handler.Requests, Is.EqualTo (2), "One login and one refresh; the overlap must wait.");
			}
		finally { _ = handler.Release.TrySetResult (true); }
		await Ready (Task.WhenAll (first, second));
		Assert.That (handler.Requests, Is.EqualTo (2));
		await client.DisposeAsync ();
		}

	[Test]
	public async Task Gate_CancellationCallbackMayReenterStopWithoutDeadlock ()
		{
		using var gate = new AsyncClientGate (typeof (ClientConcurrencyTests));
		using AsyncClientGate.Operation operation = await gate.EnterAsync (exclusive: false);
		Task? reentrant = null;
		using CancellationTokenRegistration callback = operation.Token.Register (() => reentrant = gate.Stop ());
		Task stopped = gate.Stop ();
		Assert.That (reentrant, Is.SameAs (stopped));
		operation.Dispose ();
		await Ready (stopped);
		}
	[Test]
	public async Task IndependentReads_ReachTransportConcurrentlyWithoutChangingDefaultHeaders ()
		{
		using var handler = new BlockedHandler ("[]", expectedReaders: 2);
		using var http = new HttpClient (handler);
		var client = CreateClient (http);
		http.DefaultRequestHeaders.Add ("X-Caller", "preserved");
		Task first = client.GetDevices ();
		Task second = client.GetGateways ();
		try
			{
			await Ready (handler.Entered.Task);
			Assert.That (handler.Requests, Is.EqualTo (2));
			Assert.That (http.DefaultRequestHeaders.Authorization, Is.Null);
			Assert.That (http.DefaultRequestHeaders.GetValues ("X-Caller"), Is.EqualTo (new List<string> { "preserved" }));
			}
		finally { _ = handler.Release.TrySetResult (true); }
		await Ready (Task.WhenAll (first, second));
		await client.DisposeAsync ();
		}

	[Test]
	public async Task Dispose_CancelsQueuedCallsAndPreventsLateSetupPublication ()
		{
		using var handler = new BlockedHandler ("{\"devices\":[],\"gateways\":[]}");
		using var http = new HttpClient (handler);
		var client = CreateClient (http);
		Task read = client.GetSetup ();
		await Ready (handler.Entered.Task);
		Task queued = client.GetGateways ();
		Task disposal = client.DisposeAsync ().AsTask ();
		Assert.That (client.DisposeAsync ().AsTask (), Is.SameAs (disposal));
		try
			{
			Assert.That (disposal.IsCompleted, Is.False, "Transport deliberately ignores cancellation until released.");
			await Ready (ObserveFailure (queued));
			Assert.That (handler.Requests, Is.EqualTo (1));
			}
		finally { _ = handler.Release.TrySetResult (true); }
		await Ready (ObserveFailure (read));
		await Ready (disposal);
		Assert.That (client.Setup, Is.Null);
		Assert.That (client.Devices, Is.Empty);
		Assert.That (client.Gateways, Is.Empty);
		Assert.That (handler.Disposed, Is.False, "Caller owns the supplied transport.");
		await Assert.ThatAsync (() => client.GetSetup (), Throws.TypeOf<ObjectDisposedException> ());
		}

	[Test]
	public async Task SwitchingGateway_DrainsPreviousSetupBeforeInvalidatingIt ()
		{
		using var handler = new BlockedHandler ("{\"devices\":[],\"gateways\":[]}");
		using var http = new HttpClient (handler);
		var client = new OverkizClient ("fake", "fake", OverkizConst.SupportedServers[Server.Rexel], "fake-token", http);
		client.SelectRexelGateway ("first");
		Task read = client.GetSetup ();
		await Ready (handler.Entered.Task);
		Task change = Task.Run (() => client.SelectRexelGateway ("second"));
		_ = handler.Release.TrySetResult (true);
		await Ready (Task.WhenAll (read, change));
		Assert.That (client.SelectedGatewayId, Is.EqualTo ("second"));
		Assert.That (client.Setup, Is.Null, "The old setup cannot survive the completed switch.");
		await client.DisposeAsync ();
		}

	[Test]
	public async Task Gate_ConcurrentReadersQueueWriterAndDoNotStarveIt ()
		{
		using var gate = new AsyncClientGate (typeof (ClientConcurrencyTests));
		using AsyncClientGate.Operation first = await gate.EnterAsync (exclusive: false);
		using AsyncClientGate.Operation second = await gate.EnterAsync (exclusive: false);
		Task<AsyncClientGate.Operation> writer = gate.EnterAsync (exclusive: true);
		Task<AsyncClientGate.Operation> lateReader = gate.EnterAsync (exclusive: false);
		Assert.That (writer.IsCompleted, Is.False);
		Assert.That (lateReader.IsCompleted, Is.False);
		first.Dispose ();
		Assert.That (writer.IsCompleted, Is.False);
		second.Dispose ();
		using AsyncClientGate.Operation writing = await Ready (writer);
		Assert.That (lateReader.IsCompleted, Is.False);
		writing.Dispose ();
		using AsyncClientGate.Operation last = await Ready (lateReader);
		}

	[Test]
	public async Task Gate_QueuedCancellationDoesNotBlockFollowingOperations ()
		{
		using var gate = new AsyncClientGate (typeof (ClientConcurrencyTests));
		using AsyncClientGate.Operation first = await gate.EnterAsync (exclusive: false);
		using var cancellation = new CancellationTokenSource ();
		Task<AsyncClientGate.Operation> writer = gate.EnterAsync (exclusive: true, cancellation.Token);
		Task<AsyncClientGate.Operation> reader = gate.EnterAsync (exclusive: false);
		cancellation.Cancel ();
		await Assert.ThatAsync (async () => await Ready (writer), Throws.InstanceOf<OperationCanceledException> ());
		using AsyncClientGate.Operation admitted = await Ready (reader);
		}

	[Test]
	public async Task Gate_StopRejectsNewCallsAndCommitsAndWaitsForEveryAdmittedCaller ()
		{
		using var gate = new AsyncClientGate (typeof (ClientConcurrencyTests));
		using AsyncClientGate.Operation active = await gate.EnterAsync (exclusive: true);
		Task<AsyncClientGate.Operation> queued = gate.EnterAsync (exclusive: false);
		Task stopped = gate.Stop ();
		Assert.That (active.Token.IsCancellationRequested, Is.True);
		Assert.That (stopped.IsCompleted, Is.False);
		await Assert.ThatAsync (async () => await Ready (queued), Throws.InstanceOf<OperationCanceledException> ());
		await Assert.ThatAsync (async () => await gate.EnterAsync (exclusive: false), Throws.TypeOf<ObjectDisposedException> ());
		Assert.That (() => gate.Commit (() => Assert.Fail ("No late publication allowed.")), Throws.TypeOf<ObjectDisposedException> ());
		active.Dispose ();
		active.Dispose ();
		await Ready (stopped);
		await Ready (gate.Stop ());
		}

	private static OverkizClient CreateClient (HttpClient http) => new ("fake", "fake",
		new OverkizServer { Name = "Fake", Manufacturer = "Fake", Endpoint = "https://audit.invalid/api/" }, "fake-token", http);
	private static async Task ObserveFailure (Task task)
		{
		try
			{
			await task.ConfigureAwait (false);
			}
		catch (Exception error) when (error is OperationCanceledException or ObjectDisposedException) { return; }
		Assert.Fail ("Stopped operation unexpectedly completed successfully.");
		}
	private sealed class BlockedHandler (string response, int expectedReaders = 1) : HttpMessageHandler
		{
		internal TaskCompletionSource<bool> Entered { get; } = new (TaskCreationOptions.RunContinuationsAsynchronously);
		internal TaskCompletionSource<bool> Release { get; } = new (TaskCreationOptions.RunContinuationsAsynchronously);
		internal int Requests;
		internal bool Disposed;
		protected override async Task<HttpResponseMessage> SendAsync (HttpRequestMessage request, CancellationToken cancellationToken)
			{
			if (Interlocked.Increment (ref Requests) == expectedReaders)
				_ = Entered.TrySetResult (true);
			await Release.Task.ConfigureAwait (false); // Deliberately complete despite cancellation.
			return new HttpResponseMessage (HttpStatusCode.OK) { Content = new StringContent (response) };
			}
		protected override void Dispose (bool disposing)
			{
			Disposed = true;
			base.Dispose (disposing);
			}
		}
	private sealed class RefreshHandler : HttpMessageHandler
		{
		internal TaskCompletionSource<bool> Entered { get; } = new (TaskCreationOptions.RunContinuationsAsynchronously);
		internal TaskCompletionSource<bool> Release { get; } = new (TaskCreationOptions.RunContinuationsAsynchronously);
		internal int Requests;
		protected override async Task<HttpResponseMessage> SendAsync (HttpRequestMessage request, CancellationToken cancellationToken)
			{
			Assert.That (request.RequestUri!.AbsoluteUri, Is.EqualTo ("https://accounts.somfy.com/oauth/oauth/v2/token/jwt"));
			if (Interlocked.Increment (ref Requests) > 1)
				{
				_ = Entered.TrySetResult (true);
				await Release.Task.ConfigureAwait (false);
				}
			return new HttpResponseMessage (HttpStatusCode.OK)
				{
				Content = new StringContent ("{\"access_token\":\"fake-access\",\"refresh_token\":\"fake-refresh\",\"expires_in\":3600}")
				};
			}
		}
	}