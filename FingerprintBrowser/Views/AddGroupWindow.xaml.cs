using System.Windows;
using System.Windows.Media;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.Views
{
    public partial class AddGroupWindow : Window
    {
        public EnvironmentGroup? Result { get; private set; }

        public AddGroupWindow()
        {
            InitializeComponent();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(GroupNameBox.Text))
            {
                MessageBox.Show("请输入分组名称", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string color = "#1677ff";
            if (ColorRed.IsChecked == true) color = "#f5222d";
            else if (ColorBlue.IsChecked == true) color = "#1677ff";
            else if (ColorGreen.IsChecked == true) color = "#52c41a";
            else if (ColorOrange.IsChecked == true) color = "#fa8c16";
            else if (ColorPurple.IsChecked == true) color = "#722ed1";
            else if (ColorCyan.IsChecked == true) color = "#13c2c2";

            Result = new EnvironmentGroup
            {
                Name = GroupNameBox.Text.Trim(),
                Color = color
            };

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public EnvironmentGroup GetGroup() => Result ?? new EnvironmentGroup { Name = "未命名", Color = "#1677ff" };
    }
}
