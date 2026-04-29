using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm;
using FingerprintBrowser.Data;
using FingerprintBrowser.Models;
using FingerprintBrowser.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FingerprintBrowser.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly BrowserDbContext _db;
    private readonly PlaywrightService _playwrightService;

    [ObservableProperty]
    private ObservableCollection<BrowserEnvironment> _environments = new();

    [ObservableProperty]
    private ObservableCollection<BrowserEnvironment> _filteredEnvironments = new();

    [ObservableProperty]
    private ObservableCollection<EnvironmentGroup> _groups = new();

    [ObservableProperty]
    private BrowserEnvironment? _selectedEnvironment;

    [ObservableProperty]
    private EnvironmentGroup? _selectedGroup;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _runningCount;

    public MainViewModel()
    {
        _db = new BrowserDbContext();
        _playwrightService = PlaywrightService.Instance;
    }

    public async Task InitializeAsync()
    {
        await LoadGroupsAsync();
        await LoadEnvironmentsAsync();
    }

    public async Task LoadGroupsAsync()
    {
        try
        {
            var groups = await _db.Groups.OrderBy(g => g.SortOrder).ToListAsync();
            Groups = new ObservableCollection<EnvironmentGroup>(groups);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载分组失败");
        }
    }

    public async Task LoadEnvironmentsAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "正在加载环境...";

            var query = _db.Environments
                .Include(e => e.Group)
                .Include(e => e.Proxy)
                .AsQueryable();

            if (SelectedGroup != null)
            {
                query = query.Where(e => e.GroupId == SelectedGroup.Id);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(e => e.Name.Contains(SearchText) || 
                    (e.Remark != null && e.Remark.Contains(SearchText)));
            }

            var list = await query.OrderBy(e => e.Name).ToListAsync();
            Environments = new ObservableCollection<BrowserEnvironment>(list);
            FilteredEnvironments = new ObservableCollection<BrowserEnvironment>(list);
            TotalCount = list.Count;

            StatusMessage = $"已加载 {TotalCount} 个环境";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载环境失败");
            StatusMessage = "加载失败";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        _ = LoadEnvironmentsAsync();
    }

    partial void OnSelectedGroupChanged(EnvironmentGroup? value)
    {
        _ = LoadEnvironmentsAsync();
    }

    public async Task StartBrowserAsync(BrowserEnvironment env)
    {
        try
        {
            env.Status = BrowserStatus.Starting;
            env.StatusMessage = "正在启动...";
            OnPropertyChanged(nameof(env));

            var success = await _playwrightService.LaunchBrowserAsync(env);

            if (success)
            {
                env.Status = BrowserStatus.Running;
                env.StatusMessage = "运行中";
                await _db.SaveChangesAsync();
            }
            else
            {
                env.Status = BrowserStatus.Error;
                env.StatusMessage = "启动失败";
            }

            UpdateRunningCount();
            OnPropertyChanged(nameof(env));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "启动浏览器失败: {Name}", env.Name);
            env.Status = BrowserStatus.Error;
            env.StatusMessage = ex.Message;
            OnPropertyChanged(nameof(env));
        }
    }

    public async Task StopBrowserAsync(BrowserEnvironment env)
    {
        try
        {
            env.Status = BrowserStatus.Stopping;
            env.StatusMessage = "正在关闭...";
            OnPropertyChanged(nameof(env));

            await _playwrightService.CloseBrowserAsync(env.Id);
            await _db.SaveChangesAsync();

            env.Status = BrowserStatus.Idle;
            env.StatusMessage = "已关闭";
            env.RunningBrowserId = null;

            UpdateRunningCount();
            OnPropertyChanged(nameof(env));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "关闭浏览器失败: {Name}", env.Name);
            env.Status = BrowserStatus.Error;
            env.StatusMessage = ex.Message;
            OnPropertyChanged(nameof(env));
        }
    }

    public async Task BatchStartAsync()
    {
        var toStart = FilteredEnvironments.Where(e => e.Status == BrowserStatus.Idle).ToList();
        if (toStart.Count == 0)
        {
            System.Windows.MessageBox.Show("没有可启动的环境", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        StatusMessage = $"正在批量启动 {toStart.Count} 个环境...";

        foreach (var env in toStart)
        {
            await StartBrowserAsync(env);
            await Task.Delay(500);
        }

        StatusMessage = $"已启动 {toStart.Count} 个环境";
    }

    public async Task BatchStopAsync()
    {
        var toStop = FilteredEnvironments.Where(e => e.Status == BrowserStatus.Running).ToList();
        if (toStop.Count == 0)
        {
            System.Windows.MessageBox.Show("没有运行中的环境", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        StatusMessage = $"正在批量关闭 {toStop.Count} 个环境...";

        foreach (var env in toStop)
        {
            await StopBrowserAsync(env);
        }

        StatusMessage = $"已关闭 {toStop.Count} 个环境";
    }

    private void UpdateRunningCount()
    {
        RunningCount = Environments.Count(e => e.Status == BrowserStatus.Running);
    }

    public void SelectGroup(EnvironmentGroup? group)
    {
        SelectedGroup = group;
    }

    public void FilterEnvironments(string searchText)
    {
        SearchText = searchText;
    }

    public void ShowEnvironmentDetails(BrowserEnvironment? env)
    {
        SelectedEnvironment = env;
    }

    public async Task CopyEnvironmentAsync(int id)
    {
        var source = await _db.Environments.FindAsync(id);
        if (source == null) return;

        var copy = new BrowserEnvironment
        {
            Name = source.Name + " (副本)",
            GroupId = source.GroupId,
            BrowserType = source.BrowserType,
            Resolution = source.Resolution,
            UserAgent = source.UserAgent,
            Timezone = source.Timezone,
            Languages = source.Languages,
            EnableWebRTC = source.EnableWebRTC,
            EnableCookies = source.EnableCookies,
            EnableJavaScript = source.EnableJavaScript,
            WebGLVendor = source.WebGLVendor,
            WebGLRenderer = source.WebGLRenderer,
            ProxyId = source.ProxyId,
            StartupUrl = source.StartupUrl,
            Remark = source.Remark,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _db.Environments.Add(copy);
        await _db.SaveChangesAsync();
        await LoadEnvironmentsAsync();
    }

    public async Task DeleteEnvironmentAsync(int id)
    {
        var env = await _db.Environments.FindAsync(id);
        if (env == null) return;

        if (env.Status == BrowserStatus.Running)
        {
            await StopBrowserAsync(env);
        }

        _db.Environments.Remove(env);
        await _db.SaveChangesAsync();
        await LoadEnvironmentsAsync();
    }

    public async Task CreateGroupAsync(string name, string color)
    {
        var group = new EnvironmentGroup
        {
            Name = name,
            Color = color,
            SortOrder = Groups.Count
        };

        _db.Groups.Add(group);
        await _db.SaveChangesAsync();
        await LoadGroupsAsync();
    }

    public async Task DeleteGroupAsync(int id)
    {
        var group = await _db.Groups.FindAsync(id);
        if (group == null) return;

        _db.Groups.Remove(group);
        await _db.SaveChangesAsync();
        await LoadGroupsAsync();
        await LoadEnvironmentsAsync();
    }

    public void ChangeTheme(string themeName)
    {
        var app = System.Windows.Application.Current;
        app.Resources.MergedDictionaries.Clear();

        var skinUri = themeName switch
        {
            "Dark" => new Uri("pack://application:,,,/HandyControl;component/Themes/SkinDefault.xaml"),
            "Light" => new Uri("pack://application:,,,/HandyControl;component/Themes/SkinLight.xaml"),
            "Purple" => new Uri("pack://application:,,,/HandyControl;component/Themes/SkinPurple.xaml"),
            _ => new Uri("pack://application:,,,/HandyControl;component/Themes/SkinDefault.xaml")
        };

        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = skinUri });
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/HandyControl;component/Themes/Theme.xaml") });
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/FingerprintBrowser;component/Resources/CustomStyles.xaml") });
    }
}
