# AIM Classic 6.0 Mobile (Android `.apk` & Apple iPhone `.ipa`)

Native cross-platform Flutter project sharing the exact `ntfy.sh` SHA-256 global room, 1-on-1 messaging, public groups, AI bots (`Argon`, `Hydrogen`, `Astra`, `Gemini`, `GPT`), and Frutiger Aero / XP Luna styling with `AIMClassic.exe`.

## Option 1: Instant Phone Install from PC (Zero Tools Required)
Run `AIMClassic.exe` -> open **File -> AIM Mobile App (Android & iPhone)...** (or click **Phone / Моб.**), connect your phone to the same Wi-Fi, and scan the QR code:
- **Android (Chrome):** Menu (`⋮`) -> **Install app**
- **Apple iPhone (Safari):** Share (`□↑`) -> **Add to Home Screen**

## Option 2: Automated Cloud Build (`.apk` & `.ipa` via GitHub Actions)
Push this repository to GitHub. The workflow in `.github/workflows/build-mobile.yml` automatically builds:
- `AIMClassicMobile-Android-APK` (`app-release.apk` for sideloading on Android)
- `AIMClassicMobile-iOS-IPA` (`AIMClassicMobile-unsigned.ipa` for Sideloadly / AltStore / Xcode signing on iPhone)

## Option 3: Local Build (if Flutter SDK + Android Studio / Xcode are installed)
```bash
cd mobile_native
flutter create --org com.aimclassic --project-name aim_classic_mobile --platforms=android,ios .
flutter pub get
flutter build apk --release
```
