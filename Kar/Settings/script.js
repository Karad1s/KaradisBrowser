let settingsData = {};

// 1. Инициализация данных
async function init() {
    // Пытаемся взять данные из localStorage
    const localData = localStorage.getItem('settingsData');
    
    if (localData) {
        settingsData = JSON.parse(localData);
    } else {
        // Если в localStorage пусто, загружаем из файла (первый запуск)
        const response = await fetch('data.json');
        settingsData = await response.json();
        localStorage.setItem('settingsData', JSON.stringify(settingsData));
    }
    
    showSelection('security');
}

// 2. Отображение категории
function showSelection(category) {
    const section = settingsData[category];
    if (!section) return;

    document.getElementById('section-title').textContent = section.title;
    document.getElementById('section-content').textContent = section.content;

    document.querySelectorAll('.menu-item').forEach(item => {
        item.classList.remove('active');
        if(item.innerText === section.title) item.classList.add('active');
    });
}

// 3. Загрузка и рендеринг списка настроек
async function loadSettings() {
    try {
        let settings;
        const localSettings = localStorage.getItem('userSettingsList');

        if (localSettings) {
            settings = JSON.parse(localSettings);
        } else {
            const response = await fetch('settings.json');
            settings = await response.json();
            localStorage.setItem('userSettingsList', JSON.stringify(settings));
        }

        const list = document.getElementById('settings-list');
        list.innerHTML = ''; // Очищаем список перед рендером

        settings.forEach(item => {
            const row = document.createElement('div');
            row.className = 'setting-item';
            
            let inputHtml = '';

            switch(item.type) {
                case 'toggle':
                    inputHtml = `<input type="checkbox" id="${item.id}" ${item.value ? 'checked' : ''} onchange="updateSetting('${item.id}', this.checked)">`;
                    break;
                case 'text':
                    inputHtml = `<input type="text" id="${item.id}" value="${item.value}" oninput="updateSetting('${item.id}', this.value)">`;
                    break;
                case 'select':
                    const options = item.options.map(opt => 
                        `<option value="${opt}" ${opt === item.value ? 'selected' : ''}>${opt}</option>`
                    ).join('');
                    inputHtml = `<select id="${item.id}" onchange="updateSetting('${item.id}', this.value)">${options}</select>`;
                    break;
            }

            row.innerHTML = `
                <label for="${item.id}">${item.label}</label>
                ${inputHtml}
            `;
            list.appendChild(row);
        });
    } catch (err) {
        console.error("Не удалось загрузить настройки", err);
    }
}

// 4. Функция сохранения изменений в localStorage
function updateSetting(id, newValue) {
    const localSettings = JSON.parse(localStorage.getItem('userSettingsList'));
    const updatedSettings = localSettings.map(item => {
        if (item.id === id) {
            return { ...item, value: newValue };
        }
        return item;
    });
    
    localStorage.setItem('userSettingsList', JSON.stringify(updatedSettings));
    console.log(`Настройка ${id} обновлена:`, newValue);
}

// Запуск
init();
loadSettings();