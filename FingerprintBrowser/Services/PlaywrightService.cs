using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FingerprintBrowser.Models;
using Microsoft.Playwright;

namespace FingerprintBrowser.Services
{
    public class PlaywrightService : IDisposable
    {
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private readonly Dictionary<int, IBrowserContext> _contexts = new();

        public async Task InitializeAsync()
        {
            _playwright = await Playwright.CreateAsync();
        }

        public async Task<IBrowserContext> CreateContextAsync(BrowserEnvironment env)
        {
            if (_playwright == null)
                await InitializeAsync();

            _browser ??= await _playwright!.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false
            });

            var contextOptions = new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize
                {
                    Width = int.Parse(env.Resolution.Split('x')[0]),
                    Height = int.Parse(env.Resolution.Split('x')[1])
                },
                UserAgent = env.UserAgent,
                Locale = env.Languages ?? "en-US",
                TimezoneId = env.Timezone ?? "America/New_York",
                JavaScriptEnabled = env.EnableJavaScript
            };

            if (env.ProxyType != ProxyType.None && !string.IsNullOrEmpty(env.ProxyHost))
            {
                contextOptions.Proxy = new Proxy
                {
                    Server = $"{env.ProxyType.ToString().ToLower()}://{env.ProxyHost}:{env.ProxyPort}"
                };
                if (!string.IsNullOrEmpty(env.ProxyUsername))
                    contextOptions.Proxy.Username = env.ProxyUsername;
                if (!string.IsNullOrEmpty(env.ProxyPassword))
                    contextOptions.Proxy.Password = env.ProxyPassword;
            }

            var context = await _browser.NewContextAsync(contextOptions);

            if (!env.EnableWebRTC)
            {
                await context.GrantPermissionsAsync(Array.Empty<string>());
            }

            _contexts[env.Id] = context;
            return context;
        }

        public async Task CloseContextAsync(int envId)
        {
            if (_contexts.TryGetValue(envId, out var context))
            {
                await context.CloseAsync();
                _contexts.Remove(envId);
            }
        }

        public bool IsContextRunning(int envId) => _contexts.ContainsKey(envId);

        public void Dispose()
        {
            foreach (var ctx in _contexts.Values)
            {
                try { ctx.CloseAsync().Wait(); } catch { }
            }
            _contexts.Clear();
            try { _browser?.CloseAsync().Wait(); } catch { }
            _playwright?.Dispose();
        }
    }
}
