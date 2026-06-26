# Magic Tiles

## How to Run

### Prerequisites
* **Unity Version**: Unity 2022.3.0*
* **Target Platforms**: Standalone (PC/Mac/Linux) or mobile simulation.

### Steps to Run
1. **Open Unity Hub** on your machine.
2. **Add Project**:
   * Click the **Add** button -> **Add project from disk**.
   * Select the `MagicTiles` directory.
3. **Select Editor Version**: Ensure the project is set to run with Unity **2022.3.0*** (or compatible 2022.3 LTS editor).
4. **Open Project**: Click the project name in Unity Hub to launch the editor.
5. **Open Game Scene**: In the Project window, navigate to `Assets/Scenes` and open `GameScene.unity`.
6. **Play**: Press the **Play** button at the top-center of the Unity Editor.
7. **Gameplay Instructions**:
   * Tap at the **Press to Play** text to begin.
   * Tiles will spawn in four lanes and descend toward the bottom.
   * Tap each tile **when it reaches the line** glowing near the bottom.
   * Tapping empty spaces or letting a tile pass below the screen will trigger a loss.

---

## Architecture & Design Choices


### 1. Rhythm Synchronization System
* **Implementation**: Managed by `AudioManager` and `TileSpawner`.
* Implemented synchronization using `AudioSettings.dspTime` to ensure note spawning matches the exact audio playback timestamp of the beatmap notes.
* **Structure**: Notes are loaded from a JSON-based beatmap via `AudioManager.LoadBeatmap` and spawned at precise DSP time intervals in `TileSpawner.BeatmapSpawnLoop`.

### 2. Object Pooling
* **Implementation**: The `TileSpawner` maintains a queue-based pool of **Tile** instances.
* Frequent instantiation and destruction of game objects causes Garbage Collection (GC) spikes and performance stutters, which are unacceptable in rhythm games requiring fast input. Tiles are recycled via `TileSpawner.ReturnTile` and `TileSpawner.GetTileFromPool`.

### 3. Game Feel & Visual Polish
* **Camera Shake**: A screen shake effect is triggered in `GameManager.UpdateCameraShake` upon invalid taps or empty lane taps.
* **Tap Ripples** creates smooth canvas-space expanding ripples at the exact pointer position of every screen tap.
* **Score Popups** handles score floating text that spawns above clicked tiles.
* **Miss Reveal Sequence**: When a player misses a tile, the game triggers a fail sequence in `GameManager.PlayMissSequence`:
  * The missed tile rapidly flickers from white to red.
  * Music stops immediately, playing a miss sound effect.
  * All remaining active tiles on screen slowly rise up before the final game over screen shows.
* **UI Easing**: The menus fade and scale using smooth custom easing curves in `UIPopPanel`.

---

## AI Usage

AI tools were utilized during the development of this project:
* **Prefab Analysis & Code Optimization**: Claude and Grok were leveraged to inspect the serialized YAML representation of the particle systems inside `ParticleEffectsUnoptimize.prefab` and identify exact bottlenecks (like culling settings, high particle count limits, and expensive noise/trail modules).
* **Boilerplate & Math Curves**: AI assisted in producing clean structures for UI easing interpolation (e.g., cubic curves in `UIPopPanel` and math-based sine glows.
* **Assets Generation**: Nana Banana 2 was used to generate image asset for the replay menu.