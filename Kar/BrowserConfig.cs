using System;
using System.IO;

namespace Kar
{
    public static class BrowserConfig
    {
        // Директории и системные пути
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

        // Внешние конечные точки
        public static readonly string ChromeExtensionsUrl = "https://chromewebstore.google.com/category/extensions";
        public static readonly string GoogleSuggestionsApiUrl = "http://suggestqueries.google.com/complete/search?client=firefox&q=";

        // Кастомный протокол или резервные ключи
        public static readonly string AboutHome = "about:home";
        public static readonly string FallbackHomeRelative = "Kar/Homepage/home.html";

        // Домены аутентификации провайдеров
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

        // Стандартные ключевые слова аутентификации в пути
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

        // Универсальные параметры запроса OAuth2
        public static readonly string[] OAuthParameters = new[]
        {
            "client_id=",
            "response_type=",
            "redirect_uri="
        };
    }
}
