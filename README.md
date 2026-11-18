# AIM Classic 6.0 — Desktop + Android APK + Mobile PWA

Classic 2005–2008 AOL Instant Messenger (AIM) reimagined for modern **Windows**, **Android**, and **Apple iPhone**, featuring **Frutiger Aero (2007)** & **Windows XP Luna** themes, global P2P/cloud chat, public group rooms, retro music player, AOL Mail, theme workshop, and AI bots.

## Downloads / Ready Binaries
- **Windows Desktop Client:** `AIMClassic.exe` & Installer `AIM_Setup.exe` (EN / RU language selection)
- **Android App (`.apk`):** `AIMClassicMobile.apk` (built via `build_apk.bat` — copy to Android 7.0+ phone and install)
- **iPhone & Browser PWA (Zero-Install):** Open **Phone / Моб.** inside `AIMClassic.exe` and scan the Wi-Fi QR code on Android Chrome or iPhone Safari (**Add to Home Screen**)

## Key Features
- **Real Friends & Global Cloud (`ntfy.sh` SHA-256 + LAN/TCP Relay):** Add and chat with real PC, Android, and iPhone friends worldwide in shared Global Rooms
- **Global Public Groups Hub:** Browse, create, and chat live in `#Global_AIM_Plaza`, `#FrutigerAero_Hub`, `#AIM_Lobby_2006`, `#AI_Work_Lab`
- **AIM Theme Workshop & Rotating Banners:** 7 built-in themes (*Frutiger Aero*, *XP Luna Blue*, *XP Royale*, *AIM 6.0 Triton Ice*, *XP Silver*, *XP Homestead Olive*, *Y2K Cyber Neon*), Custom Theme Creator Studio (`.aimtheme.json` export/import), 16 classic 2005–2008 avatars, and 19 authentic AIM `.wav` sounds
- **AOL/AIM Mail & Music Player:** Read/compose global AIM Mail and play built-in synth tracks or local MP3/WAV files with mood-status sync
- **AI Assistants & Local Offline Brain:** Supports Gemini, OpenAI ChatGPT, OpenRouter, DeepSeek, Grok, Groq, or automatic local Ollama setup when no API key is configured
- **Support Diagnostics & Update Center:** System diagnostics, connection guide, support ticket generator, and update manifest checker

## Building From Source
- **Windows PC (`.NET 4.0` C# Compiler):** run `powershell -ExecutionPolicy Bypass -File build.ps1`
- **Android APK (`build_apk.bat`):** run `build_apk.bat` on Windows (requires JDK 17; auto-downloads official Android SDK Build-Tools)
- **Cloud Native Builds (`.apk` & unsigned `.ipa`):** automated in `.github/workflows/build-mobile.yml` from `mobile_native/`
