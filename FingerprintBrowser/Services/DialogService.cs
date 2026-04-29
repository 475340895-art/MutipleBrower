using System.Windows;
using HandyControl.Controls;
using Serilog;
using Application = System.Windows.Application;

namespace FingerprintBrowser.Services;

/// <summary>
/// 对话框服务接口
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// 显示确认对话框
    /// </summary>
    bool Confirm(string message, string title = "确认");

    /// <summary>
    /// 显示警告对话框
    /// </summary>
    void Warning(string message, string title = "警告");

    /// <summary>
    /// 显示错误对话框
    /// </summary>
    void Error(string message, string title = "错误");

    /// <summary>
    /// 显示信息对话框
    /// </summary>
    void Info(string message, string title = "提示");

    /// <summary>
    /// 显示成功对话框
    /// </summary>
    void Success(string message, string title = "成功");

    /// <summary>
    /// 显示异常对话框
    /// </summary>
    void ShowException(Exception ex, string title = "异常");

    /// <summary>
    /// 显示输入对话框
    /// </summary>
    string? Input(string message, string title = "输入", string defaultValue = "");

    /// <summary>
    /// 显示密码输入对话框
    /// </summary>
    string? Password(string title = "输入密码");

    /// <summary>
    /// 显示文件选择对话框
    /// </summary>
    string? OpenFile(string filter = "所有文件 (*.*)|*.*", string? initialDirectory = null);

    /// <summary>
    /// 显示多文件选择对话框
    /// </summary>
    string[]? OpenFiles(string filter = "所有文件 (*.*)|*.*");

    /// <summary>
    /// 显示保存文件对话框
    /// </summary>
    string? SaveFile(string filter = "所有文件 (*.*)|*.*", string? defaultFileName = null);

    /// <summary>
    /// 显示文件夹选择对话框
    /// </summary>
    string? OpenFolder();
}

/// <summary>
/// 对话框服务实现 - 使用 HandyControl
/// </summary>
public class DialogService : IDialogService
{
    public bool Confirm(string message, string title = "确认")
    {
        Log.Debug("显示确认对话框: {Title} - {Message}", title, message);
        var result = HandyControl.Controls.MessageBox.Ask(message, title);
        return result == MessageBoxResult.OK;
    }

    public void Warning(string message, string title = "警告")
    {
        Log.Debug("显示警告对话框: {Title} - {Message}", title, message);
        HandyControl.Controls.MessageBox.Warning(message, title);
    }

    public void Error(string message, string title = "错误")
    {
        Log.Debug("显示错误对话框: {Title} - {Message}", title, message);
        HandyControl.Controls.MessageBox.Error(message, title);
    }

    public void Info(string message, string title = "提示")
    {
        Log.Debug("显示信息对话框: {Title} - {Message}", title, message);
        HandyControl.Controls.MessageBox.Info(message, title);
    }

    public void Success(string message, string title = "成功")
    {
        Log.Debug("显示成功对话框: {Title} - {Message}", title, message);
        HandyControl.Controls.MessageBox.Success(message, title);
    }

    public void ShowException(Exception ex, string title = "异常")
    {
        Log.Error(ex, "显示异常对话框: {Title}", title);
        HandyControl.Controls.MessageBox.Error(
            $"发生错误: {ex.Message}\n\n详细信息请查看日志",
            title);
    }

    public string? Input(string message, string title = "输入", string defaultValue = "")
    {
        Log.Debug("显示输入对话框: {Title}", title);

        var dialog = new InputDialog(message, title, defaultValue)
        {
            Owner = Application.Current.MainWindow
        };

        return dialog.ShowDialog() == true ? dialog.InputText : null;
    }

    public string? Password(string title = "输入密码")
    {
        Log.Debug("显示密码输入对话框: {Title}", title);

        var dialog = new PasswordDialog(title)
        {
            Owner = Application.Current.MainWindow
        };

        return dialog.ShowDialog() == true ? dialog.Password : null;
    }

