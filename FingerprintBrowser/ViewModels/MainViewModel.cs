using System.Collections.ObjectModel;
using System.Windows;
using FingerprintBrowser.Models;
using FingerprintBrowser.Services;

namespace FingerprintBrowser.ViewModels;

public class MainViewModel
{
    public ObservableCollection<BrowserEnvironment> Environments { get; set; } = new();
    public ObservableCollection<EnvironmentGroup> Groups { get; set; } = new();
    public ObservableCollection<BrowserEnvironment> FilteredEnvironments { get; set; } = new();
    public EnvironmentGroup? SelectedGroup { get; set; }
    public BrowserEnvironment? SelectedEnvironment { get; set; }
    public string SearchText { get; set; } = "";
    public int TotalCount => Environments.Count;
    public int RunningCount => Environments.Count(e => e.Status == BrowserStatus.Running);
    public bool IsLoading { get; set; }
    public string StatusMessage { get; set; } = "就绪";

    private readonly BrowserDbContext _db;
    private readonly BrowserService _browserService;
    private readonly ImportExportService _importExportService;

    public MainViewModel()
    {
        _db = new BrowserDbContext();
        _browserService = BrowserService.Instance;
        _importExportService = new ImportExportService(_db);
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        StatusMessage = "加载中...";
        try
        {
            await LoadGroupsAsync();
            await LoadEnvironmentsAsync();
            StatusMessage = $"已加载 {TotalCount} 个环境";
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

    public async Task LoadGroupsAsync()
    {
        var groups = await Task.Run(() => _db.Groups.OrderBy(g => g.SortOrder).ToList());
        Groups.Clear();
        Groups.Add(new EnvironmentGroup { Id = 0, Name = "全部" });
        foreach (var g in groups) Groups.Add(g);
    }

    public async Task LoadEnvironmentsAsync()
    {
        var envs = await Task.Run(() => _db.Environments.ToList());
        Environments.Clear();
        foreach (var e in envs) Environments.Add(e);
        FilterEnvironments();
    }

    public void FilterEnvironments()
    {
        FilteredEnvironments.Clear();
        var filtered = Environments.AsEnumerable();
        if (SelectedGroup != null && SelectedGroup.Id > 0)
            filtered = filtered.Where(e => e.GroupName == SelectedGroup.Name);
        if (!string.IsNullOrWhiteSpace(SearchText))
            filtered = filtered.Where(e => e.Name.Contains(SearchText) || (e.Remark?.Contains(SearchText) ?? false));
        foreach (var e in filtered) FilteredEnvironments.Add(e);
        OnPropertyChanged(nameof(TotalCount));
    }

    public async Task CreateEnvironmentAsync()
    {
        var win = new Views.EnvironmentEditWindow(null);
        if (win.ShowDialog() == true)
        {
            await LoadEnvironmentsAsync();
            StatusMessage = "环境已创建";
        }
    }

    public async Task EditEnvironmentAsync(BrowserEnvironment env)
    {
        var win = new Views.EnvironmentEditWindow(env);
        if (win.ShowDialog() == true)
        {
            await LoadEnvironmentsAsync();
            StatusMessage = "环境已更新";
        }
    }

    public async Task DeleteEnvironmentAsync(BrowserEnvironment env)
    {
        var result = System.Windows.MessageBox.Show($"确定删除环境 '{env.Name}'?", "确认", 
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            await _browserService.CloseBrowserAsync(env.Id);
            _db.Environments.Remove(env);
            _db.SaveChanges();
            Environments.Remove(env);
            FilterEnvironments();
            StatusMessage = "环境已删除";
        }
    }

    public async Task CopyEnvironmentAsync(BrowserEnvironment env)
    {
        var copy = new BrowserEnvironment
        {
            Name = env.Name + " (副本)",
            GroupName = env.GroupName,
            UserAgent = env.UserAgent,
            Resolution = env.Resolution,
            Timezone = env.Timezone,
            Languages = env.Languages,
            EnableWebRTC = env.EnableWebRTC,
            EnableCookies = env.EnableCookies,
            EnableJavaScript = env.EnableJavaScript,
            WebGLVendor = env.WebGLVendor,
            WebGLRenderer = env.WebGLRenderer,
            Remark = env.Remark,
            BrowserType = env.BrowserType
        };
        _db.Environments.Add(copy);
        _db.SaveChanges();
        Environments.Add(copy);
        FilterEnvironments();
        StatusMessage = "环境已复制";
    }

    public async Task StartBrowserAsync(BrowserEnvironment env)
    {
        try
        {
            env.Status = BrowserStatus.Starting;
            StatusMessage = $"正在启动 {env.Name}...";
            await _browserService.LaunchBrowserAsync(env);
            env.Status = BrowserStatus.Running;
            StatusMessage = $"{env.Name} 已启动";
            OnPropertyChanged(nameof(RunningCount));
        }
        catch (Exception ex)
        {
            env.Status = BrowserStatus.Error;
            StatusMessage = $"启动失败: {ex.Message}";
        }
    }

    public async Task StopBrowserAsync(BrowserEnvironment env)
    {
        try
        {
            env.Status = BrowserStatus.Stopping;
            StatusMessage = $"正在停止 {env.Name}...";
            await _browserService.CloseBrowserAsync(env.Id);
            env.Status = BrowserStatus.Idle;
            StatusMessage = $"{env.Name} 已停止";
            OnPropertyChanged(nameof(RunningCount));
        }
        catch (Exception ex)
        {
            StatusMessage = $"停止失败: {ex.Message}";
        }
    }

    public async Task BatchStartAsync()
    {
        IsLoading = true;
        int count = 0;
        foreach (var env in FilteredEnvironments.Where(e => e.Status == BrowserStatus.Idle))
        {
            await StartBrowserAsync(env);
            count++;
            await Task.Delay(500);
        }
        IsLoading = false;
        StatusMessage = $"批量启动完成: {count} 个";
    }

    public async Task BatchStopAsync()
    {
        IsLoading = true;
        int count = 0;
        foreach (var env in Environments.Where(e => e.Status == BrowserStatus.Running))
        {
            await StopBrowserAsync(env);
            count++;
        }
        IsLoading = false;
        StatusMessage = $"批量停止完成: {count} 个";
    }

    public async Task ImportEnvironmentsAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "JSON文件|*.json|Excel文件|*.xlsx|所有文件|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var imported = await _importExportService.ImportEnvironmentsAsync(dialog.FileName);
                await LoadEnvironmentsAsync();
                StatusMessage = $"导入成功: {imported} 个环境";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"导入失败: {ex.Message}", "错误");
            }
        }
    }

    public async Task ExportEnvironmentsAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON文件|*.json",
            FileName = $"browsers_{DateTime.Now:yyyyMMdd}"
        };
        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _importExportService.ExportEnvironmentsAsync(dialog.FileName, FilteredEnvironments.ToList());
                StatusMessage = "导出成功";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"导出失败: {ex.Message}", "错误");
            }
        }
    }

    public void ChangeTheme(string theme)
    {
        var app = System.Windows.Application.Current as Application;
        if (app == null) return;
        
        var dict = new ResourceDictionary();
        switch (theme)
        {
            case "Dark":
                dict.Source = new Uri("pack://application:,,,/HandyControl;component/Themes/SkinDark.xaml");
                break;
            case "Light":
                dict.Source = new Uri("pack://application:,,,/HandyControl;component/Themes/SkinDefault.xaml");
                break;
            case "Blue":
                dict.Source = new Uri("pack://application:,,,/HandyControl;component/Themes/SkinBlack.xaml");
                break;
        }
        app.Resources.MergedDictionaries[0] = dict;
    }

    public void SelectGroup(EnvironmentGroup? group)
    {
        SelectedGroup = group;
        FilterEnvironments();
    }

    public void OpenBrowserUrl(BrowserEnvironment env)
    {
        if (env.Status == BrowserStatus.Running)
        {
            _browserService.OpenUrl(env.Id, "https://www.google.com");
        }
    }

    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    public event PropertyChangedEventHandler? PropertyChanged;
}
