# Lightning Bolt System

A real-time procedural lightning simulation for Unity URP. Generates branching stepped-leader geometry on the CPU, renders via pooled LineRenderers, and animates through progression, peak, and regression phases with dynamic point light illumination.

---

## Requirements

- Unity 6 (6000.x)
- Universal Render Pipeline (URP) 17+
- URP Bloom enabled in your post-processing volume

---

## Setup

### 1. Add the prefab to your scene

Drag `PFB_LightningBolt` into your scene hierarchy.

### 2. Assign strike points

On the `LightningBolt` GameObject, find the `Lightning System` component and assign:
- **Origin Point** — a Transform at the start of the bolt (e.g. sky position)
- **Target Point** — a Transform at the strike destination (e.g. ground)

### 3. Assign the material

On the `Lightning Renderer` component, assign `M_LightningBolt` to the **Bolt Material** field.

### 4. Enable Bloom

In your scene's post-processing volume, enable **Bloom** with:
- Threshold: `0.8` or lower
- Intensity: `1.5` or higher

The bolt material emits at HDR value ~8, well above the threshold — Bloom drives the glow, no custom shader needed.

---

## How to Use

### Trigger from the Inspector

Select the `LightningBolt` GameObject → right-click the `Lightning System` component header → **Trigger Strike**.

### Trigger from code

```csharp
// Using the configured Origin/Target points from the Inspector
GetComponent<LightningSystem>().TriggerStrike();

// Or between any two world positions at runtime
GetComponent<LightningSystem>().TriggerStrike(
    new Vector3(0f, 15f, 0f),   // origin
    new Vector3(3f,  0f, -2f)   // target
);
```

### Instantiate at runtime

```csharp
var go = Instantiate(lightningBoltPrefab);
go.GetComponent<LightningSystem>().TriggerStrike(origin, target);
```

### Listen for completion

```csharp
var animator = GetComponent<LightningAnimator>();
animator.OnStrikeComplete += () => Debug.Log("Strike finished");
```

---

## Configuration

All parameters are exposed in the Inspector.

### Lightning System

| Field | Default | Description |
|---|---|---|
| Segment Count | `60` | Path resolution of the main bolt — higher = more jagged detail |
| Max Deviation | `0.4` | Maximum lateral offset per segment. Increase for a more chaotic path |
| Branch Probability | `0.25` | Chance `[0–1]` that a branch forks off at each segment |
| Max Branch Depth | `2` | How many levels of sub-branches are generated |
| Use Random Seed | `true` | Unique shape on every strike. Disable to use a fixed seed |
| Fixed Seed | `42` | Deterministic seed — same shape every time when random seed is off |
| Origin Point | — | Transform at the top of the bolt |
| Target Point | — | Transform at the strike destination |

### Lightning Renderer

| Field | Default | Description |
|---|---|---|
| Bolt Material | — | URP Unlit HDR additive material. Assign `M_LightningBolt` |
| Pool Size | `32` | Maximum simultaneous LineRenderers (main bolt + branches combined) |
| Main Bolt Width | `0.08` | World-space width of the main channel in metres |
| Branch Width Multiplier | `0.5` | Width scale per sub-branch depth level — deeper branches are thinner |

### Lightning Animator

| Field | Default | Description |
|---|---|---|
| Progression Duration | `0.15s` | Time for the bolt to grow from origin to target |
| Peak Duration | `0.05s` | Time at full brightness before decay begins |
| Regression Duration | `0.6s` | Total flicker and fade duration |
| Damping Coefficient | `4` | How fast the flicker decays — higher = quicker fade |
| Flicker Frequency | `12` | Oscillations per second during regression — higher = faster flicker |

### Lightning Light Controller

| Field | Default | Description |
|---|---|---|
| Strike Light | *(auto-created)* | Optional Point light override. Auto-created as a child if left empty |
| Peak Intensity | `8` | Point light intensity at the moment of strike |
| Light Range | `30` | World-space radius of the lightning illumination in metres |
| Light Color | blue-white | Color of the point light |
| Enable Camera Shake | `true` | Shakes `Camera.main` on each strike |
| Shake Intensity | `0.15` | Maximum camera position offset in metres |
| Shake Duration | `0.2s` | Duration of the shake — tapers off toward the end |

---

## Files

```
Scripts/Lightning/
  LightningSystem.cs           — Orchestrator. Entry point, wires all components
  LightningGenerator.cs        — CPU path generation, branching, noise
  LightningRenderer.cs         — LineRenderer pool, renders strike geometry
  LightningAnimator.cs         — Coroutine phases: progression / peak / regression
  LightningLightController.cs  — Point light flicker and camera shake
  AnimationCurveHelper.cs      — Shared math: damped sine, progression normalise
  LightningBranch.cs           — Data: single branch path + metadata
  LightningStrike.cs           — Data: complete strike (main bolt + all branches)

Materials/Lightning/
  M_LightningBolt.mat          — URP Unlit, Additive blend, HDR white (intensity 3)

Prefabs/Lightning/
  PFB_LightningBolt.prefab     — Ready-to-use prefab with all components configured
```

---

## Further Reading

- [`architecture.md`](architecture.md) — full script reference, Inspector parameter definitions, data flow diagram
