using System.Security.Cryptography;
using System.Text;

namespace FingerprintBrowser.Services;

/// <summary>
/// 浏览器指纹工具类
/// </summary>
public static class FingerprintGenerator
{
    private static readonly Random _random = new();

    /// <summary>
    /// 生成随机 User Agent
    /// </summary>
    public static string GenerateUserAgent(BrowserType type = BrowserType.Chrome)
    {
        var chromeVersions = new[]
        {
            "119.0.6045.106",
            "120.0.6099.109",
            "121.0.6167.85",
            "122.0.6261.57",
            "123.0.6312.59"
        };

        var version = chromeVersions[_random.Next(chromeVersions.Length)];

        return type switch
        {
            BrowserType.Chrome => $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{version} Safari/537.36",
            BrowserType.Firefox => GenerateFirefoxUserAgent(),
            BrowserType.Edge => GenerateEdgeUserAgent(version),
            _ => $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{version} Safari/537.36"
        };
    }

    private static string GenerateFirefoxUserAgent()
    {
        var version = $"{(int)(100 + _random.Next(30))}.0";
        return $"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:{version}) Gecko/20100101 Firefox/{version}";
    }

    private static string GenerateEdgeUserAgent(string chromeVersion)
    {
        return $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chromeVersion} Safari/537.36 Edg/{chromeVersion}";
    }

    /// <summary>
    /// 生成随机分辨率
    /// </summary>
    public static (int Width, int Height) GenerateResolution()
    {
        var resolutions = new[]
        {
            (1920, 1080), (1366, 768), (1440, 900), (1600, 900),
            (1280, 720), (1536, 864), (2560, 1440), (3840, 2160),
            (1280, 800), (1280, 1024), (1680, 1050), (1400, 900),
            (1920, 1200), (2560, 1600), (1360, 768), (1024, 768)
        };
        return resolutions[_random.Next(resolutions.Length)];
    }

    /// <summary>
    /// 生成随机时区偏移
    /// </summary>
    public static int GenerateTimezone()
    {
        return _random.Next(-12, 13);
    }

    /// <summary>
    /// 生成随机语言
    /// </summary>
    public static string GenerateLanguage()
    {
        var languages = new[]
        {
            "zh-CN", "en-US", "en-GB", "ja-JP", "ko-KR",
            "de-DE", "fr-FR", "es-ES", "it-IT", "pt-BR",
            "ru-RU", "zh-TW", "en-CA", "en-AU"
        };
        return languages[_random.Next(languages.Length)];
    }

    /// <summary>
    /// 生成随机平台
    /// </summary>
    public static string GeneratePlatform()
    {
        var platforms = new[] { "Win32", "MacIntel", "Linux x86_64" };
        return platforms[_random.Next(platforms.Length)];
    }

    /// <summary>
    /// 生成随机 CPU 核心数
    /// </summary>
    public static string GenerateHardwareConcurrency()
    {
        var cores = new[] { "2", "4", "6", "8", "12", "16" };
        return cores[_random.Next(cores.Length)];
    }

    /// <summary>
    /// 生成随机设备内存
    /// </summary>
    public static string GenerateDeviceMemory()
    {
        var memory = new[] { "2", "4", "8", "16", "32" };
        return memory[_random.Next(memory.Length)];
    }

    /// <summary>
    /// 生成随机 WebGL 信息
    /// </summary>
    public static (string Vendor, string Renderer) GenerateWebGLInfo()
    {
        var vendors = new[] { "Intel Inc.", "NVIDIA Corporation", "AMD", "Google Inc." };
        var renderers = new[]
        {
            "Intel Iris OpenGL Engine",
            "Intel UHD Graphics 620",
            "NVIDIA GeForce GTX 1060 OpenGL Engine",
            "NVIDIA GeForce RTX 3060 OpenGL Engine",
            "AMD Radeon Pro 5500M OpenGL Engine",
            "AMD Radeon RX 580 OpenGL Engine",
            "ANGLE (Intel, Intel(R) UHD Graphics Direct3D11 vs_5_0 ps_5_0)"
        };

        return (vendors[_random.Next(vendors.Length)], renderers[_random.Next(renderers.Length)]);
    }

    /// <summary>
    /// 生成 Canvas 指纹哈希
    /// </summary>
    public static string GenerateCanvasHash()
    {
        // Canvas 指纹通常基于 Canvas API 绘制结果
        // 这里生成一个模拟的哈希值
        var bytes = new byte[16];
        _random.NextBytes(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// 生成随机 AudioContext 指纹
    /// </summary>
    public static double GenerateAudioFingerprint()
    {
        return Math.Round(_random.NextDouble() * 0.01 + 0.001, 6);
    }
}

/// <summary>
/// 浏览器类型
/// </summary>
public enum BrowserType
{
    Chrome,
    Firefox,
    Edge
}
