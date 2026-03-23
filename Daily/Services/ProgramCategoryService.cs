using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Daily.Models;

namespace Daily.Services;

/// <summary>
/// Provides category classification for known applications.
/// Covers common Windows programs and popular Chinese software.
/// User-defined overrides are persisted to %APPDATA%\Daily\categories.json.
/// </summary>
public class ProgramCategoryService
{
    public const string CategoryWork = "Work";
    public const string CategoryBrowser = "Browser";
    public const string CategorySocial = "Social";
    public const string CategoryCommunication = "Communication";
    public const string CategoryDevelopment = "Development";
    public const string CategoryEntertainment = "Entertainment";
    public const string CategoryGaming = "Gaming";
    public const string CategoryMedia = "Media";
    public const string CategoryUtility = "Utility";
    public const string CategoryEducation = "Education";
    public const string CategoryFinance = "Finance";
    public const string CategorySecurity = "Security";
    public const string CategoryOther = "Other";

    /// <summary>All available category names.</summary>
    public static readonly IReadOnlyList<string> AllCategories =
    [
        CategoryWork, CategoryBrowser, CategorySocial, CategoryCommunication,
        CategoryDevelopment, CategoryEntertainment, CategoryGaming, CategoryMedia,
        CategoryUtility, CategoryEducation, CategoryFinance, CategorySecurity,
        CategoryOther
    ];

    private static readonly string UserConfigFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Daily", "categories.json");

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    /// <summary>
    /// Built-in preset mappings (process name → category).
    /// </summary>
    private static readonly Dictionary<string, string> BuiltinMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // ── Work / Office ────────────────────────────────────────────────
        { "winword",            CategoryWork },
        { "excel",              CategoryWork },
        { "powerpnt",           CategoryWork },
        { "outlook",            CategoryWork },
        { "onenote",            CategoryWork },
        { "msaccess",           CategoryWork },
        { "mspub",              CategoryWork },
        { "visio",              CategoryWork },
        { "project",            CategoryWork },
        { "teams",              CategoryWork },      // Microsoft Teams
        { "lync",               CategoryWork },      // Skype for Business
        { "notion",             CategoryWork },
        { "obsidian",           CategoryWork },
        { "evernote",           CategoryWork },
        { "typora",             CategoryWork },
        { "marktext",           CategoryWork },
        { "workflowy",          CategoryWork },
        { "roamresearch",       CategoryWork },
        { "trello",             CategoryWork },
        { "asana",              CategoryWork },
        // WPS Office (popular in China)
        { "wps",                CategoryWork },
        { "wpsoffice",          CategoryWork },
        { "et",                 CategoryWork },      // WPS Spreadsheets
        { "wpp",                CategoryWork },      // WPS Presentation
        { "kingsoft",           CategoryWork },
        { "dingding",           CategoryWork },      // DingTalk / 钉钉
        { "dingtalk",           CategoryWork },
        { "钉钉",               CategoryWork },
        { "feishu",             CategoryWork },      // Feishu / 飞书
        { "larkapp",            CategoryWork },      // Lark (global Feishu)
        { "worktile",           CategoryWork },
        { "teambition",         CategoryWork },
        { "wxwork",             CategoryWork },      // WeCom / 企业微信

        // ── Browsers ─────────────────────────────────────────────────────
        { "chrome",             CategoryBrowser },
        { "firefox",            CategoryBrowser },
        { "msedge",             CategoryBrowser },
        { "opera",              CategoryBrowser },
        { "brave",              CategoryBrowser },
        { "iexplore",           CategoryBrowser },
        { "safari",             CategoryBrowser },
        { "vivaldi",            CategoryBrowser },
        { "360chrome",          CategoryBrowser },   // 360 Browser
        { "360se",              CategoryBrowser },   // 360 Secure Browser / 360安全浏览器
        { "qqbrowser",          CategoryBrowser },   // QQ Browser / QQ浏览器
        { "2345explorer",       CategoryBrowser },
        { "ucbrowser",          CategoryBrowser },   // UC Browser
        { "sogouexplorer",      CategoryBrowser },   // Sogou Browser / 搜狗浏览器
        { "liebao",             CategoryBrowser },   // Liebao / 猎豹浏览器
        { "maxthon",            CategoryBrowser },

