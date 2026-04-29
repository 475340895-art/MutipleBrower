using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FingerprintBrowser.Models;
using FingerprintBrowser.Services;

namespace FingerprintBrowser.ViewModels
{
    public class EnvironmentEditViewModel
    {
        private readonly BrowserService _browserService;

        public EnvironmentEditViewModel()
        {
            _browserService = new BrowserService();
            Groups = new List<EnvironmentGroup>();
        }

        public List<EnvironmentGroup> Groups { get; private set; }
        public BrowserEnvironment? Environment { get; private set; }

        public Task LoadGroupsAsync()
        {
            return Task.Run(async () =>
            {
                var groups = await _browserService.GetGroupsAsync();
                Groups = groups;
            });
        }

        public Task LoadEnvironmentAsync(int environmentId)
        {
            return Task.Run(async () =>
            {
                Environment = await _browserService.GetEnvironmentAsync(environmentId);
            });
        }

        public void CreateEnvironment(BrowserEnvironment environment)
        {
            _browserService.CreateEnvironment(environment);
        }

        public void UpdateEnvironment(BrowserEnvironment environment)
        {
            _browserService.UpdateEnvironment(environment);
        }
    }
}
