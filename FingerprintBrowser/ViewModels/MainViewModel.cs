using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using FingerprintBrowser.Data;
using FingerprintBrowser.Models;
using FingerprintBrowser.Services;
using FingerprintBrowser.Views;
using Microsoft.EntityFrameworkCore;

namespace FingerprintBrowser.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly BrowserDbContext _db;
    private string _searchText = "";
    private EnvironmentGroup? _selectedGroup;
    private BrowserEnvironment? _selectedEnvironment;
    private string _statusMessage = "就绪";
    private bool _isLoading;
    private int _totalCount;
    private int _runningCount;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<BrowserEnvironment> Environments { get; } = new();
    public ObservableCollection<BrowserEnvironment> FilteredEnvironments { get; } = new();
    public ObservableCollection<EnvironmentGroup> Groups { get; } = new();

    public string SearchText
    {
        get => _searchText;
        set { _searchText = value; OnPropertyChanged(); FilterEnvironments(); }
    }

    public EnvironmentGroup? SelectedGroup
    {
        get => _selectedGroup;
        set { _selectedGroup = value; OnPropertyChanged(); FilterEnvironments(); }
    }

    public BrowserEnvironment? SelectedEnvironment
    {
        get => _selectedEnvironment;
        set { _selectedEnvironment = value; OnPropertyChanged(); }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public int TotalCount
    {
        get => _totalCount;
        set { _totalCount = value; OnPropertyChanged(); }
    }

    public int RunningCount
    {
        get => _runningCount;
        set { _runningCount = value; OnPropertyChanged(); }
    }

    public MainViewModel()
    {
        _db = new BrowserDbContext();
    }

    public async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "正在加载数据...";
            await DatabaseInitializer.InitializeAsync();
            await LoadGroupsAsync();
            await LoadEnvironmentsAsync();
            StatusMessage = "就绪";
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadGroupsAsync()
    {
        var groups = await _db.EnvironmentGroups.ToListAsync();
        Groups.Clear();
        Groups.Add(new EnvironmentGroup { Id = 0, Name = "全部分组", Color = "#667EEA" });
        foreach (var group in groups)
        {
            Groups.Add(group);
        }
    }

    public async Task LoadEnvironmentsAsync()
    {
        var envs = await _db.BrowserEnvironments.ToListAsync();
        Environments.Clear();
        foreach (var env in envs)
        {
            Environments.Add(env);
        }
        FilterEnvironments();
        TotalCount = Environments.Count;
        UpdateRunningCount();
    }

    private void FilterEnvironments()
    {
        FilteredEnvironments.Clear();
        var filtered = Environments.AsEnumerable();

        if (SelectedGroup != null && SelectedGroup.Id != 0)
        {
            filtered = filtered.Where(e => e.GroupId == SelectedGroup.Id);
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(e =>
                e.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (e.Remark?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        foreach (var env in filtered)
        {
            FilteredEnvironments.Add(env);
        }
    }

    private void UpdateRunningCount()
    {
        RunningCount = BrowserService.Instance.RunningCount;
    }

    public async Task CreateEnvironmentAsync()
    {
        var window = new EnvironmentEditWindow();
        if (window.ShowDialog() == true && window.Result != null)
        {
            _db.Environments.Add(window.Result);
            await _db.SaveChangesAsync();
            await LoadEnvironmentsAsync();
            StatusMessage = "环境创建成功";
        }
    }

    public async Task EditEnvironmentAsync(BrowserEnvironment? env)
    {
        if (env == null) return;
        var window = new EnvironmentEditWindow();
        window.SetEnvironment(env);
        if (window.ShowDialog() == true)
        {
            await _db.SaveChangesAsync();
            await LoadEnvironmentsAsync();
            StatusMessage = "环境更新成功";
        }
    }

    public async Task DeleteEnvironmentAsync(BrowserEnvironment? env)
    {
        if (env == null) return;
        if (BrowserService.Instance.IsRunning(env.Id))
        {
            await BrowserService.Instance.CloseBrowserAsync(env.Id);
        }
        _db.Environments.Remove(env);
        await _db.SaveChangesAsync();
        await LoadEnvironmentsAsync();
        StatusMessage = "环境已删除";
    }

    public async Task StartBrowserAsync(BrowserEnvironment? env)
    {
        if (env == null) return;
        if (BrowserService.Instance.IsRunning(env.Id))
        {
            StatusMessage = "浏览器已在运行";
            return;
        }

        try
        {
            StatusMessage = "正在启动浏览器...";
            var result = await BrowserService.Instance.LaunchBrowserAsync(env);
            if (result.Success)
            {
                StatusMessage = "浏览器已启动";
            }
            else
            {
                StatusMessage = $"启动失败: {result.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"启动失败: {ex.Message}";
        }
        UpdateRunningCount();
    }

    public async Task StopBrowserAsync(BrowserEnvironment? env)
    {
        if (env == null) return;
        await BrowserService.Instance.CloseBrowserAsync(env.Id);
        StatusMessage = "浏览器已关闭";
        UpdateRunningCount();
    }

    public async Task BatchStartAsync()
    {
        IsLoading = true;
        StatusMessage = "正在批量启动...";
        var toStart = FilteredEnvironments.Where(e => !BrowserService.Instance.IsRunning(e.Id)).ToList();
        int started = 0;

        foreach (var env in toStart)
        {
            var result = await BrowserService.Instance.LaunchBrowserAsync(env);
            if (result.Success) started++;
            await Task.Delay(500);
        }

        StatusMessage = $"已启动 {started} 个浏览器";
        IsLoading = false;
        UpdateRunningCount();
    }

    public async Task BatchStopAsync()
    {
        IsLoading = true;
        StatusMessage = "正在批量关闭...";
        await BrowserService.Instance.CloseAllAsync();
        StatusMessage = "所有浏览器已关闭";
        IsLoading = false;
        UpdateRunningCount();
    }

    public void OpenProxyWindow()
    {
        var window = new ProxyWindow();
        window.ShowDialog();
    }

    public void OpenSettingsWindow()
    {
        var window = new SettingsWindow();
        window.ShowDialog();
    }

    public async Task AddGroupAsync(string name, string color)
    {
        var group = new EnvironmentGroup { Name = name, Color = color };
        _db.EnvironmentGroups.Add(group);
        await _db.SaveChangesAsync();
        await LoadGroupsAsync();
    }

    public async Task DeleteGroupAsync(EnvironmentGroup? group)
    {
        if (group == null || group.Id == 0) return;
        _db.EnvironmentGroups.Remove(group);
        await _db.SaveChangesAsync();
        await LoadGroupsAsync();
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
