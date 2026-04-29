using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FingerprintBrowser.Models;
using Microsoft.Playwright;

namespace FingerprintBrowser.Services
{
    public class PlaywrightService
    {
        private static PlaywrightService? _instance;
        private static readonly object _lock = new();
        private IPlaywright? _playwright;
        private readonly Dictionary<int, IBrowser> _browsers = new();
        private bool _isInitialized;

        public static PlaywrightService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new PlaywrightService();
                    }
                }
                return _instance;
            }
        }

        private PlaywrightService() { }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Playwright 初始化失败: {ex.Message}", ex);
            }
        }

        public async Task<IBrowser> LaunchBrowserAsync(BrowserEnvironment env)
        {
            if (_playwright == null)
            {
                await InitializeAsync();
            }

            if (_browsers.ContainsKey(env.Id))
            {
                return _browsers[env.Id];
            }

            var options = new BrowserTypeLaunchOptions
            {
                Headless = false,
                Args = new[]
                {
                    "--disable-blink-features=AutomationControlled",
                    "--disable-dev-shm-usage",
                    "--no-sandbox"
                }
            };

            // 设置代理
            if (env.ProxyConfig != null)
            {
                options.Proxy = new Proxy
                {
                    Server = $"{env.ProxyConfig.Protocol.ToLower()}://{env.ProxyConfig.Host}:{env.ProxyConfig.Port}",
                    Username = string.IsNullOrEmpty(env.ProxyConfig.Username) ? null : env.ProxyConfig.Username,
                    Password = string.IsNullOrEmpty(env.ProxyConfig.Password) ? null : env.ProxyConfig.Password
                };
            }

            IBrowser browser;

            switch (env.BrowserType.ToLower())
            {
                case "firefox":
                    browser = await _playwright!.Firefox.LaunchAsync(options);
                    break;
                case "webkit":
                    browser = await _playwright!.WebKit.LaunchAsync(options);
                    break;
                default:
                    browser = await _playwright!.Chromium.LaunchAsync(options);
                    break;
            }

            // 创建上下文并应用指纹
            var contextOptions = new BrowserNewContextOptions
            {
                ViewportSize = ParseResolution(env.Resolution),
                UserAgent = string.IsNullOrEmpty(env.UserAgent) ? null : env.UserAgent,
                IgnoreHTTPSErrors = true
            };

            // 设置语言
            if (!string.IsNullOrEmpty(env.Languages))
            {
                var langs = env.Languages.Split(',');
                contextOptions.Languages = langs;
            }

            var context = await browser.NewContextAsync(contextOptions);

            // 应用高级指纹设置
            await ApplyFingerprintAsync(context, env);

            // 打开启动URL
            if (!string.IsNullOrEmpty(env.StartupUrl))
            {
                var page = await context.NewPageAsync();
                await page.GotoAsync(env.StartupUrl);
            }

            _browsers[env.Id] = browser;
            return browser;
        }

        public async Task CloseBrowserAsync(int envId)
        {
            if (_browsers.TryGetValue(envId, out var browser))
            {
                await browser.CloseAsync();
                _browsers.Remove(envId);
            }
        }

        public async Task OpenUrl(int envId, string url)
        {
            if (_browsers.TryGetValue(envId, out var browser))
            {
                var pages = browser.Contexts.SelectMany(c => c.Pages).ToList();
                if (pages.Any())
                {
                    var page = pages.First();
                    await page.BringToFrontAsync();
                    await page.GotoAsync(url);
                }
                else
                {
                    var context = browser.Contexts.FirstOrDefault();
                    if (context != null)
                    {
                        var page = await context.NewPageAsync();
                        await page.GotoAsync(url);
                    }
                }
            }
        }

        public async Task CloseAllBrowsersAsync()
        {
            foreach (var browser in _browsers.Values.ToList())
            {
                await browser.CloseAsync();
            }
            _browsers.Clear();
        }

        public bool IsBrowserRunning(int envId)
        {
            return _browsers.ContainsKey(envId);
        }

        private async Task ApplyFingerprintAsync(IBrowserContext context, BrowserEnvironment env)
        {
            var page = await context.NewPageAsync();

            // WebGL 指纹
            if (!string.IsNullOrEmpty(env.WebGLVendor))
            {
                await page.AddInitScriptAsync($@"
                    WebGLRenderingContext.prototype.getParameter = function(parameter) {{
                        if (parameter === 37445) return '{env.WebGLVendor}';
                        if (parameter === 37446) return '{env.WebGLRenderer}';
                        return this.getParameter(parameter);
                    }};
                ");
            }

            // WebRTC 指纹
            if (!env.EnableWebRTC)
            {
                await page.AddInitScriptAsync(@"
                    window.RTCPeerConnection = undefined;
                    window.webkitRTCPeerConnection = undefined;
                ");
            }

            // 时区
            if (!string.IsNullOrEmpty(env.Timezone))
            {
                await context.AddInitScriptAsync($@"
                    Intl.DateTimeFormat = new Proxy(Intl.DateTimeFormat, {{
                        construct(target, args) {{
                            const result = new target(...args);
                            result.resolvedOptions = () => ({{
                                ...target.prototype.resolvedOptions.call(result),
                                timeZone: '{env.Timezone}'
                            }});
                            return result;
                        }}
                    }});
                ");
            }

            await page.CloseAsync();
        }

        private ViewportSize? ParseResolution(string resolution)
        {
            if (string.IsNullOrEmpty(resolution)) return null;

            var parts = resolution.Split('x');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out var width) &&
                int.TryParse(parts[1], out var height))
            {
                return new ViewportSize { Width = width, Height = height };
            }

            return new ViewportSize { Width = 1920, Height = 1080 };
        }

        public async Task InstallBrowsersAsync()
        {
            if (_playwright == null)
            {
                await InitializeAsync();
            }

            await _playwright!.Chromium.InstallAsync();
            await _playwright!.Firefox.InstallAsync();
            await _playwright!.WebKit.InstallAsync();
        }
    }
}
