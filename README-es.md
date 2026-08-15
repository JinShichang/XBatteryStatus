# 🔋 XBatteryStatus

> Monitoriza el nivel de batería de tus dispositivos Bluetooth en la bandeja del sistema de Windows, con **aviso emergente estilo logro de Xbox** cuando la batería está baja (¡Logro desbloqueado: tu mando se está quedando sin batería 🤣)

🌐 **Idiomas:** [English](README.md) | [简体中文](README-zh-CN.md) | [Русский](README-ru.md) | [Español](README-es.md) | [Português](README-pt.md) | [Deutsch](README-de.md) | [日本語](README-ja.md) | [Français](README-fr.md) | [Polski](README-pl.md) | [한국어](README-ko.md) | [العربية](README-ar.md)


## 📸 Demo

![Demo 1: animación del aviso](Icons/1.gif)

![Demo 2](Icons/2.jpg)

![Demo 3](Icons/3.jpg)

![Demo 4](Icons/4.jpg)

![Demo 5](Icons/5.jpg)

---

## ✨ Características

- 🎮 **Monitorización multidispositivo**: descubre automáticamente todos los dispositivos Bluetooth emparejados con servicio de batería (mandos de Xbox, auriculares Bluetooth, mandos de otras marcas, etc.), sin duplicados por **dirección Bluetooth**, así los dispositivos con el mismo nombre nunca se mezclan
- 🟢 **Aviso emergente estilo logro de Xbox**: un círculo aparece en el centro inferior → se expande en una tarjeta → un brillo la recorre → se contrae automáticamente. Una réplica fiel de la animación de logros de Xbox Game Bar en Windows 11
- 🎚️ **Tres niveles de aviso personalizables**: cada dispositivo puede tener sus propios 3 valores (por defecto 35% / 30% / 25%). El aviso suena una vez al cruzar un umbral, con detección de caídas bruscas (p. ej. 51% → 49% también activa)
- ✏️ **Aviso totalmente personalizable**: texto de título/subtítulo (con los marcadores `{battery}` y `{device}`), fuentes independientes por línea, escala global (100%–200%), **posicionamiento interactivo** (flechas para ajustar, Enter para confirmar)
- 🔔 **Sonidos de aviso**: varios sonidos integrados, compatibles con archivos `.wav` personalizados (solo hay que ponerlos en la carpeta `sound` junto al exe), silenciado por defecto
- 🌍 **Interfaz multilingüe**: chino simplificado, inglés, ruso, español, portugués, alemán, japonés, francés, polaco, coreano, árabe; cambia automáticamente al chino en sistemas chinos
- 🚀 **Iniciar con Windows**: activado por defecto, conmutador de un clic
- 📄 **Conmutador de registro**: activa registros detallados cuando sea necesario, escritos en la carpeta del exe (límite de 10 MB, truncado automático)
- 📁 **Portátil**: todos los archivos (configuración, registro, sonidos) viven dentro de la carpeta del exe — no ensucia el sistema

## ⚙️ Uso

La aplicación vive en la bandeja del sistema. Haz clic derecho en el icono:

| Elemento del menú | Descripción |
|---|---|
| 🌗 Theme | Tema del icono: Auto / Claro / Oscuro |
| 🌐 Language | Idioma de la interfaz (Auto sigue al sistema) |
| 🎮 Devices | Gestión de dispositivos: avisos, umbrales, nombres, intervalo de sondeo, registro |
| 🎨 Popup Settings | Personalización del aviso: texto, fuentes, tamaño, posición |
| 🚀 Iniciar con Windows | Ejecutar al iniciar sesión (activado por defecto) |
| 🔔 Sonido | Elegir el sonido de aviso (silenciado por defecto) |
| ❌ Exit | Salir de la aplicación |

## 🎨 Configuración del aviso

- **Título / Subtítulo**: texto personalizado; déjalo vacío para usar el texto localizado por defecto. El subtítulo admite marcadores:
  - `{battery}` → el nivel de batería real (p. ej. `35`)
  - `{device}` → el nombre mostrado del dispositivo
