using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FingerprintBrowser.Models;
using FingerprintBrowser.Services;
using Serilog;

namespace FingerprintBrowser.ViewModels;

/// <summary>
/// 代理管理视图模型
/// </summary>
public partial class ProxyViewModel : ObservableObject
{
    private readonly IProxyService _proxyService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private ObservableCollection<ProxyConfig> _proxies = new();

    [ObservableProperty]
    private ProxyConfig? _selectedProxy;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    public ProxyViewModel()
    {
        _proxyService = ServiceLocator.Get<IProxyService>();
        _dialogService = ServiceLocator.Get<IDialogService>();

        _ = LoadProxiesAsync();
    }

    [RelayCommand]
    private async Task LoadProxiesAsync()
    {
        try
        {
            IsLoading = true;
            var list = await _proxyService.GetAllProxiesAsync();
            Proxies = new ObservableCollection<ProxyConfig>(list);
            StatusMessage = $"已加载 {list.Count} 个代理";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载代理失败");
            _dialogService.ShowException(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddProxyAsync()
    {
        var name = _dialogService.Input("请输入代理名称:", "添加代理");
        if (string.IsNullOrWhiteSpace(name)) return;

        var host = _dialogService.Input("请输入代理主机:", "添加代理");
        if (string.IsNullOrWhiteSpace(host)) return;

        var portStr = _dialogService.Input("请输入代理端口:", "添加代理");
        if (!int.TryParse(portStr, out var port))
        {
            _dialogService.Warning("端口格式错误");
            return;
        }

        try
        {
            var proxy = new ProxyConfig
            {
                Name = name,
                Host = host,
                Port = port,
                ProxyType = "HTTP"
            };

            await _proxyService.AddProxyAsync(proxy);
            await LoadProxiesAsync();

            StatusMessage = $"已添加代理: {name}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "添加代理失败");
            _dialogService.ShowException(ex);
        }
    }

    [RelayCommand]
    private async Task EditProxyAsync()
    {
        if (SelectedProxy == null)
        {
            _dialogService.Warning("请先选择一个代理");
            return;
        }

        // 在实际应用中，这里应该打开编辑对话框
        // 这里简化处理，直接更新
        try
        {
            await _proxyService.UpdateProxyAsync(SelectedProxy);
            await LoadProxiesAsync();
            StatusMessage = $"已更新代理: {SelectedProxy.Name}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "更新代理失败");
            _dialogService.ShowException(ex);
        }
    }

    [RelayCommand]
    private async Task DeleteProxyAsync()
    {
        if (SelectedProxy == null)
        {
            _dialogService.Warning("请先选择一个代理");
            return;
        }

        if (!_dialogService.Confirm($"确定要删除代理 '{SelectedProxy.Name}' 吗?")) return;

        try
        {
            await _proxyService.DeleteProxyAsync(SelectedProxy.Id);
            await LoadProxiesAsync();
            SelectedProxy = null;
            StatusMessage = "代理已删除";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "删除代理失败");
            _dialogService.ShowException(ex);
        }
    }

    [RelayCommand]
    private async Task TestProxyAsync()
    {
        if (SelectedProxy == null)
        {
            _dialogService.Warning("请先选择一个代理");
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "正在测试代理...";

            var (success, responseTime) = await _proxyService.TestProxyAsync(SelectedProxy);

            if (success)
            {
                StatusMessage = $"代理 {SelectedProxy.Name} 可用 (响应: {responseTime}ms)";
                _dialogService.Success($"代理可用\n响应时间: {responseTime}ms", "测试结果");
            }
            else
            {
                StatusMessage = $"代理 {SelectedProxy.Name} 不可用";
                _dialogService.Error("代理不可用，请检查配置", "测试结果");
            }

            await LoadProxiesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "测试代理失败");
            _dialogService.ShowException(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task TestAllProxiesAsync()
    {
        if (!Proxies.Any())
        {
            _dialogService.Warning("没有可测试的代理");
            return;
        }

        try
        {
            IsLoading = true;
            StatusMessage = "正在批量测试代理...";

            await _proxyService.TestAllProxiesAsync();
            await LoadProxiesAsync();

            var available = Proxies.Count(p => p.Status == (int)ProxyStatus.Normal);
            StatusMessage = $"测试完成: {available}/{Proxies.Count} 个可用";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "批量测试代理失败");
            _dialogService.ShowException(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ImportProxiesAsync()
    {
        var text = _dialogService.Input("请输入代理列表 (每行一个，格式: host:port)", "导入代理");
        if (string.IsNullOrWhiteSpace(text)) return;

        try
        {
            IsLoading = true;
            var count = await ServiceLocator.Get<IImportExportService>()
                .ImportProxiesFromTextAsync(text, "host:port");

            await LoadProxiesAsync();
            StatusMessage = $"已导入 {count} 个代理";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "导入代理失败");
            _dialogService.ShowException(ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportProxiesAsync()
    {
        if (!Proxies.Any())
        {
            _dialogService.Warning("没有可导出的代理");
            return;
        }

        try
        {
            var text = await ServiceLocator.Get<IImportExportService>()
                .ExportProxiesToTextAsync("host:port");

            // 复制到剪贴板
            System.Windows.Clipboard.SetText(text);
            StatusMessage = "代理列表已复制到剪贴板";
            _dialogService.Success("代理列表已复制到剪贴板");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "导出代理失败");
            _dialogService.ShowException(ex);
        }
    }
}
