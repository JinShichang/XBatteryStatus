# 🔋 XBatteryStatus

> Surveillez le niveau de batterie de vos appareils Bluetooth dans la barre d'état système de Windows, avec une **notification contextuelle style succès Xbox** quand la batterie est faible (Succès débloqué : votre manette est en train de mourir 🤣)

🌐 **Langues :** [English](README.md) | [简体中文](README-zh-CN.md) | [Русский](README-ru.md) | [Español](README-es.md) | [Português](README-pt.md) | [Deutsch](README-de.md) | [日本語](README-ja.md) | [Français](README-fr.md) | [Polski](README-pl.md) | [한국어](README-ko.md) | [العربية](README-ar.md)


## 📸 Démonstration

![Démo 1 : animation de la notification](Icons/1.gif)

![Démo 2](Icons/2.jpg)

![Démo 3](Icons/3.jpg)

![Démo 4](Icons/4.jpg)

![Démo 5](Icons/5.jpg)

---

## ✨ Fonctionnalités

- 🎮 **Surveillance multi-appareils** : détecte automatiquement tous les appareils Bluetooth appairés disposant d'un service de batterie (manettes Xbox, casques Bluetooth, claviers et souris Bluetooth, manettes d'autres marques, etc.), dédupliqués par **adresse Bluetooth** pour que des appareils homonymes ne soient jamais mélangés
- 🟢 **Notification style succès Xbox** : un cercle apparaît en bas au centre → se déploie en carte → un reflet la parcourt → se replie automatiquement. Une reproduction fidèle de l'animation de succès de la Xbox Game Bar sous Windows 11
- 🎚️ **Trois seuils d'alerte personnalisables** : chaque appareil peut avoir ses propres 3 valeurs (par défaut 35 % / 30 % / 25 %). L'alerte se déclenche une fois au franchissement d'un seuil, avec détection des chutes brutales (ex. 51 % → 49 % déclenche aussi)
- ✏️ **Notification entièrement personnalisable** : texte du titre/sous-titre (avec les espaces réservés `{battery}` et `{device}`), polices indépendantes par ligne, échelle globale (100 %–200 %), **positionnement interactif** (flèches pour ajuster, Entrée pour confirmer)
- 🔔 **Sons d'alerte** : plusieurs sons intégrés, prise en charge de fichiers `.wav` personnalisés (il suffit de les placer dans le dossier `sound` à côté de l'exe), muet par défaut
- 🌍 **Interface multilingue** : chinois simplifié, anglais, russe, espagnol, portugais, allemand, japonais, français, polonais, coréen, arabe ; bascule automatiquement en chinois sur les systèmes chinois
- 🚀 **Démarrage avec Windows** : activé par défaut, bascule en un clic
- 📄 **Interrupteur de journalisation** : activez des journaux détaillés en cas de besoin, écrits dans le dossier de l'exe (limite de 10 Mo, troncature automatique)
- 📁 **Portable** : tous les fichiers (configuration, journal, sons) restent dans le dossier de l'exe — rien ne pollue le système

## ⚙️ Utilisation

L'application vit dans la barre d'état système. Clic droit sur l'icône :

| Élément du menu | Description |
|---|---|
| 🌗 Theme | Thème de l'icône : Auto / Clair / Sombre |
| 🌐 Language | Langue de l'interface (Auto suit le système) |
| 🎮 Devices | Gestion des appareils : alertes, seuils, noms, intervalle de lecture, journal |
| 🎨 Popup Settings | Personnalisation de la notification : texte, polices, taille, position |
| 🚀 Démarrer avec Windows | Exécuter à l'ouverture de session (activé par défaut) |
| 🔔 Son | Choisir le son d'alerte (muet par défaut) |
| ❌ Exit | Quitter l'application |

## 🎨 Paramètres de la notification

- **Titre / Sous-titre** : texte personnalisé ; laissez vide pour utiliser le texte localisé par défaut. Le sous-titre prend en charge les espaces réservés :
  - `{battery}` → le niveau de batterie réel (ex. `35`)
  - `{device}` → le nom affiché de l'appareil
- **Polices** : la ligne du titre et celle du sous-titre ont chacune **leur propre police indépendante** (taille, graisse)
- **Taille** : échelle globale de 100 % à 200 %, tout est agrandi proportionnellement
- **Position** : cliquez sur « Set Position... » et la notification reste affichée ; **les flèches déplacent de 1 px par pression, de 10 px en maintien**, `Entrée` confirme, `Échap` annule ; « Reset Position » ramène en bas au centre à tout moment
- **Test** : affiche immédiatement une notification de test ; « Test (5s) » l'affiche 5 secondes plus tard, pratique pour vérifier les scénarios plein écran dans un jeu

## 🎮 Appareils

