@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

echo ========================================================
echo   AIM Classic 6.0 Mobile - Android APK Builder (.bat)
echo ========================================================

REM [1/7] Locate JDK 11/17+
set "JDK_DIR="
if defined JAVA_HOME (
    if exist "%JAVA_HOME%\bin\javac.exe" set "JDK_DIR=%JAVA_HOME%"
)
if not defined JDK_DIR (
    if exist "C:\Program Files\Java\jdk-17\bin\javac.exe" set "JDK_DIR=C:\Program Files\Java\jdk-17"
)
if not defined JDK_DIR (
    for /d %%D in ("C:\Program Files\Java\jdk-*" "C:\Program Files\Eclipse Adoptium\jdk-*" "C:\Program Files\Microsoft\jdk-*") do (
        if exist "%%D\bin\javac.exe" set "JDK_DIR=%%D"
    )
)
if not defined JDK_DIR (
    echo ERROR: Java JDK ^(javac.exe^) not found! Install JDK 17+: winget install Oracle.JDK.17
    if not defined AIM_NO_PAUSE pause
    exit /b 1
)
set "JAVA_HOME=%JDK_DIR%"
set "PATH=%JAVA_HOME%\bin;%PATH%"
echo [1/7] Using JDK: %JAVA_HOME%

REM [2/7] Ensure Android SDK (platforms;android-34 + build-tools;34.0.0)
set "BUILD_DIR=%~dp0android_apk\_build"
if exist "%BUILD_DIR%" rmdir /s /q "%BUILD_DIR%"
mkdir "%BUILD_DIR%\res"
mkdir "%BUILD_DIR%\assets\pwa"
mkdir "%BUILD_DIR%\gen"
mkdir "%BUILD_DIR%\classes"
mkdir "%BUILD_DIR%\dex"

echo [2/7] Checking / installing Android SDK Build Tools...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0android_apk\ensure_sdk.ps1" -OutBat "%BUILD_DIR%\sdk_env.bat"
if errorlevel 1 (
    echo ERROR: Failed to prepare Android SDK. Check internet connection and try again.
    if not defined AIM_NO_PAUSE pause
    exit /b 1
)
call "%BUILD_DIR%\sdk_env.bat"

REM [3/7] Stage resources and mobile_pwa assets
echo [3/7] Staging icons, resources, and mobile_pwa app files...
xcopy /E /I /Q /Y "%~dp0android_apk\res\*" "%BUILD_DIR%\res\" >nul
for %%M in (mdpi hdpi xhdpi xxhdpi xxxhdpi) do (
    if not exist "%BUILD_DIR%\res\mipmap-%%M" mkdir "%BUILD_DIR%\res\mipmap-%%M"
    copy /Y "%~dp0mobile_native\android\app\src\main\res\mipmap-%%M\ic_launcher.png" "%BUILD_DIR%\res\mipmap-%%M\ic_launcher.png" >nul
)
xcopy /E /I /Q /Y "%~dp0mobile_pwa\*" "%BUILD_DIR%\assets\pwa\" >nul

REM [4/7] Compile & link resources with aapt2
echo [4/7] Compiling Android resources with aapt2...
"%BUILD_TOOLS%\aapt2.exe" compile --dir "%BUILD_DIR%\res" -o "%BUILD_DIR%\res.zip"
if errorlevel 1 exit /b 1

"%BUILD_TOOLS%\aapt2.exe" link -o "%BUILD_DIR%\unaligned.apk" -I "%ANDROID_JAR%" --manifest "%~dp0android_apk\AndroidManifest.xml" --java "%BUILD_DIR%\gen" "%BUILD_DIR%\res.zip" --auto-add-overlay
if errorlevel 1 exit /b 1

REM [5/7] Compile Java sources -> classes.dex
echo [5/7] Compiling Java sources and converting to Dalvik DEX...
"%JAVA_HOME%\bin\javac.exe" -source 1.8 -target 1.8 -encoding UTF-8 -Xlint:-options -classpath "%ANDROID_JAR%" -d "%BUILD_DIR%\classes" "%BUILD_DIR%\gen\com\aimclassic\mobile\R.java" "%~dp0android_apk\src\com\aimclassic\mobile\MainActivity.java"
if errorlevel 1 exit /b 1

"%JAVA_HOME%\bin\jar.exe" cf "%BUILD_DIR%\classes.jar" -C "%BUILD_DIR%\classes" .
if errorlevel 1 exit /b 1

call "%BUILD_TOOLS%\d8.bat" --release --min-api 24 --lib "%ANDROID_JAR%" --output "%BUILD_DIR%\dex" "%BUILD_DIR%\classes.jar"
if errorlevel 1 exit /b 1

REM [6/7] Insert classes.dex + assets with forward slashes + zipalign
echo [6/7] Packaging classes.dex, assets, and aligning APK...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0android_apk\pack_dex.ps1" -ApkPath "%BUILD_DIR%\unaligned.apk" -DexPath "%BUILD_DIR%\dex\classes.dex" -AssetsRoot "%BUILD_DIR%\assets"
if errorlevel 1 exit /b 1

"%BUILD_TOOLS%\zipalign.exe" -p -f 4 "%BUILD_DIR%\unaligned.apk" "%BUILD_DIR%\aligned.apk"
if errorlevel 1 exit /b 1

REM [7/7] Sign & verify AIMClassicMobile.apk
echo [7/7] Signing AIMClassicMobile.apk...
set "KS=%~dp0android_apk\debug.keystore"
if not exist "%KS%" (
    "%JAVA_HOME%\bin\keytool.exe" -genkeypair -v -keystore "%KS%" -storepass android -alias androiddebugkey -keypass android -keyalg RSA -keysize 2048 -validity 10000 -dname "CN=AIM Classic,O=AIM,C=US" >nul 2>&1
)
call "%BUILD_TOOLS%\apksigner.bat" sign --ks "%KS%" --ks-pass pass:android --ks-key-alias androiddebugkey --key-pass pass:android --out "%~dp0AIMClassicMobile.apk" "%BUILD_DIR%\aligned.apk"
if errorlevel 1 exit /b 1

call "%BUILD_TOOLS%\apksigner.bat" verify --verbose "%~dp0AIMClassicMobile.apk"
if errorlevel 1 exit /b 1

echo.
echo ========================================================
echo   SUCCESS! Created signed Android APK:
echo   %~dp0AIMClassicMobile.apk
echo ========================================================
exit /b 0
