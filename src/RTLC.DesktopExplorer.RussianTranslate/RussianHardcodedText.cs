using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal static class RussianHardcodedText
    {
        private sealed class AppliedValue
        {
            public string Source;
            public string Displayed;
        }

        private static readonly ConditionalWeakTable<TMP_Text, AppliedValue> values = new ConditionalWeakTable<TMP_Text, AppliedValue>();
        private static bool applying;

        private static readonly Dictionary<string, string> Exact = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Anubs"] = "Анубис",
            ["ANUBS"] = "АНУБИС",
            ["Okeeffe.img"] = "Сёра.img",
            ["Okeefee.img"] = "Сёра.img",
            ["Warhol.img"] = "Индиана.img",
            ["Edvard.img"] = "Лихтенштейн.img",
            ["Rembrandt.img"] = "Арчимбольдо.img",
            ["Please Select a Category"] = "Выберите категорию",
            ["Primary Controler:"] = "Основной контроллер:",
            ["Primary Controller:"] = "Основной контроллер:",
            ["Secondary Controler:"] = "Дополнительный контроллер:",
            ["Secondary Controller:"] = "Дополнительный контроллер:",
            ["Primary Worker:"] = "Основной исполнитель:",
            ["Secondary Worker:"] = "Дополнительный исполнитель:",
            ["Основной контроллер:"] = "Основной контроллер:",
            ["Дополнительный контроллер:"] = "Дополнительный контроллер:",
            ["Основной исполнитель:"] = "Основной исполнитель:",
            ["Дополнительный исполнитель:"] = "Дополнительный исполнитель:",
            ["Please add a Title to your report"] = "Укажите заголовок отчёта",
            ["Please add a Summary to your report"] = "Добавьте краткое описание отчёта",
            ["Feedback Sent"] = "Отзыв отправлен",
            ["Bug"] = "Ошибка",
            ["Bug Report"] = "Сообщить об ошибке",
            ["Suggestion"] = "Предложение",
            ["Other"] = "Другое",
            ["Select Category:"] = "Категория:",
            ["Title:"] = "Заголовок:",
            ["Send Feedback"] = "Отправить отзыв",
            ["Desktop Explorer Feedback Form"] = "Обратная связь",
            ["Desktop Explorer Installer"] = "Установщик «Проводника»",
            ["Notepage"] = "Блокнот",
            ["Enter text..."] = "Введите текст...",
            ["Enter command..."] = "Введите команду...",
            ["- Demo Version -"] = "- Демо-версия -",
            ["Next OS - User"] = "НЕКСТ ОС — пользователь",
            ["Enter your password:"] = "Введите пароль:",
            ["Apagar Sistema"] = "Выключить систему",
            ["Attempting to restore previous session"] = "Восстановление предыдущего сеанса",
            ["Serial Key:"] = "Серийный ключ:",
            ["Look in:"] = "Папка:",
            ["Documents\u200b"] = "Документы\u200b",
            ["Mute:"] = "Без звука:",
            ["Floor Hallway"] = "Коридор этажа",
            ["to Interact"] = "для взаимодействия",
            ["+ Nueva Entrada"] = "+ Новая запись",
            ["Send to printer"] = "Отправить на печать",
            ["Mandar a imprimir"] = "Отправить на печать",
            ["Iniciar sesión"] = "Войти",
            ["Cerrar sesión"] = "Выйти",
            ["Regresar a direcciones"] = "Вернуться к адресам",
            ["Tus Respuestas"] = "Ваши ответы",
            ["Sin Respuesta"] = "Без ответа",
            ["Rango: #3"] = "Место: #3",
            ["Revela la verdad:"] = "Раскрой правду:",
            ["[In Development]"] = "[В разработке]",
            [" [In Development]"] = " [в разработке]",
            ["GameVersion\n"] = "Версия игры\n",
            ["Output: you are doomed to repeat the mistakes of the past if you don't learn from them. Hope that helps! Be kind to yourself!"] = "Вывод: не усвоишь уроки прошлого — будешь снова повторять его ошибки. Надеюсь, это поможет! Береги себя!",
            ["It looks like you're trying to summon the devil, is that ok?"] = "Похоже, ты пытаешься призвать дьявола. Всё нормально?",
            ["today was the last day before blah blah"] = "сегодня был последний день перед бла-бла-бла",
            ["Ya llegaron las pipshas.Ya llegaron las pipshas.Ya llegaron las pipshas.Ya llegaron las pipshas.Ya llegaron las pipshas."] = "Пиццы уже приехали! Пиццы уже приехали! Пиццы уже приехали! Пиццы уже приехали! Пиццы уже приехали!",
            ["Please select your language"] = "Выберите язык",
            ["<b>Alert!</b>"] = "<b>Тревога!</b>",
            ["something just happened"] = "Что-то произошло",
            ["Copyright © 1997-1998 Spiralsoft Corp."] = "© 1997–1998 корпорация «Спиралсофт».",
            ["08/21/2006 - NextRooms is now permanently offline!"] = "21.08.2006 — серверы «Комнат» отключены навсегда!",
            ["Log in to load previous save data in offline mode and re-live your conversations with friends with the new ChatLog Replay feature!"] = "Войдите, чтобы загрузить данные прежнего сохранения для автономного режима и заново пережить беседы с друзьями благодаря функции «Повтор бесед»!",
            ["Who made the soundtrack for Closet Pop?"] = "Кто написал саундтрек к фильму «Гуашевые грёзы»?",
            ["What type of monster is Slasher Fest's villain?"] = "Что за монстр стал злодеем фильма «Фестиваль слэшеров»?",
            ["What's the name of the second movie in Linkearlier's After Trilogy?"] = "Как называется второй фильм трилогии «После» режиссёра Линкэрлиера?",
            ["What color is the bait from Rod & Reel's protagonist?"] = "Какого цвета приманка у главного героя фильма «На крючке»?",
            ["No hay preguntas tontas"] = "Глупых вопросов не бывает",
            ["\"No hay preguntas tontas\""] = "«Глупых вопросов не бывает»",
            ["Welcome to Storyteller+"] = "Добро пожаловать в «Рассказчик+»",
            ["Reset input when switching zones"] = "Сбрасывать управление при переходе между зонами",
            ["Storyteller+ Bug Report 24"] = "«Рассказчик+»: отчёт об ошибке 24",
            ["gotcha gotcha, Ill bring my old CDs"] = "Поняла, поняла! Возьму старые диски.",
            ["gugup! im omw to hal’s see ya in a bit!"] = "Гуп, я уже еду к Хэлу, скоро увидимся!",
            ["happy new years! hope youre all doing alright!"] = "С Новым годом! Надеюсь, у вас всё хорошо!",
            ["you better not forget the sweaters!!!"] = "И про свитера не забудь!!!",
            ["Welcome to Desktop Explorer!"] = "Добро пожаловать в «Проводник»!",
            ["Welcome to Desktop Explorer!\nPlease select your language"] = "Добро пожаловать в «Проводник»!\nВыберите язык",
            ["hey Max! its still on! hey Max! its still on! "] = "Эй, Макс! Всё в силе! Эй, Макс! Всё в силе! ",
            ["the plan is to meet at uncle hal’s and drive down to the lake"] = "План такой: встречаемся у дяди Хэла и едем к озеру",
            ["how many months baby can eat solid food"] = "со скольких месяцев ребёнку можно давать твёрдую пищу",
            ["Direccion guardada \"Casa\""] = "Адрес «Дом» сохранён",
            ["Dirección guardada \"Casa Leila\""] = "Адрес «Дом Лейлы» сохранён",
            ["©1996-1997 Chainmailer. All rights reserved."] = "© 1996–1997 Чейнмейлер. Все права защищены.",
            ["©1997 dw33bnation All rights reserved."] = "© 1997 dw33bnation. Все права защищены.",
            ["contact information  & addresses"] = "Контактная информация и адреса",
            ["!Nuevas etiquetas, acceso a beta y más!"] = "Новые теги, доступ к бета-версии и не только!",
            ["YOU WON!!! 03' Sweepstakes WINNER!!!!!"] = "ТЫ ВЫИГРАЛ!!! ПОБЕДИТЕЛЬ РОЗЫГРЫША 03'!!!!!",
            ["版权所有（c）1996，冰箱磁贴公司。保留所有权利\n未经原作者书面许可，不得将任何已发布的艺术、作品用于商业用途。"] = "© 1996, «Магнитики на холодильник». Все права защищены.\nИспользовать опубликованные произведения искусства в коммерческих целях можно только с письменного разрешения их авторов.",
            ["Whoops! Did you type in the <b><u>exact web address</u></b> of the webpage you’re trying to visit? \n\nWithout internet connection, WebSurfer2.0 will only display webpages that have been <b><u>previously visited</u></b> and restored by this user profile.\nClick the History button to see a list of restored websites.\n\nSome hyperlinks will still work if they lead to a restored or restorable website! Working links will appear <b><color=blue>blue</color></b> and broken ones will appear <b><color=red>red</color></b>."] = "Ой! Ты точно ввёл <b><u>правильный веб-адрес</u></b>? \n\nБез подключения к интернету «Браузер» показывает только страницы, которые <b><u>уже посещались</u></b> и были восстановлены для этого профиля.\nНажми кнопку «История», чтобы открыть список восстановленных сайтов.\n\nНекоторые ссылки тоже работают, если ведут на восстановленный сайт или страницу, которую можно восстановить. Рабочие ссылки будут <b><color=blue>синими</color></b>, а нерабочие — <b><color=red>красными</color></b>.",
            ["WebSurfer2"] = "Браузер",
            ["Due to your nextOS session cache capabilities, you can restore previously\nvisited webpages inside WebSurfer2.0, as long as you write exact web address or find a working hyperlink. Working links will appear <b><color=blue>blue</color></b> and broken ones will appear <b><color=red>red</color></b>.\nBelow is a list of the websites you’ve recovered so far:"] = "Благодаря кэшу сеанса «НЕКСТ ОС» в «Браузере» можно восстановить ранее посещённые\nстраницы: введи точный веб-адрес или перейди по рабочей ссылке. Рабочие ссылки будут <b><color=blue>синими</color></b>, нерабочие — <b><color=red>красными</color></b>.\nНиже перечислены восстановленные сайты:",
            ["\"Your current save file has a <color=#F9A31B>different language</color> from the one selected on the settings menu. Your save file will <color=#F9A31B>remain in its original selected language</color>. If you wish to play the game in this new language, you must start a new save file. \nContinue game anyways?\""] = "«В текущем сохранении <color=#F9A31B>выбран другой язык</color>, чем в настройках. Язык сохранения <color=#F9A31B>останется прежним</color>. Чтобы играть на выбранном языке, нужно начать новую игру.\nВсё равно продолжить?»",
            ["This will <color=#F9A31B>DELETE</color> your current save data. Are you sure? Your selected language <color=#F9A31B>CANNOT</color> be changed later in the game."] = "Текущие данные сохранения будут <color=#F9A31B>УДАЛЕНЫ</color>. Вы уверены? Позже изменить выбранный язык в игре будет <color=#F9A31B>НЕВОЗМОЖНО</color>.",
            ["Chat disabled in offline mode"] = "Чат недоступен без интернета",
            ["Saved address “Home”"] = "Адрес «Дом» сохранён",
            ["Missing Cursor"] = "Курсор недоступен",
            ["Waiting for track"] = "Ожидание трека",
            ["Track Paused"] = "На паузе",
            ["Stories were told, then written, then filmed. Now we use StoryTeller+"] = "Сначала истории рассказывали, потом записывали, а затем снимали. Теперь для этого есть «Рассказчик+».",
            ["\"Stories were told, then written, then filmed. Now we use StoryTeller+\""] = "«Сначала истории рассказывали, потом записывали, а затем снимали. Теперь для этого есть „Рассказчик+“.»",
            ["Stories were told, then written, then filmed.\nNow we use StoryTeller+"] = "Сначала истории рассказывали, потом записывали, а затем снимали.\nТеперь для этого есть «Рассказчик+».",
            ["Attempting to set nonexistent key in selected repo"] = "Попытка задать несуществующий ключ в выбранном хранилище",
            ["Misconfigured persistence key! Go to SaveDataDebugMenuController.cs"] = "Неверно настроен ключ сохранения. Проверьте его настройки.",
            ["Value Set!"] = "Значение сохранено!",
            ["Value type mismatch! Check the value type dropdown."] = "Тип значения не совпадает. Проверьте выбранный тип.",
            ["Thanks for Playing"] = "Спасибо за игру!",
            ["Attempting system restore in"] = "Восстановление системы через",
            ["YES, CONTINUE"] = "ДА, ПРОДОЛЖИТЬ",
            ["Yes, please"] = "Да, пожалуйста",
            ["Got it"] = "Понятно",
            ["UNDER DEVELOPMENT"] = "В РАЗРАБОТКЕ",
            ["Settings [In Development]"] = "Настройки [в разработке]",
            ["Boat Cursor"] = "Курсор-лодка",
            ["Cursor"] = "Курсор",
            ["Zoom"] = "Масштаб",
            ["CountDown Timer"] = "Таймер обратного отсчёта",
            ["INSTRUCTIONS"] = "ПРАВИЛА",
            ["Slow"] = "Медленно",
            ["Fast\n"] = "Быстро\n",
            ["GO LEFT"] = "ДВИГАЙТЕСЬ ВЛЕВО",
            ["LMB = ToSpeedUp"] = "ЛКМ = ускориться",
            ["HIGH SCORES"] = "РЕКОРДЫ",
            ["HIGHSCORES"] = "РЕКОРДЫ",
            ["CREDITS: 0"] = "КРЕДИТЫ: 0",
            ["Credit 1"] = "Кредит 1",
            ["Six months later"] = "Шесть месяцев спустя",
            ["Go Desktop"] = "На рабочий стол",
            ["Developer Console"] = "Консоль разработчика",
            ["Persistence Repository"] = "Хранилище сохранений",
            ["Persistence  Key"] = "Ключ сохранения",
            ["Value Type"] = "Тип значения",
            ["Int or String Value"] = "Значение (число или строка)",
            ["Custom Text:"] = "Пользовательский текст:",
            ["Debug Floor Menu:"] = "Меню отладки этажей:",
            ["Deebog Menu "] = "Меню отладки",
            ["DISCLAIMER: THIS MIGHT DESTABILIZE THE CURRENT SESSION - PROCEED WITH CAUTION "] = "ВНИМАНИЕ: ЭТО МОЖЕТ НАРУШИТЬ РАБОТУ ТЕКУЩЕГО СЕАНСА. ДЕЙСТВУЙТЕ ОСТОРОЖНО.",
            ["Close All Tabs"] = "Закрыть все вкладки",
            ["Disable Pizarro Reply Bubble"] = "Отключить облачко реплик Писарро",
            ["NextRoom Cutscenes"] = "Сюжетные сцены «Комнат»",
            ["NextRooms SignIn"] = "Вход в «Комнаты»",
            ["Pizarro Animation:"] = "Анимация Писарро:",
            ["Pizarro System Tips Dialogues"] = "Системные подсказки Писарро",
            ["Pizarro TalkToMe Dialogues"] = "Реплики Писарро",
            ["Refresh debug menu"] = "Обновить меню отладки",
            ["Reset Session\n"] = "Сбросить сеанс\n",
            ["restart save data"] = "Сбросить сохранение",
            ["refresh debug menu"] = "Обновить меню отладки",
            ["Start Admin Interlude Sequence"] = "Запустить интерлюдию администратора",
            ["Start Animation"] = "Запустить анимацию",
            ["Stop Animation"] = "Остановить анимацию",
            ["Start Post Crash Sequence"] = "Запустить сценарий после сбоя",
            ["Start Safe Mode Sequence"] = "Запустить безопасный режим",
            ["Trigger Cutscenes"] = "Запустить сцены",
            ["Uncap FPS"] = "Снять ограничение FPS",
            ["Unlock All Chronologs For current User\n"] = "Открыть все записи «Хронолога» для текущего пользователя\n",
            ["Unlock all Guppy with ans_1"] = "Открыть все записи Гуппи (ответ 1)",
            ["Unlock all Guppy with ans_2"] = "Открыть все записи Гуппи (ответ 2)",
            ["Unlock all Guppy with ans_3"] = "Открыть все записи Гуппи (ответ 3)",
            ["Unlock all original-user Chrono entries"] = "Открыть все исходные записи «Хронолога»",
            ["Unlock all Polaroids"] = "Открыть все снимки Polaroid",
            ["Unlock All Roomie Conversations"] = "Открыть все беседы с соседями",
            ["Unlock All Roomies"] = "Открыть всех соседей",
            ["Movement direction:"] = "Направление движения:",
            ["Movement Type:"] = "Тип перемещения:",
            ["Movement Speed"] = "Скорость перемещения",
            ["Standard Rotation Speed"] = "Обычная скорость поворота",
            ["Cursor Speed (WIP)"] = "Скорость курсора (в разработке)",
            ["Cursor Ghosting"] = "Шлейф курсора",
            ["Screen resolution"] = "Разрешение экрана",
            ["English"] = "Английский",
            ["Spanish"] = "Испанский",
            ["French"] = "Французский",
            ["German"] = "Немецкий",
            ["Portuguese"] = "Португальский",
            ["Japanese"] = "Японский",
            ["Russian"] = "Русский",
            ["Simplified Chinese"] = "Упрощённый китайский",
            ["Traditional Chinese"] = "Традиционный китайский",
            ["español (México)"] = "Испанский (Мексика)",
            ["mall movement debug UI"] = "Отладка передвижения по торговому центру",
            ["Cutscenes"] = "Сюжетные сцены",
            ["Localization"] = "Локализация",
            ["Puzzles"] = "Загадки",
            ["Puzzle\n"] = "Загадка\n",
            ["Desktops"] = "Рабочие столы",
            ["SaveData\n"] = "Сохранения\n",
            ["Stats"] = "Статистика",
            ["Pizarro TalkTo Me Dialogues"] = "Реплики Писарро",
            ["--- FPS CONTROLS ---"] = "--- УПРАВЛЕНИЕ FPS ---",
            ["--- FPS ---"] = "--- FPS ---",
            ["--- RENDERING ---"] = "--- ОТРИСОВКА ---",
            ["--- SYSTEM ---"] = "--- СИСТЕМА ---",
            ["--- MEMORY ---"] = "--- ПАМЯТЬ ---",
            ["Safe Mode"] = "Безопасный режим",
            ["GetStuckInMaze:"] = "Застрять в лабиринте:",
            ["Bool Value"] = "Логическое значение",
            ["ClearedPuzzles"] = "Решённые загадки",
            ["StartedPuzzles"] = "Начатые загадки",
            ["Play"] = "Играть",
            ["Set\n"] = "Задать\n",
            ["10000000 Item(s)"] = "10 000 000 элементов",
            ["Miscellaneous"] = "Прочее",
            ["Value"] = "Значение",
            ["Override GuppyNextRooms Mode:"] = "Принудительно задать режим «Комнат» для Гуппи:",
            ["Override Pizarro Dialogue:"] = "Принудительно заменить реплику Писарро:",
            ["Override Loading Screen:"] = "Принудительно заменить экран загрузки:",
            ["con todo configurado\u200b"] = "Всё уже настроено\u200b",
            ["For the brightest..."] = "Для самых ярких...",
            ["Hola Amigos"] = "Привет, друзья!",
            ["Los mismos de siempre"] = "Всё те же",
            ["Cuantos tacos te comiste?"] = "Сколько тако ты съел?",
            ["Lo que aprendi en la escuela de botes es"] = "Вот чему меня научили в лодочной школе:",
            ["Section"] = "Раздел",
            ["Name LastName"] = "Имя Фамилия",
            ["Header"] = "Заголовок",
            ["Title"] = "Название",
            ["Example file"] = "Пример файла",
            ["Example size"] = "Пример размера",
            ["Example date"] = "Пример даты",
            ["Example author"] = "Пример автора",
            ["so what if my location is gigantic??"] = "Ну и что, что путь такой длинный?",
            ["Warning"] = "Предупреждение",
            ["<b>Alert!</b> \nsomething just happened"] = "<b>Тревога!</b>\nЧто-то произошло",
            ["this is an input dialog question"] = "Вопрос для диалога ввода",
            ["This is a test text"] = "Это тестовая строка",
            ["This is a very long validation text that only appears if the submitted input is invalid!"] = "Очень длинный текст проверки; он появляется только при неверном вводе!",
            ["Report\"\n"] = "Отчёт\"\n",
            ["Send Feedback\n"] = "Отправить отзыв\n",
            ["MixerTitle\n"] = "Микшер\n",
            ["Standard"] = "Стандартный",
            [" to Interact\n"] = " для взаимодействия\n",
            ["Floor 665 - Hallway "] = "Этаж 665 — коридор ",
            ["Hide Debug UI "] = "Скрыть отладочный интерфейс ",
            ["Restoring the_max\"s room"] = "Восстанавливаем комнату the_max",
            ["Tank Rotation Speed"] = "Скорость вращения танка",
            ["Press    \n"] = "Нажмите    \n",
            ["Tank\n"] = "Танк\n",
            ["Reset input when switching zones\n"] = "Сбрасывать управление при переходе между зонами\n",
            ["Snapped to Cardinal Direction"] = "Привязка к сторонам света",
            ["Input change when switching zones:"] = "Смена управления при переходе между зонами:",
            ["Directly relative to Camera"] = "Относительно камеры",
            ["On zone switch"] = "При переходе между зонами",
            ["On new input added"] = "При добавлении нового элемента управления",
            ["meow"] = "мяу",
            ["miau"] = "мяу",
            ["Type <b>devconsole</b> for instructions on how to use the developer console."] = "Чтобы узнать, как пользоваться консолью разработчика, введите <b>devconsole</b>."
        };

        private static readonly Dictionary<string, string> Prefixes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Initializing "] = "Инициализация: ",
            ["Primary Controler: "] = "Основной контроллер: ",
            ["Primary Controller: "] = "Основной контроллер: ",
            ["Secondary Controler: "] = "Дополнительный контроллер: ",
            ["Secondary Controller: "] = "Дополнительный контроллер: ",
            ["Primary Worker: "] = "Основной исполнитель: ",
            ["Secondary Worker: "] = "Дополнительный исполнитель: ",
            ["Основной контроллер: "] = "Основной контроллер: ",
            ["Дополнительный контроллер: "] = "Дополнительный контроллер: ",
            ["Основной исполнитель: "] = "Основной исполнитель: ",
            ["Дополнительный исполнитель: "] = "Дополнительный исполнитель: ",
            ["Updated: "] = "Обновлено: ",
            ["Min FPS: "] = "Минимум FPS: ",
            ["Max FPS: "] = "Максимум FPS: ",
            ["Target FPS: "] = "Целевой FPS: ",
            ["Current FPS: "] = "Текущий FPS: ",
            ["Average FPS: "] = "Средний FPS: ",
            ["VSync: "] = "Вертикальная синхронизация: ",
            ["Total Reserved: "] = "Зарезервировано: ",
            ["Total Allocated: "] = "Выделено: ",
            ["GC Collections: "] = "Сборки мусора: ",
            ["GPU: "] = "Графический процессор: ",
            ["Frame Time: "] = "Время кадра: ",
            ["Mono Heap: "] = "Куча Mono: ",
            ["Draw Calls: "] = "Вызовы отрисовки: ",
            ["Mono Used: "] = "Использовано Mono: ",
            ["System Memory: "] = "Системная память: ",
            ["GPU Memory: "] = "Видеопамять: ",
            ["Processor: "] = "Процессор: ",
            ["Quality: "] = "Качество: ",
            ["Resolution: "] = "Разрешение: "
        };

        private static readonly (string Source, string Replacement)[] BootLabelPrefixes =
        {
            ("Primary Controller", "Основной контроллер"),
            ("Primary Controler", "Основной контроллер"),
            ("Secondary Controller", "Дополнительный контроллер"),
            ("Secondary Controler", "Дополнительный контроллер"),
            ("Primary Worker", "Основной исполнитель"),
            ("Secondary Worker", "Дополнительный исполнитель"),
            ("Основной контроллер", "Основной контроллер"),
            ("Дополнительный контроллер", "Дополнительный контроллер"),
            ("Основной исполнитель", "Основной исполнитель"),
            ("Дополнительный исполнитель", "Дополнительный исполнитель")
        };

        internal static bool IsRussian => LocalizationSettings.SelectedLocale?.Identifier.Code == Plugin.RussianCode;

        internal static string Translate(string value)
        {
            if (!IsRussian || string.IsNullOrEmpty(value))
                return value;

            // Reopened Storyteller pages no longer have the one-shot replacement event.
            // Display an ellipsis instead of exposing the unresolved authoring token.
            value = value.Replace("{incorrect_word}", "…");
            value = value.Replace("M4XL0V3L4C3", "М4КСЛ0В3Л4ЙС");
            value = System.Text.RegularExpressions.Regex.Replace(value, @"USR:\s*([A-Za-z0-9_-]+)", match =>
            {
                string user = match.Groups[1].Value;
                switch (user.ToUpperInvariant())
                {
                    case "HALIBUT": user = "Палтус"; break;
                    case "GUPPY": user = "Гуппи"; break;
                    case "MAX": case "MAXINE": user = "Максин"; break;
                    case "ADMIN": user = "Администратор"; break;
                }
                return "ПОЛЬЗОВАТЕЛЬ:" + user;
            });
            string plainBoot = System.Text.RegularExpressions.Regex.Replace(value, "<[^>]+>", "");
            if (System.Text.RegularExpressions.Regex.IsMatch(plainBoot,
                @"Press\s+ANY\s+KEY\s+to\s+boot\s+into\s+SAFE\s+MODE", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                return "Нажмите любую клавишу для загрузки в безопасном режиме";

            // Presentation only: canonical file-size identifiers remain untouched.
            if (System.Text.RegularExpressions.Regex.IsMatch(value, @"^\d+(?:[.,]\d+)?\s*[Kk][Bb]$"))
                return value.Substring(0, value.Length - 2) + "кб";
            if (value == "KB" || value == "kb") return "кб";

            if (Exact.TryGetValue(value, out string translated))
                return translated;
            if (TryTranslateBootLabel(value, out translated))
                return translated;
            if (RussianSerializedTextVariants.TryTranslate(value, out translated))
                return translated;

            if (value.StartsWith("Game Version: ", StringComparison.Ordinal))
                return "Версия игры: " + value.Substring("Game Version: ".Length);
            if (value.StartsWith("Memory Testing : ", StringComparison.Ordinal))
            {
                string reading = value.Substring("Memory Testing : ".Length);
                if (reading.EndsWith(" OK", StringComparison.Ordinal))
                    reading = reading.Substring(0, reading.Length - " OK".Length) + " ОК";
                return "Тест памяти: " + reading;
            }
            if (value.StartsWith("fallback text, please update me! - ", StringComparison.Ordinal))
                return "Текст не найден, сообщите разработчику! — " + value.Substring("fallback text, please update me! - ".Length);
            if (value.StartsWith("Music player state: ", StringComparison.Ordinal))
                return "Состояние музыкального проигрывателя: " + value.Substring("Music player state: ".Length);
            if (value.StartsWith("Music Player State: ", StringComparison.Ordinal))
                return "Состояние музыкального проигрывателя: " + value.Substring("Music Player State: ".Length);
            if (value.StartsWith("Playing track GUID: ", StringComparison.Ordinal))
                return "Идентификатор текущего трека: " + value.Substring("Playing track GUID: ".Length);
            if (value.StartsWith("Playing Track GUID: ", StringComparison.Ordinal))
                return "Идентификатор текущего трека: " + value.Substring("Playing Track GUID: ".Length);
            if (value.StartsWith("Now playing - ", StringComparison.Ordinal))
                return "Играет: " + value.Substring("Now playing - ".Length);
            foreach (var prefix in Prefixes)
                if (value.StartsWith(prefix.Key, StringComparison.Ordinal))
                    return prefix.Value + value.Substring(prefix.Key.Length);

            return value;
        }

        private static bool TryTranslateBootLabel(string value, out string translated)
        {
            foreach (var item in BootLabelPrefixes)
            {
                if (!value.StartsWith(item.Source, StringComparison.Ordinal))
                    continue;

                int cursor = item.Source.Length;
                if (cursor < value.Length && !char.IsWhiteSpace(value[cursor]) && value[cursor] != ':')
                    continue;

                while (cursor < value.Length && char.IsWhiteSpace(value[cursor]))
                    cursor++;
                bool hasColon = cursor < value.Length && value[cursor] == ':';
                if (hasColon)
                {
                    cursor++;
                }
                while (cursor < value.Length && char.IsWhiteSpace(value[cursor]))
                    cursor++;

                string separator = cursor < value.Length ? " " : "";
                translated = item.Replacement + (hasColon ? ":" : "") + separator + value.Substring(cursor);
                return true;
            }

            translated = value;
            return false;
        }

        private static string TranslateFor(TMP_Text owner, string value)
        {
            if (!IsRussian || IsEditableInputValue(owner)) return value;
            // These three-character initials are display labels, not saved player identifiers.
            if (value == "COL") return "КОЛ";
            if (value == "MAX") return "МАК";
            if (value == "История чатов")
            {
                var navigation = owner.GetComponentInParent<Program.Mall.UI.NextRoomButtonController>(true);
                if (navigation)
                {
                    var field = AccessTools.Field(typeof(Program.Mall.UI.NextRoomButtonController), "nextRoomsFullButton");
                    var full = field.GetValue(navigation) as GameObject;
                    if (full && full.transform is RectTransform rect)
                        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                            Mathf.Max(rect.rect.width, owner.GetPreferredValues(value).x + 112f));
                    var labelRect = owner.rectTransform;
                    labelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                        Mathf.Max(labelRect.rect.width, owner.GetPreferredValues(value).x + 16f));
                }
            }
            var properties = owner.GetComponentInParent<Program.PropertiesController>(true);
            if (properties != null &&
                ReferenceEquals(AccessTools.Field(typeof(Program.PropertiesController), "fileAuthorText").GetValue(properties), owner))
            {
                switch (value)
                {
                    case "System": case "Sysyem": return "Система";
                    case "DesktopExplorer": case "DesctopExplorer": case "Desktop Explorer": return "Проводник";
                    case "Halibut": return "Палтус";
                    case "Maximus": case "M4ximus": case "M4X1MUS": return "М4кс1мус";
                    case "Admin": return "Админ";
                    case "Guppy": return "Гуппи";
                }
            }
            value = RussianSettingValueLabels.Translate(owner, value);
            return Translate(RussianCursorText.Translate(owner,
                RussianProfileNames.Translate(owner, RussianCalendarLabels.Translate(owner, value))));
        }

        private static bool ContainsCyrillic(string value) =>
            !string.IsNullOrEmpty(value) && value.Any(character => character >= '\u0400' && character <= '\u04FF');

        internal static bool IsEditableInputValue(TMP_Text owner)
        {
            var input = owner != null ? owner.GetComponentInParent<TMP_InputField>() : null;
            return input != null && input.textComponent == owner;
        }

        internal static void BeforeAssignment(TMP_Text owner, ref string value)
        {
            if (applying || owner == null || IsEditableInputValue(owner)) return;
            if (values.TryGetValue(owner, out var previous) && value == previous.Displayed)
                return;
            values.Remove(owner);
            string translated = TranslateFor(owner, value);
            if (string.Equals(translated, value, StringComparison.Ordinal))
            {
                // Some serialized/in-game labels are already authored in Russian.
                // Their text can be assigned after OnEnable, so they still need a
                // Cyrillic atlas even though the localization map did not change it.
                if (ContainsCyrillic(value))
                    RussianStaticLabelFonts.Apply(owner, value, requiresCyrillic: true);
                return;
            }
            values.Add(owner, new AppliedValue { Source = value, Displayed = translated });
            RussianStaticLabelFonts.Apply(owner, value, requiresCyrillic: ContainsCyrillic(translated));
            value = translated;
        }

        internal static void Apply(TMP_Text owner)
        {
            if (owner == null || IsEditableInputValue(owner)) return;
            string source = owner.text;
            if (values.TryGetValue(owner, out var previous) && owner.text == previous.Displayed)
                source = previous.Source;
            values.Remove(owner);
            string translated = TranslateFor(owner, source);
            if (source != translated || owner.text != translated)
                values.Add(owner, new AppliedValue { Source = source, Displayed = translated });
            RussianStaticLabelFonts.Apply(owner, source, requiresCyrillic: ContainsCyrillic(translated));
            if (owner.text == translated) return;
            applying = true;
            try { owner.text = translated; }
            finally { applying = false; }
        }

        internal static void ApplyCurrentLocale()
        {
            RussianArtistWebsitePatch.ApplyCurrentLocale();
            foreach (var text in Resources.FindObjectsOfTypeAll<TMP_Text>())
                if (text && text.gameObject.scene.IsValid()) Apply(text);
        }
    }

    [HarmonyPatch(typeof(TMP_Text), "set_text")]
    internal static class RussianHardcodedTextSetterPatch
    {
        private static void Prefix(TMP_Text __instance, ref string value) => RussianHardcodedText.BeforeAssignment(__instance, ref value);
    }

    [HarmonyPatch]
    internal static class RussianHardcodedTextSetTextPatch
    {
        private static System.Reflection.MethodBase TargetMethod() => AccessTools.Method(typeof(TMP_Text), "SetText", new[] { typeof(string), typeof(bool) });

        private static void Prefix(TMP_Text __instance, ref string sourceText) => RussianHardcodedText.BeforeAssignment(__instance, ref sourceText);
    }

    [HarmonyPatch(typeof(TextMeshProUGUI), "OnEnable")]
    internal static class RussianSerializedUiTextPatch
    {
        private static void Postfix(TextMeshProUGUI __instance) => RussianHardcodedText.Apply(__instance);
    }

    [HarmonyPatch(typeof(TextMeshPro), "OnEnable")]
    internal static class RussianSerializedWorldTextPatch
    {
        private static void Postfix(TextMeshPro __instance) => RussianHardcodedText.Apply(__instance);
    }
}
