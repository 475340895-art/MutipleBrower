using System.Windows;
using FingerprintBrowser.Data;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Views;

public partial class ProxyWindow : HandyControl.Controls.Window
{
    private readonly BrowserDbContext _db;
    
    public ProxyWindow()
    {
        InitializeComponent();
        _db = new BrowserDbContext();
        LoadProxies();
    }
    
    private void LoadProxies()
    {
        var proxies = _db.ProxyConfigs.ToList();
        ProxyGrid.ItemsSource = proxies;
        StatusText.Text = $"共 {proxies.Count} 条代理";
    }
    
    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddProxyDialog();
        if (dialog.ShowDialog() == true)
        {
            _db.ProxyConfigs.Add(dialog.Proxy);
            _db.SaveChanges();
            LoadProxies();
            StatusText.Text = "添加成功";
        }
    }
    
    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (ProxyGrid.SelectedItem is ProxyConfig proxy)
        {
            var result = HandyControl.Controls.MessageBox.Show(
                $"确定删除代理 {proxy.Host}:{proxy.Port} 吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                _db.ProxyConfigs.Remove(proxy);
                _db.SaveChanges();
                LoadProxies();
                StatusText.Text = "删除成功";
            }
        }
        else
        {
            StatusText.Text = "请先选择要删除的代理";
        }
    }
    
    private void BtnTest_Click(object sender, RoutedEventArgs e)
    {
        if (ProxyGrid.SelectedItem is ProxyConfig proxy)
        {
            TestProxy(proxy);
        }
        else
        {
            StatusText.Text = "请先选择要测试的代理";
        }
    }
    
    private async void TestProxy(ProxyConfig proxy)
    {
        StatusText.Text = "正在测试...";
        proxy.Status = "测试中";
        LoadProxies();
        
        try
        {
            using var client = new System.Net.Http.HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await client.GetAsync("https://www.google.com");
            sw.Stop();
            
            if (response.IsSuccessStatusCode)
            {
                proxy.Status = "可用";
                proxy.Latency = $"{sw.ElapsedMilliseconds}ms";
                StatusText.Text = $"测试成功，延迟：{sw.ElapsedMilliseconds}ms";
            }
            else
            {
                proxy.Status = "不可用";
                StatusText.Text = "测试失败";
            }
        }
        catch
        {
            proxy.Status = "超时";
            StatusText.Text = "测试超时";
        }
        
        _db.SaveChanges();
        LoadProxies();
    }
    
    private void BtnImport_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "文本文件|*.txt|所有文件|*.*",
            Title = "导入代理"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var lines = System.IO.File.ReadAllLines(dialog.FileName);
                int count = 0;
                
                foreach (var line in lines)
                {
                    var parts = line.Trim().Split(':');
                    if (parts.Length >= 2)
                    {
                        var proxy = new ProxyConfig
                        {
                            Type = "HTTP",
                            Host = parts[0],
                            Port = parts[1],
                            Username = parts.Length > 2 ? parts[2] : "",
                            Password = parts.Length > 3 ? parts[3] : "",
                            Status = "未测试"
                        };
                        _db.ProxyConfigs.Add(proxy);
                        count++;
                    }
                }
                
                _db.SaveChanges();
                LoadProxies();
                StatusText.Text = $"导入成功，共 {count} 条";
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"导入失败：{ex.Message}");
            }
        }
    }
    
    private void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "文本文件|*.txt",
            Title = "导出代理",
            FileName = $"proxies_{DateTime.Now:yyyyMMdd}"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var proxies = _db.ProxyConfigs.ToList();
                var lines = proxies.Select(p => $"{p.Host}:{p.Port}:{p.Username}:{p.Password}");
                System.IO.File.WriteAllLines(dialog.FileName, lines);
                StatusText.Text = $"导出成功，共 {proxies.Count} 条";
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show($"导出失败：{ex.Message}");
            }
        }
    }
    
    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

// 简单的添加代理对话框
public class AddProxyDialog : HandyControl.Controls.Window
{
    public ProxyConfig Proxy { get; private set; } = new();
    
