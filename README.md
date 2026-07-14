# DeskNotes

Minimalistische Desktop-Notizliste für Windows — offline, schnell, immer griffbereit im Tray.

<p align="center">
  <img src="Assets/desknote-gelb.png" alt="DeskNotes" width="96">
</p>

![.NET](https://img.shields.io/badge/.NET-10.0-purple)
![Platform](https://img.shields.io/badge/platform-Windows-blue)
![License](https://img.shields.io/badge/license-MIT-green)

## Features

- **Notizen** — anlegen, bearbeiten, abhaken, löschen, per Drag & Drop sortieren
- **Markdown** — `**fett**`, `*kursiv*`, Links, Listen und Inline-Code in Notizen
- **Filter** — Alle, Aktiv, Erledigt
- **Tray-Icon** — App läuft im Hintergrund, Schließen minimiert ins Tray
- **Globaler Hotkey** — `Strg + Alt + Leertaste` öffnet die Eingabe von überall
- **Autostart** — optional mit Windows starten
- **Immer im Vordergrund** — optional aktivierbar
- **Addon-System** — erweiterbar über DLLs im `Addons/`-Ordner

### Addons (mitgeliefert)

| Addon | Beschreibung | Kurzbefehl |
|-------|--------------|------------|
| **Export** | Notizen als JSON oder Markdown exportieren | Tray-Menü |
| **Timer** | Fokus-Timer mit Sound-Profilen | `timer`, `timer 25`, `timer stop` |
| **Confetti** | Konfetti beim Abhaken einer Notiz | Einstellungen |
| **Disco** | Disco-Modus | `disco` |

## Voraussetzungen

- Windows 10 oder 11
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)

## Installation

### Aus dem Quellcode bauen

```powershell
git clone https://github.com/kptnChr7s/DeskNotes.git
cd DeskNotes
dotnet build DeskNotes.sln -c Release
```

Die App liegt danach unter `bin\Release\net10.0-windows\DeskNotes.exe`.

### Veröffentlichen (Release-Ordner)

```powershell
dotnet publish DeskNotes.csproj -c Release -p:PublishProfile=FolderProfile
```

Ausgabe: `publish\` (inkl. `Addons\`-Ordner).

### Portable ZIP (empfohlen für Downloads)

```powershell
dotnet publish DeskNotes.csproj -c Release -r win-x64 --self-contained true -o publish\portable
```

> Der `Addons\`-Ordner wird automatisch mitkopiert. Für Releases den gesamten `publish\portable\`-Ordner als ZIP packen.

### Fertiges Release

Lade die neueste Version von [GitHub Releases](https://github.com/kptnChr7s/DeskNotes/releases) herunter, entpacke die ZIP und starte `DeskNotes.exe`.

## Datenspeicherung

Alle Daten werden lokal gespeichert — keine Cloud, kein Account:

| Datei | Inhalt |
|-------|--------|
| `%LocalAppData%\DeskNotes\todo.json` | Notizen |
| `%LocalAppData%\DeskNotes\settings.json` | Fenster, Filter, Einstellungen |
| `%LocalAppData%\DeskNotes\addons\` | Addon-Einstellungen |

## Tastenkürzel

| Taste | Aktion |
|-------|--------|
| `Enter` | Notiz hinzufügen / speichern |
| `Entf` | Ausgewählte Notiz löschen |
| `Doppelklick` | Notiz bearbeiten |
| `Esc` | Bearbeitung abbrechen / App minimieren |
| `Strg + Alt + Leertaste` | App öffnen (global) |

## Projektstruktur

```
DeskNotes/
├── DeskNotes.csproj          # Hauptanwendung (WPF)
├── DeskNotes.Abstractions/   # Addon-Interfaces & Events
├── Addons/                   # Addon-Projekte
│   ├── DeskNotes.Addon.Export/
│   ├── DeskNotes.Addon.Timer/
│   ├── DeskNotes.Addon.Confetti/
│   └── DeskNotes.Addon.Disco/
├── Core/Addons/              # Addon-Host, Loader, EventBus
├── ViewModels/               # MVVM
├── Services/                 # Persistenz, Hotkey, Autostart
└── Themes/                   # Dark-Theme Styles
```

## Lizenz

MIT — siehe [LICENSE](LICENSE).