using System.Windows;
using System.Windows.Controls;
using HandyControl.Controls;

namespace FingerprintBrowser.Views;

public class TextInputWindow : HandyControl.Controls.Window
{
    public string InputText { get; private set; } = "";
    
    private System.Windows.Controls.TextBox _textBox = null!;
    
    public TextInputWindow(string title, string label, string defaultValue = "")
    {
        Title = title;
        Width = 400;
        Height = 180;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        
        var lbl = new System.Windows.Controls.TextBlock
        {
            Text = label,
            Margin = new Thickness(20, 20, 20, 10)
        };
        Grid.SetRow(lbl, 0);
        grid.Children.Add(lbl);
        
        _textBox = new System.Windows.Controls.TextBox
        {
            Text = defaultValue,
            Margin = new Thickness(20, 0, 20, 10),
            Padding = new Thickness(8, 6, 8, 6)
        };
        Grid.SetRow(_textBox, 1);
        grid.Children.Add(_textBox);
        
        var btnPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(20, 0, 20, 20)
        };
        
        var okBtn = new System.Windows.Controls.Button
        {
            Content = "确定",
            Width = 80,
            Margin = new Thickness(0, 0, 10, 0)
        };
        okBtn.Click += (s, e) => { InputText = _textBox.Text; DialogResult = true; };
        btnPanel.Children.Add(okBtn);
        
        var cancelBtn = new System.Windows.Controls.Button
        {
            Content = "取消",
            Width = 80
        };
        cancelBtn.Click += (s, e) => { DialogResult = false; };
        btnPanel.Children.Add(cancelBtn);
        
        Grid.SetRow(btnPanel, 2);
        grid.Children.Add(btnPanel);
        
        Content = grid;
    }
}
