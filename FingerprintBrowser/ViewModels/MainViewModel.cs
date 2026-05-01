using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FingerprintBrowser.Data;
using FingerprintBrowser.Models;
using FingerprintBrowser.Services;
using FingerprintBrowser.Views;
using Microsoft.EntityFrameworkCore;

namespace FingerprintBrowser.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly BrowserDbContext _db;
        private readonly BrowserService _browserService;
        private readonly PlaywrightService _playwrightService;

        // ========== Properties ==========

        [ObservableProperty]
        private ObservableCollection<BrowserEnvironment> _environments = new();

        [ObservableProperty]
        private ObservableCollection<EnvironmentGroup> _groups = new();

        [ObservableProperty]
        private ObservableCollection<ProxyConfigModel> _proxyConfigs = new();

        [ObservableProperty]
        private ObservableCollection<BrowserEnvironment> _filteredEnvironments = new();

        [ObservableProperty]
        private EnvironmentGroup? _selectedGroup;

        [ObservableProperty]
        private BrowserEnvironment? _selectedEnvironment;

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

        [ObservableProperty]
        private int _stoppedCount;

        [ObservableProperty]
        private int _selectedTabIndex;

        [ObservableProperty]
        private bool _isListView = true;

        [ObservableProperty]
        private int _maxConcurrency = 5;

        [ObservableProperty]
        private int _proxyNormalCount;

        [ObservableProperty]
        private int _proxyFailedCount;

        [ObservableProperty]
        private int _proxyUntestedCount;

        // ========== Constructor ==========

        public MainViewModel()
        {
            _db = new BrowserDbContext();
            _playwrightService = new PlaywrightService();
            _browserService = new BrowserService(_db, _playwrightService);

            DatabaseInitializer.Initialize(_db);
        }

        // ========== Load Data ==========

        [RelayCommand]
        public async Task LoadDataAsync()
        {
            IsLoading = true;
            StatusMessage = "加载数据中...";

            try
            {
                await LoadGroupsAsync();
                await LoadEnvironmentsAsync();
                await LoadProxyConfigsAsync();
                UpdateCounts();
                StatusMessage = "数据加载完成";
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
            var groups = await _db.Groups.OrderBy(g => g.Id).ToListAsync();
            foreach (var g in groups)
            {
                g.EnvironmentCount = await _db.Environments.CountAsync(e => e.GroupName == g.Name);
            }
            Groups = new ObservableCollection<EnvironmentGroup>(groups);
        }

        public async Task LoadEnvironmentsAsync()
        {
            var query = _db.Environments.OrderByDescending(e => e.LastOpenedAt).AsQueryable();

            if (SelectedGroup != null)
            {
                query = query.Where(e => e.GroupName == SelectedGroup.Name);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var keyword = SearchText.ToLower();
                query = query.Where(e => e.Name.ToLower().Contains(keyword) ||
                                         (e.Remarks != null && e.Remarks.ToLower().Contains(keyword)));
            }

            var envs = await query.ToListAsync();
            Environments = new ObservableCollection<BrowserEnvironment>(envs);
            FilteredEnvironments = new ObservableCollection<BrowserEnvironment>(envs);
        }

        public async Task LoadProxyConfigsAsync()
        {
            var proxies = await _db.ProxyConfigs.OrderBy(p => p.Id).ToListAsync();
            ProxyConfigs = new ObservableCollection<ProxyConfigModel>(proxies);
            ProxyNormalCount = proxies.Count(p => p.Status == ProxyStatus.Normal);
            ProxyFailedCount = proxies.Count(p => p.Status == ProxyStatus.Failed);
            ProxyUntestedCount = proxies.Count(p => p.Status == ProxyStatus.Untested);
        }

        private void UpdateCounts()
        {
            TotalCount = Environments.Count;
            RunningCount = Environments.Count(e => e.Status == BrowserStatus.Running);
            StoppedCount = Environments.Count(e => e.Status == BrowserStatus.Stopped);
        }

        // ========== Environment Commands ==========

        [RelayCommand]
        public async Task StartBrowserAsync(BrowserEnvironment? env)
        {
            if (env == null) return;

            if (RunningCount >= MaxConcurrency)
            {
                System.Windows.MessageBox.Show($"已达到最大并发数 {MaxConcurrency}，请先停止其他环境", "提示");
                return;
            }

            StatusMessage = $"正在启动 {env.Name}...";
            var success = await _browserService.StartBrowserAsync(env);

            if (success)
            {
                StatusMessage = $"{env.Name} 已启动";
            }
            else
            {
                StatusMessage = $"{env.Name} 启动失败";
                System.Windows.MessageBox.Show($"启动 {env.Name} 失败，请检查配置", "错误");
            }

            await LoadEnvironmentsAsync();
            UpdateCounts();
        }

        [RelayCommand]
        public async Task StopBrowserAsync(BrowserEnvironment? env)
        {
            if (env == null) return;

            StatusMessage = $"正在停止 {env.Name}...";
            await _browserService.StopBrowserAsync(env);
            StatusMessage = $"{env.Name} 已停止";

            await LoadEnvironmentsAsync();
            UpdateCounts();
        }

        [RelayCommand]
        public async Task StartAllAsync()
        {
            var stopped = Environments.Where(e => e.Status == BrowserStatus.Stopped).ToList();
            int started = 0;
            foreach (var env in stopped)
            {
                if (RunningCount >= MaxConcurrency) break;
                if (await _browserService.StartBrowserAsync(env)) started++;
            }
            await LoadEnvironmentsAsync();
            UpdateCounts();
            StatusMessage = $"已启动 {started} 个环境";
        }

        [RelayCommand]
        public async Task StopAllAsync()
        {
            await _browserService.StopAllAsync();
            await LoadEnvironmentsAsync();
            UpdateCounts();
            StatusMessage = "所有环境已停止";
        }

        [RelayCommand]
        public async Task DeleteEnvironmentAsync(BrowserEnvironment? env)
        {
            if (env == null) return;

            var result = System.Windows.MessageBox.Show($"确定删除环境 \"{env.Name}\"？", "确认删除",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                if (env.Status == BrowserStatus.Running)
                    await _browserService.StopBrowserAsync(env);

                _db.Environments.Remove(env);
                await _db.SaveChangesAsync();
                await LoadEnvironmentsAsync();
                await LoadGroupsAsync();
                UpdateCounts();
                StatusMessage = $"已删除 {env.Name}";
            }
        }

        [RelayCommand]
        public async Task BatchDeleteAsync()
        {
            var selected = Environments.Where(e => e.Status == BrowserStatus.Stopped).ToList();
            if (!selected.Any())
            {
                System.Windows.MessageBox.Show("没有可删除的已停止环境", "提示");
                return;
            }

            var result = System.Windows.MessageBox.Show(
                $"确定删除 {selected.Count} 个已停止的环境？", "批量删除",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _db.Environments.RemoveRange(selected);
                await _db.SaveChangesAsync();
                await LoadEnvironmentsAsync();
                await LoadGroupsAsync();
                UpdateCounts();
                StatusMessage = $"已批量删除 {selected.Count} 个环境";
            }
        }

        // ========== New/Edit Environment ==========

        [RelayCommand]
        public void NewEnvironment()
        {
            var dialog = new EnvironmentEditWindow();
            dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                var env = dialog.GetEnvironment();
                _db.Environments.Add(env);
                _db.SaveChanges();
                LoadEnvironmentsAsync().Wait();
                LoadGroupsAsync().Wait();
                UpdateCounts();
                StatusMessage = $"已创建环境 {env.Name}";
            }
        }

        [RelayCommand]
        public void EditEnvironment(BrowserEnvironment? env)
        {
            if (env == null) return;

            var dialog = new EnvironmentEditWindow();
            dialog.Owner = Application.Current.MainWindow;
            dialog.SetEnvironment(env);

            if (dialog.ShowDialog() == true)
            {
                var updated = dialog.GetEnvironment();
                updated.Id = env.Id;
                _db.Environments.Update(updated);
                _db.SaveChanges();
                LoadEnvironmentsAsync().Wait();
                StatusMessage = $"已更新环境 {updated.Name}";
            }
        }

        // ========== Fingerprint Edit ==========

        [RelayCommand]
        public void EditFingerprint(BrowserEnvironment? env)
        {
            if (env == null) return;

            var dialog = new FingerprintEditWindow(env);
            dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                var updated = dialog.GetEnvironment();
                _db.Environments.Update(updated);
                _db.SaveChanges();
                LoadEnvironmentsAsync().Wait();
                StatusMessage = $"已更新 {updated.Name} 的指纹配置";
            }
        }

        // ========== Group Commands ==========

        [RelayCommand]
        public void NewGroup()
        {
            var dialog = new AddGroupWindow();
            dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                var group = dialog.GetGroup();
                _db.Groups.Add(group);
                _db.SaveChanges();
                LoadGroupsAsync().Wait();
                StatusMessage = $"已创建分组 {group.Name}";
            }
        }

        [RelayCommand]
        public async Task DeleteGroupAsync(EnvironmentGroup? group)
        {
            if (group == null) return;

            var count = await _db.Environments.CountAsync(e => e.GroupName == group.Name);
            if (count > 0)
            {
                System.Windows.MessageBox.Show($"分组 \"{group.Name}\" 下有 {count} 个环境，请先移除", "无法删除");
                return;
            }

            var result = System.Windows.MessageBox.Show($"确定删除分组 \"{group.Name}\"？", "确认",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _db.Groups.Remove(group);
                await _db.SaveChangesAsync();
                await LoadGroupsAsync();
                SelectedGroup = null;
                await LoadEnvironmentsAsync();
                StatusMessage = $"已删除分组 {group.Name}";
            }
        }

        partial void OnSelectedGroupChanged(EnvironmentGroup? value)
        {
            LoadEnvironmentsAsync().Wait();
        }

        partial void OnSearchTextChanged(string value)
        {
            LoadEnvironmentsAsync().Wait();
        }

        // ========== Proxy Commands ==========

        [RelayCommand]
        public void NewProxy()
        {
            var dialog = new AddProxyWindow();
            dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                var proxy = dialog.GetProxyConfig();
                _db.ProxyConfigs.Add(proxy);
                _db.SaveChanges();
                LoadProxyConfigsAsync().Wait();
                StatusMessage = $"已添加代理 {proxy.Name}";
            }
        }

        [RelayCommand]
        public async Task TestProxyAsync(ProxyConfigModel? proxy)
        {
            if (proxy == null) return;

            StatusMessage = $"正在测试代理 {proxy.Name}...";
            var success = await _browserService.TestProxyAsync(proxy);
            await LoadProxyConfigsAsync();
            StatusMessage = success ? $"代理 {proxy.Name} 连接正常" : $"代理 {proxy.Name} 连接失败";
        }

        [RelayCommand]
        public async Task DeleteProxyAsync(ProxyConfigModel? proxy)
        {
            if (proxy == null) return;

            _db.ProxyConfigs.Remove(proxy);
            await _db.SaveChangesAsync();
            await LoadProxyConfigsAsync();
            StatusMessage = $"已删除代理 {proxy.Name}";
        }

        // ========== Settings ==========

        [ObservableProperty]
        private bool _autoStartBrowser;

        [ObservableProperty]
        private bool _minimizeToTray;

        [ObservableProperty]
        private bool _enableAutoUpdate = true;

        [ObservableProperty]
        private bool _enableCanvasNoise = true;

        [ObservableProperty]
        private bool _enableAudioNoise = true;

        [ObservableProperty]
        private bool _enableWebGLNoise;

        [ObservableProperty]
        private bool _enableFontNoise;

        [ObservableProperty]
        private bool _blockWebRTC;

        [ObservableProperty]
        private string _defaultUserAgent = "Chrome 120 / Win10";

        [ObservableProperty]
        private string _defaultResolution = "1920x1080";

        [ObservableProperty]
        private string _defaultLanguage = "en-US";

        [ObservableProperty]
        private string _defaultTimezone = "America/New_York";

        [ObservableProperty]
        private string _browserPath = string.Empty;

        [ObservableProperty]
        private int _startupDelay = 2;

        [ObservableProperty]
        private string _dataPath = string.Empty;
    }
}