    private System.Windows.Controls.TextBox _hostBox = null!;
    private System.Windows.Controls.TextBox _portBox = null!;
    private System.Windows.Controls.TextBox _userBox = null!;
    private HandyControl.Controls.PasswordBox _passBox = null!;
    private System.Windows.Controls.ComboBox _typeCombo = null!;
    
    public AddProxyDialog()
    {
        Title = "添加代理";
        Width = 400;
        Height = 320;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        
        var grid = new System.Windows.Controls.Grid { Margin = new Thickness(20) };
        
        var rows = new[] { 40, 40, 40, 40, 40 };
        for (int i = 0; i < rows.Length; i++)
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(rows[i]) });
        grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(80) });
        grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition());
        
        void AddLabel(string text, int row)
        {
            var lb = new System.Windows.Controls.TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center };
            System.Windows.Controls.Grid.SetRow(lb, row);
            grid.Children.Add(lb);
        }
        
        AddLabel("类型：", 0);
        AddLabel("主机：", 1);
        AddLabel("端口：", 2);
        AddLabel("用户名：", 3);
        AddLabel("密码：", 4);
        
        _typeCombo = new System.Windows.Controls.ComboBox { Margin = new Thickness(0, 5, 0, 5) };
        _typeCombo.Items.Add("HTTP");
        _typeCombo.Items.Add("HTTPS");
        _typeCombo.Items.Add("SOCKS5");
        _typeCombo.SelectedIndex = 0;
        System.Windows.Controls.Grid.SetRow(_typeCombo, 0);
        System.Windows.Controls.Grid.SetColumn(_typeCombo, 1);
        grid.Children.Add(_typeCombo);
        
        _hostBox = new System.Windows.Controls.TextBox { Margin = new Thickness(0, 5, 0, 5) };
        System.Windows.Controls.Grid.SetRow(_hostBox, 1);
        System.Windows.Controls.Grid.SetColumn(_hostBox, 1);
        grid.Children.Add(_hostBox);
        
        _portBox = new System.Windows.Controls.TextBox { Margin = new Thickness(0, 5, 0, 5) };
        System.Windows.Controls.Grid.SetRow(_portBox, 2);
        System.Windows.Controls.Grid.SetColumn(_portBox, 1);
        grid.Children.Add(_portBox);
        
        _userBox = new System.Windows.Controls.TextBox { Margin = new Thickness(0, 5, 0, 5) };
        System.Windows.Controls.Grid.SetRow(_userBox, 3);
        System.Windows.Controls.Grid.SetColumn(_userBox, 1);
        grid.Children.Add(_userBox);
        
        _passBox = new HandyControl.Controls.PasswordBox { Margin = new Thickness(0, 5, 0, 5) };
        System.Windows.Controls.Grid.SetRow(_passBox, 4);
        System.Windows.Controls.Grid.SetColumn(_passBox, 1);
        grid.Children.Add(_passBox);
        
        var btnPanel = new System.Windows.Controls.StackPanel 
        { 
            Orientation = System.Windows.Controls.Orientation.Horizontal, 
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 20, 0, 0)
        };
        System.Windows.Controls.Grid.SetRow(btnPanel, 5);
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
        
        var btnOk = new System.Windows.Controls.Button { Content = "确定", Width = 80, Height = 30 };
        btnOk.Click += (s, e) =>
        {
            Proxy.Type = _typeCombo.SelectedItem?.ToString() ?? "HTTP";
            Proxy.Host = _hostBox.Text;
            Proxy.Port = _portBox.Text;
            Proxy.Username = _userBox.Text;
            Proxy.Password = _passBox.Password;
            Proxy.Status = "未测试";
            DialogResult = true;
            Close();
        };
        btnPanel.Children.Add(btnOk);
        
        var btnCancel = new System.Windows.Controls.Button { Content = "取消", Width = 80, Height = 30, Margin = new Thickness(10, 0, 0, 0) };
        btnCancel.Click += (s, e) => Close();
        btnPanel.Children.Add(btnCancel);
        
        grid.Children.Add(btnPanel);
        Content = grid;
    }
}
