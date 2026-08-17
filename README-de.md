# 🔋 XBatteryStatus

> Überwacht den Akkustand deiner Bluetooth-Geräte im Windows-Systemtray — mit **Popup-Benachrichtigung im Xbox-Erfolgsstil** bei schwachem Akku (Erfolg freigeschaltet: Dein Controller stirbt 🤣)

🌐 **Sprachen:** [English](README.md) | [简体中文](README-zh-CN.md) | [Русский](README-ru.md) | [Español](README-es.md) | [Português](README-pt.md) | [Deutsch](README-de.md) | [日本語](README-ja.md) | [Français](README-fr.md) | [Polski](README-pl.md) | [한국어](README-ko.md) | [العربية](README-ar.md)


## 📸 Vorführung

![Demo 1: Popup-Animation](Icons/1.gif)

![Demo 2](Icons/2.jpg)

![Demo 3](Icons/3.jpg)

![Demo 4](Icons/4.jpg)

![Demo 5](Icons/5.jpg)

---

## ✨ Funktionen

- 🎮 **Überwachung mehrerer Geräte**: erkennt automatisch alle gekoppelten Bluetooth-Geräte mit Akku-Dienst (Xbox-Controller, Bluetooth-Kopfhörer, Bluetooth-Tastaturen und -Mäuse, Controller anderer Marken usw.), bereinigt Duplikate anhand der **Bluetooth-Adresse**, damit gleichnamige Geräte nie verwechselt werden
- 🟢 **Popup im Xbox-Erfolgsstil**: ein Kreis erscheint unten in der Mitte → weitet sich zu einer Karte aus → ein Glanzstreifen läuft darüber → klappt automatisch wieder ein. Eine originalgetreue Nachbildung der Erfolgsanimation der Xbox Game Bar unter Windows 11
- 🎚️ **Drei frei einstellbare Warnstufen**: jedes Gerät kann seine eigenen 3 Werte haben (Standard 35 % / 30 % / 25 %). Die Warnung ertönt einmal beim Unterschreiten eines Schwellenwerts, mit Erkennung von Sprungabfällen (z. B. 51 % → 49 % löst ebenfalls aus)
- ✏️ **Vollständig anpassbares Popup**: Titel-/Untertiteltext (mit den Platzhaltern `{battery}` und `{device}`), unabhängige Schriftarten pro Zeile, Gesamtskalierung (100 %–200 %), **interaktive Positionierung** (Pfeiltasten zum Feinjustieren, Enter zum Bestätigen)
- 🔔 **Warntöne**: mehrere integrierte Töne, Unterstützung für eigene `.wav`-Dateien (einfach in den Ordner `sound` neben der exe legen), standardmäßig stumm
- 🌍 **Mehrsprachige Oberfläche**: vereinfachtes Chinesisch, Englisch, Russisch, Spanisch, Portugiesisch, Deutsch, Japanisch, Französisch, Polnisch, Koreanisch, Arabisch; wechselt auf chinesischen Systemen automatisch zu Chinesisch
- 🚀 **Mit Windows starten**: standardmäßig aktiviert, Ein-Klick-Schalter
- 📄 **Protokoll-Schalter**: bei Bedarf ausführliche Protokolle aktivieren, geschrieben in den exe-Ordner (Limit 10 MB, automatisch gekürzt)
- 📁 **Portabel**: alle Dateien (Konfiguration, Protokoll, Töne) liegen im exe-Ordner — nichts verschmutzt das System

## ⚙️ Bedienung

Die App lebt im Systemtray. Rechtsklick auf das Symbol:

| Menüpunkt | Beschreibung |
|---|---|
| 🌗 Theme | Symbol-Design: Auto / Hell / Dunkel |
| 🌐 Language | Oberflächensprache (Auto folgt dem System) |
| 🎮 Devices | Geräteverwaltung: Warnungen, Schwellenwerte, Namen, Abfrageintervall, Protokoll |
| 🎨 Popup Settings | Popup-Anpassung: Text, Schriftarten, Größe, Position |
| 🚀 Mit Windows starten | Beim Anmelden ausführen (standardmäßig aktiviert) |
| 🔔 Ton | Warnton auswählen (standardmäßig stumm) |
| ❌ Exit | Beenden |

## 🎨 Popup-Einstellungen

- **Titel / Untertitel**: eigener Text; leer lassen, um den lokalisierten Standardtext zu verwenden. Der Untertitel unterstützt Platzhalter:
  - `{battery}` → der tatsächliche Akkustand (z. B. `35`)
  - `{device}` → der Anzeigename des Geräts
- **Schriftarten**: Titelzeile und Untertitelzeile haben jeweils ihre **eigene unabhängige Schriftart** (Größe, Stärke)
- **Größe**: Gesamtskalierung von 100 % bis 200 %, alles skaliert proportional
- **Position**: auf „Set Position..." klicken, das Popup bleibt sichtbar; **Pfeiltasten bewegen 1 px pro Tastendruck, 10 px bei gedrückt halten**, `Enter` bestätigt, `Esc` bricht ab; „Reset Position" bringt es jederzeit zurück nach unten Mitte
- **Test**: zeigt sofort ein Test-Popup; „Test (5s)" erscheint 5 Sekunden später – praktisch, um Vollbild-Szenarien im Spiel zu prüfen

## 🎮 Geräte

