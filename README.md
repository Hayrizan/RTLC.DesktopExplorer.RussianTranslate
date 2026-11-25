# Desktop Explorer — русский перевод

Русская локализация Desktop Explorer от **RTLC TEAM**. Переведены интерфейс, диалоги, записки, сайты во встроенном браузере и текстуры. Для головоломок добавлены русские варианты ответов.

## Установка

1. Скачайте `RTLC.DesktopExplorer.RussianTranslate.zip` из [релизов](https://github.com/Hayrizan/RTLC.DesktopExplorer.RussianTranslate/releases).
2. Закройте игру. В Steam откройте «Управление» → «Просмотреть локальные файлы».
3. Распакуйте содержимое архива рядом с `DesktopExplorer.exe` и запустите игру. Русский доступен в меню выбора языка.

Пакет предназначен для Steam-версии на Windows x64 (Unity Mono). В архив уже входит BepInEx 5.4.23.5 x64. При обновлении удалите прежнюю папку `BepInEx/plugins/RTLC.DesktopExplorer.RussianTranslate` перед распаковкой. Если перевод не загружается, проверьте `BepInEx/LogOutput.log`.

## Сборка из исходников

Понадобятся установленная игра с BepInEx 5.4.23.5, .NET SDK и Unity Editor 2022.3.62f2 с поддержкой Windows. Сценарий собирает бандл и DLL, сохраняет предыдущую установку и устанавливает новую. На время сборки закройте игру.

```powershell
.\build.ps1 -GameDir '<папка Desktop Explorer>' -UnityEditor '<путь к Unity.exe>'
```

В установленной папке плагина два файла: DLL и `rtlc.bundle`. Таблицы, шрифты и текстуры для сборки находятся в `translations/`, `fonts/` и `web-assets/`.

**Hayrizan** — перевод текста и текстур. **DimaLooper** — тестирование.
Лицензии шрифтов находятся в `fonts/` и включены в бандл. [BepInEx](https://github.com/BepInEx/BepInEx) распространяется отдельно от игры и включён в установочный ZIP. Для разработки и проверок использовался OpenAI Codex.
