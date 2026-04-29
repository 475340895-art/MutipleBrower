using CommunityToolkit.Mvvm;
using FingerprintBrowser.Data;
using FingerprintBrowser.Models;
using Microsoft.EntityFrameworkCore;

namespace FingerprintBrowser.ViewModels;

public partial class EnvironmentEditViewModel : ObservableObject
{
    private readonly int? _environmentId;
    private BrowserEnvironment _environment;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _startupUrl = "https://www.google.com";

    [ObservableProperty]
    private string _remark = string.Empty;

    [ObservableProperty]
    private string _resolution = "1920x1080";

    [ObservableProperty]
    private string _timezone = "Asia/Shanghai";

    [ObservableProperty]
    private string _languages = "zh-CN,zh,en-US,en";

    [ObservableProperty]
    private string _userAgent = string.Empty;

    [ObservableProperty]
    private string _webGLVendor = string.Empty;

    [ObservableProperty]
    private string _webGLRenderer = string.Empty;

    [ObservableProperty]
    private bool _enableWebRTC = true;

    [ObservableProperty]
    private bool _enableCookies = true;

    [ObservableProperty]
    private bool _enableJavaScript = true;

    [ObservableProperty]
    private List<EnvironmentGroup> _groups = new();

    [ObservableProperty]
    private List<ProxyConfig> _proxies = new();

    [ObservableProperty]
    private EnvironmentGroup? _selectedGroup;

    [ObservableProperty]
    private ProxyConfig? _selectedProxy;

    public EnvironmentEditViewModel(int? environmentId = null)
    {
        _environmentId = environmentId;
        _environment = new BrowserEnvironment();

        LoadData();
    }

    private void LoadData()
    {
        using var db = new BrowserDbContext();
        Groups = db.Groups.ToList();
        Proxies = db.Proxies.ToList();

        if (_environmentId.HasValue)
        {
            var env = db.Environments.Find(_environmentId.Value);
            if (env != null)
            {
                _environment = env;
                Name = env.Name;
                StartupUrl = env.StartupUrl;
                Remark = env.Remark;
                Resolution = env.Resolution;
                Timezone = env.Timezone;
                Languages = env.Languages;
                UserAgent = env.UserAgent;
                WebGLVendor = env.WebGLVendor;
                WebGLRenderer = env.WebGLRenderer;
                EnableWebRTC = env.EnableWebRTC;
                EnableCookies = env.EnableCookies;
                EnableJavaScript = env.EnableJavaScript;
                SelectedGroup = Groups.FirstOrDefault(g => g.Id == env.GroupId);
                SelectedProxy = Proxies.FirstOrDefault(p => p.Id == env.ProxyId);
            }
        }
    }

    public BrowserEnvironment GetEnvironment()
    {
        _environment.Name = Name;
        _environment.StartupUrl = StartupUrl;
        _environment.Remark = Remark;
        _environment.Resolution = Resolution;
        _environment.Timezone = Timezone;
        _environment.Languages = Languages;
        _environment.UserAgent = UserAgent;
        _environment.WebGLVendor = WebGLVendor;
        _environment.WebGLRenderer = WebGLRenderer;
        _environment.EnableWebRTC = EnableWebRTC;
        _environment.EnableCookies = EnableCookies;
        _environment.EnableJavaScript = EnableJavaScript;
        _environment.GroupId = SelectedGroup?.Id;
        _environment.ProxyId = SelectedProxy?.Id;
        _environment.UpdatedAt = DateTime.Now;

        return _environment;
    }
}
