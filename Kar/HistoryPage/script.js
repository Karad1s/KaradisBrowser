async function initHistory() {

    await CefSharp.BindObjectAsync("historyBridge");

        function renderHistoryItem(item) {
        const historyArea = document.querySelector('.history_aria');
        historyArea.innerHTML += '';

        if (item.length > 0) {
            item.forEach(row =>{
                const historyItem = document.createElement('div');
                historyItem.classList.add('history_item');
                historyItem.innerHTML = `
                    <div class="history_item_date">${new Date(row.VisitTime).toLocaleString()}</div>
                    <div class="history_item_title">${row.title}</div>
                    <div class="history_item_url">${row.url}</div>
                `;
                historyArea.appendChild(historyItem);
            });
        }
    } 

    async function loadHistory() {
    const jsonString = await historyBridge.getHistory();
    const historyData = JSON.parse(jsonString);
    renderHistoryItem(historyData);
    }

    document.getElementById('delete').addEventListener('click', async () => {
        await historyBridge.clearHistoryAsync();
        await loadHistory();
    });

    await loadHistory();
}

initHistory().catch(error => console.error('Error initializing history:', error));