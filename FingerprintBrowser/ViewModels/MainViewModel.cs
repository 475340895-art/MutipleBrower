using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FingerprintBrowser.Models;
using FingerprintBrowser.Services;

namespace FingerprintBrowser.ViewModels;

/// <summary>
/// 主窗口 ViewModel
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly BrowserService _browserService;
    private readonly ProxyService _proxyService;
    private System.Timers.Timer? _refreshTimer;

    [ObservableProperty]
    private ObservableCollection<BrowserEnvironment> _environments = new();

    [ObservableProperty]
    private ObservableCollection<BrowserEnvironment> _filteredEnvironments = new();

    [ObservableProperty]
    private ObservableCollection<EnvironmentGroup> _groups = new();

    [ObservableProperty]
    private ObservableCollection<EnvironmentGroup> _filterGroups = new();

    [ObservableProperty]
    private BrowserEnvironment? _selectedEnvironment;

    [ObservableProperty]
    private EnvironmentGroup? _selectedGroup;

    [ObservableProperty]
    private EnvironmentGroup? _selectedFilterGroup;

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    [ObservableProperty]
    private int _runningCount;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private string _currentTheme = "深色";

    public MainViewModel()
    {
        _browserService = new BrowserService();
        _proxyService = new ProxyService();
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        await LoadDataAsync();
        StartRefreshTimer();
    }

    private void StartRefreshTimer()
    {
        _refreshTimer = new System.Timers.Timer(2000);
        _refreshTimer.Elapsed += async (s, e) =>
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await LoadEnvironmentsAsync(false);
            });
        };
        _refreshTimer.Start();
    }

    public async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "加载数据中...";

            await LoadGroupsAsync();
            await LoadEnvironmentsAsync(true);
            await LoadProxyListAsync();

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
        var groups = await _browserService.GetGroupsAsync();
        Groups.Clear();
        Groups.Add(new EnvironmentGroup { Id = 0, Name = "全部环境", Color = "#667eea" });
        foreach (var group in groups)
        {
            Groups.Add(group);
        }

        FilterGroups.Clear();
        FilterGroups.Add(new EnvironmentGroup { Id = 0, Name = "全部", Color = "#667eea" });
        foreach (var group in groups)
        {
            FilterGroups.Add(group);
        }
    }

    public async Task LoadEnvironmentsAsync(bool showLoading = false)
    {
        if (showLoading)
        {
            IsLoading = true;
        }

        try
        {
            var envs = await _browserService.GetEnvironmentsAsync();
            Environments.Clear();
            foreach (var env in envs)
            {
                Environments.Add(env);
            }

            TotalCount = Environments.Count;
            RunningCount = Environments.Count(e => e.Status == 1);

            FilterEnvironments();
        }
        finally
        {
            if (showLoading)
            {
                IsLoading = false;
            }
        }
    }

    private async Task LoadProxyListAsync()
    {
        // Proxy list is handled by ProxyViewModel
    }

    partial void OnSearchTextChanged(string value)
    {
        FilterEnvironments();
    }

    partial void OnSelectedFilterGroupChanged(EnvironmentGroup? value)
    {
        FilterEnvironments();
    }

    public void FilterEnvironments()
    {
        FilteredEnvironments.Clear();

        var filtered = Environments.AsEnumerable();

        // 按分组筛选
        if (SelectedFilterGroup != null && SelectedFilterGroup.Id > 0)
        {
            filtered = filtered.Where(e => e.GroupId == SelectedFilterGroup.Id);
        }

        // 按搜索文本筛选
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLower();
            filtered = filtered.Where(e =>
                e.Name.ToLower().Contains(search) ||
                (e.Remark?.ToLower().Contains(search) ?? false));
        }

        foreach (var env in filtered)
        {
            FilteredEnvironments.Add(env);
        }
    }

    public void SelectGroup(EnvironmentGroup? group)
    {
        SelectedGroup = group;
        if (group != null)
        {
            SelectedFilterGroup = FilterGroups.FirstOrDefault(g => g.Id == group.Id);
            FilterEnvironments();
        }
    }

    [RelayCommand]
    public async Task AddEnvironmentAsync()
    {
        var window = new Views.EnvironmentEditWindow();
        window.Owner = Application.Current.MainWindow;
        if (window.ShowDialog() == true)
        {
            await LoadEnvironmentsAsync(true);
        }
    }

    [RelayCommand]
    public async Task EditEnvironmentAsync(BrowserEnvironment? env)
    {
        if (env == null) return;

        var window = new Views.EnvironmentEditWindow(env.Id);
        window.Owner = Application.Current.MainWindow;
        if (window.ShowDialog() == true)
        {
            await LoadEnvironmentsAsync(true);
        }
    }

    [RelayCommand]
    public async Task CopyEnvironmentAsync(int id)
    {
        try
        {
            await _browserService.CopyEnvironmentAsync(id);
            StatusMessage = "环境复制成功";
            await LoadEnvironmentsAsync(true);
        }
        catch (Exception ex)
        {
            StatusMessage = $"复制失败: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task DeleteEnvironmentAsync(int id)
    {
        var result = HandyControl.Controls.MessageBox.Show(
            "确定要删除这个环境吗？",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _browserService.DeleteEnvironmentAsync(id);
                StatusMessage = "环境已删除";
                await LoadEnvironmentsAsync(true);
            }
            catch (Exception ex)
            {
                StatusMessage = $"删除失败: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    public async Task StartBrowserAsync(int id)
    {
        try
        {
            StatusMessage = "正在启动浏览器...";
            await _browserService.StartBrowserAsync(id);
            StatusMessage = "浏览器已启动";
            await LoadEnvironmentsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"启动失败: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task StopBrowserAsync(int id)
    {
        try
        {
            StatusMessage = "正在关闭浏览器...";
            await _browserService.StopBrowserAsync(id);
            StatusMessage = "浏览器已关闭";
            await LoadEnvironmentsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"关闭失败: {ex.Message}";
        }
    }

    public void OpenBrowserUrl(int environmentId)
    {
        try
        {
            _browserService.OpenBrowserUrl(environmentId);
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开URL失败: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task BatchStartAsync()
    {
        try
        {
            StatusMessage = "正在批量启动...";
            var toStart = Environments.Where(e => e.Status == 0).Take(5).ToList();
            foreach (var env in toStart)
            {
                try
                {
                    await _browserService.StartBrowserAsync(env.Id);
                }
                catch { }
            }
            StatusMessage = "批量启动完成";
            await LoadEnvironmentsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"批量启动失败: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task BatchStopAsync()
    {
        try
        {
            StatusMessage = "正在批量关闭...";
            var toStop = Environments.Where(e => e.Status == 1).ToList();
            foreach (var env in toStop)
            {
                try
                {
                    await _browserService.StopBrowserAsync(env.Id);
                }
                catch { }
            }
            StatusMessage = "批量关闭完成";
            await LoadEnvironmentsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"批量关闭失败: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task ImportEnvironmentsAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON文件|*.json|所有文件|*.*",
                Title = "导入环境"
            };

            if (dialog.ShowDialog() == true)
            {
                filePath = dialog.FileName;
            }
            else
            {
                return;
            }
        }

        try
        {
            StatusMessage = "正在导入...";
            await _browserService.ImportEnvironmentsAsync(filePath);
            StatusMessage = "导入成功";
            await LoadEnvironmentsAsync(true);
        }
        catch (Exception ex)
        {
            StatusMessage = $"导入失败: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task ExportEnvironmentsAsync(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON文件|*.json",
                Title = "导出环境",
                FileName = $"environments_{DateTime.Now:yyyyMMdd}.json"
            };

            if (dialog.ShowDialog() == true)
            {
                filePath = dialog.FileName;
            }
            else
            {
                return;
            }
        }

        try
        {
            StatusMessage = "正在导出...";
            await _browserService.ExportEnvironmentsAsync(filePath);
            StatusMessage = "导出成功";
        }
        catch (Exception ex)
        {
            StatusMessage = $"导出失败: {ex.Message}";
        }
    }

    [RelayCommand]
    public void OpenSettings()
    {
        var window = new Views.SettingsWindow();
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
    }

    [RelayCommand]
    public void OpenProxyManager()
    {
        var window = new Views.ProxyWindow();
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
    }

    [RelayCommand]
    public void ChangeTheme(string themeName)
    {
        CurrentTheme = themeName;
        var skin = themeName switch
        {
            "浅色" => HandyControl.Data.SkinType.Default,
            "深蓝" => HandyControl.Data.SkinType.Dark,
            _ => HandyControl.Data.SkinType.Dark
        };
        HandyControl.Controls.SkinManager.Instance.SetSkin((HandyControl.Data.SkinType)skin);
    }

    [RelayCommand]
    public void ShowEnvironmentDetails(BrowserEnvironment? env)
    {
        SelectedEnvironment = env;
    }

    public void Cleanup()
    {
        _refreshTimer?.Stop();
        _refreshTimer?.Dispose();
    }
}
