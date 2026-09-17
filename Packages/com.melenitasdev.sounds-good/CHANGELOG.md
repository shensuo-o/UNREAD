# Changelog

All notable changes to this project are documented in this file.

This project follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) format and uses [Semantic Versioning](https://semver.org/).

---

## [2.2.2] – 2026-08-29

### Changed
- Music, playlists and dynamic music now ignore `Time.timeScale` by default, so pausing the game no longer silences them. Sound effects still follow it.

### Fixed
- Playlists with a fade in or a fade out played silently.
- Pausing during a fade left the sound quieter for good.
- The 3D demo scene failed to compile on projects using the new Input System. It now runs on either one, with no action map to set up.

### Added
- **Sounds Good Lite**, a free edition, is now on the Asset Store. Projects that start there can upgrade without losing anything.
- Sounds Good now spots the Lite-only files an upgrade leaves behind and offers to remove them.

---

## [2.2.1] – 2026-08-26

### Changed
- The 3D demo scene's baked lighting was rebaked at a lower resolution, taking the sample's lightmap data from around 270 MB down to about 30 MB. The scene looks the same; the download is a lot smaller.

### Fixed
- Fixed **components reverting to their pre-2.2 version after updating from an older release**. A .unitypackage never deletes files, so the scripts and prefabs renamed in 2.2.0 stayed in the project alongside their replacements. Because the renames deliberately kept their GUIDs, both copies then claimed the same one and Unity loaded whichever it had seen first, which was always the old one — a Music Zone, for instance, kept showing its old Inspector and ignored the new custom editor. Sounds Good now spots those leftovers on load and offers to remove them, after which every scene and prefab resolves to the current version on its own.
- Fixed the **3D demo scene rendering unlit on the Universal Render Pipeline**. Unity 6 samples lightmaps from a texture array by default, and only falls back to the classic single-texture binding while the `USE_LEGACY_LIGHTMAPS` keyword is on. The demo shaders declared the other lightmap keywords but not that one, so the variant URP needed did not exist and every lightmapped surface sampled an array that was never bound — the baked lighting was there, it just never reached the screen. Affected `SG_Standard` and `SG_Triplanar`.
- Fixed **`SG_Triplanar` ignoring baked lighting entirely** on URP. It declared the lightmap keywords but only ever sampled light probes, so anything using it was lit by ambient alone. It now carries the lightmap UV through the vertex stage and samples the lightmap like the rest of the demo shaders.
- Fixed the **demo shaders not being set up for URP on a read-only install**. Writing to a package that Unity installed read-only raises `UnauthorizedAccessException`, which does not derive from `IOException` and so escaped the handler — aborting the setup partway through and leaving the remaining shaders untouched. Each shader now fails on its own with a clear warning instead of taking the rest down with it.
- Fixed **a few transparent materials in the 3D demo scene rendering wrong on URP**. They were still on shaders with no Universal Render Pipeline support, so they now use the demo's own dual-pipeline `SG_Standard`, which renders correctly on both pipelines.
- Fixed the **3D demo scene's music particles rendering as solid magenta on URP**. They used Unity's `Particles/Alpha Blended`, which only exists on the Built-In pipeline, so URP had no shader to draw them with. They now use a new dual-pipeline `SG_ParticleUnlit` that reads the particle system's vertex colour, so tints and alpha-over-lifetime still work.
- `SG_Standard` materials now have a **Surface Type** dropdown. Transparency was already supported, but only by setting the blend modes, depth write and render queue by hand — and missing the render queue left a material blended yet still drawn in the opaque pass, which looks like transparency simply not working. Picking Transparent now sets all four.
- Fixed **three demo sound clips shipping outside the package**. The footstep and dialogue sounds used by the 3D demo scene lived in the project's own `Assets/` folder rather than in the sample, so they were missing from the published package and their emitters played nothing.

---

## [2.2.0] – 2026-08-22

### Added

**Audio effects**
- New **Effect Creator** window to design reusable audio effect presets — reverb, echo, chorus, distortion, low pass, high pass, volume and pitch — with a live preview.
- Apply effects to any audio with `SetEffect(Effect effect, float intensity)` on Sound, Music, Playlist and Dynamic Music. The intensity blends the effect gradually, from none (0) to exactly as saved (1).
- `SetEffect(string effectTag, float intensity)` overload to reference effects by tag, so a call keeps compiling even if that effect is later removed.
- **Audio Effect Zone** component — applies an effect to sounds inside a spherical or box zone, fading it across the zone's margin. Choose what it reaches: **Sources** (sounds inside, heard with the effect from anywhere), **Listener** (everything while the listener is inside, e.g. going underwater), or **Both**. Box zones follow the object's rotation and support an independent fade distance per face (uniform by default). Only 3D sounds are affected, so 2D music and UI stay dry, and reverb/echo tails ring out naturally instead of cutting off.
- **Global and per-Output effects** from code: `SoundsGoodManager.SetGlobalEffect(...)`, `SetOutputEffect(Output, ...)` and their `Remove...` counterparts (also with a string effect tag). Apply an effect to every sound, or just one Output, at runtime with a fade — ideal for muffling everything on pause (keeps working while the game is paused).

**Emitters — configure audio from the Inspector**
- New **Sound Emitter**, **Music Emitter**, **Playlist Emitter** and **Dynamic Music Emitter** components. Configure a Sound/Music/Playlist/Dynamic Music entirely from the Inspector — the same options as the code API — and drive playback with Play/Pause/Resume/Stop. Each exposes its underlying audio object so you can call any `Set*` method (or blend Dynamic Music layers) at runtime, fires its callbacks as UnityEvents, and offers test buttons in Play Mode.

**Other no-code components**
- **Audio Trigger** — plays/stops the emitter on the same GameObject from physics, lifecycle or manual/UnityEvent triggers, with layer/tag filtering, cooldown and play-once.
- **Music Zone** — fades an emitter's music in and out based on the listener's position inside a spherical or box zone (same per-face fade and rotation support as the effect zone).
- **Random Ambience** — plays random sounds at random intervals (birds, drips, creaks), from a fixed point, within a radius, or following the object.
- **Collision Sound** — plays a sound on impact, scaling volume with the collision strength.
- A custom **component icon** for every Sounds Good component, so they stand out in the Hierarchy and Project.

**New audio API**
- `SetRandomVolume()` and `SetRandomVolume(float min, float max)` on Sound, Music and Playlist — pick a fresh random volume on each play, like the existing random pitch.
- `SetRandomPitch(float min, float max)` overload on Sound.
- `ChangeEffect(...)` on Sound, Music, Playlist and Dynamic Music — change an effect's intensity (or swap the effect) while playing, optionally over a lerp time, so you can fade effects in and out at runtime (like `ChangeVolume`/`ChangePitch`).
- **Doppler Level** control on the spatial audio objects and components.

**Time scaling**
- New **Time Mode** option on every audio object (`SetTimeMode(TimeMode.Scaled / TimeMode.Unscaled)`), emitter and no-code component. **Scaled** (the default) makes the audio follow `Time.timeScale`: it speeds up and finishes sooner in fast motion, slows down in slow motion, and freezes on pause, so it stays in sync with game time. **Unscaled** keeps it playing at normal speed regardless of the time scale — ideal for menus, UI, or music that must keep going while the game is paused.

**Occlusion settings**
- Exposed the occlusion **depth** controls in the Settings Window: **Max Occlusion Layers**, **Extra Depth Volume Multiplier**, and **Extra Depth Cutoff Multiplier** (previously only editable in the raw settings asset).
- Added a scroll view to the Settings Window so it stays usable at smaller sizes, and a minimum size to every editor window.

**Occlusion materials**
- New **Occlusion Material Creator** window to author reusable occlusion materials — each with a **density** from 0 (sound passes through) to 1 (fully blocked) — with a live audio preview of how muffled a fully occluded sound will be at that density.
- New **Audio Occlusion Surface** component: put it on any object with a collider and pick a reusable **Material** or a **Custom** density, so a thin window occludes less than a dense concrete wall instead of every collider blocking the same. An optional **Include Children** toggle covers a whole structure's colliders with one component.
- Occlusion raycasts now weight each hit by the surface's density (direct, bounce and depth). Stacked surfaces combine the way real ones do — each only lets `(1 - density)` through, so two 50% windows block 75%, not 100%. A collider without the component keeps blocking fully, so existing scenes are unaffected.
- Ships with four ready-made example materials (Concrete, Metal, Wood, Glass), copied into your project on first use.

**Audio outputs**
- Outputs are now created **automatically**: type a name in the Output Manager, press CREATE, and the mixer group is added under Master with its volume already exposed under that same name. This replaces the five manual steps in the Audio Mixer (add child group, rename, expose the volume fader, rename the exposed parameter to match) — the exposed parameter name is the part that had to match the group exactly, and getting it wrong left the output silent.
- Outputs can also be **renamed and deleted** from the window, with the group and its exposed parameter always kept in sync. Master is left alone, and both actions warn that `Output.YourName` references in your code will stop compiling.
- The manual guide is still there as a fallback: if a future Unity version changes the editor internals this relies on, the window detects it and shows the step-by-step instructions instead of failing.

**Dynamic music**
- Dynamic Music now has a **master volume** on top of the per-track ones: `SetMasterVolume(volume)` before playing, `ChangeMasterVolume(volume, lerpTime)` while it plays, and a `MasterVolume` property. It scales every layer at once, so the general volume can go up or down without disturbing the blend you've set between layers.
- The **Dynamic Music Emitter** exposes it as a *Master Volume* slider, live while playing, plus `MasterVolume` and `SetMasterVolume(volume, fadeTime)` for code.

**Audio latency**
- New **Audio Silence Trimmer** window (`Tools > Sounds Good > Audio Silence Trimmer`) for sounds that feel like they play a moment late. It measures the silence at the start of every registered clip, lists what it found, and rewrites the ones you pick as trimmed WAVs — repointing the Sound and Music collections and keeping each clip's compression preset. That silence usually isn't yours: an MP3 always carries the encoder's padding plus the decoder's own delay, and Unity bakes both into the audio it decodes at import, so a gunshot can start tens of milliseconds after you asked for it.
- Converting to WAV and trimming are separate options, because a compressed file can't be trimmed without re-encoding it (which puts the padding straight back). Trimming is off by default for music tracks: dynamic music layers stay in sync because they start at the same sample, so trimming each one by a different amount would desync the mix.
- The silence threshold is configurable (default -60 dBFS), since a fade, dither or a lossy codec leave a very quiet floor instead of true digital zero. Originals are kept unless you confirm deleting them.
- **Keep Before Attack** (0-20 ms, default 1) sets how much of the original silence is left in front of the first audible sample. It's a guard rather than padding — cutting exactly on the transient starts the file mid-waveform, which clicks — so raise it when a soft attack that begins under the threshold is getting clipped.
- Exposed Unity's **DSP Buffer Size** in the Settings window, under a new *Latency* section, with a live readout of what the chosen size costs in milliseconds. It's the single biggest factor in how long a sound takes to be heard after `Play()`, and Unity's default (*Best performance*) adds around 40 ms — enough to feel out of step with a visual effect. It's a project setting, so the window reads and writes Unity's own value rather than keeping a copy of it.

### Changed
- Component inspectors were redesigned to match the Sounds Good editor windows (UI Toolkit, with an IMGUI fallback for assets that customize the inspector), now with **foldable sections** and a tooltip on every option. Every no-code component exposes the full audio configuration: hear distance (min/max), volume rolloff curve (Logarithmic/Linear/Custom), audio effect + intensity, fixed pitch, doppler and occlusion. Options that only affect 3D audio hide automatically when **Spatial Sound** is off.
- Component inspectors now support **multi-object editing**: select several Sounds Good components at once and configure them together (previously the inspector showed "Multi-object editing not supported").
- Numeric fields in the inspectors can be **scrubbed by dragging their label** (like Unity's native fields) and no longer clamp a value while you are still typing it.
- Numeric fields now round to **2 decimals** and, wherever there's a meaningful value to go back to, carry the same **reset button** the sliders have. No more `0.30000001` left behind by a drag.
- The remaining stock Unity fields in the inspectors (integers like *Clip Index*, and ranges like *Volume Range* and *Pitch Range*) were replaced with matching Sounds Good controls, so every row now shares the same layout, label scrubbing and reset button. Range fields label their two numbers **Min** and **Max** instead of X and Y.
- Sliders in the inspectors now have a **reset button** to restore the default value, and you can **click the number to type an exact value**.
- Components draw helpful **gizmos when selected**: min/max hear-distance spheres on emitters, and the zone plus its fade margin on the effect and music zones.
- Creating a component from the *GameObject ▸ Sounds Good* menu now makes a **plain copy** (not a linked prefab instance), so editing one object and applying overrides can never change the others.
- The *Add Component* and *GameObject ▸ Sounds Good* menus are reorganized, with **Emitters** and **Effects** shown first.
- Occlusion is now **gradual** instead of nearly all-or-nothing: direct occlusion is sampled with several rays spread perpendicular to the line of sight, so partial obstacles produce partial muffling.
- Occlusion **depth** (stacked walls/rooms) is now measured and applied continuously, adding extra attenuation and filtering the more obstacles sit between the listener and the source.
- A sound spawned already behind obstacles now **starts occluded** instead of fading in from a clear state.
- `SetOcclusion(true)` no longer forces a sound into 3D spatial mode when occlusion is globally disabled.
- The **Settings asset** is now stored in your project (`Assets/SoundsGood/Resources/`) instead of inside the package, so your configuration survives package updates and works on read-only installs.
- Audio data references (Sound/Music/Output collections, generated enums and the master mixer) are now kept in a dedicated **runtime data asset** in your project, decoupled from the package. This keeps them safe across updates and correctly included in builds.
- Editor menus moved from `Tools/Melenitas Dev/Sounds Good/` to `Tools/Sounds Good/`.
- The demo scenes now render on the **Universal Render Pipeline** as well as Built-In. The five demo shaders gained a URP SubShader, and the demo materials moved off Unity's Standard shader onto a new dual-pipeline `Melenitas Dev/SG_Standard` (flat colour or base map, opaque or transparent). Nothing is required of you: Sounds Good detects whether the URP package is installed and sets the demo shaders up accordingly, re-running by itself if you add or remove URP later.
- The demo scenes are now named after **how** they use Sounds Good rather than what they show: `SG_Demo2D (with code)` drives everything from the code API, and `SG_Demo3D (with emitters)` uses the no-code components. Their menu entries are now `Tools > Sounds Good > Open 2D Demo Scene (with code)` and `Open 3D Demo Scene (with emitters)`, so it's clear which workflow each one demonstrates.
- Renamed the package display name back to **Sounds Good** (dropped the "2.0").
- Runtime components no longer use the `SG_` prefix (now reserved for the demo/sample scripts) and are grouped under a **Sounds Good/** submenu in both the *Add Component* and *GameObject* menus. Existing scenes and prefabs keep working (references are kept by GUID).

### Fixed
- Fixed audio references potentially ending up **null in a build**: the pre-build step now resolves *and* saves the references before building, instead of only logging them.
- Fixed a sound staying **muted** after a `Pause` with fade-out followed by a `Resume` with fade-in.
- Optimized occlusion raycasts to avoid per-check memory allocations, and stopped evaluating occlusion while a source is paused.
- Fixed a possible division-by-zero when **Max Occlusion Layers** was set to 1.
- `Reset to Defaults` in the settings now restores all occlusion values (including the depth settings) with correct defaults.
- Fixed **playlists changing track at the wrong moment when the pitch isn't 1**. The track change was scheduled from the clip's length, which is its duration at normal speed, while the countdown ran in real time: a sped-up track left a silence before the next one started, and a slowed-down one was cut off early. The same mismatch made fade-outs start late or early on pitched sounds. Timing now comes from the live playback position, so it also corrects itself if you change the pitch mid-track.
- Fixed a playlist's **`OnLoopCycleComplete`** callback never firing on a playlist with more than one track, and firing every frame on a single-track one. The check compared the position inside the current clip against the total length of every clip. The cycle is now counted in tracks, so it fires once, exactly when the playlist wraps back to its first track.
- Fixed **`OnLoopCycleComplete`** firing on every frame for a looping Sound or Music instead of once per pass. Its guard was inverted, so the condition that should have skipped the frames in the middle of a cycle skipped nothing.
- Fixed **uninstalling the package leaving the project with compile errors**. The generated pseudo-enum files live in your Assets so updates cannot wipe them, but they declare half of a type whose other half ships inside the package, so removing Sounds Good left them declaring a type they could no longer build. They are now written behind a compilation guard that only holds while the package is installed, so uninstalling leaves them inert instead of breaking the project. Existing projects get their generated files rewritten automatically on the first script reload after updating.
- Removed a stray console log that printed one line per output every time the outputs were reloaded.
- The editor no longer runs its data-setup check on every editor frame. Creating and migrating the Sounds Good data folders is initialization, so it now runs once per script reload (and again when you change the data path in the settings), instead of querying the AssetDatabase continuously in the background.

### Removed
- Removed the redundant `Create > Sounds Good > Settings` asset menu; the settings asset is created and managed automatically.

---

## [2.1.1] – 2025-11-25

### Fixed
- Fixed a critical issue where serialized SFX, Track, or Output fields left intentionally as `Null` would internally store an empty string instead of the `"Null"` value.
- This led to runtime errors when attempting to access clips or properties from these audio types.
- Updated the core SFX, Track, and Output classes to ensure they can no longer produce or accept empty string values. Their default serialized state is now always `Null`, preventing invalid states and ensuring safe runtime behavior.

---

## [2.1.0] – 2025-11-21

### Added
- Full sound occlusion system with real-time evaluation and demo integration.
- New **Sounds Good Settings Window** for global configuration.
- Ability to change the **Data Root Path** where all Sounds Good assets are stored.
- `ChangePitch(float)` method to modify the pitch while audio is playing.
- New property to query the current pitch of any sound.
- Search bar for SFX, Track, and Output selection fields in the Inspector (supports both UI Toolkit and IMGUI).
- Option to select **Null** in auto-generated enums (SFX, Track, Output).
- New demo scene showcasing the occlusion system.

### Changed
- All `SetX(bool)` methods now default their boolean parameter to `true`.
- Auto-generated enums (SFX, Track, Output) are now alphabetically sorted when serialized.
- Improved internal validation and folder creation logic when modifying the Data Root Path, preventing invalid paths and infinite folder generation.
- The Version Upgrader window for migrating from 1.0 → 2.0 has been hidden.
- General improvements to internal documentation and code readability.
- Updated marketing materials, including the official update trailer.

---

## [2.0.3] – 2025-11-10

### Fixed
- Fixed an issue that could occur after building the game, where the `Asset Locator` might have missing references to one or more of its required collections (such as Sounds, Music, or Outputs).
- Added a new class that triggers a callback right before the build process, automatically ensuring that all necessary references are properly assigned.
- This fix prevents null reference errors in runtime builds caused by incomplete `Asset Locator` references.

---

## [2.0.2] – 2025-06-25

### Fixed
- Fixed a bug where the `SetClipByIndex` method on music objects had no effect.
- Fixed a compatibility issue with Odin Inspector, VInspector, and similar tools that prevented SFX, Track, and Output fields from showing their selection popup in the inspector.

### Removed
- Removed all legacy Editor Windows from the codebase to keep the package clean and up to date.
- Removed the deprecated `AudioToolsLibrary` class.
- Deleted hidden empty groups that were lingering inside the default Master Mixer asset.

---

## [2.0.1] – 2025-06-05

### Added
- Locked editor window functionalities during Play Mode to prevent unintended changes.
- Added Scroll View to the Audio Creator window, ensuring proper layout when resizing to smaller dimensions.

### Fixed
- Resolved persistent issue causing playlists to stop playing unexpectedly.

---

## [2.0.0] – 2025-06-04

### Added
- Ability to construct audio objects without providing an AudioClip in the constructor.
- New `SetClipByIndex(int)` method to assign a clip by its index.
- Support for initializing audio objects at the point of variable declaration.
- Converted Sounds Good into a Unity Package Manager (UPM) package.
- Option to select a distance-based volume curve (includes two built-in curves and support for a custom curve).
- Added `SetDopplerLevel` method to adjust Doppler effect at runtime.
- Added `SetDynamicMusic` method (before only configurable through the constructor).
- Error handling when passing an empty array to a Playlist or Dynamic Music.
- `Playlist.Shuffle()` method to randomize playback order.
- New property on `Playlist` to query the current ordered list of clips.
- `SetPlayProbability` method to define playback probability for a Sound.
- New methods in `SoundsGoodManager`:
  - Generic `Pause(id)` and `Stop(id)` methods to replace deprecated pause/stop methods.
  - `Resume(id)` and `ResumeAll()` to resume specific or all playing audio.
- Context menu integration in the Unity Editor to create Sounds Good prefabs directly in the scene (GameObject > Sounds Good).
- “Open Demo Scene” button added under Tools > Melenitas Dev > Sounds Good for quick access to the demo scene.

### Changed
- Removed dependency on the **Resources** folder; the user’s sounds, music database, and outputs are now automatically generated under `Assets/SoundsGood/Data`.
- Renamed class `AudioManager` to `SoundsGoodManager` for clarity.
- Renamed class `SourcePoolElement` to `SoundsGoodAudioSource`.
- Moved Prefabs folder out of the Demo folder and Demo assembly to isolate core assets.
- Updated Playlist and Dynamic Music construction to accept parameters (`params`) for easier initialization.
- Audio outputs now automatically set their last saved volume before use.
- Improved the UI layout and styling of all Sounds Good Editor windows.
- Changed `SoundsGoodAudioSource` class to `internal` to hide implementation details.
- Removed “hear distance” option from the `SetVolume` method (all overloads using hear distance are now deprecated).

### Deprecated
- Marked `AudioManager.GetLastSavedVolume` as obsolete; last saved volume is now updated automatically.
- Deprecated all `SetVolume` overloads that accepted a “hear distance” parameter.
- Deprecated the following methods in `SoundsGoodManager`:
  - `PauseSound(id)`
  - `PauseMusic(id)`
  - `StopSound(id)`
  - `StopMusic(id)`
  - `PauseAllMusic()`
  - `PauseAllSound()`

### Fixed
- Fixed bug where AudioCollection search was case-sensitive and did not recognize uppercase characters.
- Corrected issue where a tag did not update properly on `Update` (it only changed in the enum).
- Resolved user database loss when upgrading to version 2.0.0 (Version Upgrader window).
- Fixed playlist stopping unexpectedly after playing for a while.
- Fixed demo script errors that occurred when demo audio clips were removed from Audio Collection.
- Fixed bug in playlists where changing songs could overwrite another audio source’s output if it was still in use.
- Minor adjustments to ensure outputs load the correct volume upon initialization.

### Documentation
- Significantly improved internal documentation and code comments across all Sounds Good classes and methods.

---

## [1.0.1] – 2023-12-04

### Added
- Introduced a property to query playback volume.

### Fixed
- Fixed compilation errors on Unity 2021.1.x and earlier versions.

---

## [1.0.0] – 2023-11-30

### Added
- First public release of Sounds Good.

---
