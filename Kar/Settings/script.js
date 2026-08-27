let settingsData = {};
let installedBrowsers = [];

async function init() {
    await CefSharp.BindObjectAsync("csharpSettingsBridge");
    
    try {

        const settingsJson = await csharpSettingsBridge.getSettings();
        settingsData = JSON.parse(settingsJson);
        console.log("Настройки получены из C#:", settingsData);

        const browserJson = await csharpSettingsBridge.getInstalledBrowsers();
        installedBrowsers = JSON.parse(browserJson);

        setupEventListeners();
        showSelection('security');

    } catch (error) {
        console.error("Критическая ошибка инициализации моста:", error);
    }

    try {
        const importModal = document.getElementById('importModal');
        const startImportBtn = document.getElementById('startImportBtn');
        const cancelImportBtn = document.getElementById('cancelImportBtn'); 

        cancelImportBtn.addEventListener('click', () => {
            importModal.style.display = 'none';
            const select = document.getElementById('browserSelect');
            if (select) select.value = "";
        });

        startImportBtn.addEventListener('click', async () => {
            const browserSelect = document.getElementById('browserSelect');
            if (!browserSelect) return;

            const selectedBrowser = browserSelect.value;
            const options = {
                history: document.getElementById('importHistory').checked,
                bookmarks: document.getElementById('importBookmarks').checked,
                passwords: document.getElementById('importPasswords').checked,
                cookies: document.getElementById('importCookies').checked
            };

            let isRunning = await csharpSettingsBridge.isBrowserRunning(selectedBrowser);

            while(isRunning) {
                alert(`Пожалуйста, закройте ${selectedBrowser} для продолжения импорта.`);
                return;
            }
            
            try {
                const result = await csharpSettingsBridge.importData(selectedBrowser, JSON.stringify(options));
                alert(result);
            } catch(importError) {
                console.error("Ошибка при переносе данных:", importError);
            }

            importModal.style.display = 'none';
            browserSelect.value = "";
        });

    } catch(error) {
        console.error("Ошибка инициализации модального окна:", error);
    }
}

document.addEventListener('change',(e) =>{
    if(e.target.id === 'browserSelect' && e.target.value){
        document.getElementById('importModal').style.display="block"
    }
});

function setupEventListeners() {
    // Sidebar navigation clicks
    document.getElementById('sidebar').addEventListener('click', (event) => {
        const item = event.target.closest('.menu-item');
        if (item) {
            const category = item.getAttribute('data-category');
            if (category && settingsData[category]) {
                showSelection(category);
                // Clear search bar on navigation
                document.getElementById('settings-search').value = '';
            }
        }
    });

    // Settings search bar filtering
    document.getElementById('settings-search').addEventListener('input', (e) => {
        const query = e.target.value.toLowerCase().trim();
        const items = document.querySelectorAll('.setting-item');
        
        items.forEach(item => {
            const labelText = item.querySelector('.setting-label').textContent.toLowerCase();
            if (labelText.includes(query)) {
                item.style.display = 'flex';
            } else {
                item.style.display = 'none';
            }
        });
    });
}

function showSelection(category) {
    const section = settingsData[category];
    if (!section) return;

    const titleElem = document.getElementById('section-title');
    const contentElem = document.getElementById('section-content');

    titleElem.textContent = section.title;
    contentElem.innerHTML = '';

    if (typeof section.content === 'string') {
        const desc = document.createElement('div');
        desc.className = 'section-description';
        desc.textContent = section.content;
        contentElem.appendChild(desc);
    } else if (Array.isArray(section.content)) {
        section.content.forEach(item => {
            contentElem.appendChild(renderSingleSetting(item));
        });
    } else {
        contentElem.appendChild(renderSingleSetting(section.content));
    }

    // Update active visual state in sidebar
    document.querySelectorAll('.menu-item').forEach(item => {
        item.classList.toggle('active', item.getAttribute('data-category') === category);
    });
}

function renderSingleSetting(item) {
    const container = document.createElement('div');
    container.className = 'setting-item';

    const info = document.createElement('div');
    info.className = 'setting-info';

    const label = document.createElement('div');
    label.className = 'setting-label';
    label.textContent = item.label;
    info.appendChild(label);

    container.appendChild(info);

    let controlHtml = '';
    const optionsList = item.options || item.choise;

    if (optionsList) {
        const options = optionsList.map(opt =>
            `<option value="${opt}" ${opt === item.value ? 'selected' : ''}>${opt}</option>`
        ).join('');
        controlHtml = `<select id="${item.id}">${options}</select>`;
    } else if (item.type === 'toggle' || typeof item.value === 'boolean' || item.id.toLowerCase().includes('mode')) {
        controlHtml = `
            <label class="switch">
                <input type="checkbox" id="${item.id}" ${item.value ? 'checked' : ''}>
                <span class="slider"></span>
            </label>
        `;
    }else if(item.type === 'select'){
        const optionsHtml = installedBrowsers.map(b=> `<option value="${b}">${b}</option>`).join('');

        controlHtml = `<select id="browserSelect">
        <option value=""> Выберите браузер...</option>
        ${optionsHtml}
        </select>`;
    }else if (item.value !== undefined) {
        controlHtml = `<input type="text" id="${item.id}" value="${item.value}">`;
    } 

    if (controlHtml !== '') {
        const controlWrapper = document.createElement('div');
        controlWrapper.innerHTML = controlHtml;

        const controlNode = controlWrapper.firstElementChild;
        container.appendChild(controlNode);

        container.addEventListener('change', async (e) => {
            const target = e.target;
            if (target.type === 'checkbox') {
                item.value = target.checked;
            } else {
                item.value = target.value;
            }

            console.log(`Setting changed: ${item.id} -> ${item.value}`);

            try {
                const isSaved = await csharpSettingsBridge.saveSettings(JSON.stringify(settingsData));
                if (isSaved) {
                    console.log('Настройки успешно сохранены в C#!');
                } else {
                    console.error("ОШИБКА: C# вернул false при сохранении настроек.");
                }
            } catch (error) {
                console.error("СИСТЕМНАЯ ОШИБКА JS ПРИ СОХРАНЕНИИ:", error);
            }
        });
    } else {
        container.classList.add('info-only');
    }

    return container;
}

document.addEventListener("DOMContentLoaded", init);