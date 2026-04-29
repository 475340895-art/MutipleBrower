using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FingerprintBrowser.Data;
using FingerprintBrowser.Models;
using FingerprintBrowser.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FingerprintBrowser.ViewModels;

/// <summary>
/// 主窗口视图模型
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IBrowserService _browserService;
    private readonly IDialogService _dialogService;
    private readonly IImportExportService _importExportService;

    [ObservableProperty]
    private ObservableCollection<EnvironmentGroup> _groups = new();

    [ObservableProperty]
    private EnvironmentGroup? _selectedGroup;

    [ObservableProperty]
    private BrowserEnvironment? _selectedEnvironment;

    [ObservableProperty]
    private ObservableCollection<BrowserEnvironment> _selectedEnvironments = new();

    [ObservableProperty]
    private int _runningCount;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    public MainViewModel()
    {
        _browserService = ServiceLocator.Get<IBrowserService>();
        _dialogService = ServiceLocator.Get<IDialogService>();
        _importExportService = ServiceLocator.Get<IImportExportService>();

        LoadDataCommand.Execute(null);
    }

    /// <summary>
    /// 加载数据
    /// </summary>
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "正在加载数据...";

            using var context = new BrowserDbContext();
            await context.Groups
                .Include(g => g.Environments)
                .ThenInclude(e => e.Proxy)
                .LoadAsync();

            Groups = new ObservableCollection<EnvironmentGroup>(
                context.Groups.OrderBy(g => g.SortOrder));

            UpdateStatistics();

            StatusMessage = $"加载完成，共 {Groups.Sum(g => g.TotalCount)} 个环境";
            Log.Information("数据加载完成: {Count} 个分组", Groups.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载数据失败");
            _dialogService.ShowException(ex);
            StatusMessage = "加载失败";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 创建新环境
    /// </summary>
    [RelayCommand]
    private async Task CreateEnvironmentAsync()
    {
        try
        {
            var name = _dialogService.Input("请输入环境名称:", "新建环境");
            if (string.IsNullOrWhiteSpace(name)) return;

            var groupId = SelectedGroup?.Id ?? Groups.FirstOrDefault()?.Id ?? 1;

            var environment = new BrowserEnvironment
            {
                Name = name,
                GroupId = groupId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            using var context = new BrowserDbContext();
            context.Environments.Add(environment);
            await context.SaveChangesAsync();

            // 刷新数据
            await LoadDataAsync();

            // 选中新建的环境
            var group = Groups.FirstOrDefault(g => g.Id == groupId);
            if (group != null)
            {
                SelectedGroup = group;
                SelectedEnvironment = group.Environments.LastOrDefault();
            }

            StatusMessage = $"已创建环境: {name}";
            Log.Information("创建环境成功: {Name}", name);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "创建环境失败");
            _dialogService.ShowException(ex);
        }
    }

    /// <summary>
    /// 复制环境
    /// </summary>
    [RelayCommand]
    private async Task DuplicateEnvironmentAsync()
    {
        if (SelectedEnvironment == null)
        {
            _dialogService.Warning("请先选择一个环境");
            return;
        }

        try
        {
            using var context = new BrowserDbContext();
            var clone = SelectedEnvironment.Clone();
            context.Environments.Add(clone);
            await context.SaveChangesAsync();

            await LoadDataAsync();

            StatusMessage = $"已复制环境: {clone.Name}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "复制环境失败");
            _dialogService.ShowException(ex);
        }
    }

    /// <summary>
    /// 删除环境
    /// </summary>
    [RelayCommand]
    private async Task DeleteEnvironmentAsync()
    {
        if (SelectedEnvironment == null)
        {
            _dialogService.Warning("请先选择一个环境");
            return;
        }

        if (!_dialogService.Confirm($"确定要删除环境 '{SelectedEnvironment.Name}' 吗?"))
        {
            return;
        }

        try
        {
            // 如果正在运行，先停止
            if (SelectedEnvironment.Status == (int)BrowserStatus.Running)
            {
                await _browserService.StopEnvironmentAsync(SelectedEnvironment.Id);
            }

            using var context = new BrowserDbContext();
            var environment = await context.Environments.FindAsync(SelectedEnvironment.Id);
            if (environment != null)
            {
                context.Environments.Remove(environment);
                await context.SaveChangesAsync();
            }

            await LoadDataAsync();
            SelectedEnvironment = null;

            StatusMessage = "环境已删除";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "删除环境失败");
            _dialogService.ShowException(ex);
        }
    }

    /// <summary>
    /// 启动选中环境
    /// </summary>
    [RelayCommand]
    private async Task StartEnvironmentAsync()
    {
        if (SelectedEnvironment == null)
        {
            _dialogService.Warning("请先选择一个环境");
            return;
        }

        if (SelectedEnvironment.Status == (int)BrowserStatus.Running)
        {
            _dialogService.Info("该环境已经在运行中");
            return;
        }

        try
        {
            StatusMessage = $"正在启动: {SelectedEnvironment.Name}...";
            var success = await _browserService.StartEnvironmentAsync(SelectedEnvironment);

            if (success)
            {
                SelectedEnvironment.Status = (int)BrowserStatus.Running;
                UpdateStatistics();
                StatusMessage = $"已启动: {SelectedEnvironment.Name}";
            }
            else
            {
                StatusMessage = $"启动失败: {SelectedEnvironment.Name}";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "启动环境失败");
            _dialogService.ShowException(ex);
            StatusMessage = "启动失败";
        }
    }

    /// <summary>
    /// 停止选中环境
    /// </summary>
    [RelayCommand]
    private async Task StopEnvironmentAsync()
    {
        if (SelectedEnvironment == null)
        {
            _dialogService.Warning("请先选择一个环境");
            return;
        }

        if (SelectedEnvironment.Status != (int)BrowserStatus.Running)
        {
            _dialogService.Info("该环境未在运行");
            return;
        }

        try
        {
            StatusMessage = $"正在停止: {SelectedEnvironment.Name}...";
            var success = await _browserService.StopEnvironmentAsync(SelectedEnvironment.Id);

            if (success)
            {
                SelectedEnvironment.Status = (int)BrowserStatus.Stopped;
                UpdateStatistics();
                StatusMessage = $"已停止: {SelectedEnvironment.Name}";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "停止环境失败");
            _dialogService.ShowException(ex);
        }
    }

    /// <summary>
    /// 批量启动
    /// </summary>
    [RelayCommand]
    private async Task BatchStartAsync()
    {
        var toStart = GetFilteredEnvironments()
            .Where(e => e.Status != (int)BrowserStatus.Running)
            .ToList();

        if (!toStart.Any())
        {
            _dialogService.Info("没有可启动的环境");
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = $"正在批量启动 {toStart.Count} 个环境...";

            var ids = toStart.Select(e => e.Id);
            await _browserService.StartEnvironmentsAsync(ids);

            await LoadDataAsync();

            StatusMessage = $"已启动 {toStart.Count} 个环境";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "批量启动失败");
            _dialogService.ShowException(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 批量停止
    /// </summary>
    [RelayCommand]
    private async Task BatchStopAsync()
    {
        var toStop = GetFilteredEnvironments()
            .Where(e => e.Status == (int)BrowserStatus.Running)
            .ToList();

        if (!toStop.Any())
        {
            _dialogService.Info("没有运行中的环境");
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = $"正在批量停止 {toStop.Count} 个环境...";

            var ids = toStop.Select(e => e.Id);
            await _browserService.StopEnvironmentsAsync(ids);

            await LoadDataAsync();

            StatusMessage = $"已停止 {toStop.Count} 个环境";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "批量停止失败");
            _dialogService.ShowException(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 创建分组
    /// </summary>
    [RelayCommand]
    private async Task CreateGroupAsync()
    {
        var name = _dialogService.Input("请输入分组名称:", "新建分组");
        if (string.IsNullOrWhiteSpace(name)) return;

        try
        {
            using var context = new BrowserDbContext();
            var group = new EnvironmentGroup
            {
                Name = name,
                SortOrder = Groups.Count,
                CreatedAt = DateTime.Now
            };
            context.Groups.Add(group);
            await context.SaveChangesAsync();

            await LoadDataAsync();
            StatusMessage = $"已创建分组: {name}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "创建分组失败");
            _dialogService.ShowException(ex);
        }
    }

    /// <summary>
    /// 删除分组
    /// </summary>
    [RelayCommand]
    private async Task DeleteGroupAsync()
    {
        if (SelectedGroup == null)
        {
            _dialogService.Warning("请先选择一个分组");
            return;
        }

        if (SelectedGroup.TotalCount > 0)
        {
            _dialogService.Warning("该分组下还有环境，请先删除或移动环境");
            return;
        }

        if (!_dialogService.Confirm($"确定要删除分组 '{SelectedGroup.Name}' 吗?")) return;

        try
        {
            using var context = new BrowserDbContext();
            var group = await context.Groups.FindAsync(SelectedGroup.Id);
            if (group != null)
            {
                context.Groups.Remove(group);
                await context.SaveChangesAsync();
            }

            await LoadDataAsync();
            SelectedGroup = Groups.FirstOrDefault();
            StatusMessage = "分组已删除";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "删除分组失败");
            _dialogService.ShowException(ex);
        }
    }

    /// <summary>
    /// 导入环境
    /// </summary>
    [RelayCommand]
    private async Task ImportEnvironmentAsync()
    {
        var filePath = _dialogService.OpenFile("JSON 文件 (*.json)|*.json|所有文件 (*.*)|*.*");
        if (string.IsNullOrEmpty(filePath)) return;

        try
        {
            IsLoading = true;
            StatusMessage = "正在导入...";

            await _importExportService.ImportEnvironmentAsync(filePath);
            await LoadDataAsync();

            StatusMessage = "导入成功";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "导入失败");
            _dialogService.ShowException(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 导出环境
    /// </summary>
    [RelayCommand]
    private async Task ExportEnvironmentAsync()
    {
        if (SelectedEnvironment == null)
        {
            _dialogService.Warning("请先选择一个环境");
            return;
        }

        var filePath = _dialogService.SaveFile(
            "JSON 文件 (*.json)|*.json",
            $"{SelectedEnvironment.Name}.json");
        if (string.IsNullOrEmpty(filePath)) return;

        try
        {
            IsLoading = true;
            await _importExportService.ExportEnvironmentAsync(SelectedEnvironment.Id, filePath);
            StatusMessage = $"已导出到: {filePath}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "导出失败");
            _dialogService.ShowException(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 获取筛选后的环境列表
    /// </summary>
    private IEnumerable<BrowserEnvironment> GetFilteredEnvironments()
    {
        var all = Groups.SelectMany(g => g.Environments);

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            all = all.Where(e =>
                e.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (e.Remarks?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (SelectedGroup != null)
        {
            all = all.Where(e => e.GroupId == SelectedGroup.Id);
        }

        return all;
    }

    /// <summary>
    /// 更新统计信息
    /// </summary>
    private void UpdateStatistics()
    {
        RunningCount = Groups.Sum(g => g.RunningCount);
        TotalCount = Groups.Sum(g => g.TotalCount);
    }

    /// <summary>
    /// 刷新选中环境
    /// </summary>
    partial void OnSelectedEnvironmentChanged(BrowserEnvironment? value)
    {
        if (value != null)
        {
            StatusMessage = $"已选中: {value.Name}";
        }
    }
}
