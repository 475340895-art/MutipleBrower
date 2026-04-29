using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FingerprintBrowser.Data;
using FingerprintBrowser.Models;
using Microsoft.EntityFrameworkCore;

namespace FingerprintBrowser.ViewModels
{
    public partial class EnvironmentEditViewModel : ObservableObject
    {
        private readonly BrowserDbContext _db;
        private readonly int? _editingId;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private int _groupId;

        [ObservableProperty]
        private int? _proxyId;

        [ObservableProperty]
        private string _browserType = "Chromium";

        [ObservableProperty]
        private string _resolution = "1920x1080";

        [ObservableProperty]
        private string _timezone = "Asia/Shanghai";

        [ObservableProperty]
        private string _languages = "zh-CN,zh,en-US,en";

        [ObservableProperty]
        private string _userAgent = string.Empty;

        [ObservableProperty]
        private string _webGLVendor = "Intel Inc.";

        [ObservableProperty]
        private string _webGLRenderer = "Intel Iris OpenGL Engine";

        [ObservableProperty]
        private bool _enableWebRTC = true;

        [ObservableProperty]
        private bool _enableCookies = true;

        [ObservableProperty]
        private bool _enableJavaScript = true;

        [ObservableProperty]
        private string _startupUrl = "https://www.google.com";

        [ObservableProperty]
        private string _remark = string.Empty;

        [ObservableProperty]
        private ObservableCollection<EnvironmentGroup> _groups = new();

        [ObservableProperty]
        private ObservableCollection<ProxyConfig> _proxies = new();

        [ObservableProperty]
        private ObservableCollection<string> _resolutions = new()
        {
            "1920x1080", "1366x768", "1536x864", "1440x900",
            "1280x720", "1600x900", "2560x1440", "3840x2160",
            "1280x800", "1280x1024", "1680x1050", "1400x1050"
        };

        [ObservableProperty]
        private ObservableCollection<string> _timezones = new()
        {
            "Asia/Shanghai", "Asia/Tokyo", "Asia/Seoul", "Asia/Singapore",
            "America/New_York", "America/Los_Angeles", "America/Chicago",
            "Europe/London", "Europe/Paris", "Europe/Berlin",
            "Pacific/Auckland", "Australia/Sydney"
        };

        [ObservableProperty]
        private ObservableCollection<string> _browserTypes = new()
        {
            "Chromium", "Firefox", "WebKit"
        };

        [ObservableProperty]
        private string _dialogTitle = "新建环境";

        public bool DialogResult { get; private set; }

        public EnvironmentEditViewModel(int? editingId = null)
        {
            _db = new BrowserDbContext();
            _editingId = editingId;
            DialogTitle = editingId.HasValue ? "编辑环境" : "新建环境";
        }

        public async Task LoadDataAsync()
        {
            var groups = await _db.Groups.ToListAsync();
            Groups.Clear();
            Groups.Add(new EnvironmentGroup { Id = 0, Name = "未分组" });
            foreach (var g in groups)
            {
                Groups.Add(g);
            }

            var proxies = await _db.Proxies.ToListAsync();
            Proxies.Clear();
            Proxies.Add(new ProxyConfig { Id = 0, Name = "无代理" });
            foreach (var p in proxies)
            {
                Proxies.Add(p);
            }

            if (_editingId.HasValue)
            {
                var env = await _db.Environments.FindAsync(_editingId.Value);
                if (env != null)
                {
                    Name = env.Name;
                    GroupId = env.GroupId;
                    ProxyId = env.ProxyId;
                    BrowserType = env.BrowserType;
                    Resolution = env.Resolution;
                    Timezone = env.Timezone;
                    Languages = env.Languages;
                    UserAgent = env.UserAgent;
                    WebGLVendor = env.WebGLVendor;
                    WebGLRenderer = env.WebGLRenderer;
                    EnableWebRTC = env.EnableWebRTC;
                    EnableCookies = env.EnableCookies;
                    EnableJavaScript = env.EnableJavaScript;
                    StartupUrl = env.StartupUrl;
                    Remark = env.Remark;
                }
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                return;
            }

            try
            {
                if (_editingId.HasValue)
                {
                    var env = await _db.Environments.FindAsync(_editingId.Value);
                    if (env != null)
                    {
                        env.Name = Name;
                        env.GroupId = GroupId;
                        env.ProxyId = ProxyId == 0 ? null : ProxyId;
                        env.BrowserType = BrowserType;
                        env.Resolution = Resolution;
                        env.Timezone = Timezone;
                        env.Languages = Languages;
                        env.UserAgent = UserAgent;
                        env.WebGLVendor = WebGLVendor;
                        env.WebGLRenderer = WebGLRenderer;
                        env.EnableWebRTC = EnableWebRTC;
                        env.EnableCookies = EnableCookies;
                        env.EnableJavaScript = EnableJavaScript;
                        env.StartupUrl = StartupUrl;
                        env.Remark = Remark;
                        env.UpdatedAt = DateTime.Now;
                        await _db.SaveChangesAsync();
                    }
                }
                else
                {
                    var env = new BrowserEnvironment
                    {
                        Name = Name,
                        GroupId = GroupId,
                        ProxyId = ProxyId == 0 ? null : ProxyId,
                        BrowserType = BrowserType,
                        Resolution = Resolution,
                        Timezone = Timezone,
                        Languages = Languages,
                        UserAgent = UserAgent,
                        WebGLVendor = WebGLVendor,
                        WebGLRenderer = WebGLRenderer,
                        EnableWebRTC = EnableWebRTC,
                        EnableCookies = EnableCookies,
                        EnableJavaScript = EnableJavaScript,
                        StartupUrl = StartupUrl,
                        Remark = Remark
                    };
                    _db.Environments.Add(env);
                    await _db.SaveChangesAsync();
                }

                DialogResult = true;
            }
            catch
            {
                DialogResult = false;
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            DialogResult = false;
        }

        [RelayCommand]
        private void RandomizeFingerprint()
        {
            var random = new Random();

            var resolutions = new[] { "1920x1080", "1366x768", "1536x864", "1440x900" };
            Resolution = resolutions[random.Next(resolutions.Length)];

            var vendors = new[] { "Intel Inc.", "NVIDIA Corporation", "AMD" };
            WebGLVendor = vendors[random.Next(vendors.Length)];

            var renderers = new[] { "Intel Iris OpenGL Engine", "GeForce GTX 1060/PCIe/SSE2", "Radeon RX 580 Series" };
            WebGLRenderer = renderers[random.Next(renderers.Length)];
        }
    }
}