- Listet automatisch alle gekoppelten Bluetooth-Geräte mit Akku-Dienst auf (Duplikate anhand der Adresse bereinigt)
- Pro Gerät konfigurierbar:
  - ✅ Ob Warnungen bei schwachem Akku aktiviert sind (Controller standardmäßig aktiviert)
  - 🔢 3 Akku-Werte für Warnungen (1–100 %)
  - ✏️ Ein eigener Anzeigename (leer = Gerätename; wird vom Platzhalter `{device}` verwendet)
- **Abfrageintervall**: wie oft der Akku gelesen wird, in Sekunden (Standard 15 s)
- **Protokoll aktivieren**: wenn aktiviert, werden ausführliche Laufzeitprotokolle geschrieben

## 🌍 Sprachen

11 Sprachen werden unterstützt: vereinfachtes Chinesisch, Englisch, Russisch, Spanisch, Portugiesisch, Deutsch, Japanisch, Französisch, Polnisch, Koreanisch, Arabisch.

- **Auto** (Standard): verwendet Chinesisch bei chinesischer Systemsprache, sonst Englisch; die übrigen Sprachen wählt man manuell
- Manuelles Umschalten ist im Tray-Menü verfügbar

## 🔔 Ton

- Tray-Menü „Ton" → „Stumm" oder Audiodatei auswählen (Menüpunkte werden nach Dateinamen aufgelistet)
- Eigene Töne: `.wav`-Dateien in den **Ordner `sound` neben der exe** legen; das Menü aktualisiert sich automatisch, kein Neustart nötig
- Nur `.wav` wird unterstützt

## 📁 Dateien & Verzeichnisse

Die App ist von Natur aus portabel – alles wird im **exe-Verzeichnis** erzeugt:

```
📁 exe-Verzeichnis/
├── 📄 XBatteryStatus.exe      ← Hauptprogramm
├── 📄 log.txt                 ← Laufzeitprotokoll (geschrieben bei aktiviertem Protokoll, Limit 10 MB)
├── 📁 sound/                  ← Ton-Ordner (nur lesen, eigene wav hier ablegen)
└── 📁 config/                 ← die gesamte Konfiguration
    ├── 📄 settings.json       ← App-Einstellungen (Sprache/Popup/Ton/Start usw.)
    └── 📄 devices.json        ← Warn-Konfiguration pro Gerät
```

## ⚠️ Hinweise

- **Exklusive Vollbild-Spiele** (Exclusive Fullscreen) können das Popup nicht anzeigen – das ist eine Windows-Einschränkung für alle Drittanbieter-Tools; stelle deine Spiele auf **randloses Fenster** (Borderless Windowed) um
- Für eigene Töne werden nur `.wav`-Dateien unterstützt
- Warnungen folgen der Regel „**einmal pro Schwellenwert-Unterschreitung**": erneut wird nur gewarnt, wenn der Akku über den Schwellenwert aufgeladen und wieder darunter fällt; ein Gerät, das sich bereits unter dem Schwellenwert wieder verbindet, warnt ebenfalls einmal

## 🛠️ Build

Erfordert das **.NET 8 SDK** (Windows).

```bash
dotnet build XBatteryStatus/XBatteryStatus.csproj -c Release
```

Die Ausgabe liegt unter `XBatteryStatus/bin/Release/net8.0-windows10.0.19041.0/`:

- Die integrierten Töne werden automatisch in den `sound/`-Ordner der Ausgabe kopiert
- `config/` und `log.txt` werden zur Laufzeit neben der exe erzeugt; manuell muss nichts angelegt werden

## ❓ FAQ

- F: Warum wurde dieses Projekt erstellt?

- A: Bestehende Tools warnen über Windows-Benachrichtigungen, aber beim Spielen unterdrückt Windows standardmäßig Benachrichtigungen unterhalb der höchsten Priorität, und man muss die Priorität manuell erhöhen — das ist lästig. Selbst nach der Einrichtung fragt Windows nach einer Weile, ob die Benachrichtigungen der Controller-Akku-App deaktiviert werden sollen, weil man sie nie geöffnet hat; Microsoft hält sie für unwichtig und empfiehlt, sie zu deaktivieren. Deshalb habe ich den nativen Benachrichtigungskanal umgangen und die Hinweise anders umgesetzt.

- F: Warum zeigt der Adapter keinen genauen Akkustand an?

- A: XInput bietet nur 4 Stufen (Empty leer / Low niedrig / Medium mittel / Full voll); es gibt keinen präzisen Prozentsatz wie beim Bluetooth-GATT. Das ist eine Einschränkung des XInput-Protokolls selbst; daran kann ich nichts ändern.

Unterstützt mehrere Geräte

## 🙏 Referenzen & Danksagung

> https://github.com/NiyaShy/XB1ControllerBatteryIndicator

> https://github.com/tommaier123/XBatteryStatus

> https://github.com/SteamAchievementNotifier/SteamAchievementNotifier

> https://github.com/gopi470/Nox

> https://github.com/SpartanX1/bluetooth_classic_battery_windows

> https://github.com/o0Zz/PeripheralBatteryMonitor

## 🍋 Unterstützung

Gefällt es dir? Gib mir einen Limonade aus ~

![Alipay-Spenden-QR](Icons/zfb.jpg)


## 📄 Lizenz

- Dieses Projekt ist unter der **GPL-3.0**-Lizenz veröffentlicht, siehe [LICENSE](LICENSE).
