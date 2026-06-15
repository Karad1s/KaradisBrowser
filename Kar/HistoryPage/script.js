async function initHistory() {
    // Wait for the CefSharp bridge object to be bound
    await CefSharp.BindObjectAsync("HistoryBridgeCStoJS");

    let allHistoryItems = [];
    const historyArea = document.querySelector('.history_aria');
    const emptyState = document.getElementById('empty-state');
    const searchInput = document.getElementById('search-input');

    // Function to render items in the DOM
    function renderHistory(items) {
        historyArea.innerHTML = '';

        if (!items || items.length === 0) {
            emptyState.style.display = 'flex';
            historyArea.style.display = 'none';
            return;
        }

        emptyState.style.display = 'none';
        historyArea.style.display = 'flex';

        items.forEach(row => {
            const historyItem = document.createElement('div');
            historyItem.classList.add('history_item');

            // Format date nicely
            let formattedDate = 'Неизвестно';
            if (row.VisitTime) {
                try {
                    formattedDate = new Date(row.VisitTime).toLocaleString();
                } catch (e) {
                    console.error("Error parsing date:", e);
                }
            }

            // Fallback for empty titles
            const title = row.title ? row.title.trim() : 'Без названия';
            const url = row.url ? row.url : '';

            historyItem.innerHTML = `
                <div class="history_item_left">
                    <div class="favicon-placeholder">
                        <svg class="favicon-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <circle cx="12" cy="12" r="10"></circle>
                            <line x1="2" y1="12" x2="22" y2="12"></line>
                            <path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z"></path>
                        </svg>
                    </div>
                    <div class="history_item_details">
                        <a href="${url}" class="history_item_title" target="_blank" title="${title}">${title}</a>
                        <a href="${url}" class="history_item_url" target="_blank" title="${url}">${url}</a>
                    </div>
                </div>
                <div class="history_item_right">
                    <span class="history_item_date">${formattedDate}</span>
                    <button class="delete-single-btn" data-url="${url}" title="Удалить запись">
                        <svg class="delete-single-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <polyline points="3 6 5 6 21 6"></polyline>
                            <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
                        </svg>
                    </button>
                </div>
            `;

            // Bind single item deletion event
            const deleteBtn = historyItem.querySelector('.delete-single-btn');
            deleteBtn.addEventListener('click', async (e) => {
                e.preventDefault();
                const itemUrl = deleteBtn.getAttribute('data-url');
                
                // Add fade out animation in UI
                historyItem.style.opacity = '0';
                historyItem.style.transform = 'scale(0.95)';
                historyItem.style.transition = 'all 0.2s ease-out';
                
                setTimeout(async () => {
                    await HistoryBridgeCStoJS.deleteItem(itemUrl);
                    await loadHistory();
                }, 200);
            });

            historyArea.appendChild(historyItem);
        });
    }

    // Function to load history from C# database
    async function loadHistory() {
        try {
            const jsonString = await HistoryBridgeCStoJS.getHistory();
            allHistoryItems = JSON.parse(jsonString);
            filterAndRender();
        } catch (error) {
            console.error('Error loading history:', error);
            renderHistory([]);
        }
    }

    // Real-time client side search filtering
    function filterAndRender() {
        const query = searchInput.value.toLowerCase().trim();
        if (!query) {
            renderHistory(allHistoryItems);
            return;
        }

        const filteredItems = allHistoryItems.filter(item => {
            const matchTitle = item.title && item.title.toLowerCase().includes(query);
            const matchUrl = item.url && item.url.toLowerCase().includes(query);
            return matchTitle || matchUrl;
        });

        renderHistory(filteredItems);
    }

    // Event listeners
    searchInput.addEventListener('input', filterAndRender);

    document.getElementById('delete').addEventListener('click', async () => {
        if (confirm("Вы уверены, что хотите полностью очистить историю поиска?")) {
            await HistoryBridgeCStoJS.clearHistoryAsync();
            await loadHistory();
        }
    });

    // Initial load
    await loadHistory();
}

// Initialize history page
initHistory().catch(error => console.error('Error initializing history:', error));