using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using BepInEx.Logging;
using Newtonsoft.Json;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    /// <summary>
    /// Hand-reviewed fallback translations for older serialized text copies that do not use StringTables.
    /// </summary>
    internal static class RussianSerializedTextVariants
    {
        private sealed class VariantFile
        {
            [JsonProperty("variants")] public List<HashedVariant> Variants { get; set; }
        }

        private sealed class HashedVariant
        {
            [JsonProperty("sourceLength")] public int SourceLength { get; set; }
            [JsonProperty("sourceSha256")] public string SourceSha256 { get; set; }
            [JsonProperty("text")] public string Text { get; set; }
            [JsonProperty("replacements")] public List<Replacement> Replacements { get; set; }
        }

        private sealed class Replacement
        {
            [JsonProperty("source")] public string Source { get; set; }
            [JsonProperty("target")] public string Target { get; set; }
        }

        private static readonly Dictionary<string, HashedVariant> HashedExact =
            new Dictionary<string, HashedVariant>(StringComparer.Ordinal);
        private static readonly HashSet<int> HashedSourceLengths = new HashSet<int>();

        private static readonly Dictionary<string, string> Exact = new Dictionary<string, string>
        {
            ["sup everyone >_> I see a lot of kids talking about films on other forums and wanted to scout out posers with a movie quiz. how many answers did you have to look up? >:)"] = "всем привет >_> на других форумах куча ребят рассуждает о кино. решил устроить викторину и вывести позёров на чистую воду. сколько ответов пришлось искать? >:)",
            ["dude.. ur srsly putting Rod & Reel on the same level as the linkerlier trilogy? u might b a posr urself"] = "чувак... ты серьёзно ставишь «На крючке» в один ряд с трилогией «После» Линкэрлиера? сам-то не позёр?",
            ["However, in some shots near the end we start seeing the same ships with different colors. My theory is that the color of the ship will act as a modifier to how many points you get from shooting it down. What are they exactly? I guess I’ll ask my magic ball cause the game isn’t out yet."] = "Но ближе к концу ролика появляются те же корабли, только другого цвета. Думаю, цвет корабля влияет на количество очков за его уничтожение. Что именно означают цвета? Спрошу у своего волшебного шара — игра ещё не вышла.",
            ["Man found hanged in own domicile. Suicide figures on the rise statewide."] = "Мужчину нашли повешенным дома. В штате растёт число самоубийств.",
            ["Baby Rescued In Fire. One Dead, Three Injured. Arson Charges Dropped."] = "Ребёнок спасён при пожаре. Один погибший, трое пострадавших. Обвинения в поджоге сняты.",
            ["From left to right the buttons on the menu are:\n\nRoomies - Allows you to see and manage your friends\nChatlogs - Opens a folder in your computer that the conversations you’ve had with other Roomies.\nInventory - Provides access to the items you are holding.\nFloors - Fast travel to floors you have visited before."] = "Кнопки меню слева направо:\n\nСоседи — просмотр списка друзей и управление ими.\nИстория чатов — папка с вашими беседами с соседями.\nИнвентарь — предметы, которые вы носите с собой.\nЭтажи — быстрый переход на уже посещённые этажи.",
            ["whoa, ive been following you for a while and i gotta say you are already so good for your age. my personal inspiration is mexican painter Frida Kahlo. i really respect strong, artistic women and you seem like one. we should be friends!"] = "Ничего себе, я давно за тобой наблюдаю и должна сказать: для своего возраста ты уже очень талантлива. Меня вдохновляет мексиканская художница Фрида Кало. Я уважаю сильных и творческих женщин, и ты напоминаешь одну из них. Давай дружить!",
            ["I just saw the trailer for Wormhole 2 and I have some theories as to how the new point system will work:\nIn the beginning of the trailer only <color=purple>purple</color> enemies appear and it seems like the amount of points you get is equal to the number of corners the shape of the enemy ship has."] = "Только что посмотрел трейлер Wormhole 2, и у меня появилась теория о новой системе очков:\nВ начале ролика появляются только <color=purple>фиолетовые</color> враги. Похоже, число очков за них равно количеству углов у формы вражеского корабля.",
            ["this is a good one! the most common question for Nientiendo’s Lost in time is the answer behind its mazes. it’s common knowledge that the kojami code works throughout the game to traverse the first set of labirynths. however, to traverse the second set, you’ll have to use a secret route:"] = "отличная игра! чаще всего в «Затерянных во времени» от «Ниентендо» спрашивают, как пройти лабиринты. все знают, что для первой группы лабиринтов работает код Коджами. но чтобы пройти вторую, нужен маршрут с этой карты!",
            ["Storyteller+ Bug Report 24\nOperating System crashes after stacking style tags.\nExample to replicate bug:\n<grow><swing><glow>TEST</glow></swing></grow>"] = "«Рассказчик+»: отчёт об ошибке 24\nОперационная система аварийно завершает работу при наложении стилевых тегов.\nПример для воспроизведения ошибки:\n<grow><swing><glow>TEST</glow></swing></grow>",
            ["CHR-ISSUE92: Chronolog Feature Request\nEnable command line access to restoring .res files.\nProposed format:\u2028CHRONO RESTORE <FILENAME>"] = "CHR-ISSUE92: запрос функции «Хронолога»\nДобавить возможность восстановления повреждённых файлов .res через командную строку.\nПредлагаемая команда:\u2028ВОССТАНОВЛЕНИЕ ХРОНОЛОГА [ПАПКА/ИМЯ_ФАЙЛА]",
            ["In 08/21/2003 NextRooms will go permanently offline. In an attempt to preserve the priceless moments of friendship and memories, The NextRooms team at Spiralsoft has developed an offline mode. The following features will become unavailable in the update:\n• Customizing your Roomie\n• Decorating your Room\n• Visiting other Floors\nYou will be able to preserve your main floor with all its <b>Roomies</b> and their <b>Rooms</b> as they were before the shut down.\nA new feature called <b>Chatlog Replay</b> will be introduced, allowing you to interact with your friends and replay old conversations as cutscenes!\n<b>IMPORTANT!</b>\n<u>You will still need your credentials to enter offline mode!</u> The first time you log in it will take some time to restore your friends, their rooms and your chat logs. To prioritize the recovery of a Roomie re-<u>add them</u> to your list with their Roomie Code."] = "21.08.2003 серверы «Комнат» отключатся навсегда. Чтобы сохранить дорогие сердцу воспоминания о дружбе, команда «Комнат» из компании «Спиралсофт» разработала автономный режим. После обновления станут недоступны следующие возможности:\n• Настройка соседа\n• Оформление комнаты\n• Переход на другие этажи\nВы сможете сохранить свой главный этаж вместе со всеми <b>соседями</b> и их <b>комнатами</b> — такими, какими они были до отключения.\nПоявится новая функция <b>«Повтор бесед»</b>, которая позволит общаться с друзьями и пересматривать старые беседы в виде игровых сцен!\n<b>ВАЖНО!</b>\n<u>Для входа в автономный режим по-прежнему понадобятся данные вашей учётной записи!</u> При первом входе восстановление списка друзей, их комнат и истории чатов займёт некоторое время. Чтобы в первую очередь восстановить данные конкретного соседа, <u>добавьте его</u> в список повторно по коду соседа.",
            ["you are so real for this k4m4r3n4. I can’t stand these jizz stains talking like they know ANYTHING about cinema. most of them weren’t even born yet when Impostor 2 came out... smh"] = "в точку, k4m4r3n4. терпеть не могу этих задротов, которые рассуждают о кино так, будто ХОТЬ ЧТО-ТО в нём понимают. большинство ещё даже не родилось, когда вышел «Самозванец 2»... фейспалм",
            ["X. This is the longest direction that there has ever been written oh my god"] = "Х. Это самое длинное описание направления из всех, что когда-либо писали. Вот это да!",
            ["California - Written and edited 11/06/1996\nComputer software mega corporation Spiralsoft has been under intense public scrutiny after former lead engineer Daniel Agzamov held a press conference leading up to his trial. Spiralsoft sued Agzamov due to his online claims on the company’s core values and its similarities with “cult-like” behaviors among employees.\n\n“There have been multiple instances of complete projects being halted or scrapped as they interfere with what directors called ‘Church of Synchronicity’” said Agzamov on stage. “I no longer feel comfortable disclosing these matters publicly and will wait patiently for the trial.”\n\nAgzamov’s vague descriptions and fear of disclosure after terminating work on Spiralsoft has sparked curiosity on the online community, leading many to believe the “Church of Synchronicity” is a cult of some sort. Agzamov’s trial is scheduled to begin 07/05/1997.\n"] = "Калифорния — подготовлено и отредактировано 06.11.1996\nКрупная корпорация «Спиралсофт» оказалась под пристальным вниманием общественности после пресс-конференции её бывшего ведущего инженера Дэниела Агзамова, состоявшейся перед судом над ним. Компания подала на Агзамова в суд из-за его публикаций в интернете, где он сопоставлял корпоративные ценности компании «Спиралсофт» с поведением её сотрудников, которое считал сектантским.\n\n«Целые проекты не раз приостанавливали или закрывали, если они противоречили тому, что руководство называло „Церковью синхроничности“», — заявил Агзамов. «Я больше не готов обсуждать это публично и буду терпеливо ждать суда».\n\nРасплывчатые объяснения Агзамова и его нежелание раскрывать подробности после ухода из компании «Спиралсофт» пробудили интерес интернет-сообщества. Многие считают, что «Церковь синхроничности» — своего рода секта. Судебное разбирательство по делу «Д. Агзамов против компании „Спиралсофт“» начнётся 05.07.1997.",
            ["Washington - Written and edited 09/21/1997\nResidents of the city of Gold Peak demand the state’s governor take action after another instance of suicide has taken place this year. Emmett Sounder was found hanged in his bedroom after a wellness check was requested by neighbors that same night. No other family members were harmed.\n\nThis constitutes the 47th account of self-harm death in Gold Peak city this year and the 624th state-wide, leading experts to predict an unprecedented 3% increase compared to last year. \n\n“I don’t know if it’s a public health issue or an economic issue or whatever, but something needs to be done as soon as possible. People are desperate.” said Gold Peak citizen Lauren Bose. Sounder’s family did not comment on the matter.\n"] = "Вашингтон — подготовлено и отредактировано 21.09.1997\nЖители Голд-Пика требуют от губернатора штата принять меры после очередного самоубийства в этом году. Той же ночью соседи попросили проверить, всё ли с Эмметтом Саундером в порядке. Позже его нашли повешенным в спальне. Остальные члены семьи не пострадали.\n\nЭто уже 47-й случай самоубийства в Голд-Пике с начала года и 624-й во всём штате. Эксперты прогнозируют беспрецедентный рост числа самоубийств на 3% по сравнению с прошлым годом.\n\n«Не знаю, дело ли в здравоохранении, в экономике или в чём-то ещё, но действовать нужно как можно скорее. Люди в отчаянии», — сказал житель Голд-Пика Лорен Бозе. Семья Саундера отказалась от комментариев.\n",
            ["Each of these works represent steps in the progress of a piece. I named them after my favorite artists. Funny how their initials spell what I feel when creating art!aaaaa"] = "Каждая из этих работ — этап в создании картины. Я назвал их в честь любимых художников. Забавно, что их инициалы выражают то, что я чувствую, когда творю!",
            ["08/21/2006 - NextRooms is now permanently offline!\nLog in to load previous save data in offline mode and re-live your conversations with friends with the new ChatLog Replay feature! "] = "21.08.2006 — серверы «Комнат» отключены навсегда!\nВойдите, чтобы загрузить прежнее сохранение и снова пережить беседы с друзьями благодаря новой функции «Повтор бесед»! ",
            ["guppy’s detective log\n\ntoday was the last day before my uncle left for intensive care.. it was very surreal having him hug me while i was crying because he didn’t even know why i was so sad in the first place. \n\nthis is his old computer.<page>i replaced the mouse, keyboard and speakers but i couldn’t hook it up to any internet connection. it doesn’t really matter. everything i need is here. i just read the letter he left me. i thought i had no more tears left in me but i was wrong.\n\ni feel like this is the start of a detective mission.<page>it’s time to install desktop explorer and see what this is about. the password for the folder.. uncle hal was the only one to call me by that nickname\n\nthis one’s for you captain halibut\n"] = "детективный дневник гуппи\n\nсегодня был последний день перед тем, как дядю перевели в реанимацию… всё казалось каким-то нереальным: он обнимал меня, пока я плакала, и даже не понимал, почему мне так грустно.\n\nэто его старый компьютер.<page>я заменила мышь, клавиатуру и колонки, но так и не смогла подключить его к интернету. неважно, всё нужное уже здесь. я только что прочитала его письмо. думала, что слёз больше не осталось, но ошиблась.\n\nкажется, это начало настоящего расследования.<page>пора установить «Проводник» и разобраться, в чём дело. пароль от папки… дядя хэл был единственным, кто называл меня этим прозвищем.\n\nэта запись — для тебя, капитан Палтус",
            ["Hello i am going to test being the biggest label i can be, ready? yeah! i think this works. and it doesnt need sketchy code calculations!"] = "Привет! Проверю, какого размера может быть эта надпись. Готов? Ага, похоже, всё работает. И никаких сомнительных вычислений в коде не нужно!",
            ["Welcome to Storyteller+\nalksdjhflaksjdhfasd\nfasd;kfjahdslkfjahdslkfjahsdlfkajshdf\n\nasldkfjhasldkfjhasldkfjahsdljkfhasd\nfasldjkhfalsdkjfhasd\nfasdlkfjhasldkfjhasd\nf\n\n\nalskdjhflaksdjhflaksjdhf\naaaaa\nalsdjkhfalksdjfhalskdjf\n\nasdlkfjhalsdkfj"] = "Добро пожаловать в «Рассказчик+»\nalksdjhflaksjdhfasd\nfasd;kfjahdslkfjahdslkfjahsdlfkajshdf\n\nasldkfjhasldkfjhasldkfjahsdljkfhasd\nfasldjkhfalsdkjfhasd\nfasdlkfjhasldkfjhasd\nf\n\n\nalskdjhflaksdjhflaksjdhf\naaaaa\nalsdjkhfalksdjfhalsdkjf\n\nasdlkfjhalsdkfj",
            ["Pitorazzo (17:31 10/17)"] = "Питораццо (17:31 10/17)"
        };

        internal static int LoadedVariantCount => HashedExact.Count;

        internal static void Load(LocalizationBundle payload, ManualLogSource log)
        {
            const string path = "serialized_text_variants.json";
            var file = JsonConvert.DeserializeObject<VariantFile>(payload.Require<UnityEngine.TextAsset>(path).text.TrimStart('\uFEFF'));
            if (file?.Variants == null || file.Variants.Count == 0)
                throw new InvalidDataException("Serialized Russian text variants are empty: " + path);

            HashedExact.Clear();
            HashedSourceLengths.Clear();
            foreach (var variant in file.Variants)
            {
                if (variant == null || variant.SourceLength <= 0 ||
                    string.IsNullOrEmpty(variant.SourceSha256) || variant.SourceSha256.Length != 64 ||
                    (variant.Text == null) == (variant.Replacements == null) ||
                    (variant.Text != null && string.IsNullOrWhiteSpace(variant.Text)) ||
                    (variant.Replacements != null && variant.Replacements.Count == 0))
                    throw new InvalidDataException("Invalid serialized Russian text variant in " + path);

                string key = MakeKey(variant.SourceLength, variant.SourceSha256);
                if (!HashedExact.TryAdd(key, variant))
                    throw new InvalidDataException("Duplicate serialized text hash: " + key);
                HashedSourceLengths.Add(variant.SourceLength);
            }

            log?.LogInfo("Loaded " + HashedExact.Count + " hash-matched serialized Russian text variants.");
        }

        internal static bool TryTranslate(string source, out string translated)
        {
            if (source == null)
            {
                translated = null;
                return false;
            }
            if (Exact.TryGetValue(source, out translated))
                return true;

            translated = null;
            if (string.IsNullOrEmpty(source) || !HashedSourceLengths.Contains(source.Length))
                return false;

            string key = MakeKey(source.Length, ComputeSha256(source));
            if (!HashedExact.TryGetValue(key, out var variant))
                return false;

            if (variant.Text != null)
            {
                translated = variant.Text;
                return true;
            }

            string result = source;
            foreach (var replacement in variant.Replacements)
            {
                if (replacement == null || string.IsNullOrEmpty(replacement.Source) || replacement.Target == null)
                    return false;
                int first = result.IndexOf(replacement.Source, StringComparison.Ordinal);
                if (first < 0 || result.IndexOf(replacement.Source, first + replacement.Source.Length, StringComparison.Ordinal) >= 0)
                    return false;
                result = result.Substring(0, first) + replacement.Target +
                         result.Substring(first + replacement.Source.Length);
            }

            translated = result;
            return true;
        }

        private static string MakeKey(int length, string hash) =>
            length.ToString(CultureInfo.InvariantCulture) + ":" + hash.ToUpperInvariant();

        private static string ComputeSha256(string value)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
                var result = new StringBuilder(hash.Length * 2);
                foreach (byte item in hash)
                    result.Append(item.ToString("X2", CultureInfo.InvariantCulture));
                return result.ToString();
            }
        }
    }
}
