using System.Collections.Concurrent;
using System.Diagnostics;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Services;

public class BrowserService
{
    private static BrowserService? _instance;
    public static BrowserService Instance => _instance ??= new BrowserService();

    private readonly ConcurrentDictionary<int, IBrowser> _runningBrowsers = new();
    private readonly PlaywrightService _playwrightService;
    private readonly SemaphoreSlim _semaphore;

    public BrowserService()
    {
        _playwrightService = PlaywrightService.Instance;
        _semaphore = new SemaphoreSlim(AppConstants.MaxConcurrency);
    }

    public async Task<BrowserLaunchResult> LaunchBrowserAsync(BrowserEnvironment environment)
    {
        try
        {
            await _semaphore.WaitAsync();
            var result = await _playwrightService.LaunchBrowserAsync(environment);
            if (result.Success && result.Browser != null)
            {
                _runningBrowsers[environment.Id] = result.Browser;
            }
            return result;
        }
        catch (Exception ex)
        {
            return new BrowserLaunchResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task CloseBrowserAsync(int environmentId)
    {
        if (_runningBrowsers.TryRemove(environmentId, out var browser))
        {
            try
            {
                await browser.CloseAsync();
            }
            catch { }
        }
        _semaphore.Release();
    }

    public bool IsRunning(int environmentId) => _runningBrowsers.ContainsKey(environmentId);

    public async Task CloseAllAsync()
    {
        foreach (var kvp in _runningBrowsers)
        {
            try
            {
                await kvp.Value.CloseAsync();
            }
            catch { }
        }
        _runningBrowsers.Clear();
    }

    public int RunningCount => _runningBrowsers.Count;
}

public class BrowserLaunchResult
{
    public bool Success { get; set; }
    public IBrowser? Browser { get; set; }
    public string? ErrorMessage { get; set; }
}
