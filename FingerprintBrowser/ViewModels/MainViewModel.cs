using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using FingerprintBrowser.Data;
using FingerprintBrowser.Models;
using FingerprintBrowser.Services;

namespace FingerprintBrowser.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private BrowserDbContext _db = null!;
    public ObservableCollection<BrowserEnvironment> Environments { get; } = new();
    public ObservableCollection<EnvironmentGroup> Groups { get; } = new();
    public ObservableCollection<ProxyConfigModel> Proxies { get; } = new();
    
    private EnvironmentGroup? _selectedGroup;
    public EnvironmentGroup? SelectedGroup
    {
        get => _selectedGroup;
        set { _selectedGroup = value; OnPropertyChanged(); FilterEnvironments(); }
    }

    private BrowserEnvironment? _selectedEnvironment;
    public BrowserEnvironment? SelectedEnvironment
    {
        get => _selectedEnvironment;
        set { _selectedEnvironment = value; OnPropertyChanged(); }
    }

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set { _searchText = value; OnPropertyChanged(); FilterEnvironments(); }
    }

    public string StatusMessage { get; private set; } = "就绪";
    public int TotalCount => Environments.Count;
    public int RunningCount => Environments.Count(e => e.Status == BrowserStatus.Running);

    public ICommand NewEnvironmentCommand { get; }
    public ICommand EditEnvironmentCommand { get; }
    public ICommand DeleteEnvironmentCommand { get; }
    public ICommand CopyEnvironmentCommand { get; }
    public ICommand StartBrowserCommand { get; }
    public ICommand StopBrowserCommand { get; }
    public ICommand BatchStartCommand { get; }
    public ICommand BatchStopCommand { get; }
    public ICommand OpenUrlCommand { get; }
    public ICommand ImportCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand OpenProxyCommand { get; }
    public ICommand OpenSettingsCommand { get; }

    public MainViewModel()
    {
        NewEnvironmentCommand = new RelayCommand(_ => NewEnvironment());
        EditEnvironmentCommand = new RelayCommand(e => EditEnvironment(e as BrowserEnvironment), e => e is BrowserEnvironment);
        DeleteEnvironmentCommand = new RelayCommand(e => DeleteEnvironment(e as BrowserEnvironment), e => e is BrowserEnvironment);
        CopyEnvironmentCommand = new RelayCommand(e => CopyEnvironment(e as BrowserEnvironment), e => e is BrowserEnvironment);
        StartBrowserCommand = new RelayCommand(e => StartBrowser(e as BrowserEnvironment), e => e is BrowserEnvironment);
        StopBrowserCommand = new RelayCommand(e => StopBrowser(e as BrowserEnvironment), e => e is BrowserEnvironment);
        BatchStartCommand = new RelayCommand(_ => BatchStart());
        BatchStopCommand = new RelayCommand(_ => BatchStop());
        OpenUrlCommand = new RelayCommand(e => OpenUrl(e as BrowserEnvironment), e => e is BrowserEnvironment);
        ImportCommand = new RelayCommand(_ => ImportEnvironments());
        ExportCommand = new RelayCommand(_ => ExportEnvironments());
        OpenProxyCommand = new RelayCommand(_ => OpenProxyWindow());
        OpenSettingsCommand = new RelayCommand(_ => OpenSettingsWindow());
    }

    public async Task InitializeAsync()
    {
        _db = new BrowserDbContext();
        await _db.Database.EnsureCreatedAsync();
        await LoadGroupsAsync();
        await LoadEnvironmentsAsync();
        await LoadProxiesAsync();
    }

    private async Task LoadGroupsAsync()
    {
        try
        {
            var groups = await _db.EnvironmentGroups.ToListAsync();
            Groups.Clear();
            Groups.Add(new EnvironmentGroup { Id = 0, Name = "全部" });
            foreach (var g in groups) Groups.Add(g);
            OnPropertyChanged(nameof(TotalCount));
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载分组失败: {ex.Message}";
        }
    }

    private async Task LoadEnvironmentsAsync()
    {
        try
        {
            var envs = await _db.BrowserEnvironments.ToListAsync();
            Environments.Clear();
            foreach (var e in envs) Environments.Add(e);
            FilterEnvironments();
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(RunningCount));
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载环境失败: {ex.Message}";
        }
    }

    private async Task LoadProxiesAsync()
    {
        try
        {
            var proxies = await _db.ProxyConfigs.ToListAsync();
            Proxies.Clear();
            foreach (var p in proxies) Proxies.Add(p);
        }
        catch { }
    }

    private void FilterEnvironments()
    {
        OnPropertyChanged(nameof(TotalCount));
    }

    public async Task<BrowserEnvironment> CreateEnvironmentAsync()
    {
        var env = new BrowserEnvironment { Name = $"环境 {DateTime.Now:HHmmss}", CreatedAt = DateTime.Now };
        _db.BrowserEnvironments.Add(env);
        await _db.SaveChangesAsync();
        Environments.Add(env);
        OnPropertyChanged(nameof(TotalCount));
        return env;
    }

    private void NewEnvironment()
    {
        var env = new BrowserEnvironment { Name = $"新环境 {DateTime.Now:HHmmss}", CreatedAt = DateTime.Now };
        _db.BrowserEnvironments.Add(env);
        _db.SaveChanges();
        Environments.Add(env);
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(RunningCount));
        StatusMessage = "已创建新环境";
    }

    public async Task SaveEnvironmentAsync(BrowserEnvironment env)
    {
        env.UpdatedAt = DateTime.Now;
        _db.BrowserEnvironments.Update(env);
        await _db.SaveChangesAsync();
        var idx = Environments.IndexOf(Environments.FirstOrDefault(e => e.Id == env.Id)!);
        if (idx >= 0) Environments[idx] = env;
        StatusMessage = "环境已保存";
    }

    private async void EditEnvironment(BrowserEnvironment? env)
    {
        if (env == null) return;
        var win = new Views.EnvironmentEditWindow(env, _db);
        if (win.ShowDialog() == true)
        {
            await SaveEnvironmentAsync(env);
        }
    }

    private async void DeleteEnvironment(BrowserEnvironment? env)
    {
        if (env == null) return;
        var result = System.Windows.MessageBox.Show($"确定删除环境 '{env.Name}'?", "确认删除",
            System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
        if (result != System.Windows.MessageBoxResult.Yes) return;

        await StopBrowser(env);
        _db.BrowserEnvironments.Remove(env);
        await _db.SaveChangesAsync();
        Environments.Remove(env);
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(RunningCount));
        StatusMessage = "环境已删除";
    }

    private async void CopyEnvironment(BrowserEnvironment? env)
    {
        if (env == null) return;
        var copy = new BrowserEnvironment
        {
            Name = env.Name + " (副本)",
            GroupId = env.GroupId,
            StartupUrl = env.StartupUrl,
            UserAgent = env.UserAgent,
            Resolution = env.Resolution,
            Timezone = env.Timezone,
            Languages = env.Languages,
            WebGLVendor = env.WebGLVendor,
            WebGLRenderer = env.WebGLRenderer,
            EnableWebRTC = env.EnableWebRTC,
            EnableCookies = env.EnableCookies,
            EnableJavaScript = env.EnableJavaScript,
            ProxyConfig = env.ProxyConfig,
            Remark = env.Remark,
            Status = BrowserStatus.Idle,
            CreatedAt = DateTime.Now
        };
        _db.BrowserEnvironments.Add(copy);
        await _db.SaveChangesAsync();
        Environments.Add(copy);
        OnPropertyChanged(nameof(TotalCount));
        StatusMessage = "环境已复制";
    }

    private async void StartBrowser(BrowserEnvironment? env)
    {
        if (env == null) return;
        try
        {
            StatusMessage = $"正在启动 {env.Name}...";
            env.Status = BrowserStatus.Starting;
            OnPropertyChanged(nameof(RunningCount));

            ProxyConfigModel? proxy = null;
            if (!string.IsNullOrEmpty(env.ProxyConfig))
            {
                proxy = JsonSerializer.Deserialize<ProxyConfigModel>(env.ProxyConfig);
            }

            var page = await BrowserService.Instance.LaunchBrowserAsync(env, proxy);
            if (page != null)
            {
                env.Status = BrowserStatus.Running;
                if (!string.IsNullOrEmpty(env.StartupUrl))
                {
                    await page.GotoAsync(env.StartupUrl);
                }
                StatusMessage = $"{env.Name} 已启动";
            }
            else
            {
                env.Status = BrowserStatus.Error;
                StatusMessage = $"{env.Name} 启动失败";
            }
            OnPropertyChanged(nameof(RunningCount));
        }
        catch (Exception ex)
        {
            env.Status = BrowserStatus.Error;
            StatusMessage = $"启动失败: {ex.Message}";
            OnPropertyChanged(nameof(RunningCount));
        }
    }

    private async void StopBrowser(BrowserEnvironment? env)
    {
        if (env == null) return;
        try
        {
            await BrowserService.Instance.CloseBrowserAsync(env.Id);
            env.Status = BrowserStatus.Idle;
            OnPropertyChanged(nameof(RunningCount));
            StatusMessage = $"{env.Name} 已关闭";
        }
        catch (Exception ex)
        {
            StatusMessage = $"关闭失败: {ex.Message}";
        }
    }

    private async void BatchStart()
    {
        var toStart = Environments.Where(e => e.Status == BrowserStatus.Idle).ToList();
        StatusMessage = $"正在批量启动 {toStart.Count} 个环境...";
        foreach (var env in toStart)
        {
            StartBrowser(env);
            await Task.Delay(500);
        }
    }

    private async void BatchStop()
    {
        var toStop = Environments.Where(e => e.Status == BrowserStatus.Running).ToList();
        StatusMessage = $"正在批量关闭 {toStop.Count} 个环境...";
        foreach (var env in toStop)
        {
            StopBrowser(env);
            await Task.Delay(200);
        }
    }

    private async void OpenUrl(BrowserEnvironment? env)
    {
        if (env == null || env.Status != BrowserStatus.Running) return;
        var win = new Views.TextInputWindow("输入网址", "请输入要打开的网址：", "https://");
        if (win.ShowDialog() == true && !string.IsNullOrWhiteSpace(win.InputText))
        {
            await BrowserService.Instance.OpenUrlAsync(env.Id, win.InputText);
            StatusMessage = $"已在 {env.Name} 打开 {win.InputText}";
        }
    }

    private async void ImportEnvironments()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "JSON文件|*.json|所有文件|*.*",
            Title = "导入环境"
        };
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var json = await System.IO.File.ReadAllTextAsync(dialog.FileName);
                var imported = JsonSerializer.Deserialize<List<BrowserEnvironment>>(json);
                if (imported != null)
                {
                    foreach (var env in imported)
                    {
                        env.Id = 0;
                        env.CreatedAt = DateTime.Now;
                        _db.BrowserEnvironments.Add(env);
                    }
                    await _db.SaveChangesAsync();
                    await LoadEnvironmentsAsync();
                    StatusMessage = $"成功导入 {imported.Count} 个环境";
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"导入失败: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    private async void ExportEnvironments()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "JSON文件|*.json",
            Title = "导出环境",
            FileName = $"environments_{DateTime.Now:yyyyMMdd}.json"
        };
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var envs = await _db.BrowserEnvironments.ToListAsync();
                var json = JsonSerializer.Serialize(envs, new JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(dialog.FileName, json);
                StatusMessage = $"已导出 {envs.Count} 个环境";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"导出失败: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    private void OpenProxyWindow()
    {
        var win = new Views.ProxyWindow(_db);
        win.ShowDialog();
    }

    private void OpenSettingsWindow()
    {
        var win = new Views.SettingsWindow();
        win.ShowDialog();
    }

    public async Task CleanupAsync()
    {
        foreach (var env in Environments.Where(e => e.Status == BrowserStatus.Running))
        {
            await BrowserService.Instance.CloseBrowserAsync(env.Id);
        }
        _db?.Dispose();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
