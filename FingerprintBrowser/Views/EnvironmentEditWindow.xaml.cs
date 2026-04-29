using System.Windows;
using System.Windows.Controls;
using FingerprintBrowser.Models;
using FingerprintBrowser.Data;
using Microsoft.EntityFrameworkCore;

namespace FingerprintBrowser.Views;

public partial class EnvironmentEditWindow : HandyControl.Controls.Window
{
    private readonly BrowserEnvironment? _editEnv;
    private readonly BrowserDbContext _db;
    
    public EnvironmentEditWindow(BrowserEnvironment? env = null)
    {
        InitializeComponent();
        _editEnv = env;
        _db = new BrowserDbContext();
        InitControls();
        
        if (_editEnv != null)
        {
            Title = "编辑环境";
            LoadEnvironment();
        }
        else
        {
            Title = "新建环境";
        }
    }
    
    private void InitControls()
    {
        // 加载分组
        var groups = _db.Groups.ToList();
        GroupCombo.ItemsSource = groups;
        GroupCombo.DisplayMemberPath = "Name";
        if (groups.Count > 0) GroupCombo.SelectedIndex = 0;
        
        // 分辨率
        ResolutionCombo.ItemsSource = new[] {
            "1920x1080", "1366x768", "1536x864", "1440x900", 
            "1280x720", "1600x900", "2560x1440"
        };
        ResolutionCombo.SelectedIndex = 0;
        
        // 时区
        TimezoneCombo.ItemsSource = new[] {
            "Asia/Shanghai", "America/New_York", "Europe/London", 
            "Asia/Tokyo", "Asia/Singapore", "UTC"
        };
        TimezoneCombo.SelectedIndex = 0;
        
        // 语言
        LanguageCombo.ItemsSource = new[] {
            "zh-CN,zh", "en-US,en", "ja-JP,ja", 
            "ko-KR,ko", "es-ES,es", "ru-RU,ru"
        };
        LanguageCombo.SelectedIndex = 0;
    }
    
    private void LoadEnvironment()
    {
        if (_editEnv == null) return;
        
        NameBox.Text = _editEnv.Name;
        UrlBox.Text = _editEnv.StartupUrl;
        RemarkBox.Text = _editEnv.Remark;
        ResolutionCombo.SelectedItem = _editEnv.Resolution;
        TimezoneCombo.SelectedItem = _editEnv.Timezone;
        LanguageCombo.SelectedItem = _editEnv.Languages;
        
        // 加载分组
        foreach (var item in GroupCombo.Items)
        {
            if (item is EnvironmentGroup g && g.Id == _editEnv.GroupId)
            {
                GroupCombo.SelectedItem = item;
                break;
            }
        }
    }
    
    private void ProxyTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var hasProxy = ProxyTypeCombo.SelectedIndex > 0;
        ProxyHostBox.IsEnabled = hasProxy;
        ProxyPortBox.IsEnabled = hasProxy;
        ProxyUserBox.IsEnabled = hasProxy;
        ProxyPassBox.IsEnabled = hasProxy;
    }
    
    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            HandyControl.Controls.MessageBox.Show("请输入环境名称！");
            return;
        }
        
        try
        {
            if (_editEnv == null)
            {
                // 新建
                var env = new BrowserEnvironment
                {
                    Name = NameBox.Text,
                    GroupId = (GroupCombo.SelectedItem as EnvironmentGroup)?.Id ?? 1,
                    StartupUrl = UrlBox.Text ?? "",
                    Remark = RemarkBox.Text ?? "",
                    Resolution = ResolutionCombo.SelectedItem?.ToString() ?? "1920x1080",
                    Timezone = TimezoneCombo.SelectedItem?.ToString() ?? "Asia/Shanghai",
                    Languages = LanguageCombo.SelectedItem?.ToString() ?? "zh-CN,zh",
                    Status = BrowserStatus.Idle,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                
                // 代理设置
                if (ProxyTypeCombo.SelectedIndex > 0)
                {
                    env.ProxyConfig = new ProxyConfig
                    {
                        Type = ProxyTypeCombo.SelectedIndex switch {
                            1 => "HTTP",
                            2 => "HTTPS", 
                            3 => "SOCKS5",
                            _ => "HTTP"
                        },
                        Host = ProxyHostBox.Text,
                        Port = ProxyPortBox.Text,
                        Username = ProxyUserBox.Text,
                        Password = ProxyPassBox.Password
                    };
                }
                
                _db.Environments.Add(env);
            }
            else
            {
                // 更新
                _editEnv.Name = NameBox.Text;
                _editEnv.GroupId = (GroupCombo.SelectedItem as EnvironmentGroup)?.Id ?? _editEnv.GroupId;
                _editEnv.StartupUrl = UrlBox.Text ?? "";
                _editEnv.Remark = RemarkBox.Text ?? "";
                _editEnv.Resolution = ResolutionCombo.SelectedItem?.ToString() ?? "1920x1080";
                _editEnv.Timezone = TimezoneCombo.SelectedItem?.ToString() ?? "Asia/Shanghai";
                _editEnv.Languages = LanguageCombo.SelectedItem?.ToString() ?? "zh-CN,zh";
                _editEnv.UpdatedAt = DateTime.Now;
                
                if (ProxyTypeCombo.SelectedIndex > 0)
                {
                    _editEnv.ProxyConfig = new ProxyConfig
                    {
                        Type = ProxyTypeCombo.SelectedIndex switch {
                            1 => "HTTP",
                            2 => "HTTPS", 
                            3 => "SOCKS5",
                            _ => "HTTP"
                        },
                        Host = ProxyHostBox.Text,
                        Port = ProxyPortBox.Text,
                        Username = ProxyUserBox.Text,
                        Password = ProxyPassBox.Password
                    };
                }
                else
                {
                    _editEnv.ProxyConfig = null;
                }
                
                _db.Environments.Update(_editEnv);
            }
            
            _db.SaveChanges();
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            HandyControl.Controls.MessageBox.Show($"保存失败：{ex.Message}");
        }
    }
    
    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