- Liste automatiquement tous les appareils Bluetooth appairés avec service de batterie (dédupliqués par adresse)
- Par appareil, vous pouvez configurer :
  - ✅ Si les alertes de batterie faible sont activées (manettes activées par défaut)
  - 🔢 3 valeurs de batterie pour les alertes (1–100 %)
  - ✏️ Un nom affiché personnalisé (vide = nom de l'appareil ; utilisé par l'espace réservé `{device}`)
- **Intervalle de lecture** : fréquence de lecture de la batterie, en secondes (15 s par défaut)
- **Activer la journalisation** : si coché, un journal détaillé est enregistré

## 🌍 Langues

11 langues sont prises en charge : chinois simplifié, anglais, russe, espagnol, portugais, allemand, japonais, français, polonais, coréen, arabe.

- **Auto** (par défaut) : utilise le chinois si la langue du système est le chinois simplifié, sinon l'anglais ; les autres langues se choisissent manuellement
- Le changement manuel est disponible dans le menu de la barre d'état

## 🔔 Son

- Menu de la barre d'état « Son » → « Muet » ou choisir un fichier audio (les éléments du menu sont listés par nom de fichier)
- Sons personnalisés : placez des fichiers `.wav` dans le **dossier `sound` à côté de l'exe** ; le menu se rafraîchit automatiquement, sans redémarrage
- Seul le format `.wav` est pris en charge

## 📁 Fichiers et répertoires

L'application est portable par conception — tout est généré dans le **répertoire de l'exe** :

```
📁 répertoire de l'exe/
├── 📄 XBatteryStatus.exe      ← programme principal
├── 📄 log.txt                 ← journal (écrit quand la journalisation est activée, limite 10 Mo)
├── 📁 sound/                  ← dossier des sons (lecture seule, placez vos wav ici)
└── 📁 config/                 ← toute la configuration
    ├── 📄 settings.json       ← paramètres de l'application (langue/notification/son/démarrage, etc.)
    └── 📄 devices.json        ← configuration des alertes par appareil
```

## ⚠️ Remarques

- Les jeux en **plein écran exclusif** (Exclusive Fullscreen) ne peuvent pas afficher la notification — c'est une limitation de Windows qui s'applique à tous les outils tiers ; passez vos jeux en mode **fenêtré sans bordure** (Borderless Windowed)
- Pour les sons personnalisés, seuls les fichiers `.wav` sont pris en charge
- Les alertes suivent la politique « **une fois par franchissement de seuil** » : nouvelle alerte uniquement si la batterie est rechargée au-dessus du seuil puis redescend ; un appareil qui se reconnecte déjà sous le seuil alerte également une fois

## 🛠️ Compilation

Nécessite le **.NET 8 SDK** (Windows).

```bash
dotnet build XBatteryStatus/XBatteryStatus.csproj -c Release
```

La sortie est écrite dans `XBatteryStatus/bin/Release/net8.0-windows10.0.19041.0/` :

- Les sons intégrés sont copiés automatiquement dans le dossier `sound/` de la sortie
- `config/` et `log.txt` sont générés à l'exécution à côté de l'exe ; rien à créer manuellement

## ❓ FAQ

- Q: Pourquoi ce projet a-t-il été créé ?

- R: Les outils existants alertent via les notifications Windows, mais pendant une partie Windows masque par défaut les notifications qui ne sont pas en priorité maximale, et il faut relever la priorité manuellement — c'est pénible. Même après réglage, Windows finit par demander s'il faut désactiver les notifications de l'application de batterie de la manette, car vous ne les avez jamais ouvertes ; Microsoft les juge sans importance et recommande de les désactiver. J'ai donc contourné le canal de notifications natif et implémenté les alertes autrement.

- Q: Pourquoi l'adaptateur ne montre-t-il pas un niveau de batterie précis ?

- R: XInput ne fournit que 4 niveaux (Empty vide / Low bas / Medium moyen / Full plein) ; il n'y a pas de pourcentage précis comme le GATT Bluetooth. C'est une limitation du protocole XInput lui-même ; je n'y peux rien.

Prend en charge plusieurs appareils

## 🙏 Références et remerciements

> https://github.com/NiyaShy/XB1ControllerBatteryIndicator

> https://github.com/tommaier123/XBatteryStatus

> https://github.com/SteamAchievementNotifier/SteamAchievementNotifier

> https://github.com/gopi470/Nox

> https://github.com/SpartanX1/bluetooth_classic_battery_windows

> https://github.com/o0Zz/PeripheralBatteryMonitor

## 🍋 Soutien

Ça vous plaît ? Offrez-moi un verre de limonade ~

![QR code de don Alipay](Icons/zfb.jpg)


## 📄 Licence

- Ce projet est publié sous licence **GPL-3.0**, voir [LICENSE](LICENSE).
