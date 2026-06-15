const input = document.getElementById('SearchInput');

// Обработка поиска
input.addEventListener('keypress', function (e) {
    if (e.key === "Enter") {
        const query = input.value;
        if (query.trim() !== "") {
            window.location.href = "https://www.google.com/search?q=" + encodeURIComponent(query);
        }
    }
});

// Загрузка популярных сайтов
async function loadPopularSites() {
    try {
        await CefSharp.BindObjectAsync("HistoryBridgeCStoJS");

        const jsonString = await HistoryBridgeCStoJS.getPopularSites(12);
        const sites = JSON.parse(jsonString);

        const grid = document.getElementById('PopularSitesGrid');
        grid.innerHTML = '';

        sites.forEach(site => {
            const div = document.createElement('div');
            div.className = 'popularWeb';

            let domain = "";
            try {
                domain = new URL(site.url).hostname;
            } catch (e) { }

            const iconUrl = domain ? `https://www.google.com/s2/favicons?domain=${domain}&sz=64` : '';
            const displayTitle = site.title && site.title !== 'Без названия' ? site.title : domain;

            div.innerHTML = `
                <a href="${site.url}" style="display:flex; flex-direction:column; align-items:center; justify-content:center; width:100%; height:100%; text-decoration:none; color:inherit; padding:10px; box-sizing:border-box;">
                    <img src="${iconUrl}" style="width:32px; height:32px; border-radius:50%; margin-bottom:8px;" onerror="this.style.display='none'">
                    <span style="font-size:12px; text-align:center; display:-webkit-box; -webkit-line-clamp:2; -webkit-box-orient:vertical; overflow:hidden;">${displayTitle}</span>
                </a>
            `;
            grid.appendChild(div);
        });

        const emptySlots = 12 - sites.length;
        for (let i = 0; i < emptySlots; i++) {
            const emptyDiv = document.createElement('div');
            emptyDiv.className = 'popularWeb';
            emptyDiv.style.opacity = '0.5';
            grid.appendChild(emptyDiv);
        }

    } catch (error) {
        console.error("Ошибка загрузки популярных сайтов:", error);
    }
}

loadPopularSites();