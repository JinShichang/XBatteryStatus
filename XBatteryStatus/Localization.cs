using System;
using System.Collections.Generic;
using System.Globalization;

namespace XBatteryStatus
{
    public enum AppLanguage
    {
        Auto = 0,
        English = 1,
        Chinese = 2,
        Russian = 3,
        Spanish = 4,
        Portuguese = 5,
        German = 6,
        Japanese = 7,
        French = 8,
        Polish = 9,
        Korean = 10,
        Arabic = 11
    }

    /// <summary>
    /// Simple localization: 10 languages + auto detection.
    /// Auto detection: Simplified Chinese system locale -> Chinese, everything else -> English.
    /// </summary>
    public static class Localization
    {
        private static readonly Dictionary<AppLanguage, Dictionary<string, string>> tables = new();
        private static readonly Dictionary<AppLanguage, string> selfNames = new();

        public static AppLanguage Current { get; private set; } = AppLanguage.English;

        public static void Initialize()
        {
            var setting = (AppLanguage)AppConfig.Language;
            Current = setting == AppLanguage.Auto ? Detect() : setting;
        }

        public static AppLanguage Detect()
        {
            string name = CultureInfo.CurrentUICulture.Name;
            if (name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            {
                return AppLanguage.Chinese;
            }
            return AppLanguage.English;
        }

        public static string Tr(string key, params object[] args)
        {
            string value;
            if (tables.TryGetValue(Current, out var table))
            {
                table.TryGetValue(key, out value);
            }
            else
            {
                value = null;
            }

            if (value == null)
            {
                tables.TryGetValue(AppLanguage.English, out var en);
                en?.TryGetValue(key, out value);
            }

            if (value == null) value = key;

            return args != null && args.Length > 0 ? string.Format(value, args) : value;
        }

        public static string LanguageName(AppLanguage language)
        {
            if (language == AppLanguage.Auto) return Tr("Auto");
            return selfNames.TryGetValue(language, out var name) ? name : language.ToString();
        }

        // key, English, Chinese, Russian, Spanish, Portuguese, German, Japanese, French, Polish, Korean, Arabic
        private static readonly (string key, string en, string zh, string ru, string es, string pt, string de, string ja, string fr, string pl, string ko, string ar)[] Table =
        {
            ("Theme", "Theme", "主题", "Тема", "Tema", "Tema", "Design", "テーマ", "Thème", "Motyw", "테마", "المظهر"),
            ("Auto", "Auto", "自动", "Авто", "Auto", "Automático", "Automatisch", "自動", "Auto", "Auto", "자동", "تلقائي"),
            ("Light", "Light", "浅色", "Светлая", "Claro", "Claro", "Hell", "ライト", "Clair", "Jasny", "밝게", "فاتح"),
            ("Dark", "Dark", "深色", "Тёмная", "Oscuro", "Escuro", "Dunkel", "ダーク", "Sombre", "Ciemny", "어둡게", "داكن"),
            ("AutoHide", "Auto Hide", "自动隐藏", "Автоскрытие", "Auto ocultar", "Ocultar automaticamente", "Automatisch ausblenden", "自動非表示", "Masquer automatiquement", "Auto ukrywanie", "자동 숨김", "إخفاء تلقائي"),
            ("Numeric", "Numeric", "数字电量", "Цифры", "Numérico", "Numérico", "Numerisch", "数字表示", "Numérique", "Numeryczny", "숫자 표시", "رقمي"),
            ("Devices", "Devices", "设备", "Устройства", "Dispositivos", "Dispositivos", "Geräte", "デバイス", "Appareils", "Urządzenia", "기기", "الأجهزة"),
            ("PopupSettings", "Popup Settings", "弹窗设置", "Настройки всплывающего окна", "Configuración de ventana emergente", "Configuração da janela pop-up", "Popup-Einstellungen", "ポップアップ設定", "Paramètres de la fenêtre contextuelle", "Ustawienia okna podręcznego", "팝업 설정", "إعدادات النافذة المنبثقة"),
            ("Startup", "Start with Windows", "开机自启", "Автозапуск", "Iniciar con Windows", "Iniciar com o Windows", "Mit Windows starten", "Windowsと一緒に起動", "Démarrer avec Windows", "Uruchamiaj z systemem Windows", "Windows 시작 시 실행", "بدء التشغيل مع Windows"),
            ("Sound", "Sound", "提示音", "Звук", "Sonido", "Som", "Ton", "サウンド", "Son", "Dźwięk", "소리", "الصوت"),
            ("Mute", "Mute", "静音", "Без звука", "Silencio", "Sem som", "Stumm", "ミュート", "Muet", "Wycisz", "음소거", "كتم الصوت"),
            ("EnableLogging", "Enable Logging", "启用日志", "Вести журнал", "Habilitar registro", "Ativar registro", "Protokollierung aktivieren", "ログを有効にする", "Activer le journal", "Włącz dziennik", "로그 활성화", "تفعيل السجل"),
            ("Battery", "Battery", "电量", "Заряд", "Batería", "Bateria", "Akku", "バッテリー", "Batterie", "Bateria", "배터리", "البطارية"),
            ("BatteryFull", "Full", "满", "Полный", "Lleno", "Cheio", "Voll", "満", "Plein", "Pełny", "가득", "ممتلئ"),
            ("BatteryMedium", "Medium", "中", "Средний", "Medio", "Médio", "Mittel", "中", "Moyen", "Średni", "중간", "متوسط"),
            ("BatteryLow", "Low", "低", "Низкий", "Bajo", "Baixo", "Niedrig", "低", "Faible", "Niski", "낮음", "منخفض"),
            ("BatteryEmpty", "Empty", "空", "Пустой", "Vacío", "Vazio", "Leer", "空", "Vide", "Pusty", "빈", "فارغ"),
            ("NoAlert", "No alert", "不提醒", "Не уведомлять", "Sin aviso", "Sem aviso", "Kein Alarm", "通知しない", "Aucune alerte", "Bez alertu", "알림 없음", "بدون تنبيه"),
            ("Adapter", "Adapter", "适配器", "Адаптер", "Adaptador", "Adaptador", "Adapter", "アダプター", "Adaptateur", "Adapter", "어댑터", "المحول"),
            ("Bluetooth", "Bluetooth", "蓝牙", "Bluetooth", "Bluetooth", "Bluetooth", "Bluetooth", "Bluetooth", "Bluetooth", "Bluetooth", "블루투스", "بلوتوث"),
            ("XboxControllerSlot", "Xbox Controller {0}", "Xbox 手柄 {0}", "Xbox Controller {0}", "Xbox Controller {0}", "Xbox Controller {0}", "Xbox-Controller {0}", "Xbox コントローラー {0}", "Xbox Controller {0}", "Kontroler Xbox {0}", "Xbox 컨트롤러 {0}", "وحدة تحكم Xbox {0}"),
            ("Language", "Language", "语言", "Язык", "Idioma", "Idioma", "Sprache", "言語", "Langue", "Język", "언어", "اللغة"),
            ("Exit", "Exit", "退出", "Выход", "Salir", "Sair", "Beenden", "終了", "Quitter", "Wyjdź", "종료", "خروج"),
            ("StatusLooking", "XBatteryStatus: Looking for paired devices", "XBatteryStatus：正在查找已配对的蓝牙设备", "XBatteryStatus: поиск сопряжённых устройств", "XBatteryStatus: Buscando dispositivos emparejados", "XBatteryStatus: Procurando dispositivos emparelhados", "XBatteryStatus: Suche nach gekoppelten Geräten", "XBatteryStatus: ペアリング済みデバイスを探しています", "XBatteryStatus: Recherche d'appareils appairés", "XBatteryStatus: Szukanie sparowanych urządzeń", "XBatteryStatus: 페어링된 기기를 찾는 중", "XBatteryStatus: البحث عن الأجهزة المقترنة"),
            ("StatusNoDevices", "XBatteryStatus: No paired device with battery service found", "XBatteryStatus：未找到带电池服务的已配对设备", "XBatteryStatus: нет сопряжённых устройств с батареей", "XBatteryStatus: No se encontró ningún dispositivo emparejado con batería", "XBatteryStatus: Nenhum dispositivo emparelhado com bateria encontrado", "XBatteryStatus: Kein gekoppeltes Gerät mit Batterie gefunden", "XBatteryStatus: 電池サービスを持つペアリング済みデバイスが見つかりません", "XBatteryStatus: Aucun appareil appairé avec batterie trouvé", "XBatteryStatus: Nie znaleziono sparowanego urządzenia z baterią", "XBatteryStatus: 배터리 서비스가 있는 페어링된 기기를 찾지 못함", "XBatteryStatus: لم يتم العثور على جهاز مقترن مزود ببطارية"),
            ("StatusDisconnected", "XBatteryStatus: No device is connected", "XBatteryStatus：没有已连接的设备", "XBatteryStatus: нет подключённых устройств", "XBatteryStatus: No hay ningún dispositivo conectado", "XBatteryStatus: Nenhum dispositivo conectado", "XBatteryStatus: Kein Gerät verbunden", "XBatteryStatus: 接続されているデバイスがありません", "XBatteryStatus: Aucun appareil connecté", "XBatteryStatus: Brak połączonych urządzeń", "XBatteryStatus: 연결된 기기가 없음", "XBatteryStatus: لا توجد أجهزة متصلة"),
            ("StatusBleOff", "XBatteryStatus: Bluetooth is turned off", "XBatteryStatus：蓝牙已关闭", "XBatteryStatus: Bluetooth выключен", "XBatteryStatus: El Bluetooth está desactivado", "XBatteryStatus: O Bluetooth está desativado", "XBatteryStatus: Bluetooth ist deaktiviert", "XBatteryStatus: Bluetooth がオフです", "XBatteryStatus: Le Bluetooth est désactivé", "XBatteryStatus: Bluetooth jest wyłączony", "XBatteryStatus: Bluetooth가 꺼져 있음", "XBatteryStatus: البلوتوث مغلق"),
            ("StatusBattery", "{0}", "{0}", "{0}", "{0}", "{0}", "{0}", "{0}", "{0}", "{0}", "{0}", "{0}"),
            ("Device", "Device", "设备", "Устройство", "Dispositivo", "Dispositivo", "Gerät", "デバイス", "Appareil", "Urządzenie", "기기", "الجهاز"),
            ("EnableAlert", "Alert", "提醒", "Уведомлять", "Avisar", "Avisar", "Benachrichtigen", "通知", "Notifier", "Powiadamiać", "알림", "تنبيه"),
            ("AlertAt", "Warn at", "提醒电量", "Порог заряда", "Avisar en", "Avisar em", "Warnen bei", "通知する値", "Avertir à", "Ostrzegaj przy", "알림 기준", "تنبيه عند"),
            ("CustomName", "Name", "名称", "Имя", "Nombre", "Nome", "Name", "名前", "Nom", "Nazwa", "이름", "الاسم"),
            ("NameHint", "(leave blank for device name)", "（留空使用设备名）", "(пусто — имя устройства)", "(vacío = nombre del dispositivo)", "(vazio = nome do dispositivo)", "(leer = Gerätename)", "（空欄でデバイス名）", "(vide = nom de l'appareil)", "(puste = nazwa urządzenia)", "(비우면 기기 이름)", "(فارغ = اسم الجهاز)"),
            ("PollInterval", "Poll interval (seconds)", "电量读取间隔（秒）", "Интервал опроса (сек)", "Intervalo de sondeo (s)", "Intervalo de leitura (s)", "Abfrageintervall (s)", "読み取り間隔（秒）", "Intervalle de lecture (s)", "Interwał odczytu (s)", "읽기 간격(초)", "فترة القراءة (ثانية)"),
            ("OK", "OK", "确定", "ОК", "Aceptar", "OK", "OK", "OK", "OK", "OK", "확인", "موافق"),
            ("Cancel", "Cancel", "取消", "Отмена", "Cancelar", "Cancelar", "Abbrechen", "キャンセル", "Annuler", "Anuluj", "취소", "إلغاء"),
            ("Test", "Test", "测试", "Тест", "Probar", "Testar", "Testen", "テスト", "Tester", "Testuj", "테스트", "اختبار"),
            ("TestDelayed", "Test (5s)", "测试（5秒后）", "Тест (через 5 с)", "Probar (5 s)", "Testar (5 s)", "Testen (5 s)", "テスト（5秒後）", "Tester (5 s)", "Testuj (po 5 s)", "테스트(5초 후)", "اختبار (بعد 5 ثوانٍ)"),
            ("PopupTitle", "Title", "标题", "Заголовок", "Título", "Título", "Titel", "タイトル", "Titre", "Tytuł", "제목", "العنوان"),
            ("PopupSubtitle", "Subtitle", "副标题", "Подзаголовок", "Subtítulo", "Subtítulo", "Untertitel", "サブタイトル", "Sous-titre", "Podtytuł", "부제목", "العنوان الفرعي"),
            ("Placeholders", "Placeholders: {battery} {device}", "可用占位符：{battery}、{device}", "Плейсхолдеры: {battery} {device}", "Marcadores: {battery} {device}", "Espaços reservados: {battery} {device}", "Platzhalter: {battery} {device}", "プレースホルダー: {battery} {device}", "Espaces réservés : {battery} {device}", "Symbole zastępcze: {battery} {device}", "자리 표시자: {battery} {device}", "العناصر النائبة: {battery} {device}"),
            ("TitleFont", "Title font...", "标题字体...", "Шрифт заголовка...", "Fuente del título...", "Fonte do título...", "Titelschriftart...", "タイトルのフォント...", "Police du titre...", "Czcionka tytułu...", "제목 글꼴...", "خط العنوان..."),
            ("SubtitleFont", "Subtitle font...", "副标题字体...", "Шрифт подзаголовка...", "Fuente del subtítulo...", "Fonte do subtítulo...", "Untertitel-Schriftart...", "サブタイトルのフォント...", "Police du sous-titre...", "Czcionka podtytułu...", "부제목 글꼴...", "خط العنوان الفرعي..."),
            ("Scale", "Size", "大小", "Масштаб", "Tamaño", "Tamanho", "Größe", "サイズ", "Taille", "Rozmiar", "크기", "الحجم"),
            ("Position", "Position", "位置", "Положение", "Posición", "Posição", "Position", "位置", "Position", "Pozycja", "위치", "الموضع"),
            ("SetPosition", "Set Position...", "设置位置...", "Задать положение...", "Establecer posición...", "Definir posição...", "Position festlegen...", "位置を設定...", "Définir la position...", "Ustaw pozycję...", "위치 설정...", "تعيين الموضع..."),
            ("ResetPosition", "Reset Position", "重置位置", "Сбросить положение", "Restablecer posición", "Redefinir posição", "Position zurücksetzen", "位置をリセット", "Réinitialiser la position", "Resetuj pozycję", "위치 초기화", "إعادة تعيين الموضع"),
            ("PositioningHint", "Arrow keys move (press: 1 px, hold: 10 px). Enter confirms, Esc cancels.", "方向键移动（点按 1 像素，长按 10 像素），回车确认，Esc 取消", "Стрелки перемещают (нажатие — 1 px, удержание — 10 px). Enter — подтвердить, Esc — отмена", "Las flechas mueven (pulsación: 1 px, mantener: 10 px). Intro confirma, Esc cancela", "As setas movem (toque: 1 px, manter: 10 px). Enter confirma, Esc cancela", "Pfeiltasten bewegen (Tippen: 1 px, Halten: 10 px). Enter bestätigt, Esc bricht ab", "矢印キーで移動（1回押し: 1px、長押し: 10px）。Enter で確定、Esc でキャンセル", "Les flèches déplacent (appui: 1 px, maintien: 10 px). Entrée confirme, Échap annule", "Strzałki przesuwają (naciśnięcie: 1 px, przytrzymanie: 10 px). Enter potwierdza, Esc anuluje", "방향키로 이동(누르기: 1px, 길게: 10px). Enter 확인, Esc 취소", "تتحرك الأسهم (ضغطة: 1 بكسل، ضغط مطول: 10 بكسل). Enter للتأكيد، Esc للإلغاء"),
            ("LowBattery", "Low Battery", "电量不足", "Низкий заряд", "Batería baja", "Bateria fraca", "Akku schwach", "バッテリー残量低下", "Batterie faible", "Niski poziom baterii", "배터리 부족", "بطارية منخفضة"),
            ("SubtitleDefault", "{battery}% - {device}", "{battery}% - {device}", "{battery}% - {device}", "{battery}% - {device}", "{battery}% - {device}", "{battery}% - {device}", "{battery}% - {device}", "{battery}% - {device}", "{battery}% - {device}", "{battery}% - {device}", "{battery}% - {device}"),
            ("Unknown", "Unknown", "未知", "Неизвестно", "Desconocido", "Desconhecido", "Unbekannt", "不明", "Inconnu", "Nieznany", "알 수 없음", "غير معروف")
        };

        static Localization()
        {
            foreach (var language in Enum.GetValues<AppLanguage>())
            {
                tables[language] = new Dictionary<string, string>(StringComparer.Ordinal);
            }

            foreach (var row in Table)
            {
                tables[AppLanguage.English][row.key] = row.en;
                tables[AppLanguage.Chinese][row.key] = row.zh;
                tables[AppLanguage.Russian][row.key] = row.ru;
                tables[AppLanguage.Spanish][row.key] = row.es;
                tables[AppLanguage.Portuguese][row.key] = row.pt;
                tables[AppLanguage.German][row.key] = row.de;
                tables[AppLanguage.Japanese][row.key] = row.ja;
                tables[AppLanguage.French][row.key] = row.fr;
                tables[AppLanguage.Polish][row.key] = row.pl;
                tables[AppLanguage.Korean][row.key] = row.ko;
                tables[AppLanguage.Arabic][row.key] = row.ar;
            }

            selfNames[AppLanguage.English] = "English";
            selfNames[AppLanguage.Chinese] = "简体中文";
            selfNames[AppLanguage.Russian] = "Русский";
            selfNames[AppLanguage.Spanish] = "Español";
            selfNames[AppLanguage.Portuguese] = "Português";
            selfNames[AppLanguage.German] = "Deutsch";
            selfNames[AppLanguage.Japanese] = "日本語";
            selfNames[AppLanguage.French] = "Français";
            selfNames[AppLanguage.Polish] = "Polski";
            selfNames[AppLanguage.Korean] = "한국어";
            selfNames[AppLanguage.Arabic] = "العربية";
        }
    }
}
