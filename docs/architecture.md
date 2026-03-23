# Lightning Bolt System — Architecture

## Overview

The lightning bolt system is split into six distinct layers, each with a single responsibility. Data flows in one direction: generation → data → rendering → animation → lighting. No layer has a reference back up the chain.

```
LightningSystem  (orchestrator — owns no logic, wires everything)
       │
       ├── LightningGenerator  (CPU — produces path data)
       │         │
       │         └── LightningStrike / LightningBranch  (data layer)
       │                   │
       ├── LightningRenderer  (GPU — LineRenderer pool)
       │
       ├── LightningAnimator  (temporal — coroutine phases)
       │         │
       │         └── AnimationCurveHelper  (math utility)
       │
       └── LightningLightController  (environment — point light + camera shake)
```

---

## Script Reference

### `LightningBranch`
**Type:** Pure C# class — no MonoBehaviour
**File:** `Assets/Scripts/Lightning/LightningBranch.cs`

A single path segment of the lightning bolt — either the main channel or a sub-branch. Immutable after construction.

| Property | Type | Description |
|---|---|---|
| `Points` | `List<Vector3>` | Ordered world-space positions forming the path of this branch |
| `Width` | `float` | Relative width scale. `1.0` for the main bolt; `1/(depth+1)` for sub-branches |
| `Depth` | `int` | Hierarchy depth. `0` = main bolt, `1` = first-level branch, `2` = second-level branch |

---

### `LightningStrike`
**Type:** Pure C# class — no MonoBehaviour
**File:** `Assets/Scripts/Lightning/LightningStrike.cs`

A complete strike event: the main bolt plus all of its sub-branches, bundled together and passed as a single unit between the generator, animator, and renderer.

| Property | Type | Description |
|---|---|---|
| `Branches` | `List<LightningBranch>` | All branches. Index 0 is always the main bolt; subsequent entries are sub-branches in generation order |
| `Origin` | `Vector3` | World-space start position (top of the bolt) |
| `Target` | `Vector3` | World-space end position (strike point) |

---

### `LightningGenerator`
**Type:** Pure C# class — no MonoBehaviour
**File:** `Assets/Scripts/Lightning/LightningGenerator.cs`

Procedurally generates the geometry of a lightning strike on the CPU. Uses Perlin noise (seeded via random offsets) for path deviation and `System.Random` for branching decisions. A new instance is created before each strike — it does not hold state between strikes.

**Key methods:**

| Method | Description |
|---|---|
| `Generate(Vector3 origin, Vector3 target)` | Produces a complete `LightningStrike` with main bolt and all sub-branches |

**Constructor parameters (set via `LightningSystem` Inspector):**

| Parameter | Default | Description |
|---|---|---|
| `segmentCount` | `60` | Number of path segments on the main bolt. Higher values produce finer geometry |
| `maxDeviation` | `0.4f` | Maximum world-space lateral offset applied per segment via Perlin noise. Increase for more jagged paths |
| `branchProbability` | `0.25f` | Probability `[0–1]` that a branch spawns at each interior segment. Clamped automatically |
| `maxBranchDepth` | `2` | How many levels of sub-branching are allowed. `1` = only direct branches from the main bolt |
| `seed` | `-1` | `-1` = random seed each call. Any value ≥ 0 = deterministic, reproducible result |

**Internal behaviour:**
- `_noiseOffsetX` / `_noiseOffsetY` are sampled from the seeded random on construction, ensuring different seeds produce genuinely different main-bolt shapes (not just different branch positions)
- `branchProbability` is clamped to `[0, 1]` in the constructor — values outside this range are silently corrected
- Branch width = `1.0 / (depth + 1)` — each level is narrower than its parent
- Branch segment count = `max(5, segmentCount / (depth + 1))` — sub-branches have fewer segments

---

### `AnimationCurveHelper`
**Type:** `static` pure C# class — no MonoBehaviour
**File:** `Assets/Scripts/Lightning/AnimationCurveHelper.cs`

Shared math utility used by both `LightningAnimator` and `LightningLightController`. Has no state.

| Method | Signature | Description |
|---|---|---|
| `DampedSine` | `(float time, float damping, float frequency) → float` | Returns `e^(-damping × time) × cos(frequency × time)`. Starts at `1.0`, oscillates, decays toward `0`. Used to drive both the bolt flicker and point light intensity |
| `ProgressionT` | `(float elapsed, float duration) → float` | Returns `Clamp01(elapsed / duration)`. Normalised progress value from 0 to 1 over a duration |

---

### `LightningRenderer`
**Type:** MonoBehaviour
**File:** `Assets/Scripts/Lightning/LightningRenderer.cs`
**Requires:** nothing (no `[RequireComponent]`)

Owns a fixed-size pool of `LineRenderer` components, pre-allocated at startup. Converts `LightningStrike` data into visible geometry. Never instantiates at runtime. Uses `MaterialPropertyBlock` for per-branch alpha changes to avoid material instance leaks.

**Inspector parameters:**

