using System;
using System.Windows.Input;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.IO;
using System.Collections.Generic;

namespace Kar.Shortcut
{
    /// <summary>
    /// Объект передачи данных (DTO), представляющий пользовательскую горячую клавишу.
    /// Используется для маппинга данных при десериализации из конфигурационного файла YAML.
    /// </summary>
    public class ShortcutDto
    {
        /// <summary>
        /// Название действия или команды, которая должна быть выполнена (например, "NewTab", "CloseWindow").
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Строковое представление комбинации клавиш (жеста) для активации действия (например, "Ctrl+T").
        /// </summary>
        public string Gesture { get; set; } = string.Empty;
    }

    /// <summary>
    /// Утилита для загрузки и парсинга пользовательских сочетаний клавиш из внешнего конфигурационного файла.
    /// </summary>
    public class ShortcutLoader
    {
        /// <summary>
        /// Считывает конфигурационный файл shortcut.yaml и десериализует его содержимое 
        /// в список объектов <see cref="ShortcutDto"/>.
        /// </summary>
        /// <returns>
        /// Возвращает список загруженных горячих клавиш. 
        /// Если файл конфигурации отсутствует или пуст, возвращается пустой список.
        /// </returns>
        public static List<ShortcutDto> LoadShortcut()
        {
            // Формирование абсолютного пути к файлу конфигурации относительно директории запуска приложения
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Shortcut", "shortcut.yaml");

            // Защита от ошибок: если файл не найден, возвращаем пустую коллекцию вместо выбрасывания исключения
            if (!File.Exists(path)) return new List<ShortcutDto>();

            // Настройка парсера YAML с поддержкой CamelCase (например, 'action' в yaml маппится на 'Action' в C#)
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            // Чтение текста из файла и попытка десериализации. 
            // Оператор ?? гарантирует, что метод не вернет null, если файл оказался пустым.
            return deserializer.Deserialize<List<ShortcutDto>>(File.ReadAllText(path)) ?? new List<ShortcutDto>();
        }
    }
}