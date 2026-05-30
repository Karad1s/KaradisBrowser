using System;
using System.IO;

namespace Kar
{
    public static class BrowserConfig
    {
        // Directories and System paths
        public static readonly string SettingsDirName = "Settings";
        public static readonly string SettingsFileName = "Settings.json";
        public static readonly string SessionFileName = "session.yaml";
        
        public static string SettingsDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsDirName);
        public static string SettingsPath => Path.Combine(SettingsDir, SettingsFileName);
        public static string SessionPath => Path.Combine(SettingsDir, SessionFileName);

        public static string HistoryDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HistoryPage");
        public static string HistoryHtmlPath => Path.Combine(HistoryDir, "history.html");
        public static string HistoryDbPath => "history.db";

        public static string LibraryDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Library");
        public static string LibraryHtmlPath => Path.Combine(LibraryDir, "library.html");

        public static string HomepageDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Homepage");
        public static string HomepageHtmlPath => Path.Combine(HomepageDir, "home.html");

        // External endpoints
        public static readonly string ChromeExtensionsUrl = "https://chromewebstore.google.com/category/extensions";
        public static readonly string GoogleSuggestionsApiUrl = "http://suggestqueries.google.com/complete/search?client=firefox&q=";

        // Custom protocol or fallback keys
        public static readonly string AboutHome = "about:home";
        public static readonly string FallbackHomeRelative = "Kar/Homepage/home.html";

        // Provider authentication domains
        public static readonly string[] AuthDomains = new[]
        {
            "accounts.google.com",
            "facebook.com",
            "login.live.com",
            "appleid.apple.com",
            "github.com/login/oauth",
            "oauth.vk.com",
            "mail.ru",
            "yandex.ru/auth",
            "ok.ru/dk"
        };

        // Standard auth keywords inside path
        public static readonly string[] AuthKeywords = new[]
        {
            "login",
            "auth",
            "oauth",
            "signin",
            "sign-in",
            "authorize",
            "register",
            "signup",
            "sign-up"
        };

        // Universal OAuth2 Query Parameters
        public static readonly string[] OAuthParameters = new[]
        {
            "client_id=",
            "response_type=",
            "redirect_uri="
        };
    }
}
