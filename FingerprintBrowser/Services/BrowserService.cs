using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FingerprintBrowser.Data;
using FingerprintBrowser.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace FingerprintBrowser.Services
{
    public class BrowserService
    {
        private readonly PlaywrightService _playwrightService;
        private readonly BrowserDbContext _db;
        private readonly Dictionary<int, DateTime> _startTimes = new();

        public BrowserService(BrowserDbContext db, PlaywrightService playwrightService)
        {
            _db = db;
            _playwrightService = playwrightService;
        }

        public async Task<bool> StartBrowserAsync(BrowserEnvironment env)
        {
            try
            {
                var context = await _playwrightService.CreateContextAsync(env);
                var page = await context.NewPageAsync();

                try { await page.BringToFrontAsync(); } catch { }

                env.Status = BrowserStatus.Running;
                env.RunningBrowserId = Guid.NewGuid().ToString("N")[..8];
                env.LastOpenedAt = DateTime.Now;
                _startTimes[env.Id] = DateTime.Now;

                _db.Environments.Update(env);
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task StopBrowserAsync(BrowserEnvironment env)
        {
            try
            {
                await _playwrightService.CloseContextAsync(env.Id);
            }
            catch { }

            env.Status = BrowserStatus.Stopped;
            env.RunningBrowserId = null;
            _startTimes.Remove(env.Id);

            _db.Environments.Update(env);
            await _db.SaveChangesAsync();
        }

        public async Task StopAllAsync()
        {
            var running = await _db.Environments
                .Where(e => e.Status == BrowserStatus.Running)
                .ToListAsync();

            foreach (var env in running)
            {
                await StopBrowserAsync(env);
            }
        }

        public int GetRunningCount()
        {
            return _db.Environments.Count(e => e.Status == BrowserStatus.Running);
        }

        public async Task<bool> TestProxyAsync(ProxyConfigModel proxy)
        {
            proxy.Status = ProxyStatus.Testing;
            _db.ProxyConfigs.Update(proxy);
            await _db.SaveChangesAsync();

            try
            {
                var pw = await Playwright.CreateAsync();
                var browser = await pw.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

                var context = await browser.NewContextAsync(new BrowserNewContextOptions
                {
                    Proxy = new Proxy
                    {
                        Server = $"{proxy.Type.ToString().ToLower()}://{proxy.Host}:{proxy.Port}",
                        Username = proxy.Username,
                        Password = proxy.Password
                    }
                });

                var page = await context.NewPageAsync();
                await page.GotoAsync("https://httpbin.org/ip", new PageGotoOptions
                {
                    Timeout = 10000
                });

                await browser.CloseAsync();
                pw.Dispose();

                proxy.Status = ProxyStatus.Normal;
                proxy.LastTestedAt = DateTime.Now;
            }
            catch
            {
                proxy.Status = ProxyStatus.Failed;
                proxy.LastTestedAt = DateTime.Now;
            }

            _db.ProxyConfigs.Update(proxy);
            await _db.SaveChangesAsync();
            return proxy.Status == ProxyStatus.Normal;
        }
    }
}