    public string? OpenFile(string filter = "所有文件 (*.*)|*.*", string? initialDirectory = null)
    {
        Log.Debug("显示打开文件对话框");

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = filter,
            InitialDirectory = initialDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Multiselect = false
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string[]? OpenFiles(string filter = "所有文件 (*.*)|*.*")
    {
        Log.Debug("显示多文件选择对话框");

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = filter,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Multiselect = true
        };

        return dialog.ShowDialog() == true ? dialog.FileNames : null;
    }

    public string? SaveFile(string filter = "所有文件 (*.*)|*.*", string? defaultFileName = null)
    {
        Log.Debug("显示保存文件对话框: {DefaultName}", defaultFileName);

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = filter,
            FileName = defaultFileName ?? string.Empty,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? OpenFolder()
    {
        Log.Debug("显示文件夹选择对话框");

        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "选择文件夹",
            ShowNewFolderButton = true,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null;
    }
}

/// <summary>
/// 输入对话框
/// </summary>
public class InputDialog : System.Windows.Window
{
    private readonly System.Windows.Controls.TextBox _textBox;
    public string InputText => _textBox.Text;

    public InputDialog(string message, string title, string defaultValue = "")
    {
        Title = title;
        Width = 400;
        Height = 160;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        WindowStyle = WindowStyle.ToolWindow;
        Background = Application.Current.Resources["RegionBrush"] as System.Windows.Media.Brush;

        var grid = new System.Windows.Controls.Grid();
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.Margin = new Thickness(20);

        var label = new System.Windows.Controls.TextBlock
        {
            Text = message,
            Margin = new Thickness(0, 0, 0, 10),
            TextWrapping = System.Windows.TextWrapping.Wrap
        };
        System.Windows.Controls.Grid.SetRow(label, 0);

        _textBox = new System.Windows.Controls.TextBox
        {
            Text = defaultValue,
            Margin = new Thickness(0, 0, 0, 15)
        };
        System.Windows.Controls.Grid.SetRow(_textBox, 1);

        var buttonPanel = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };
        System.Windows.Controls.Grid.SetRow(buttonPanel, 2);

        var okButton = new System.Windows.Controls.Button
        {
            Content = "确定",
            Width = 80,
            Margin = new Thickness(0, 0, 10, 0)
        };
        okButton.Click += (s, e) => { DialogResult = true; };

        var cancelButton = new System.Windows.Controls.Button
        {
            Content = "取消",
            Width = 80
        };
        cancelButton.Click += (s, e) => { DialogResult = false; };

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);

        grid.Children.Add(label);
        grid.Children.Add(_textBox);
        grid.Children.Add(buttonPanel);

        Content = grid;
    }
}

/// <summary>
/// 密码输入对话框
/// </summary>
public class PasswordDialog : System.Windows.Window
{
    private readonly System.Windows.Controls.PasswordBox _passwordBox;
    public string Password => _passwordBox.Password;

    public PasswordDialog(string title)
    {
        Title = title;
        Width = 350;
        Height = 150;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        WindowStyle = WindowStyle.ToolWindow;
        Background = Application.Current.Resources["RegionBrush"] as System.Windows.Media.Brush;

        var grid = new System.Windows.Controls.Grid();
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.Margin = new Thickness(20);

        var label = new System.Windows.Controls.TextBlock
        {
            Text = "请输入密码:",
            Margin = new Thickness(0, 0, 0, 10)
        };
        System.Windows.Controls.Grid.SetRow(label, 0);

        _passwordBox = new System.Windows.Controls.PasswordBox
        {
            Margin = new Thickness(0, 0, 0, 15)
        };
        System.Windows.Controls.Grid.SetRow(_passwordBox, 1);

        var buttonPanel = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };

        var okButton = new System.Windows.Controls.Button
        {
            Content = "确定",
            Width = 80,
            Margin = new Thickness(0, 0, 10, 0)
        };
        okButton.Click += (s, e) => { DialogResult = true; };

        var cancelButton = new System.Windows.Controls.Button
        {
            Content = "取消",
            Width = 80
        };
        cancelButton.Click += (s, e) => { DialogResult = false; };

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);

        grid.Children.Add(label);
        grid.Children.Add(_passwordBox);

        Content = new System.Windows.Controls.StackPanel
        {
            Children = { grid, buttonPanel },
            Margin = new Thickness(0, 0, 0, 10)
        };
    }
}
