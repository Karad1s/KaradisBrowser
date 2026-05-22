using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;
using System.Xml;

namespace Kar.HistoryPage
{
    public class HistoryItemDto
    {
        public string url { get; set; } = string.Empty;
        public string title { get; set; } = string.Empty;
        public string VisitTime { get; set; } = string.Empty;
    }

    public interface ISearchHistoryRepository
    {
        Task InitializeAsync();

        Task SaveQueryAsync(string url, string title);

        Task ClearAsync();

        Task<List<HistoryItemDto>> GetHistoryAsync();

        Task DeleteItemAsync(string url);
    }

    public class SqliteSearchHistoryRepository : ISearchHistoryRepository
    {
        private readonly string _connectionString;

        public SqliteSearchHistoryRepository(string dbPath)
        {
            _connectionString = $"Data source = {dbPath}";
        }

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
                    VisitTimeUtc TEXT NOT NULL
                )";

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SaveQueryAsync(string url, string title)
        {
            if (string.IsNullOrWhiteSpace(url)) return;

            using var conn = new SqliteConnection(_connectionString);

            await conn.OpenAsync();

            string upsetQuery = @"
                INSERT INTO SearchHistory (Url, Title, VisitTimeUtc)
                VALUES (@url, @title, @visitTime)
                ON CONFLICT(Url) DO UPDATE SET
                    Title=excluded.Title,
                    VisitTimeUtc=excluded.VisitTimeUtc";

            using var cmd = new SqliteCommand(upsetQuery, conn);

            cmd.Parameters.AddWithValue("@url", url);
            cmd.Parameters.AddWithValue("@title", title ?? string.Empty);
            cmd.Parameters.AddWithValue("@visitTime", DateTime.UtcNow.ToString("o"));

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task ClearAsync()
        {
            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new SqliteCommand("DELETE FROM SearchHistory", conn);
            await cmd.ExecuteNonQueryAsync();
        }

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

        public async Task DeleteItemAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;

            using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new SqliteCommand("DELETE FROM SearchHistory WHERE Url = @url", conn);
            cmd.Parameters.AddWithValue("@url", url);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
