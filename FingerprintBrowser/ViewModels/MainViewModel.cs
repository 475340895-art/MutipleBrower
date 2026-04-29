using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FingerprintBrowser.Data;
using FingerprintBrowser.Models;
using FingerprintBrowser.Services;
using Microsoft.EntityFrameworkCore;

namespace FingerprintBrowser.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly BrowserDbContext _db;
        private readonly PlaywrightService _playwrightService;
        private using Application = System.Windows.Application;

        [ObservableProperty]
        private ObservableCollection<BrowserEnvironment> _environments = new();

        [ObservableProperty]
        private ObservableCollection<BrowserEnvironment> _filteredEnvironments = new();

        [ObservableProperty]
        private ObservableCollection<EnvironmentGroup> _groups = new();

        [ObservableProperty]
        private EnvironmentGroup? _selectedGroup;

        [ObservableProperty]
        private BrowserEnvironment? _selectedEnvironment;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private int _runningCount;

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private string _statusMessage = "就绪";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private int _maxConcurrency = 5;

        [ObservableProperty]
        private int _startupDelay = 1000;

        public MainViewModel()
        {
            _db = new BrowserDbContext();
            _playwrightService = PlaywrightService.Instance;
        }

        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "正在加载数据...";

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
            var groups = await _db.Groups.OrderBy(g => g.SortOrder).ToListAsync();
            Groups.Clear();
            Groups.Add(new EnvironmentGroup { Id = 0, Name = "全部分组", Color = "#1890ff" });
            foreach (var group in groups)
            {
                Groups.Add(group);
            }
        }

        public async Task LoadEnvironmentsAsync()
        {
            var environments = await _db.Environments
                .Include(e => e.Group)
                .Include(e => e.ProxyConfig)
                .ToListAsync();

            Environments.Clear();
            foreach (var env in environments)
            {
                Environments.Add(env);
            }

            TotalCount = Environments.Count;
            UpdateRunningCount();
            FilterEnvironments();
        }

        partial void OnSelectedGroupChanged(EnvironmentGroup? value)
        {
            FilterEnvironments();
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterEnvironments();
        }

        public void FilterEnvironments()
        {
            var filtered = Environments.AsEnumerable();

            if (SelectedGroup != null && SelectedGroup.Id > 0)
            {
                filtered = filtered.Where(e => e.GroupId == SelectedGroup.Id);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLower();
                filtered = filtered.Where(e =>
                    e.Name.ToLower().Contains(search) ||
                    e.Remark.ToLower().Contains(search));
            }

            FilteredEnvironments.Clear();
            foreach (var env in filtered)
            {
                FilteredEnvironments.Add(env);
            }
        }

        [RelayCommand]
        private async Task CreateEnvironmentAsync()
        {
            var window = new Views.EnvironmentEditWindow();
            if (window.ShowDialog() == true)
            {
                await LoadEnvironmentsAsync();
                StatusMessage = "环境创建成功";
            }
        }

        [RelayCommand]
        private async Task EditEnvironmentAsync(BrowserEnvironment? env)
        {
            if (env == null) return;

            var window = new Views.EnvironmentEditWindow(env.Id);
            if (window.ShowDialog() == true)
            {
                await LoadEnvironmentsAsync();
                StatusMessage = "环境更新成功";
            }
        }

        [RelayCommand]
        private async Task CopyEnvironmentAsync(BrowserEnvironment? env)
        {
            if (env == null) return;

            var copy = new BrowserEnvironment
            {
                Name = $"{env.Name} (副本)",
                GroupId = env.GroupId,
                ProxyId = env.ProxyId,
                BrowserType = env.BrowserType,
                Resolution = env.Resolution,
                Timezone = env.Timezone,
                Languages = env.Languages,
                UserAgent = env.UserAgent,
                WebGLVendor = env.WebGLVendor,
                WebGLRenderer = env.WebGLRenderer,
                EnableWebRTC = env.EnableWebRTC,
                EnableCookies = env.EnableCookies,
                EnableJavaScript = env.EnableJavaScript,
                StartupUrl = env.StartupUrl,
                Remark = env.Remark
            };

            _db.Environments.Add(copy);
            await _db.SaveChangesAsync();
            await LoadEnvironmentsAsync();
            StatusMessage = "环境复制成功";
        }

        [RelayCommand]
        private async Task DeleteEnvironmentAsync(BrowserEnvironment? env)
        {
            if (env == null) return;

            var result = MessageBox.Show(
                $"确定要删除环境「{env.Name}」吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await _playwrightService.CloseBrowserAsync(env.Id);
                _db.Environments.Remove(env);
                await _db.SaveChangesAsync();
                await LoadEnvironmentsAsync();
                StatusMessage = "环境已删除";
            }
        }

        [RelayCommand]
        private async Task StartBrowserAsync(BrowserEnvironment? env)
        {
            if (env == null) return;

            try
            {
                if (env.Status == BrowserEnvironmentStatus.Running)
                {
                    StatusMessage = "环境已在运行中";
                    return;
                }

                env.Status = BrowserEnvironmentStatus.Starting;
                StatusMessage = $"正在启动 {env.Name}...";

                await _playwrightService.LaunchBrowserAsync(env);
                env.Status = BrowserEnvironmentStatus.Running;
                env.RunningBrowserId = env.Id;

                UpdateRunningCount();
                StatusMessage = $"已启动 {env.Name}";
            }
            catch (Exception ex)
            {
                env.Status = BrowserEnvironmentStatus.Error;
                StatusMessage = $"启动失败: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task StopBrowserAsync(BrowserEnvironment? env)
        {
            if (env == null) return;

            try
            {
                env.Status = BrowserEnvironmentStatus.Stopping;
                StatusMessage = $"正在停止 {env.Name}...";

                await _playwrightService.CloseBrowserAsync(env.Id);
                env.Status = BrowserEnvironmentStatus.Idle;
                env.RunningBrowserId = null;

                UpdateRunningCount();
                StatusMessage = $"已停止 {env.Name}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"停止失败: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task OpenBrowserUrlAsync(BrowserEnvironment? env)
        {
            if (env?.Status != BrowserEnvironmentStatus.Running) return;

            var urlWindow = new Views.UrlInputWindow();
            if (urlWindow.ShowDialog() == true && !string.IsNullOrWhiteSpace(urlWindow.InputUrl))
            {
                await _playwrightService.OpenUrl(env.Id, urlWindow.InputUrl);
            }
        }

        [RelayCommand]
        private async Task BatchStartAsync()
        {
            var toStart = FilteredEnvironments
                .Where(e => e.Status == BrowserEnvironmentStatus.Idle)
                .Take(MaxConcurrency)
                .ToList();

            if (!toStart.Any())
            {
                StatusMessage = "没有需要启动的环境";
                return;
            }

            StatusMessage = $"正在批量启动 {toStart.Count} 个环境...";

            foreach (var env in toStart)
            {
                await StartBrowserAsync(env);
                await Task.Delay(StartupDelay);
            }

            StatusMessage = $"已启动 {toStart.Count} 个环境";
        }

        [RelayCommand]
        private async Task BatchStopAsync()
        {
            var toStop = FilteredEnvironments
                .Where(e => e.Status == BrowserEnvironmentStatus.Running)
                .ToList();

            if (!toStop.Any())
            {
                StatusMessage = "没有正在运行的环境";
                return;
            }

            StatusMessage = $"正在批量停止 {toStop.Count} 个环境...";

            foreach (var env in toStop)
            {
                await StopBrowserAsync(env);
            }

            StatusMessage = $"已停止 {toStop.Count} 个环境";
        }

        [RelayCommand]
        private async Task ImportEnvironmentsAsync()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                Title = "导入环境"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    IsLoading = true;
                    var imported = await ImportExportService.ImportEnvironmentsAsync(dialog.FileName, _db);
                    await LoadEnvironmentsAsync();
                    StatusMessage = $"成功导入 {imported} 个环境";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"导入失败: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        [RelayCommand]
        private async Task ExportEnvironmentsAsync()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON 文件 (*.json)|*.json",
                Title = "导出环境",
                FileName = $"environments_{DateTime.Now:yyyyMMdd}.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    IsLoading = true;
                    await ImportExportService.ExportEnvironmentsAsync(dialog.FileName, FilteredEnvironments.ToList());
                    StatusMessage = "导出成功";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"导出失败: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        public async Task SelectGroupAsync(EnvironmentGroup? group)
        {
            SelectedGroup = group;
        }

        public async Task ShowEnvironmentDetailsAsync(BrowserEnvironment? env)
        {
            SelectedEnvironment = env;
        }

        [RelayCommand]
        private void OpenSettings()
        {
            var window = new Views.SettingsWindow();
            window.ShowDialog();
        }

        [RelayCommand]
        private void OpenProxyManager()
        {
            var window = new Views.ProxyWindow();
            window.ShowDialog();
        }

        [RelayCommand]
        private async Task CreateGroupAsync()
        {
            var input = new Views.TextInputWindow("新建分组", "请输入分组名称：");
            if (input.ShowDialog() == true && !string.IsNullOrWhiteSpace(input.InputText))
            {
                var group = new EnvironmentGroup
                {
                    Name = input.InputText,
                    Color = GenerateRandomColor(),
                    SortOrder = Groups.Count
                };
                _db.Groups.Add(group);
                await _db.SaveChangesAsync();
                await LoadGroupsAsync();
                StatusMessage = "分组创建成功";
            }
        }

        [RelayCommand]
        private async Task DeleteGroupAsync(EnvironmentGroup? group)
        {
            if (group == null || group.Id == 0) return;

            var result = MessageBox.Show(
                $"确定要删除分组「{group.Name}」吗？该分组下的环境将移至「未分组」。",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var envsInGroup = await _db.Environments.Where(e => e.GroupId == group.Id).ToListAsync();
                foreach (var env in envsInGroup)
                {
                    env.GroupId = 0;
                }
                _db.Groups.Remove(group);
                await _db.SaveChangesAsync();
                await LoadGroupsAsync();
                await LoadEnvironmentsAsync();
                StatusMessage = "分组已删除";
            }
        }

        public async Task ChangeThemeAsync(string theme)
        {
            try
            {
                var app = Application.Current;
                app.Dispatcher.Invoke(() =>
                {
                    var resources = app.Resources.MergedDictionaries;
                    var skinDict = resources.FirstOrDefault(r =>
                        r.Source?.OriginalString.Contains("Skin") == true);

                    if (skinDict != null)
                    {
                        resources.Remove(skinDict);
                    }

                    var newSkin = new ResourceDictionary
                    {
                        Source = theme switch
                        {
                            "Dark" => new Uri("pack://application:,,,/HandyControl;component/Themes/SkinDark.xaml"),
                            "Light" => new Uri("pack://application:,,,/HandyControl;component/Themes/SkinDefault.xaml"),
                            _ => new Uri("pack://application:,,,/HandyControl;component/Themes/SkinDark.xaml")
                        }
                    };
                    resources.Insert(0, newSkin);
                });

                await Task.CompletedTask;
            }
            catch { }
        }

        private void UpdateRunningCount()
        {
            RunningCount = Environments.Count(e => e.Status == BrowserEnvironmentStatus.Running);
        }

        private string GenerateRandomColor()
        {
            var colors = new[] { "#1890ff", "#52c41a", "#faad14", "#f5222d", "#722ed1", "#13c2c2", "#eb2f96" };
            return colors[new Random().Next(colors.Length)];
        }

        public async Task CleanupAsync()
        {
            foreach (var env in Environments.Where(e => e.Status == BrowserEnvironmentStatus.Running))
            {
                await StopBrowserAsync(env);
            }
        }
    }
}
