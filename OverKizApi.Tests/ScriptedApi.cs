// Copyright © 2026 Neil Colvin.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace OverKizApi.Tests;

// Every request is intercepted. An unexpected request fails instead of reaching a network.
internal sealed class ScriptedApi : IDisposable
	{
	internal const string DEVICE_URL = "io://test-gateway/shutter#2";
	internal const string ENCODED_DEVICE_URL = "io%3A%2F%2Ftest-gateway%2Fshutter%232";
	internal const string CLOUD_ENDPOINT = "https://overkiz.example.invalid/api/";
	private readonly Handler _handler = new ();
	private static readonly char[] _formSeparator = ['='];
	internal HttpClient Http
		{
		get;
		}
	internal OverkizClient Client
		{
		get;
		}
	internal int RequestCount => _handler.RequestCount;
	internal bool HandlerDisposed => _handler.Disposed;

	internal ScriptedApi (OverkizServer? server = null, string? token = "synthetic-token")
		{
		Http = new HttpClient (_handler);
		Client = new OverkizClient ("tester@example.invalid", "synthetic-password",
			 server ?? new OverkizServer { Name = "Test cloud", Endpoint = CLOUD_ENDPOINT, Manufacturer = "Test" }, token, Http);
		}

	internal void Expect (string method, string path, string response = "{}", HttpStatusCode status = HttpStatusCode.OK,
		 System.Action<HttpRequestMessage, string>? inspect = null, string mediaType = "application/json", string? cookie = null)
		 => _handler.Enqueue (method, new Uri (Http.BaseAddress!, path).AbsoluteUri, response, status, inspect, mediaType, cookie);

	internal void Complete ()
		{
		Assert.That (_handler.Failures, Is.Empty, "A request assertion was swallowed by library error handling.");
		Assert.That (_handler.Pending, Is.Zero, "Expected HTTP requests were not made.");
		}
	public void Dispose () => Http.Dispose ();

	internal static JsonElement Json (string text)
		{
		using var document = JsonDocument.Parse (text);
		return document.RootElement.Clone ();
		}

	internal static Dictionary<string, string> Form (string text) => text.Split ('&')
		 .Select (pair => pair.Split (_formSeparator, 2))
		 .ToDictionary (pair => Decode (pair[0]), pair => Decode (pair[1]));
	private static string Decode (string text) => Uri.UnescapeDataString (text.Replace ("+", " "));

	private sealed class Handler : HttpMessageHandler
		{
		private readonly Queue<Func<HttpRequestMessage, string, HttpResponseMessage>> _steps = new ();
		private readonly CookieContainer _cookies = new ();
		internal int Pending => _steps.Count;
		internal List<string> Failures { get; } = new ();
		internal int RequestCount
			{
			get; private set;
			}
		internal bool Disposed
			{
			get; private set;
			}

		internal void Enqueue (string method, string uri, string response, HttpStatusCode status,
			 System.Action<HttpRequestMessage, string>? inspect, string mediaType, string? cookie)
			{
			_steps.Enqueue ((request, body) =>
			{
				Assert.That (request.Method.Method, Is.EqualTo (method));
				Assert.That (request.RequestUri!.AbsoluteUri, Is.EqualTo (uri));
				inspect?.Invoke (request, body);
				var result = new HttpResponseMessage (status) { Content = new StringContent (response, Encoding.UTF8, mediaType) };
				if (cookie is not null)
					result.Headers.Add ("Set-Cookie", cookie);
				return result;
			});
			}

		protected override async Task<HttpResponseMessage> SendAsync (HttpRequestMessage request, CancellationToken cancellationToken)
			{
			cancellationToken.ThrowIfCancellationRequested ();
			RequestCount++;
			try
				{
				if (_steps.Count == 0)
					throw new AssertionException ($"Unexpected HTTP request: {request.Method} {request.RequestUri}");
#if NETFRAMEWORK
				string body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync ().ConfigureAwait (false);
#else
				string body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync (cancellationToken).ConfigureAwait (false);
#endif
				// Simulate the cookie-enabled HttpClientHandler required by cloud sessions.
				string cookie = _cookies.GetCookieHeader (request.RequestUri!);
				if (!string.IsNullOrEmpty (cookie))
					request.Headers.Add ("Cookie", cookie);
				var result = _steps.Dequeue () (request, body);
				if (result.Headers.TryGetValues ("Set-Cookie", out var values))
					foreach (string value in values)
						_cookies.SetCookies (request.RequestUri!, value);
				return result;
				}
			catch (AssertionException exception)
				{
				Failures.Add (exception.Message);
				throw;
				}
			}

		protected override void Dispose (bool disposing)
			{
			Disposed = true;
			base.Dispose (disposing);
			}
		}
	}