# 🔋 XBatteryStatus

> Monitor the battery level of your Bluetooth devices in the Windows system tray, with **Xbox achievement-style popups** when the battery is low (Achievement unlocked: your controller is dying 🤣)

🌐 **Languages:** [English](README.md) | [简体中文](README-zh-CN.md) | [Русский](README-ru.md) | [Español](README-es.md) | [Português](README-pt.md) | [Deutsch](README-de.md) | [日本語](README-ja.md) | [Français](README-fr.md) | [Polski](README-pl.md) | [한국어](README-ko.md) | [العربية](README-ar.md)


## 📸 Demo

![Demo 1: popup animation](Icons/1.gif)

![Demo 2](Icons/2.jpg)

![Demo 3](Icons/3.jpg)

![Demo 4](Icons/4.jpg)

![Demo 5](Icons/5.jpg)

---

## ✨ Features

- 🎮 **Multi-device monitoring**: automatically discovers all paired Bluetooth devices with a battery service (Xbox controllers, Bluetooth headsets, controllers of other brands, etc.), deduplicated by **Bluetooth address** so devices with the same name never get mixed up
- 🟢 **Xbox achievement-style popup**: a circle pops in at the bottom center → expands into a card → a shine sweeps across → auto-collapses. A faithful recreation of the Windows 11 Xbox Game Bar achievement toast animation
- 🎚️ **Three custom alert levels**: each device can have its own 3 alert values (default 35% / 30% / 25%). Alerts fire once when a threshold is crossed, with jump-drop detection (e.g. 51% → 49% still triggers)
- ✏️ **Fully customizable popup**: title/subtitle text (with `{battery}` and `{device}` placeholders), independent fonts per line, overall scale (100%–200%), **interactive positioning** (arrow keys to fine-tune, Enter to confirm)
- 🔔 **Alert sounds**: several built-in sounds, custom `.wav` files supported (just drop them into the `sound` folder next to the exe), muted by default
- 🌍 **Multilingual UI**: Simplified Chinese, English, Russian, Spanish, Portuguese, German, Japanese, French, Polish, Korean, Arabic; automatically switches to Chinese on Chinese systems
- 🚀 **Start with Windows**: enabled by default, one-click toggle
- 📄 **Logging toggle**: enable detailed logs when needed, written to the exe folder (10 MB cap, auto-truncated)
- 📁 **Portable**: all files (config, log, sounds) live inside the exe folder — nothing pollutes the system

## ⚙️ Usage

The app lives in the system tray. Right-click the tray icon:

| Menu item | Description |
|---|---|
| 🌗 Theme | Icon theme: Auto / Light / Dark |
| 🌐 Language | UI language (Auto follows the system) |
| 🎮 Devices | Device management: enable alerts, set thresholds, custom names, polling interval, logging |
| 🎨 Popup Settings | Popup customization: text, fonts, size, position |
| 🚀 Start with Windows | Run at logon (enabled by default) |
| 🔔 Sound | Choose the alert sound (muted by default) |
| ❌ Exit | Quit the app |

## 🎨 Popup Settings

- **Title / Subtitle**: custom text; leave empty to use the localized default. The subtitle supports placeholders:
  - `{battery}` → the actual battery level (e.g. `35`)
  - `{device}` → the device display name
- **Fonts**: the title line and subtitle line each have their **own independent font** (size, weight)
- **Size**: overall scale from 100% to 200%, everything scales proportionally
- **Position**: click "Set Position..." and the popup stays visible; **arrow keys move 1 px per press, 10 px while held**, `Enter` confirms, `Esc` cancels; "Reset Position" returns to bottom-center anytime
- **Test**: shows a test popup immediately; "Test (5s)" pops up 5 seconds later, handy for verifying fullscreen scenarios in a game

## 🎮 Devices

- Automatically lists all paired Bluetooth devices with a battery service (deduplicated by Bluetooth address)
- Per device you can configure:
  - ✅ Whether low battery alerts are enabled (gamepads enabled by default)
  - 🔢 3 alert battery values (1–100%)
  - ✏️ A custom display name (leave blank to use the device name; used by the `{device}` placeholder in the popup)
- **Polling interval**: how often the battery is read, in seconds (default 15s)
- **Enable Logging**: when checked, detailed runtime logs are recorded

## 🌍 Languages

11 languages are supported: Simplified Chinese, English, Russian, Spanish, Portuguese, German, Japanese, French, Polish, Korean, Arabic.

- **Auto** (default): uses Chinese when the system locale is Simplified Chinese, otherwise English; other languages must be selected manually
- Manual switching is available in the tray menu

## 🔔 Sound

- Tray menu "Sound" → "Mute" or pick an audio file (menu items are listed by file name)
- Custom sounds: drop `.wav` files into the **`sound` folder next to the exe**; the menu refreshes automatically, no restart needed
- Only `.wav` is supported

## 📁 Files & Directories

The app is portable by design — everything is generated in the **exe directory**:

```
📁 exe directory/
├── 📄 XBatteryStatus.exe      ← main program
├── 📄 log.txt                 ← runtime log (written when logging is enabled, 10 MB cap)
├── 📁 sound/                  ← sound folder (read-only, drop custom wav files here)
└── 📁 config/                 ← all configuration
    ├── 📄 settings.json       ← app settings (language/popup/sound/startup etc.)
    └── 📄 devices.json        ← per-device alert configuration
```

## ⚠️ Notes

- **Exclusive Fullscreen games** cannot display the popup — this is a Windows limitation that applies to all third-party tools; please switch your games to **Borderless Windowed** mode
- Only `.wav` files are supported for custom sounds
- Alerts follow the "**once per threshold crossing**" policy: re-alerts only happen after the battery is recharged above the threshold and drops below it again; a device that reconnects already below a threshold also alerts once

## 🛠️ Build

Requires the **.NET 8 SDK** (Windows).

```bash
dotnet build XBatteryStatus/XBatteryStatus.csproj -c Release
```

The output is written to `XBatteryStatus/bin/Release/net8.0-windows10.0.19041.0/`:

- The built-in sounds are copied to the `sound/` folder of the output automatically
- `config/` and `log.txt` are generated at runtime next to the exe; nothing needs to be created manually

## 🙏 References & Credits

This project is based on the following projects:
https://github.com/tommaier123/XBatteryStatus
https://github.com/SteamAchievementNotifier/SteamAchievementNotifier

## 🍋 Support

Like it? Buy me a glass of lemonade ~

![Alipay donation QR](Icons/zfb.jpg)


## 📄 License

- This project is released under the **GPL-3.0** license, see [LICENSE](LICENSE).
