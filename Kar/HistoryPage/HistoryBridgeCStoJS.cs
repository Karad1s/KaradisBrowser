using System;
using System.IO;
using System.Threading.Tasks;


namespace Kar.HistoryPage
{
    public class HistoryBridge
    {
        public Task<string> GetHistoryAsunc()
        {
            string json = "[{\"Title\":\"Пример\",\"Url\":\"https://google.com\",\"VisitTimeUtc\":\"2026-05-12T10:00:00\"}]";
            return Task.FromResult(json);
        }

        public async Task ClearHistoryAsync()
        {
            await App.HistoryRepo.ClearAsync();
        }
    }
}