| Field | Default | Description |
|---|---|---|
| **References** | | |
| `Bolt Material` | — | The URP Unlit HDR material (`M_LightningBolt`) assigned to every LineRenderer in the pool. Must be assigned — no runtime fallback |
| **Settings** | | |
| `Pool Size` | `32` | Number of LineRenderers pre-created in `Awake`. Caps the total number of simultaneous branches (main bolt + sub-branches). Branches beyond this limit are silently dropped |
| `Main Bolt Width` | `0.08` | World-space width of the main channel (depth 0) in metres |
| `Branch Width Multiplier` | `0.5` | Multiplied with `Main Bolt Width` and divided by branch depth to compute sub-branch widths. Lower values produce thinner sub-branches |

**Public API:**

| Method / Property | Description |
|---|---|
| `RenderStrike(LightningStrike)` | Clears all active renderers then populates the pool from the strike's branch list |
| `SetBranchAlpha(int index, float alpha)` | Sets the `_BaseColor` alpha on a specific branch via `MaterialPropertyBlock`. Used during regression flicker |
| `ClearAll()` | Disables all LineRenderers and resets the active count to zero |
| `ActiveBranchCount` | Number of currently enabled LineRenderers |

**Performance notes:**
- Zero runtime allocations after `Awake` during normal operation
- `SetPositions` is called via `branch.Points.ToArray()` on each `RenderStrike` call — this allocates a temporary array. Acceptable because `RenderStrike` is called at most once per strike trigger, not per frame
- Shadow casting and receiving are both disabled on all LineRenderers
- Light probe usage is set to `Off` on all LineRenderers (no ambient probe influence)

---

### `LightningAnimator`
**Type:** MonoBehaviour
**File:** `Assets/Scripts/Lightning/LightningAnimator.cs`
**Requires:** `LightningRenderer` (via `[RequireComponent]`)

Controls the temporal behaviour of a strike through three sequential phases driven by a single coroutine. Zero Update overhead when idle.

**Inspector parameters:**

| Field | Default | Description |
|---|---|---|
| **Timing** | | |
| `Progression Duration` | `0.15s` | How long the bolt takes to grow from origin to target, segment by segment |
| `Peak Duration` | `0.05s` | How long the bolt stays at full brightness before regression begins |
| `Regression Duration` | `0.6s` | Total duration of the flicker-and-fade phase |
| **Regression** | | |
| `Damping Coefficient` | `4` | Controls the decay rate of the flicker envelope. Higher = faster fade. Passed to `AnimationCurveHelper.DampedSine` |
| `Flicker Frequency` | `12` | Oscillations per second during regression. Higher = faster flicker. Passed to `AnimationCurveHelper.DampedSine` |

**Phases:**

| Phase | What happens |
|---|---|
| `Progression` | The main bolt is revealed segment-by-segment from origin to target over `Progression Duration`. Sub-branches are not shown yet |
| `Peak` | The full strike (main bolt + all sub-branches) is rendered at full opacity for `Peak Duration` |
| `Regression` | All branches fade via a damped sine wave applied to `_BaseColor` alpha each frame. The bolt flickers and dims over `Regression Duration` |
| `Idle` | No coroutine running. Renderer is cleared |

**Public API:**

| Member | Description |
|---|---|
| `PlayStrike(LightningStrike)` | Starts (or restarts) the animation coroutine for the given strike |
| `Stop()` | Immediately halts the coroutine, clears the renderer, returns to Idle |
| `CurrentPhase` | Read-only. Current `LightningPhase` enum value |
| `OnStrikeComplete` | Event fired after regression finishes and the renderer is cleared |

---

### `LightningLightController`
**Type:** MonoBehaviour
**File:** `Assets/Scripts/Lightning/LightningLightController.cs`
**Requires:** nothing (no `[RequireComponent]`)

Manages a single Point light that illuminates the scene at the strike location, flickering in sync with the bolt via `AnimationCurveHelper.DampedSine`. Optionally shakes `Camera.main` on trigger. Uses the URP additional lights system — one light per `LightningSystem` instance, keeping within URP's 4-light-per-object limit.

**Inspector parameters:**

| Field | Default | Description |
|---|---|---|
| **References** | | |
| `Strike Light` | *(auto-created)* | The Point light to animate. If left unassigned, a child GameObject with a Light component is created automatically in `Awake` |
| **Light Settings** | | |
| `Peak Intensity` | `8` | Point light intensity at the moment of strike. Decays via damped sine over the strike duration |
| `Light Range` | `30` | World-space radius of the point light's influence in metres |
| `Light Color` | `(0.8, 0.9, 1.0)` | RGB color of the point light. Default is a cool blue-white to match natural lightning |
| **Camera Shake** | | |
| `Enable Camera Shake` | `true` | Whether to apply positional shake to `Camera.main` on trigger |
| `Shake Intensity` | `0.15` | Maximum random offset applied to the camera's local position in metres |
| `Shake Duration` | `0.2s` | How long the shake lasts. Intensity tapers linearly to zero by the end |

**Public API:**

| Method | Description |
|---|---|
| `TriggerStrike(Vector3 position, float duration)` | Moves the light to `position`, starts the light flicker coroutine for `duration` seconds, and optionally starts the camera shake |
| `Stop()` | Halts both coroutines, disables the light, and restores the camera's original position if shake was interrupted mid-way |