        // ── Social / IM ──────────────────────────────────────────────────
        { "wechat",             CategorySocial },    // WeChat / 微信
        { "qq",                 CategorySocial },    // QQ
        { "qqprotect",          CategorySocial },
        { "slack",              CategorySocial },
        { "discord",            CategorySocial },
        { "telegram",           CategorySocial },
        { "whatsapp",           CategorySocial },
        { "line",               CategorySocial },
        { "kakaotalk",          CategorySocial },
        { "twitter",            CategorySocial },
        { "x",                  CategorySocial },
        { "instagram",          CategorySocial },
        { "facebook",           CategorySocial },
        { "weibo",              CategorySocial },    // Weibo / 微博
        { "douyin",             CategorySocial },    // Douyin / 抖音
        { "tiktok",             CategorySocial },
        { "kuaishou",           CategorySocial },    // Kuaishou / 快手
        { "bilibili",           CategorySocial },    // Bilibili
        { "zhihu",              CategorySocial },    // Zhihu / 知乎
        { "xiaohongshu",        CategorySocial },    // Little Red Book / 小红书

        // ── Communication ────────────────────────────────────────────────
        { "zoom",               CategoryCommunication },
        { "skype",              CategoryCommunication },
        { "thunderbird",        CategoryCommunication },
        { "foxmail",            CategoryCommunication }, // Foxmail / 福克斯邮件

        // ── Development ──────────────────────────────────────────────────
        { "devenv",             CategoryDevelopment }, // Visual Studio
        { "code",               CategoryDevelopment }, // VS Code
        { "rider",              CategoryDevelopment },
        { "idea",               CategoryDevelopment }, // IntelliJ IDEA
        { "pycharm",            CategoryDevelopment },
        { "webstorm",           CategoryDevelopment },
        { "phpstorm",           CategoryDevelopment },
        { "clion",              CategoryDevelopment },
        { "goland",             CategoryDevelopment },
        { "datagrip",           CategoryDevelopment },
        { "studio64",           CategoryDevelopment }, // Android Studio
        { "androidstudio",      CategoryDevelopment },
        { "eclipse",            CategoryDevelopment },
        { "netbeans",           CategoryDevelopment },
        { "xcode",              CategoryDevelopment },
        { "sublimetext",        CategoryDevelopment },
        { "notepad++",          CategoryDevelopment },
        { "notepadplusplus",    CategoryDevelopment },
        { "atom",               CategoryDevelopment },
        { "vim",                CategoryDevelopment },
        { "emacs",              CategoryDevelopment },
        { "neovim",             CategoryDevelopment },
        { "nvim",               CategoryDevelopment },
        { "git",                CategoryDevelopment },
        { "gitkraken",          CategoryDevelopment },
        { "sourcetree",         CategoryDevelopment },
        { "fork",               CategoryDevelopment },
        { "gitextensions",      CategoryDevelopment },
        { "postman",            CategoryDevelopment },
        { "insomnia",           CategoryDevelopment },
        { "dbeaver",            CategoryDevelopment },
        { "heidiSQL",           CategoryDevelopment },
        { "dbeaverlauncher",    CategoryDevelopment },
        { "terminal",           CategoryDevelopment },
        { "wt",                 CategoryDevelopment }, // Windows Terminal
        { "cmd",                CategoryDevelopment },
        { "powershell",         CategoryDevelopment },
        { "pwsh",               CategoryDevelopment },
        { "ssh",                CategoryDevelopment },
        { "putty",              CategoryDevelopment },
        { "mremoteng",          CategoryDevelopment },
        { "xshell",             CategoryDevelopment },
        { "finalshell",         CategoryDevelopment }, // FinalShell (popular in China)
        { "hbuilder",           CategoryDevelopment }, // HBuilder (popular in China)
        { "hbuilderx",          CategoryDevelopment },

