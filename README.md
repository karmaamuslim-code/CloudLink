[README (1).md](https://github.com/user-attachments/files/27093397/README.1.md)
# CloudLink
CloudLink: A Unity AR collaboration app where multiple users view and manipulate shared 3D objects in real-world space via VoIP. Uses ARCore Cloud Anchors, Photon PUN 2 for object sync, and Agora Voice SDK with FEC for reliable audio. Built by Karma Muslim &amp; Marckins Azard.
# CloudLink — Real-Time AR Collaboration Tool with VoIP Integration
## Project 3 | Team CloudLink

**Members:** Karma Muslim · Marckins Azard  
**Meetings:** April 14, April 16, April 25  
**Contributions:** Karma Muslim — original code, presentation, technical report | Marckins Azard — final edits, code testing & execution

---

## Overview
CloudLink enables two or more users to share and manipulate the same 3D AR object in physical space, while communicating via real-time voice, all over the internet.

## Tech Stack
| Layer | Technology |
|-------|-----------|
| Engine | Unity 2022.3 LTS |
| AR | AR Foundation 5.1 + ARCore XR Plugin |
| Cloud Anchors | Google ARCore Extensions |
| Networking | Photon PUN 2 |
| VoIP | Agora Voice SDK |
| Backend | Google Cloud Platform + Photon Cloud + Agora.io |

## Scripts

| Script | Responsibility |
|--------|---------------|
| `MultiplayerManager.cs` | Photon connection, room management, player callbacks |
| `CloudAnchorManager.cs` | ARCore Cloud Anchor hosting and resolving |
| `SharedObject.cs` | Networked object movement and rotation via touch |
| `VoiceManager.cs` | Agora voice channel with FEC packet-loss mitigation |
| `PerformanceMonitor.cs` | Latency, FPS, and packet-loss metrics display & CSV export |

## Setup Instructions

### Prerequisites
1. Unity 2022.3 LTS installed
2. Android SDK / iOS build support modules installed
3. Photon PUN 2 imported from Unity Asset Store (free)
4. ARCore Extensions package imported via Package Manager
5. Agora Voice SDK for Unity downloaded from Agora console

### Configuration
1. **Photon:** Window → Photon Unity Networking → PUN Wizard → enter your App ID
2. **Google Cloud:** Enable ARCore API, create API key in Google Cloud Console
3. **Agora:** Replace `YOUR_AGORA_APP_ID` in `VoiceManager.cs` with your Agora App ID

### Scene Setup
1. Add `AR Session` and `AR Session Origin` GameObjects
2. Add `ARAnchorManager` and `ARRaycastManager` components to AR Session Origin
3. Create empty GameObject "Managers" with:
   - `MultiplayerManager.cs`
   - `CloudAnchorManager.cs` (assign AR component references)
   - `VoiceManager.cs`
   - `PerformanceMonitor.cs` (assign UI Text references)
4. Create "SharedCube" prefab with `PhotonView` + `SharedObject.cs` + `PhotonTransformView`
5. Add prefab to Resources folder (required for `PhotonNetwork.Instantiate`)

## Testing
- Build to 2 Android devices
- Launch both — they auto-connect to same Photon room
- MasterClient taps screen to place cloud anchor and spawn shared cube
- Both users can drag/rotate the shared object; voice chat is active automatically
- Monitor stats via on-screen HUD (PerformanceMonitor)

## GitHub
Repository URL: https://github.com/cloudlink-team/project3-ar-collab
