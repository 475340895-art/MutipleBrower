using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using FingerprintBrowser.Data;
using FingerprintBrowser.Models;

namespace FingerprintBrowser.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private BrowserDbContext _db = null!;
    public ObservableCollection<BrowserEnvironment> Environments { get; } = new();
    public ObservableCollection<EnvironmentGroup> Groups { get; } = new();
    public ObservableCollection<ProxyConfigModel> Proxies { get; } = new();
    
    private EnvironmentGroup? _selectedGroup;
    public EnvironmentGroup? SelectedGroup
    {
        get => _selectedGroup;
        set { _selectedGroup = value; OnPropertyChanged(); }
    }
    
    private BrowserEnvironment? _selectedEnvironment;
    public BrowserEnvironment? SelectedEnvironment
    {
        get => _selectedEnvironment;
        set { _selectedEnvironment = value; OnPropertyChanged(); }
    }
    
    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set { _searchText = value; OnPropertyChanged(); FilterEnvironments(); }
    }
    
    public int TotalCount => Environments.Count;
    public int RunningCount => Environments.Count(e => e.Status == BrowserStatus.Running);
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public async Task InitializeAsync()
    {
        _db = new BrowserDbContext();
        await DatabaseInitializer.InitializeAsync();
        await LoadDataAsync();
    }
    
    public async Task LoadDataAsync()
    {
        var groups = await _db.Groups.OrderBy(g => g.SortOrder).ToListAsync();
        Groups.Clear();
        foreach (var g in groups) Groups.Add(g);
        
        var envs = await _db.Environments.Include(e => e.Group).ToListAsync();
        Environments.Clear();
        foreach (var e in envs) Environments.Add(e);
        
        var proxies = await _db.Proxies.ToListAsync();
        Proxies.Clear();
        foreach (var p in proxies) Proxies.Add(p);
        
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(RunningCount));
    }
    
    public void FilterEnvironments()
    {
        // Filter is handled in the View with CollectionViewSource
    }
    
    public async Task<BrowserEnvironment> CreateEnvironmentAsync(string name, int? groupId)
    {
        var env = new BrowserEnvironment 
        { 
            Name = name, 
            GroupId = groupId,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        _db.Environments.Add(env);
        await _db.SaveChangesAsync();
        Environments.Add(env);
        OnPropertyChanged(nameof(TotalCount));
        return env;
    }
    
    public async Task DeleteEnvironmentAsync(int id)
    {
        var env = await _db.Environments.FindAsync(id);
        if (env != null)
        {
            _db.Environments.Remove(env);
            await _db.SaveChangesAsync();
            var item = Environments.FirstOrDefault(e => e.Id == id);
            if (item != null) Environments.Remove(item);
            OnPropertyChanged(nameof(TotalCount));
        }
    }
    
    public async Task<BrowserEnvironment> CopyEnvironmentAsync(int id)
    {
        var original = await _db.Environments.FindAsync(id);
        if (original == null) throw new Exception("Environment not found");
        
        var copy = new BrowserEnvironment
        {
            Name = original.Name + " (副本)",
            GroupId = original.GroupId,
            StartupUrl = original.StartupUrl,
            UserAgent = original.UserAgent,
            Resolution = original.Resolution,
            Timezone = original.Timezone,
            Languages = original.Languages,
            WebGLVendor = original.WebGLVendor,
            WebGLRenderer = original.WebGLRenderer,
            EnableWebRTC = original.EnableWebRTC,
            EnableCookies = original.EnableCookies,
            EnableJavaScript = original.EnableJavaScript,
            ProxyConfig = original.ProxyConfig,
            Remark = original.Remark,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        
        _db.Environments.Add(copy);
        await _db.SaveChangesAsync();
        Environments.Add(copy);
        OnPropertyChanged(nameof(TotalCount));
        return copy;
    }
    
    public async Task UpdateEnvironmentAsync(BrowserEnvironment env)
    {
        var existing = await _db.Environments.FindAsync(env.Id);
        if (existing != null)
        {
            existing.Name = env.Name;
            existing.GroupId = env.GroupId;
            existing.StartupUrl = env.StartupUrl;
            existing.UserAgent = env.UserAgent;
            existing.Resolution = env.Resolution;
            existing.Timezone = env.Timezone;
            existing.Languages = env.Languages;
            existing.WebGLVendor = env.WebGLVendor;
            existing.WebGLRenderer = env.WebGLRenderer;
            existing.EnableWebRTC = env.EnableWebRTC;
            existing.EnableCookies = env.EnableCookies;
            existing.EnableJavaScript = env.EnableJavaScript;
            existing.ProxyConfig = env.ProxyConfig;
            existing.Remark = env.Remark;
            existing.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
        }
    }
    
    public async Task<EnvironmentGroup> CreateGroupAsync(string name, string color)
    {
        var group = new EnvironmentGroup 
        { 
            Name = name, 
            Color = color,
            SortOrder = Groups.Count
        };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();
        Groups.Add(group);
        return group;
    }
    
    public async Task DeleteGroupAsync(int id)
    {
        var group = await _db.Groups.FindAsync(id);
        if (group != null)
        {
            _db.Groups.Remove(group);
            await _db.SaveChangesAsync();
            var item = Groups.FirstOrDefault(g => g.Id == id);
            if (item != null) Groups.Remove(item);
        }
    }
    
    public async Task<ProxyConfigModel> AddProxyAsync(string host, int port, ProxyType type, string? username, string? password, string? remark)
    {
        var proxy = new ProxyConfigModel
        {
            Host = host,
            Port = port,
            Type = type,
            Username = username,
            Password = password,
            Remark = remark
        };
        _db.Proxies.Add(proxy);
        await _db.SaveChangesAsync();
        Proxies.Add(proxy);
        return proxy;
    }
    
    public async Task DeleteProxyAsync(int id)
    {
        var proxy = await _db.Proxies.FindAsync(id);
        if (proxy != null)
        {
            _db.Proxies.Remove(proxy);
            await _db.SaveChangesAsync();
            var item = Proxies.FirstOrDefault(p => p.Id == id);
            if (item != null) Proxies.Remove(item);
        }
    }
    
    public void SetEnvironmentStatus(int id, BrowserStatus status)
    {
        var env = Environments.FirstOrDefault(e => e.Id == id);
        if (env != null)
        {
            env.Status = status;
            OnPropertyChanged(nameof(RunningCount));
        }
    }
}
