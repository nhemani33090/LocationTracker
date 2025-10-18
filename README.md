# Location Tracker - Heat Map App

A cross-platform mobile app built with C# and .NET MAUI that tracks user location and displays it as a heat map.

## Features

- Real-time location tracking (every 5 seconds)
- Interactive heat map visualization
- SQLite database storage
- Cross-platform (Android, iOS, Windows, macOS)
- Color-coded density: 🔵 Blue (low) → 🟡 Yellow (medium) → 🔴 Red (high)

## Technology Stack

- **.NET MAUI 9.0**
- **C# 11**
- **SQLite** (sqlite-net-pcl)
- **Microsoft.Maui.Controls.Maps**
- **Android**: API 21+ (Android 5.0 Lollipop and above)
- **iOS**: 12.2+
- **Windows**: 10.0.17763.0+

## Setup

### Prerequisites

- Visual Studio 2022 with .NET MAUI workload
- .NET 9 SDK
- Platform-specific requirements:
  - **Android**: Android SDK
  - **iOS**: Xcode 14+ (macOS only)
  - **Windows**: Windows 10 SDK

### Installation

1. Clone the repository:
```bash
   git clone https://github.com/nhemani33090/LocationTracker.git
   cd LocationTracker
```

2. **Add Google Maps API Key** (Android only):
   - Get API key from [Google Cloud Console](https://console.cloud.google.com/)
   - Enable "Maps SDK for Android"
   - Open `Platforms/Android/AndroidManifest.xml`
   - Replace `YOUR_API_KEY_HERE` with your actual key:
```xml
     <meta-data 
         android:name="com.google.android.geo.API_KEY" 
         android:value="YOUR_ACTUAL_KEY" />
```

3. Restore packages:
```bash
   dotnet restore
```

4. Build and run:

   **For Android:**
```bash
   dotnet build -t:Run -f net9.0-android
```
   
   **For iOS** (macOS only):
```bash
   dotnet build -t:Run -f net9.0-ios
```
   
   **For Windows:**
```bash
   dotnet build -t:Run -f net9.0-windows10.0.19041.0
```
   
   **For macOS:**
```bash
   dotnet build -t:Run -f net9.0-maccatalyst
```

   **Or use Visual Studio:**
   - Open `LocationTracker.sln`
   - Select target platform from dropdown
   - Press **F5** to run

## Usage

1. Launch app and grant location permissions
2. Tap **🚀 Start Tracking**
3. Move around to see heat map build up
4. Use **🔄 Refresh** to reload or **🗑️ Clear Data** to reset

### Heat Map Colors

- **Blue**: Low frequency (passing through)
- **Yellow/Orange**: Medium frequency
- **Red**: High frequency (time spent)

### Simulating Location (For Testing)

**Android Emulator:**
1. Click **"..."** (Extended controls) on emulator toolbar
2. Go to **Location** tab
3. Enter latitude/longitude coordinates
4. Click **"Send"** to update location

**iOS Simulator:**
1. Go to **Debug** → **Location**
2. Select **Custom Location**
3. Enter coordinates or choose predefined route

## Project Structure
```
LocationTracker/
├── Models/LocationPoint.cs          # Data model
├── Services/
│   ├── DatabaseService.cs           # SQLite operations
│   └── LocationService.cs           # GPS tracking
├── Controls/HeatMapOverlay.cs       # Heat map visualization
├── MainPage.xaml[.cs]               # Main UI
└── Platforms/Android/AndroidManifest.xml  # Permissions & API key
```

## Troubleshooting

**Map not showing (Android)?**
- Check internet connection
- Verify API key is correct
- Ensure "Maps SDK for Android" is enabled in Google Cloud Console

**Permissions denied?**
- Go to device Settings → Apps → LocationTracker
- Grant location permissions

**Build errors?**
- Run `dotnet clean`
- Run `dotnet restore`
- Rebuild the project

---

**Security Note**: API key removed from public repository. Add your own key following setup instructions above.