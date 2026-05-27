let settingsData = {};

async function init() {
    try {
        await CefSharp.BindObjectAsync("csharpSettingsBridge");

        const settingsJson = await csharpSettingsBridge.getSettings();
        settingsData = JSON.parse(settingsJson);
        console.log("Настройки получены из C#:", settingsData);

        setupEventListeners();
        showSelection('security');

    } catch (error) {
        console.error("Критическая ошибка инициализации моста:", error);
    }
}

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
    } else {
        controlHtml = `<input type="text" id="${item.id}" value="${item.value || ''}">`;
    }

    const controlWrapper = document.createElement('div');
    controlWrapper.innerHTML = controlHtml;
    
    const controlNode = controlWrapper.firstElementChild;
    container.appendChild(controlNode);

    // Event listener for setting changes
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

    return container;
}

document.addEventListener("DOMContentLoaded", init);