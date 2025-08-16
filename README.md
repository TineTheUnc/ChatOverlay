# ChatOverlay

A lightweight WPF-based overlay for displaying YouTube Live Chat messages directly on screen.  
Designed to stay on top of other windows, auto-hide when inactive, and support simple text wrapping.  

⚠️ Limitations:  
- Does **not** support YouTube sponsor-only emoji.  
- Some newer emoji may not render properly.  
- Windows only.

---

## Features

- Transparent always-on-top chat overlay.  
- Messages push older ones upward automatically.  
- Auto-hide after inactivity.  
- Word-wrapping with configurable width.  
- Easy deployment using **Velopack**.  

---

## Installation

Download from the [Releases](https://github.com/TineTheUnc/ChatOverlay/releases) page.  
We provide two Windows-only options:  

1. **Setup Installer (recommended)**  
   - Automatically installs and updates via Velopack.  
   - Adds shortcuts to Start Menu.  

2. **Portable Version**  
   - Extract and run `ChatOverlay.exe`.  
   - No installation required.  

---

## Usage

1. Import your `client_secret.json`.  
2. Authorize your YouTube account.  
3. Enter the **Live Chat ID** of your stream.  
4. Press **Start** to begin fetching messages.  
5. The overlay will show messages on screen, auto-hiding when inactive.  

---

## Development

Clone the repository:

```bash
git clone https://github.com/TineTheUnc/ChatOverlay.git
cd ChatOverlay
```
Build with Visual Studio 2022 (WPF project, .NET 6+).
---

## 📜 License

MIT License. See [LICENSE](LICENSE) for details.
