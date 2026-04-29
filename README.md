# VPN Link Extractor

Десктопная утилита для Windows, которая извлекает ссылки `vless://` и XRay-конфиги из подписки в формате Happ.

## Возможности

- Загружает подписки в формате Happ (отправляет нужный `User-Agent`, чтобы получить JSON-набор конфигов).
- Собирает стандартные URI `vless://` из исходящих XRay-конфигов — поддержка TCP, Reality, TLS, WebSocket, gRPC.
- Список серверов с множественным выбором и кнопками `Copy links` / `Save links…` / `Save configs…` (действуют на выделенные строки, либо на весь список, если ничего не выделено).
- Опциональная панель `Show JSON Config` — показывает исходный XRay JSON для выбранной записи; remarks декодируются в читаемый вид.
- Запоминает до 10 последних URL (переключатель `Remember URLs`).
- Сохраняет размер, положение и состояние окна (развёрнуто/нет) между запусками в `settings.json` рядом с исполняемым файлом.
- Нативный тёмный заголовок окна на Windows 10/11.
- Защита от сюрпризов: лимит ответа 10 МБ, разрешены только схемы http/https, атомарная запись настроек, безопасный разбор JSON по каждой записи.

## Требования

- Windows 10 1809+ или Windows 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download) для сборки (или .NET 10 Runtime для framework-dependent сборки)

## Сборка

```bash
dotnet build -c Release
```

Результат: `bin/Release/net10.0-windows/VpnLinkExtractor.exe`.

## Публикация (одиночный `.exe`)

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

Результат: `bin/Release/net10.0-windows/win-x64/publish/VpnLinkExtractor.exe` — один самодостаточный исполняемый файл, .NET runtime на целевой машине не требуется.

Для меньшего по размеру framework-dependent билда используйте `--self-contained false`.

## Использование

1. Вставьте URL подписки Happ (например, `https://example.com/connection/subs/<token>`).
2. Нажмите **Fetch**.
3. Выберите одну или несколько записей (Ctrl/Shift-клик) и используйте **Copy links**, **Save links…** или **Save configs…**.
4. Включите **Show JSON Config**, чтобы увидеть исходный XRay-конфиг выбранной записи.

Если включён **Remember URLs**, успешно загруженные URL сохраняются в выпадающий список для повторного использования.

## Структура проекта

| Файл | Назначение |
|---|---|
| `Program.cs` | Точка входа + отладочные CLI-режимы `--test` / `--dump-config` |
| `MainForm.cs` | Главное окно, весь UI и обработчики действий |
| `AboutForm.cs` | Диалог «О программе» со ссылками |
| `SubscriptionFetcher.cs` | HTTP-запрос, разбор JSON / base64 / plain-text, сборка URI VLESS |
| `VpnEntry.cs` | Запись результата (remarks, URI vless, JSON конфига) |
| `AppSettings.cs` | Настройки, сохраняемые в JSON |
| `WindowGeometry.cs` | Сохраняемое состояние окна |
| `Theme.cs` | Палитра цветов и шрифты |
| `FlatBtn.cs` | Кастомная кнопка в стиле темы |
| `DarkTitleBar.cs` | Interop для тёмного заголовка через DWM |
| `app.ico` | Иконка приложения (мультиразрешение) |

## Лицензия

MIT — см. [LICENSE](LICENSE).

## Ссылки

- Сообщество: [t.me/nastya_chtoto_delaet](https://t.me/nastya_chtoto_delaet)
- Автор: [t.me/anastasia98](https://t.me/anastasia98)
