# DeskNotes

Minimalistische Desktop-Notizliste für Windows — offline, schnell, immer griffbereit im Tray.

<p align="center">
  <img src="Assets/desknote-gelb.png" alt="DeskNotes" width="96">
</p>

<p align="center">
  <a href="https://github.com/kptnChr7s/DeskNotes/releases/latest">
    <img src="https://img.shields.io/github/v/release/kptnChr7s/DeskNotes?label=Installer%20herunterladen&style=for-the-badge&color=7C6AF7" alt="Installer herunterladen">
  </a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-purple" alt=".NET 10">
  <img src="https://img.shields.io/badge/platform-Windows%2010%2F11-blue" alt="Windows">
  <img src="https://img.shields.io/badge/license-MIT-green" alt="MIT">
</p>

---

## Installation

**Windows 10/11 (64-bit)** — keine .NET-Installation nötig.

1. **[Installer herunterladen](https://github.com/kptnChr7s/DeskNotes/releases/latest)** (`DeskNotes-Setup-win-x64.exe`)
2. Doppelklick → Assistent → **Installieren**
3. App aus dem **Startmenü** starten — fertig

Deinstallation: Windows → **Installierte Apps** → DeskNotes.

---

## Features

- **Notizen** — anlegen, bearbeiten, abhaken, löschen, per Drag & Drop sortieren
- **Markdown** — `**fett**`, `*kursiv*`, Links, Listen und Inline-Code
- **Filter** — Alle, Aktiv, Erledigt
- **Tray-Icon** — läuft im Hintergrund, Schließen minimiert ins Tray
- **Globaler Hotkey** — `Strg + Alt + Leertaste` öffnet die Eingabe von überall
- **Autostart** — optional mit Windows starten
- **Immer im Vordergrund** — optional aktivierbar
- **Addon-System** — erweiterbar über DLLs

### Mitgelieferte Addons

| Addon | Beschreibung | Kurzbefehl |
|-------|--------------|------------|
| **Export** | Notizen als JSON oder Markdown exportieren | Tray-Menü |
| **Timer** | Fokus-Timer mit Sound-Profilen | `timer`, `timer 25`, `timer stop` |
| **Confetti** | Konfetti beim Abhaken einer Notiz | Einstellungen |
| **Disco** | Disco-Modus | `disco` |

---

## Tastenkürzel

| Taste | Aktion |
|-------|--------|
| `Enter` | Notiz hinzufügen / speichern |
| `Entf` | Ausgewählte Notiz löschen |
| `Doppelklick` | Notiz bearbeiten |
| `Esc` | Bearbeitung abbrechen / App minimieren |
| `Strg + Alt + Leertaste` | App öffnen (global) |

---

## Datenspeicherung

Alle Daten werden lokal gespeichert — keine Cloud, kein Account:

| Pfad | Inhalt |
|------|--------|
| `%LocalAppData%\DeskNotes\todo.json` | Notizen |
| `%LocalAppData%\DeskNotes\settings.json` | Fenster, Filter, Einstellungen |
| `%LocalAppData%\DeskNotes\addons\` | Addon-Einstellungen |

---

## Für Entwickler

### Voraussetzungen

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Bauen

```powershell
git clone https://github.com/kptnChr7s/DeskNotes.git
cd DeskNotes
dotnet build DeskNotes.sln -c Release
```

### Installer bauen

[Inno Setup 6](https://jrsoftware.org/isinfo.php) installieren, dann:

```powershell
.\installer\build-installer.ps1
```

Ausgabe: `installer\output\DeskNotes-Setup-x.x.x-win-x64.exe`

### Projektstruktur

```
DeskNotes/
├── DeskNotes.csproj          # WPF-Hauptanwendung
├── DeskNotes.Abstractions/   # Addon-Interfaces & UI-Helfer
├── Addons/                   # Export, Timer, Confetti, Disco
├── Core/                     # Addon-Host, Loader, EventBus
├── ViewModels/               # MVVM
├── Services/                 # Persistenz, Hotkey, Autostart
├── Themes/                   # Dark-Theme
└── installer/                # Inno Setup (Windows-Installer)
```

---

## Lizenz

MIT — siehe [LICENSE](LICENSE).