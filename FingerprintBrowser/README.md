# 指纹浏览器 - Fingerprint Browser

仿 AdsPower 的指纹浏览器 WPF 应用程序，基于 .NET 6 和 MVVM 架构。

## 功能特性

### 核心功能
- **多账号环境管理** - 创建、编辑、复制、删除浏览器环境
- **环境分组** - 按分组管理浏览器环境
- **浏览器指纹隔离** - 通过 Playwright 实现商业级指纹隔离
  - 自定义分辨率、时区、语言
  - 自定义 User Agent、WebGL 信息
  - Canvas 指纹保护
- **代理管理** - 支持 HTTP/HTTPS/SOCKS5 代理
- **批量操作** - 批量启动/停止浏览器环境
- **状态监控** - 实时监控环境运行状态

### 界面特性
- **深色主题** - 采用 HandyControl 深色主题
- **换肤功能** - 支持深色、浅色、深蓝三种主题
- **响应式布局** - 可调节的分组列表和环境详情面板

## 技术栈

| 技术 | 版本 | 说明 |
|------|------|------|
| .NET | 6.0 | 运行时框架 |
| WPF | - | UI 框架 |
| CommunityToolkit.Mvvm | 8.2.2 | MVVM 框架 |
| HandyControl | 3.4.0 | UI 组件库 |
| Entity Framework Core | 6.0.25 | ORM 框架 |
| SQLite | - | 数据库 |
| Microsoft.Playwright | 1.40.0 | 浏览器自动化 |
| Serilog | 3.1.1 | 日志框架 |

## 项目结构

```
FingerprintBrowser/
├── Models/                    # 数据模型
│   └── BrowserModels.cs      # 浏览器环境、代理、分组模型
├── ViewModels/               # 视图模型
│   ├── MainViewModel.cs      # 主窗口视图模型
│   ├── ProxyViewModel.cs      # 代理管理视图模型
│   ├── SettingsViewModel.cs   # 设置视图模型
│   └── EnvironmentEditViewModel.cs # 环境编辑视图模型
├── Views/                     # 视图
│   ├── MainWindow.xaml        # 主窗口
│   ├── EnvironmentEditWindow.xaml  # 环境编辑窗口
│   ├── ProxyWindow.xaml       # 代理管理窗口
│   └── SettingsWindow.xaml    # 设置窗口
├── Services/                  # 服务层
│   ├── BrowserService.cs      # 浏览器服务
│   ├── PlaywrightService.cs   # Playwright 服务
│   ├── ProxyService.cs        # 代理服务
│   ├── DialogService.cs       # 对话框服务
│   └── ImportExportService.cs # 导入导出服务
├── Data/                      # 数据层
│   └── BrowserDbContext.cs    # 数据库上下文
├── Converters/                # 值转换器
├── Resources/                 # 资源文件
│   └── CustomStyles.xaml      # 自定义样式
├── App.xaml                   # 应用程序入口
└── FingerprintBrowser.csproj  # 项目文件
```

## 编译运行

### 环境要求
- .NET 6.0 SDK 或更高版本
- Windows 10/11

### 编译步骤

```bash
# 1. 还原依赖
dotnet restore

# 2. 编译项目
dotnet build

# 3. 运行项目
dotnet run
```

### 发布为可执行文件

```bash
# 发布为自包含可执行文件
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# 或发布为依赖框架的可执行文件
dotnet publish -c Release -r win-x64
```

## 数据存储

应用程序数据存储在用户本地文件夹：
```
%LOCALAPPDATA%\FingerprintBrowser\
├── browser_data.db      # SQLite 数据库
├── settings.json        # 用户设置
└── logs/               # 日志文件
    └── app-YYYY-MM-DD.log
```

## 使用说明

### 创建新环境
1. 点击工具栏的「新建环境」按钮
2. 输入环境名称
3. 配置浏览器指纹（分辨率、时区、语言等）
4. 如需使用代理，启用代理并选择代理配置
5. 设置启动URL
6. 点击「保存」

### 批量启动/停止
1. 选择一个分组或使用搜索筛选环境
2. 点击「批量启动」或「批量停止」按钮

### 代理管理
1. 通过菜单或快捷键打开「代理管理」窗口
2. 添加或导入代理配置
3. 测试代理可用性
4. 为环境分配代理

## 注意事项

1. **首次运行** - Playwright 浏览器驱动会在首次运行时自动下载
2. **管理员权限** - 如需在某些系统上运行，可能需要管理员权限
3. **防火墙** - 确保防火墙允许应用程序访问网络

## License

MIT License
