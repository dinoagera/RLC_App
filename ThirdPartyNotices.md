# Сторонние компоненты

## DejaVu Sans

`Assets/RLCLab/Resources/Fonts/LabSans.ttf` — DejaVu Sans, файл переименован, содержимое не изменено. Используется для кириллицы и математических символов в Unity и браузере.

Полный текст уведомлений и условий: `Assets/RLCLab/Resources/Fonts/LICENSE.txt`.
Источник: системный пакет `fonts-dejavu-core`, https://dejavu-fonts.github.io/.

## Unity TextMeshPro Essential Resources

`Assets/TextMesh Pro` — основные ресурсы из официального пакета uGUI, ветка Unity 6000.0:

https://github.com/Unity-Technologies/uGUI/tree/6000.0/com.unity.ugui

Поставляются шейдеры, настройки, базовый шрифт, стили и sprite-ресурс, необходимые для автономного импорта проекта. Неиспользуемые URP/HDRP Shader Graph-файлы исключены; исходные GUID остальных файлов сохранены.

Лицензирование пакета и его ресурсов определяется условиями Unity и уведомлениями в официальном пакете. Для использования Unity необходима соответствующая лицензия Unity Editor.

Встроенный Liberation Sans: `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt`.
EmojiOne: уведомление в `Assets/TextMesh Pro/Sprites/EmojiOne Attribution.txt`.

## Пакеты и инструменты

- Unity uGUI / TextMeshPro и Unity Test Framework подключаются через `Packages/manifest.json`.
- Python, .NET и Node.js — внешние инструменты запуска/проверки, в проект не включены.
- Playwright нужен только для браузерных тестов, не для приложения.

Проект не использует удалённые CDN, рекламные системы, аналитику или сетевые сервисы расчёта.
