# Ball Brawl

A physics-based arena brawler built in Unity. Players knock each other around a stage with momentum-driven collisions, using dashes, invulnerability, and super knockback bursts to try to launch opponents into a death zone.

## Gameplay

Each player controls a sphere ("ball") that moves and collides with physics. Bumping into another player launches them based on relative direction and force — the goal is to knock opponents off the stage and into the death zone, which respawns them after a short delay.

### Controls (current build)

| Input | Action |
|---|---|
| `W` / `A` / `S` / `D` or Arrow Keys | Move |
| `Left Shift` | Dash (impulse in movement direction, on cooldown) |
| `Q` | Invulnerability (temporary, prevents being launched) |
| `E` | Super Knockback (temporary, multiplies outgoing hit force) |

## Project Structure

```
Assets/
├── Scenes/
│   └── BallBrawl.unity          # Main gameplay scene
├── Scripts/
│   ├── BrawlPlayer.cs           # Player movement, dash, invulnerability, knockback combat
│   └── DeathZone.cs             # Trigger volume that respawns players who fall off-stage
├── Prefabs/
│   └── Player.prefab            # Player ball (Rigidbody + SphereCollider + BrawlPlayer)
├── PhysicsMaterials/
│   └── BouncySurface.physicMaterial   # Bouncy collision surface for the stage
├── Settings/                    # URP render pipeline & volume profile assets (PC + Mobile)
└── InputSystem_Actions.inputactions   # New Input System action map (not yet wired into gameplay)
```

## Core Scripts

- **`BrawlPlayer`** — Handles movement via `Rigidbody` velocity, a cooldown-based dash impulse, timed invulnerability, and a timed super-knockback multiplier. On collision with another `BrawlPlayer`, applies launch force away from the point of impact unless the target is invulnerable.
- **`DeathZone`** — A trigger collider that, on contact with a `BrawlPlayer`, freezes and hides them, then respawns them at a designated `spawnPoint` (or a default position) after 3 seconds.

## Tech Stack

- **Engine:** Unity `6000.5.8f1`
- **Render Pipeline:** Universal Render Pipeline (URP), with separate PC and Mobile render/pipeline assets
- **Input:** Unity Input System package (action asset present; gameplay currently reads the legacy `Input` API)
- **Physics:** Built-in 3D physics (`Rigidbody`, `SphereCollider`, `PhysicsMaterial`)

## Getting Started

1. Open the project in **Unity 6000.5.8f1** (or newer) via Unity Hub.
2. Open the `Assets/Scenes/BallBrawl.unity` scene.
3. Press Play to test locally with a single player.

## Roadmap Ideas

- [ ] Wire up the New Input System action map instead of legacy `Input` calls
- [ ] Local multiplayer / additional player prefabs
- [ ] Stage hazards and multiple arenas
- [ ] Score tracking / round-based match flow
- [ ] UI for cooldowns, lives, and match state
