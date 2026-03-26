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

    document.querySelector('.sidebar').addEventListener('click', (event) => {
        const item = event.target.closest('.menu-item');

        if (item) {
            const category = item.getAttribute('data-category');

            if (category && settingsData[category]) {
                showSelection(category);
            }
        }
    })
}

function setupEventListeners() {
    document.querySelector('.sidebar').addEventListener('click', (event) => {
        const item = event.target.closest('.menu-item');
        if (item) {
            const category = item.getAttribute('data-category');
            if (category && settingsData[category]) {
                showSelection(category);
            }
        }
    });
}

function showSelection(category) {
    const section = settingsData[category];
    if (!section) return;

    const titleElem = document.getElementById('section-title');
    const contentElem = document.getElementById('section-content');

    titleElem.textContent = section.title;

    // Очистка и проверка типа контента
    contentElem.innerHTML = '';
    if (typeof section.content === 'string') {
        contentElem.textContent = section.content;
    } else {
        // Если контент — это объект настройки, рендерим его как input
        contentElem.appendChild(renderSingleSetting(section.content));
    }

    // Update UI active state
    document.querySelectorAll('.menu-item').forEach(item => {
        item.classList.toggle('active', item.getAttribute('data-category') === category);
    });
}

// Вспомогательная функция для генерации HTML одного элемента настройки
function renderSingleSetting(item) {
    const container = document.createElement('div');
    container.className = 'setting-item';

    let inputHtml = '';
    // Поддержка и 'options' и 'choise' для гибкости
    const optionsList = item.options || item.choise;

    if (optionsList) {
        const options = optionsList.map(opt =>
            `<option value="${opt}" ${opt === item.value ? 'selected' : ''}>${opt}</option>`
        ).join('');
        inputHtml = `<select id="${item.id}">${options}</select>`;
    } else if (typeof item.value === 'boolean' || item.id.includes('mode')) {
        inputHtml = `<input type="checkbox" id="${item.id}" ${item.value ? 'checked' : ''}>`;
    } else {
        inputHtml = `<input type="text" id="${item.id}" value="${item.value || ''}">`;
    }

    container.innerHTML = `
        <label for="${item.id}">${item.label}</label>
        ${inputHtml}
    `;

    container.addEventListener('change', async (e) => {
        if (e.target.type === 'change') {
            item.value = e.target.checked;
        } else {
            item.value = e.target.value;
        }

        try {
            const isSaved = await csharpSettingsBridge.saveSettings(JSON.stringify(settingsData));
            if (isSaved) {
                alert('Настройки успешно сохранены! Выбран:' + item.value);
            } else {
                alert("ОШИБКА: C# вернул false. Файл заблокирован или путь неверный. Загляни в 'Вывод' Visual Studio.");
            }
        } catch (error) {
            alert("СИСТЕМНАЯ ОШИБКА JS: " + (error.message || JSON.stringify(error)));
        }
    });
    return container;
}
async function SaveSettings() {
    const jsonToSave = JSON.stringify(settingsData, null, 2);
    await csharpSettingsBridge.saveSettings(jsonToSave);
}

document.addEventListener("DOMContentLoaded", init);