        // ── Entertainment / Video ────────────────────────────────────────
        { "vlc",                CategoryEntertainment },
        { "mpv",                CategoryEntertainment },
        { "potplayer",          CategoryEntertainment },
        { "potplayermini64",    CategoryEntertainment },
        { "potplayermini",      CategoryEntertainment },
        { "kmplayer",           CategoryEntertainment },
        { "mpc-hc",             CategoryEntertainment },
        { "mpc-hc64",           CategoryEntertainment },
        { "netflix",            CategoryEntertainment },
        { "plex",               CategoryEntertainment },
        { "kodi",               CategoryEntertainment },
        { "mxplayer",           CategoryEntertainment },
        { "youku",              CategoryEntertainment }, // Youku / 优酷
        { "iqiyi",              CategoryEntertainment }, // iQiYi / 爱奇艺
        { "tencentvideo",       CategoryEntertainment }, // Tencent Video / 腾讯视频
        { "mango",              CategoryEntertainment }, // Mango TV / 芒果TV
        { "qqlive",             CategoryEntertainment }, // QQ Live

        // ── Music ────────────────────────────────────────────────────────
        { "spotify",            CategoryMedia },
        { "musicbee",           CategoryMedia },
        { "foobar2000",         CategoryMedia },
        { "itunes",             CategoryMedia },
        { "winamp",             CategoryMedia },
        { "aimp",               CategoryMedia },
        { "cloudmusic",         CategoryMedia },    // NetEase Cloud Music / 网易云音乐
        { "neteasemusic",       CategoryMedia },
        { "qqmusic",            CategoryMedia },    // QQ Music / QQ音乐
        { "kugou",              CategoryMedia },    // Kugou Music / 酷狗音乐
        { "kuwo",               CategoryMedia },    // Kuwo Music / 酷我音乐

        // ── Gaming ───────────────────────────────────────────────────────
        { "steam",              CategoryGaming },
        { "epicgameslauncher",  CategoryGaming },
        { "riotclientservices", CategoryGaming },
        { "leagueclient",       CategoryGaming },
        { "r5apex",             CategoryGaming },
        { "valorant",           CategoryGaming },
        { "csgo",               CategoryGaming },
        { "cs2",                CategoryGaming },
        { "gta5",               CategoryGaming },
        { "witcher3",           CategoryGaming },
        { "minecraft",          CategoryGaming },
        { "javaw",              CategoryGaming },    // Minecraft Java Edition
        { "origin",             CategoryGaming },
        { "battle.net",         CategoryGaming },
        { "battlenet",          CategoryGaming },
        { "ubisoft",            CategoryGaming },
        { "uplay",              CategoryGaming },
        { "ubisoftgamelauncher",CategoryGaming },
        { "wegame",             CategoryGaming },    // WeGame (Tencent gaming platform)
        { "tencentgames",       CategoryGaming },

        // ── Utility / System Tools ───────────────────────────────────────
        { "everything",         CategoryUtility },
        { "wox",                CategoryUtility },
        { "launchy",            CategoryUtility },
        { "keypirinha",         CategoryUtility },
        { "listary",            CategoryUtility },
        { "7-zip",              CategoryUtility },
        { "7zfm",               CategoryUtility },
        { "winrar",             CategoryUtility },
        { "bandizip",           CategoryUtility },
        { "clipboard",          CategoryUtility },
        { "greenshot",          CategoryUtility },
        { "snagit",             CategoryUtility },
        { "sharex",             CategoryUtility },
        { "winsnap",            CategoryUtility },
        { "paint",              CategoryUtility },
        { "mspaint",            CategoryUtility },
        { "calc",               CategoryUtility },
        { "taskmgr",            CategoryUtility },
        { "regedit",            CategoryUtility },
        { "mmc",                CategoryUtility },
        { "resmon",             CategoryUtility },
        { "perfmon",            CategoryUtility },
        { "msconfig",           CategoryUtility },
        { "control",            CategoryUtility },
        { "notepad",            CategoryUtility },
        { "wordpad",            CategoryUtility },
        { "dism",               CategoryUtility },
        { "360safe",            CategorySecurity }, // 360 Antivirus / 360安全卫士
        { "360tray",            CategorySecurity },
        { "360sd",              CategorySecurity },
        { "kav",                CategorySecurity },
        { "avp",                CategorySecurity },
        { "mbam",               CategorySecurity }, // Malwarebytes

