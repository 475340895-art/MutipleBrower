using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FingerprintBrowser.Data;
using FingerprintBrowser.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace FingerprintBrowser.Services
{
    public class BrowserService
    {
        private readonly BrowserDbContext _db;
        private readonly PlaywrightService _playwrightService;
        private readonly Dictionary<int, IBrowser> _runningBrowsers = new();

        public BrowserService(BrowserDbContext db)
        {
            _db = db;
            _playwrightService = PlaywrightService.Instance;
        }

        public async Task<BrowserEnvironment?> GetEnvironmentByIdAsync(int id)
        {
            return await _db.Environments
                .Include(e => e.Group)
                .Include(e => e.ProxyConfig)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<List<BrowserEnvironment>> GetEnvironmentsByGroupAsync(int groupId)
        {
            return await _db.Environments
                .Include(e => e.ProxyConfig)
                .Where(e => e.GroupId == groupId)
                .ToListAsync();
        }

        public async Task<BrowserEnvironment> CreateEnvironmentAsync(BrowserEnvironment env)
        {
            env.CreatedAt = DateTime.Now;
            env.UpdatedAt = DateTime.Now;
            _db.Environments.Add(env);
            await _db.SaveChangesAsync();
            return env;
        }

        public async Task UpdateEnvironmentAsync(BrowserEnvironment env)
        {
            env.UpdatedAt = DateTime.Now;
            _db.Environments.Update(env);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteEnvironmentAsync(int id)
        {
            var env = await _db.Environments.FindAsync(id);
            if (env != null)
            {
                if (_runningBrowsers.ContainsKey(id))
                {
                    await CloseBrowserAsync(id);
                }
                _db.Environments.Remove(env);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<BrowserEnvironment> CopyEnvironmentAsync(int id)
        {
            var original = await GetEnvironmentByIdAsync(id);
            if (original == null)
                throw new InvalidOperationException("环境不存在");

            var copy = new BrowserEnvironment
            {
                Name = $"{original.Name} (副本)",
                GroupId = original.GroupId,
                ProxyId = original.ProxyId,
                BrowserType = original.BrowserType,
                Resolution = original.Resolution,
                Timezone = original.Timezone,
                Languages = original.Languages,
                UserAgent = original.UserAgent,
                WebGLVendor = original.WebGLVendor,
                WebGLRenderer = original.WebGLRenderer,
                EnableWebRTC = original.EnableWebRTC,
                EnableCookies = original.EnableCookies,
                EnableJavaScript = original.EnableJavaScript,
                StartupUrl = original.StartupUrl,
                Remark = original.Remark
            };

            return await CreateEnvironmentAsync(copy);
        }

        public async Task LaunchBrowserAsync(BrowserEnvironment env)
        {
            if (_runningBrowsers.ContainsKey(env.Id))
            {
                return;
            }

            var browser = await _playwrightService.LaunchBrowserAsync(env);
            _runningBrowsers[env.Id] = browser;

            env.Status = BrowserEnvironmentStatus.Running;
            env.LastRunTime = DateTime.Now;
            await UpdateEnvironmentAsync(env);
        }

        public async Task CloseBrowserAsync(int envId)
        {
            if (_runningBrowsers.TryGetValue(envId, out var browser))
            {
                await browser.CloseAsync();
                _runningBrowsers.Remove(envId);
            }

            var env = await GetEnvironmentByIdAsync(envId);
            if (env != null)
            {
                env.Status = BrowserEnvironmentStatus.Idle;
                env.RunningBrowserId = null;
                await UpdateEnvironmentAsync(env);
            }
        }

        public async Task OpenUrlAsync(int envId, string url)
        {
            if (_runningBrowsers.TryGetValue(envId, out var browser))
            {
                var pages = browser.Contexts.SelectMany(c => c.Pages).ToList();
                if (pages.Any())
                {
                    await pages.First().BringToFrontAsync();
                    await pages.First().BRINGTOFRONTAsync();
                    await pages.First().EvaluateAsync($"window.location.href = '{url}'");
                }
            }
        }

        public bool IsBrowserRunning(int envId)
        {
            return _runningBrowsers.ContainsKey(envId);
        }

        public int GetRunningCount()
        {
            return _runningBrowsers.Count;
        }

        public async Task CloseAllBrowsersAsync()
        {
            var tasks = _runningBrowsers.Keys.ToList().Select(CloseBrowserAsync);
            await Task.WhenAll(tasks);
        }
    }
}
