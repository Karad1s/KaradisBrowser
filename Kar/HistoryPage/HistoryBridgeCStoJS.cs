using System;
using System.IO;
using System.Threading.Tasks;


namespace Kar.HistoryPage
{
    public class HistoryBridge
    {
        public async Task<string> GetHistory()
        {
            var history = await App.HistoryRepo.GetHistoryAsync();
            return System.Text.Json.JsonSerializer.Serialize(history);
        }

        public async Task ClearHistoryAsync()
        {
            await App.HistoryRepo.ClearAsync();
        }

        public async Task DeleteItem(string url)
        {
            await App.HistoryRepo.DeleteItemAsync(url);
        }
    }
}

