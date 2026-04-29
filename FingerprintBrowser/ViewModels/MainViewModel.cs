using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Specialized;
using FingerprintBrowser.Models;
using FingerprintBrowser.Services;

namespace FingerprintBrowser.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly BrowserService _browserService;
        private ObservableCollection<EnvironmentGroup> _groups = new();
        private ObservableCollection<BrowserEnvironment> _environments = new();
        private ObservableCollection<BrowserEnvironment> _filteredEnvironments = new();
        private EnvironmentGroup? _selectedGroup;
        private string _groupSearchText = string.Empty;
        private string _envSearchText = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainViewModel()
        {
            _browserService = new BrowserService();
        }

        public ObservableCollection<EnvironmentGroup> Groups
        {
            get => _groups;
            set { _groups = value; OnPropertyChanged(); }
        }

        public ObservableCollection<BrowserEnvironment> Environments
        {
            get => _environments;
            set { _environments = value; OnPropertyChanged(); }
        }

        public ObservableCollection<BrowserEnvironment> FilteredEnvironments
        {
            get => _filteredEnvironments;
            set { _filteredEnvironments = value; OnPropertyChanged(); }
        }

        public EnvironmentGroup? SelectedGroup
        {
            get => _selectedGroup;
            set
            {
                _selectedGroup = value;
                OnPropertyChanged();
                FilterEnvironmentsByGroup();
            }
        }

        public string GroupSearchText
        {
            get => _groupSearchText;
            set
            {
                _groupSearchText = value;
                OnPropertyChanged();
                FilterGroups(value);
            }
        }

        public string EnvSearchText
        {
            get => _envSearchText;
            set
            {
                _envSearchText = value;
                OnPropertyChanged();
                FilterEnvironmentsBySearch(value);
            }
        }

        public async Task InitializeAsync()
        {
            await LoadGroupsAsync();
            await LoadEnvironmentsAsync();
        }

        public async Task LoadGroupsAsync()
        {
            try
            {
                var groups = await _browserService.GetGroupsAsync();
                Groups = new ObservableCollection<EnvironmentGroup>(groups);
            }
            catch (Exception ex)
            {
                throw new Exception($"加载分组失败: {ex.Message}", ex);
            }
        }

        public async Task LoadEnvironmentsAsync()
        {
            try
            {
                var envs = await _browserService.GetEnvironmentsAsync();
                Environments = new ObservableCollection<BrowserEnvironment>(envs);
                FilterEnvironmentsByGroup();
            }
            catch (Exception ex)
            {
                throw new Exception($"加载环境失败: {ex.Message}", ex);
            }
        }

        public void FilterGroups(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                Task.Run(async () =>
                {
                    var groups = await _browserService.GetGroupsAsync();
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Groups = new ObservableCollection<EnvironmentGroup>(groups);
                    });
                });
            }
            else
            {
                Task.Run(async () =>
                {
                    var groups = await _browserService.GetGroupsAsync();
                    var filtered = groups.Where(g => g.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Groups = new ObservableCollection<EnvironmentGroup>(filtered);
                    });
                });
            }
        }

        public void FilterEnvironments(string searchText)
        {
            _envSearchText = searchText;
            FilterEnvironmentsBySearch(searchText);
        }

        private void FilterEnvironmentsBySearch(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                FilterEnvironmentsByGroup();
            }
            else
            {
                var filtered = Environments
                    .Where(e => e.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                               (e.Remark?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();
                
                if (SelectedGroup != null)
                {
                    filtered = filtered.Where(e => e.GroupId == SelectedGroup.Id).ToList();
                }
                
                FilteredEnvironments = new ObservableCollection<BrowserEnvironment>(filtered);
            }
        }

        private void FilterEnvironmentsByGroup()
        {
            if (SelectedGroup == null)
            {
                FilteredEnvironments = new ObservableCollection<BrowserEnvironment>(
                    Environments.Where(e => string.IsNullOrWhiteSpace(_envSearchText) ||
                                            e.Name.Contains(_envSearchText, StringComparison.OrdinalIgnoreCase)));
            }
            else
            {
                FilteredEnvironments = new ObservableCollection<BrowserEnvironment>(
                    Environments.Where(e => e.GroupId == SelectedGroup.Id &&
                                            (string.IsNullOrWhiteSpace(_envSearchText) ||
                                            e.Name.Contains(_envSearchText, StringComparison.OrdinalIgnoreCase))));
            }
        }

        public void SelectGroup(EnvironmentGroup? group)
        {
            SelectedGroup = group;
        }

        public async Task StartBrowserAsync(int environmentId)
        {
            await _browserService.StartBrowserAsync(environmentId);
        }

        public async Task StopBrowserAsync(int environmentId)
        {
            await _browserService.StopBrowserAsync(environmentId);
        }

        public void OpenBrowserUrl(int environmentId)
        {
            _browserService.OpenBrowserUrl(environmentId);
        }

        public async Task CopyEnvironmentAsync(int environmentId)
        {
            await _browserService.CopyEnvironmentAsync(environmentId);
        }

        public async Task DeleteEnvironmentAsync(int environmentId)
        {
            await _browserService.DeleteEnvironmentAsync(environmentId);
        }

        public async Task BatchStartAsync()
        {
            await _browserService.BatchStartAsync();
        }

        public async Task BatchStopAsync()
        {
            await _browserService.BatchStopAsync();
        }

        public async Task ImportEnvironmentsAsync(string filePath)
        {
            await _browserService.ImportEnvironmentsAsync(filePath);
        }

        public async Task ExportEnvironmentsAsync(string filePath)
        {
            await _browserService.ExportEnvironmentsAsync(filePath);
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
