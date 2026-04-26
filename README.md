[README (2).md](https://github.com/user-attachments/files/27093492/README.2.md)

# CloudLink — Real-Time AR Collaboration Tool with VoIP Integration
## Project 3 | Team CloudLink

**Members:** Karma Muslim · Marckins Azard  
**Meetings:** April 14, April 16, April 25  
**Contributions:** Karma Muslim — original code, presentation, technical report | Marckins Azard — final edits, code testing & execution

---

## Overview
CloudLink enables two or more users to share and manipulate the same 3D AR object in physical space, while communicating via real-time voice, all over the internet.

## Platform Configuration
| Role | Platform |
|------|----------|
| Development machine | HP Windows PC |
| Build target / test device | Apple iPhone / iPad (iOS 16+) |

> **Note on iOS Builds from Windows:** Unity can generate an Xcode project on Windows, but Xcode (required to sign and deploy to iOS) only runs on macOS. Workflow: develop and generate the iOS build in Unity on your HP → transfer the Xcode project folder to a Mac → open in Xcode → connect iPhone → build & run. If you don't have a Mac available, use a cloud Mac service (MacStadium, GitHub Actions macOS runner, etc.) or a friend's Mac just for the deploy step.

## Tech Stack
| Layer | Technology |
|-------|-----------|
| Engine | Unity 2022.3 LTS |
| AR | AR Foundation 5.1 + ARKit XR Plugin (iOS) |
| Spatial Anchors | Azure Spatial Anchors SDK |
| Networking | Photon PUN 2 |
| VoIP | Agora Voice SDK |
| Backend | Microsoft Azure + Photon Cloud + Agora.io |

> **Why the change from ARCore?** ARCore and its Cloud Anchors are Android-only. To test on Apple devices, the project now uses **ARKit** (Apple's AR framework, integrated via AR Foundation) and **Azure Spatial Anchors**, which supports both iOS and Android and serves the same purpose as ARCore Cloud Anchors.

## Scripts

| Script | Responsibility |
|--------|---------------|
| `MultiplayerManager.cs` | Photon connection, room management, player callbacks |
| `CloudAnchorManager.cs` | Azure Spatial Anchor hosting and resolving (iOS-compatible) |
| `SharedObject.cs` | Networked object movement and rotation via touch |
| `VoiceManager.cs` | Agora voice channel with FEC packet-loss mitigation |
| `PerformanceMonitor.cs` | Latency, FPS, and packet-loss metrics display & CSV export |

---

## Setup Instructions

### Prerequisites (HP Windows PC)

1. **Unity 2022.3 LTS** — download from [unity.com](https://unity.com/download)
   - During install, add modules: **iOS Build Support** and **Windows Build Support**
   - Do NOT install Android Build Support (not needed for iOS testing)
2. **Photon PUN 2** — free from Unity Asset Store
3. **Azure Spatial Anchors SDK** — imported via Unity Package Manager (see below)
4. **ARKit XR Plugin** — imported via Unity Package Manager (see below)
5. **Agora Voice SDK for Unity** — downloaded from Agora console

### Unity Package Manager Setup

Open Unity → Window → Package Manager → Add package by name:

```
com.unity.xr.arfoundation               (AR Foundation 5.1)
com.unity.xr.arkit                      (ARKit XR Plugin — iOS)
com.microsoft.azure.spatial-anchors-sdk.unity
```

> Remove `com.unity.xr.arcore` and `Google ARCore Extensions` if previously installed — they are Android-only.

### Configuration

#### 1. Photon
Window → Photon Unity Networking → PUN Wizard → enter your App ID from [dashboard.photonengine.com](https://dashboard.photonengine.com)

#### 2. Azure Spatial Anchors
- Create a free Azure account at [portal.azure.com](https://portal.azure.com)
- Create a **Spatial Anchors** resource (search "Spatial Anchors" in Azure Portal)
- Go to resource → Keys → copy **Account ID**, **Account Domain**, **Primary key**
- Paste all three into the `CloudAnchorManager` Inspector fields in Unity

#### 3. Agora
- Create account at [agora.io](https://www.agora.io)
- Replace `YOUR_AGORA_APP_ID` in `VoiceManager.cs` with your Agora App ID

### iOS Build Settings (Unity on HP)

1. File → Build Settings → select **iOS** → Switch Platform
2. Player Settings → Other Settings:
   - **Bundle Identifier:** `com.yourname.cloudlink`
   - **Camera Usage Description:** `Required for AR`
   - **Microphone Usage Description:** `Required for voice chat`
   - **Target minimum iOS version:** 16.0
3. Player Settings → XR Plug-in Management → iOS tab → check **ARKit**
4. File → Build Settings → **Build** (not Build and Run) → save Xcode project to a folder

### Deploying to iPhone (Mac required for this step only)

1. Transfer the generated Xcode project folder to a Mac (USB drive, AirDrop, GitHub, etc.)
2. Open `Unity-iPhone.xcodeproj` in Xcode
3. Connect your iPhone via USB — trust the computer on the phone
4. Select your device in the Xcode toolbar → Product → Run
5. If signing errors appear: Xcode → Signing & Capabilities → select your Apple ID team

---

## Scene Setup

1. Add `AR Session` and `AR Session Origin` GameObjects (via GameObject → XR menu)
2. Add `ARAnchorManager` and `ARRaycastManager` components to **AR Session Origin**
3. Create empty GameObject **"Managers"** and attach:
   - `MultiplayerManager.cs`
   - `CloudAnchorManager.cs` — assign AR component references + Azure credentials in Inspector
   - `VoiceManager.cs`
   - `PerformanceMonitor.cs` — assign UI Text references
4. Create **"SharedCube"** prefab with `PhotonView` + `SharedObject.cs` + `PhotonTransformView`
5. Add the prefab to the **Resources** folder (required for `PhotonNetwork.Instantiate`)

---

## Testing

- Build to 2 iOS devices (or 1 iOS + 1 Android — Azure Spatial Anchors is cross-platform)
- Launch both — they auto-connect to same Photon room
- **MasterClient:** tap the screen on a detected surface to place anchor and spawn shared cube
  - Move the phone slowly around the anchor for ~5 seconds while it scans (required by ASA)
- **Other device(s):** app will receive the anchor ID and resolve it automatically
- Both users can drag/rotate the shared object; voice chat activates automatically
- Monitor metrics via on-screen HUD (PerformanceMonitor)

---

## Troubleshooting

| Issue | Fix |
|-------|-----|
| "ARKit not available" at runtime | Confirm ARKit XR Plugin is enabled in XR Plug-in Management → iOS |
| Azure anchor hosting fails | Ensure you moved the device slowly for ~5 sec after tapping to scan the environment |
| Azure anchor resolve times out | Make sure both devices are looking at roughly the same physical area |
| Xcode signing error | In Xcode → Signing & Capabilities → set Team to your Apple ID |
| Camera/mic permission denied | Check `Info.plist` has `NSCameraUsageDescription` and `NSMicrophoneUsageDescription` (Unity sets these via Player Settings) |

---

## GitHub
Repository URL: https://github.com/cloudlink-team/project3-ar-collab
