using Microsoft.Playwright;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Services;

public class BrowserService
{
    private readonly Dictionary<int, IBrowserContext> _runningContexts = new();
    private readonly PlaywrightService _playwrightService;
    private readonly SemaphoreSlim _semaphore = new(5);
    
    public BrowserService()
    {
        _playwrightService = new PlaywrightService();
    }
    
    public async Task StartBrowserAsync(BrowserEnvironment env)
    {
        if (_runningContexts.ContainsKey(env.Id))
        {
            var context = _runningContexts[env.Id];
            var pages = context.Pages;
            if (pages.Count > 0) await pages[0].BringToFrontAsync();
            return;
        }
        
        await _semaphore.WaitAsync();
        try
        {
            var context = await _playwrightService.CreateContextAsync(env);
            _runningContexts[env.Id] = context;
            
            if (!string.IsNullOrEmpty(env.StartupUrl))
            {
                var page = await context.NewPageAsync();
                await page.GotoAsync(env.StartupUrl);
            }
            
            env.Status = BrowserStatus.Running;
            env.RunningBrowserId = env.Id;
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async Task StopBrowserAsync(int environmentId)
    {
        if (_runningContexts.TryGetValue(environmentId, out var context))
        {
            await context.CloseAsync();
            _runningContexts.Remove(environmentId);
        }
    }
    
    public async Task OpenUrlAsync(int environmentId, string url)
    {
        if (_runningContexts.TryGetValue(environmentId, out var context))
        {
            var pages = context.Pages;
            if (pages.Count > 0)
            {
                await pages[0].BringToFrontAsync();
                await pages[0].GotoAsync(url);
            }
        }
    }
    
    public async Task CleanupAsync()
    {
        foreach (var context in _runningContexts.Values)
        {
            await context.CloseAsync();
        }
        _runningContexts.Clear();
        _playwrightService.Dispose();
    }
}