- **Fuentes**: la línea del título y la del subtítulo tienen cada una su **propia fuente independiente** (tamaño, grosor)
- **Tamaño**: escala global del 100% al 200%, todo se amplía proporcionalmente
- **Posición**: pulsa "Set Position..." y el aviso permanece visible; **las flechas mueven 1 px por pulsación, 10 px al mantener**, `Enter` confirma, `Esc` cancela; "Reset Position" devuelve al centro inferior en cualquier momento
- **Probar**: muestra un aviso de prueba al instante; "Test (5s)" lo muestra 5 segundos después, útil para verificar escenarios de pantalla completa en un juego

## 🎮 Dispositivos

- Enumera automáticamente todos los dispositivos Bluetooth emparejados con servicio de batería (sin duplicados por dirección)
- Por dispositivo puedes configurar:
  - ✅ Si los avisos de batería baja están activados (mandos activados por defecto)
  - 🔢 3 valores de batería para avisar (1–100%)
  - ✏️ Un nombre mostrado personalizado (vacío = nombre del dispositivo; lo usa el marcador `{device}`)
- **Intervalo de sondeo**: cada cuánto se lee la batería, en segundos (por defecto 15 s)
- **Habilitar registro**: al marcarlo se registra un registro detallado

## 🌍 Idiomas

Se admiten 11 idiomas: chino simplificado, inglés, ruso, español, portugués, alemán, japonés, francés, polaco, coreano, árabe.

- **Auto** (por defecto): usa chino si la configuración regional del sistema es chino simplificado; si no, inglés; los demás idiomas se eligen manualmente
- El cambio manual está disponible en el menú de la bandeja

## 🔔 Sonido

- Menú de la bandeja «Sonido» → «Silencio» o elige un archivo de audio (los elementos del menú se listan por nombre de archivo)
- Sonidos personalizados: pon archivos `.wav` en la **carpeta `sound` junto al exe**; el menú se actualiza automáticamente, sin reiniciar
- Solo se admite `.wav`

## 📁 Archivos y carpetas

La aplicación es portátil por diseño: todo se genera en el **directorio del exe**:

```
📁 directorio del exe/
├── 📄 XBatteryStatus.exe      ← programa principal
├── 📄 log.txt                 ← registro (se escribe con el registro activado, límite 10 MB)
├── 📁 sound/                  ← carpeta de sonidos (solo lectura, pon aquí tus wav)
└── 📁 config/                 ← toda la configuración
    ├── 📄 settings.json       ← ajustes de la aplicación (idioma/aviso/sonido/inicio, etc.)
    └── 📄 devices.json        ← configuración de avisos por dispositivo
```

## ⚠️ Notas

- Los juegos en **pantalla completa exclusiva** (Exclusive Fullscreen) no pueden mostrar el aviso — es una limitación de Windows que afecta a todas las herramientas de terceros; cambia tus juegos al modo **sin bordes** (Borderless Windowed)
- Para sonidos personalizados solo se admiten archivos `.wav`
- Los avisos siguen la política de «**una vez por cruce de umbral**»: solo vuelve a avisar cuando la batería se recarga por encima del umbral y vuelve a bajar; un dispositivo que se reconecta ya por debajo del umbral también avisa una vez

## 🛠️ Compilación

Requiere el **.NET 8 SDK** (Windows).

```bash
dotnet build XBatteryStatus/XBatteryStatus.csproj -c Release
```

La salida se escribe en `XBatteryStatus/bin/Release/net8.0-windows10.0.19041.0/`:

- Los sonidos integrados se copian automáticamente a la carpeta `sound/` de la salida
- `config/` y `log.txt` se generan en tiempo de ejecución junto al exe; no hay que crear nada manualmente

## 🙏 Referencias y créditos

Este proyecto se basa en los siguientes proyectos:
https://github.com/tommaier123/XBatteryStatus
https://github.com/SteamAchievementNotifier/SteamAchievementNotifier

## 🍋 Apoyo

¿Te gusta? Invítame a una limonada ~

![Código QR de donación Alipay](Icons/zfb.jpg)


## 📄 Licencia

- Este proyecto se publica bajo la licencia **GPL-3.0**, ver [LICENSE](LICENSE).
