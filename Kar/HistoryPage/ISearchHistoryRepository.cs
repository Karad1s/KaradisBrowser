using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;
using System.Xml;

namespace Kar.HistoryPage
{
    /// <summary>
    /// Объект передачи данных (DTO), представляющий отдельную запись в истории посещений браузера.
    /// </summary>
    public class HistoryItemDto
    {
        /// <summary>URL-адрес посещенной страницы.</summary>
        public string url { get; set; } = string.Empty;

        /// <summary>Заголовок веб-страницы.</summary>
        public string title { get; set; } = string.Empty;

        /// <summary>Время последнего посещения страницы (в формате UTC строки).</summary>
        public string VisitTime { get; set; } = string.Empty;

        /// <summary>Количество переходов по данному URL.</summary>
        public int VisitCount { get; set; } = 1;
    }

    /// <summary>
    /// Интерфейс для работы с хранилищем истории браузера.
    /// Определяет основные CRUD-операции.
    /// </summary>
    public interface ISearchHistoryRepository
    {
        /// <summary>Инициализирует базу данных (создает таблицы при их отсутствии).</summary>
        Task InitializeAsync();

        /// <summary>Сохраняет информацию о посещенной странице или обновляет данные существующей.</summary>
        Task SaveQueryAsync(string url, string title);

        /// <summary>Полностью очищает историю посещений.</summary>
        Task ClearAsync();

        /// <summary>Возвращает список всех записей истории.</summary>
        Task<List<HistoryItemDto>> GetHistoryAsync();

        /// <summary>Возвращает топ самых посещаемых сайтов.</summary>
        Task<List<HistoryItemDto>> GetPopularSitesAsync(int limit);

        /// <summary>Удаляет конкретную запись по URL.</summary>
        Task DeleteItemAsync(string url);
    }

    /// <summary>
    /// Реализация репозитория истории посещений с использованием локальной базы данных SQLite.
    /// </summary>
    public class SqliteSearchHistoryRepository : ISearchHistoryRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Инициализирует новый экземпляр репозитория.
        /// </summary>
        /// <param name="dbPath">Путь к файлу базы данных SQLite.</param>
        public SqliteSearchHistoryRepository(string dbPath)
        {
            _connectionString = $"Data source = {dbPath}";
        }

        /// <summary>
        /// Создает таблицу SearchHistory, если она не существует, 
        /// а также выполняет миграцию схемы (добавляет колонку VisitCount для старых версий БД).
        /// </summary>
        public async Task InitializeAsync()
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS SearchHistory(
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Url TEXT NOT NULL UNIQUE,
                    Title TEXT,
                    VisitTimeUtc TEXT NOT NULL,
                    VisitCount INTEGER DEFAULT 1
                )";

            await cmd.ExecuteNonQueryAsync();

            try
            {
                // Попытка обновления схемы (миграция для совместимости с предыдущими версиями)
                cmd.CommandText = "ALTER TABLE SearchHistory ADD COLUMN VisitCount INTEGER DEFAULT 1";
                await cmd.ExecuteNonQueryAsync();
            }
            catch
            {
                // Колонка уже существует, игнорируем ошибку
            }
        }

        /// <summary>
        /// Добавляет новую запись в историю. Если URL уже существует, обновляет заголовок, 
        /// время последнего визита и увеличивает счетчик посещений (Upsert-логика).
        /// </summary>
        /// <param name="url">Посещенный URL.</param>
        /// <param name="title">Заголовок страницы.</param>
        public async Task SaveQueryAsync(string url, string title)
        {
            if (string.IsNullOrWhiteSpace(url)) return;

            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            // Запрос с использованием конструкции ON CONFLICT для атомарного обновления/вставки
            string upsetQuery = @"
                INSERT INTO SearchHistory (Url, Title, VisitTimeUtc)
                VALUES (@url, @title, @visitTime)
                ON CONFLICT(Url) DO UPDATE SET
                    Title=excluded.Title,
                    VisitTimeUtc=excluded.VisitTimeUtc,
                    VisitCount=SearchHistory.VisitCount + 1";

            using var cmd = new SqliteCommand(upsetQuery, conn);

            cmd.Parameters.AddWithValue("@url", url);
            cmd.Parameters.AddWithValue("@title", title ?? string.Empty);
            cmd.Parameters.AddWithValue("@visitTime", DateTime.UtcNow.ToString("o"));

            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Удаляет все записи из таблицы истории.
        /// </summary>
        public async Task ClearAsync()
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new SqliteCommand("DELETE FROM SearchHistory", conn);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Извлекает всю историю посещений, отсортированную по времени по убыванию (от новых к старым).
        /// </summary>
        /// <returns>Список объектов HistoryItemDto.</returns>
        public async Task<List<HistoryItemDto>> GetHistoryAsync()
        {
            var list = new List<HistoryItemDto>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            string query = "SELECT Url, Title, VisitTimeUtc FROM SearchHistory ORDER BY VisitTimeUtc DESC";
            using var cmd = new SqliteCommand(query, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new HistoryItemDto
                {
                    url = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                    title = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    VisitTime = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
                });
            }

            return list;
        }

        /// <summary>
        /// Удаляет запись из базы данных по точному совпадению URL.
        /// </summary>
        /// <param name="url">URL для удаления.</param>
        public async Task DeleteItemAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;

            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new SqliteCommand("DELETE FROM SearchHistory WHERE Url = @url", conn);
            cmd.Parameters.AddWithValue("@url", url);
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Возвращает список самых популярных сайтов, сортируя их сначала по количеству посещений, 
        /// а затем по времени последнего визита.
        /// </summary>
        /// <param name="limit">Количество возвращаемых записей.</param>
        /// <returns>Список популярных сайтов.</returns>
        public async Task<List<HistoryItemDto>> GetPopularSitesAsync(int limit)
        {
            var list = new List<HistoryItemDto>();
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            string query = "SELECT Url, Title, VisitCount FROM SearchHistory ORDER BY VisitCount DESC, VisitTimeUtc DESC LIMIT @limit";
            using var cmd = new SqliteCommand(query, conn);
            cmd.Parameters.AddWithValue("@limit", limit);

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new HistoryItemDto
                {
                    url = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                    title = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    VisitCount = reader.IsDBNull(2) ? 1 : reader.GetInt32(2)
                });
            }
            return list;
        }
    }
}