        // ── Education ────────────────────────────────────────────────────
        { "anki",               CategoryEducation },
        { "kindle",             CategoryEducation },
        { "calibre",            CategoryEducation },
        { "sumatra",            CategoryEducation }, // SumatraPDF
        { "foxitreader",        CategoryEducation },
        { "adobereader",        CategoryEducation },
        { "acrobat",            CategoryEducation },
        { "pdf",                CategoryEducation },
        { "duolingo",           CategoryEducation },
        { "yuque",              CategoryEducation }, // Yuque / 语雀 (Alibaba wiki)
        { "xmind",              CategoryEducation }, // XMind (mind mapping)
        { "mindmanager",        CategoryEducation },
        { "freemind",           CategoryEducation },

        // ── Finance ──────────────────────────────────────────────────────
        { "alipay",             CategoryFinance },   // Alipay / 支付宝
        { "quicken",            CategoryFinance },
        { "gnucash",            CategoryFinance },
        { "financialstatement", CategoryFinance },
        { "moneymanager",       CategoryFinance },
    };

    /// <summary>User-defined overrides, loaded from disk and merged on top of the built-in map.</summary>
    private Dictionary<string, string> _userMap = new(StringComparer.OrdinalIgnoreCase);

    public ProgramCategoryService()
    {
        LoadUserOverrides();
    }

    /// <summary>
    /// Returns the category for a given process name, or <see cref="CategoryOther"/> when unknown.
    /// User overrides take priority over built-in presets.
    /// </summary>
    public string GetCategory(string processName, string executablePath = "")
    {
        if (string.IsNullOrEmpty(processName))
            return CategoryOther;

        if (_userMap.TryGetValue(processName, out var cat)) return cat;
        if (BuiltinMap.TryGetValue(processName, out cat)) return cat;

        // Fallback: check if executable path contains any known keyword
        if (!string.IsNullOrEmpty(executablePath))
        {
            var fileName = Path.GetFileNameWithoutExtension(executablePath);
            if (!string.IsNullOrEmpty(fileName))
            {
                if (_userMap.TryGetValue(fileName, out cat)) return cat;
                if (BuiltinMap.TryGetValue(fileName, out cat)) return cat;
            }
        }

        return CategoryOther;
    }

    /// <summary>
    /// Persists a user-defined category override for a process name.
    /// </summary>
    public void SetUserCategory(string processName, string category)
    {
        if (string.IsNullOrEmpty(processName)) return;
        _userMap[processName] = category;
        SaveUserOverrides();
    }

    /// <summary>
    /// Removes a user-defined override, reverting to the built-in category.
    /// </summary>
    public void RemoveUserCategory(string processName)
    {
        if (_userMap.Remove(processName))
            SaveUserOverrides();
    }

    /// <summary>
    /// Returns all mappings (built-in presets merged with user overrides).
    /// User overrides are marked with <see cref="CategoryMappingEntry.IsUserOverride"/> = true.
    /// </summary>
    public IReadOnlyList<CategoryMappingEntry> GetAllMappings()
    {
        var result = new Dictionary<string, CategoryMappingEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var kv in BuiltinMap)
            result[kv.Key] = new CategoryMappingEntry(kv.Key, kv.Value, isUserOverride: false);

        foreach (var kv in _userMap)
            result[kv.Key] = new CategoryMappingEntry(kv.Key, kv.Value, isUserOverride: true);

        return result.Values.OrderBy(e => e.ProcessName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private void LoadUserOverrides()
    {
        if (!File.Exists(UserConfigFile)) return;
        try
        {
            var json = File.ReadAllText(UserConfigFile);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (dict is not null)
                _userMap = new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
        }
        catch { /* ignore corrupt file */ }
    }

    private void SaveUserOverrides()
    {
        var dir = Path.GetDirectoryName(UserConfigFile);
        if (dir is not null)
            Directory.CreateDirectory(dir);
        File.WriteAllText(UserConfigFile, JsonSerializer.Serialize(_userMap, JsonOpts));
    }
}
