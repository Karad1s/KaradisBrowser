let settingsData = {};

async function init() {
    try {
        const response = await fetch('data.json');
        settingsData = await response.json();
        showSelection('security');
    } catch (err) {
        console.error("Initialization failed:", err);
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
        item.classList.toggle('active', iitem.getAttribute('data-category') === section.title);
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
    return container;
}

async function loadSettings() {
    try {
        const response = await fetch('settings.json');
        const settings = await response.json();
        const list = document.getElementById('settings-list');

        if (!Array.isArray(settings)) return;

        settings.forEach(item => {
            list.appendChild(renderSingleSetting(item));
        });
    } catch (err) {
        console.error("Settings load error:", err);
    }
}

init();
loadSettings();