**Notes:**
- If `TriggerStrike` is called while a shake is already running, the old shake is stopped, the camera position is restored to its saved origin, then a new shake starts. This prevents stacking jitter
- The light flicker uses `DampedSine(elapsed, damping: 3f, frequency: 10f)` — slightly different parameters from the bolt's regression flicker to keep the two slightly out of sync for a more natural result

---

### `LightningSystem`
**Type:** MonoBehaviour — thin orchestrator
**File:** `Assets/Scripts/Lightning/LightningSystem.cs`
**Requires:** `LightningRenderer`, `LightningAnimator`, `LightningLightController` (via `[RequireComponent]`)

The entry point of the entire system. Holds generation configuration, constructs `LightningGenerator`, and dispatches each strike to the animator and light controller. Contains no generation or animation logic itself.

**Inspector parameters:**

| Field | Default | Description |
|---|---|---|
| **Generation Settings** | | |
| `Segment Count` | `60` | Passed directly to `LightningGenerator`. Controls path resolution — the number of line segments in the main bolt |
| `Max Deviation` | `0.4` | Passed to `LightningGenerator`. World-space maximum lateral offset per segment |
| `Branch Probability` | `0.25` | Passed to `LightningGenerator`. Probability a branch spawns at each segment `[0–1]` |
| `Max Branch Depth` | `2` | Passed to `LightningGenerator`. Maximum recursion depth for sub-branches |
| `Use Random Seed` | `true` | If enabled, each strike uses a new random seed — every bolt looks different. If disabled, `Fixed Seed` is used and the shape is always identical |
| `Fixed Seed` | `42` | The deterministic seed used when `Use Random Seed` is off. Useful for previewing a specific bolt shape repeatedly |
| **Strike Points** | | |
| `Origin Point` | — | Transform marking the start of the bolt (typically above the scene). Must be assigned |
| `Target Point` | — | Transform marking the strike point (typically at ground level). Must be assigned |

**Public API:**

| Method | Description |
|---|---|
| `TriggerStrike()` | Uses `Origin Point` and `Target Point` from the Inspector. Logs an error if either is unassigned. Also available via right-click → **Trigger Strike** in the Inspector (ContextMenu) |
| `TriggerStrike(Vector3 origin, Vector3 target)` | Overload for runtime use — bypasses the Inspector transforms and fires between arbitrary world positions |

---

## Data Flow

```
TriggerStrike() called
        │
        ▼
LightningGenerator.Generate(origin, target)
        │  produces
        ▼
LightningStrike { List<LightningBranch> }
        │
        ├──▶ LightningAnimator.PlayStrike(strike)
        │           │
        │           │  Progression phase (per frame)
        │           ├──▶ LightningRenderer.RenderStrike(partialStrike)
        │           │
        │           │  Peak phase
        │           ├──▶ LightningRenderer.RenderStrike(fullStrike)
        │           │
        │           │  Regression phase (per frame)
        │           └──▶ LightningRenderer.SetBranchAlpha(i, intensity)
        │                        │
        │                        └── MaterialPropertyBlock → LineRenderer
        │
        └──▶ LightningLightController.TriggerStrike(targetPos, 0.8f)
                    │
                    ├── AnimateLight coroutine → Point Light intensity
                    └── ShakeCamera coroutine → Camera.main.localPosition
```

---

## Assembly Structure

| Assembly | Path | References |
|---|---|---|
| `Lightning` | `Assets/Scripts/Lightning/Lightning.asmdef` | Unity defaults (auto-referenced) |
| `EditModeTests` | `Assets/Tests/EditMode/EditModeTests.asmdef` | `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, `Lightning` |
| `PlayModeTests` | `Assets/Tests/PlayMode/PlayModeTests.asmdef` | `UnityEngine.TestRunner`, `Lightning` |

---

## Scene Hierarchy

```
SampleScene
└── LightningBolt
    ├── [Component] LightningSystem
    ├── [Component] LightningRenderer
    │       └── (runtime children) LightningLine_0 … LightningLine_31
    ├── [Component] LightningAnimator
    ├── [Component] LightningLightController
    │       └── (runtime child) LightningStrikeLight
    ├── Origin          ← position (0, 10, 0)
    └── Target          ← position (0,  0, 0)
```

---

## Material

**`M_LightningBolt`** — `Assets/Materials/Lightning/M_LightningBolt.mat`

| Property | Value |
|---|---|
| Shader | `Universal Render Pipeline/Unlit` |
| Surface Type | Transparent |
| Blend Mode | Additive (`SrcBlend = One`, `DstBlend = One`) |
| Base Color | HDR white — linear value `(8, 8, 8, 1)` ≈ intensity 3 |
| ZWrite | Off |
| Receive Shadows | Off |
| Render Queue | 3000 (Transparent) |

The high HDR value (8× standard white) ensures the bolt exceeds the Bloom threshold (`0.8`) at all times, producing a natural glow spread through URP's post-processing stack without any manual distance falloff.
