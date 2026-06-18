using System;
using System.IO;
using System.Threading.Tasks;

namespace Kar.HistoryPage
{
    /// <summary>
    /// Класс-мост для интеграции C# и JavaScript (CefSharp JS Binding).
    /// Предоставляет методы для управления историей посещений из пользовательского интерфейса (веб-страницы).
    /// </summary>
    public class HistoryBridge
    {
        /// <summary>
        /// Асинхронно получает полную историю посещений.
        /// Данные сериализуются в формат JSON для удобной обработки на стороне JavaScript.
        /// </summary>
        /// <returns>JSON-строка, содержащая массив объектов истории.</returns>
        public async Task<string> GetHistory()
        {
            var history = await App.HistoryRepo.GetHistoryAsync();
            return System.Text.Json.JsonSerializer.Serialize(history);
        }

        /// <summary>
        /// Асинхронно очищает всю историю посещений в базе данных.
        /// </summary>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        public async Task ClearHistoryAsync()
        {
            await App.HistoryRepo.ClearAsync();
        }

        /// <summary>
        /// Асинхронно удаляет конкретную запись из истории по ее URL.
        /// </summary>
        /// <param name="url">URL-адрес, который необходимо удалить.</param>
        /// <returns>Задача, представляющая асинхронную операцию.</returns>
        public async Task DeleteItem(string url)
        {
            await App.HistoryRepo.DeleteItemAsync(url);
        }

        /// <summary>
        /// Асинхронно получает список наиболее часто посещаемых сайтов.
        /// </summary>
        /// <param name="limit">Максимальное количество возвращаемых записей.</param>
        /// <returns>JSON-строка, содержащая массив популярных сайтов.</returns>
        public async Task<string> GetPopularSites(int limit)
        {
            var popularSites = await App.HistoryRepo.GetPopularSitesAsync(limit);
            return System.Text.Json.JsonSerializer.Serialize(popularSites);
        }
    }
}