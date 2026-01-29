# Hackathon VR - Unity 6000.0.36f1

Project for the VR Hackathon.

## Team
- Narjisse
- Cheikhou
- Florian

## Quick Start 🚀

1. **Clone the project**
2. **Open with Unity Hub** → Select Unity **6000.0.36f1**
3. **Connect your VR headset** (Quest Link, SteamVR, etc.)
4. **Open** `Assets/Scenes/VRScene.unity`
5. **Press Play** → VR should work immediately!

## VR Requirements

- **Unity 6000.0.36f1** (exact version required)
- **VR Headset**: Oculus Quest (via Link/AirLink), HTC Vive, Valve Index, WMR
- **VR Runtime**: Oculus App or SteamVR running in background

## What's Included

### Packages (auto-installed)
- XR Plug-in Management 4.5.3
- OpenXR Plugin 1.16.0
- XR Interaction Toolkit 3.3.0
- Oculus XR Plugin 4.5.2

### Scene Content
When you launch VRScene, you get:
- 🎮 **XR Rig** with head & controller tracking
- 🏢 **Room** with 4 walls
- 🟥 **Floating cubes** (animated)
- 🏛️ **Pillars** at corners
- 🔵 **Glowing spheres**
- 🪑 **Table**
- 💡 **Atmospheric lighting**

### VR Scripts
| Script | Description |
|--------|-------------|
| `XRSetup.cs` | Creates XR Rig + decor at runtime (main script) |
| `VRInitializer.cs` | Manages XR system startup |
| `HandPresence.cs` | Controller tracking & haptics |
| `VRLocomotion.cs` | Movement & snap turn |

### Interaction System (NEW!)
| Script | Description |
|--------|-------------|
| `VRGrabInteractable.cs` | Makes objects grabbable with physics throwing |
| `VRGrabber.cs` | Allows VR hands to grab objects |
| `VRSocketInteractor.cs` | Zones where objects can be placed (puzzles!) |
| `VRPointer.cs` | Laser pointer for distant interactions |
| `VRButton.cs` | Pressable VR buttons with feedback |
| `VRInteractionSetup.cs` | Auto-setup utility + test helpers |

## Troubleshooting

### VR doesn't start
1. Ensure **Oculus App** or **SteamVR** is running
2. Check headset is connected and recognized
3. In Unity: Edit → Project Settings → XR Plug-in Management → PC
   - ✅ OpenXR should be checked
   - ❌ Mock Runtime should be **unchecked** in OpenXR settings

### Head/controllers not tracking
- Make sure OpenXR runtime is active (SteamVR or Oculus)
- Check Console for error messages
- Restart Unity and VR software

### Performance issues
- Target 72-90 FPS for comfortable VR
- Reduce Quality settings if needed
- Use Single Pass Instanced rendering

## Project Structure

```
Assets/
├── Scenes/
│   └── VRScene.unity      # Main VR scene
├── Scripts/
│   └── VR/
│       ├── XRSetup.cs     # Auto-setup XR + decor
│       ├── VRInitializer.cs
│       ├── HandPresence.cs
│       └── VRLocomotion.cs
└── XR/
    ├── Settings/          # OpenXR configuration
    └── Loaders/           # XR loaders
```

## OpenXR Configuration (already set)

The project is pre-configured with:
- ✅ Oculus Touch Controller Profile enabled
- ✅ Mock Runtime disabled
- ✅ Foveated Rendering enabled
- ✅ Runtime Debugger enabled

**No manual configuration needed** when cloning!
