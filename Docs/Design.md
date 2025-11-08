Teramyyd — Design Overview

Goal

Create an arcade-style air-to-air combat prototype where the player pilots a fast fighter and engages waves of enemy aircraft. Focus on fun aerial movement, clear weapons, and responsive combat.

Core systems

- Input & flight: arcade forward-thrust with pitch/roll/yaw controls and look-based aiming.
- Weapons: energy/ballistic weapons that spawn projectiles from a firing bay. Support burst or continuous fire.
- Enemy AI: simple pursuit and firing; later add formations, flanking, and evasive maneuvers.
- Spawning & waves: GameManager handles spawning at designated spawn points.
- Scoring & progression: score per kill, increase difficulty over waves.

Controls (starter)

- WASD / Arrow keys: pitch/roll
- Mouse or additional keys: yaw/look (optional)
- Left mouse / Ctrl: Fire

Data shapes / contracts

- Projectile: { speed: float, damage: int, lifeTime: float }
- Aircraft: { forwardSpeed: float, turnSpeed: float, health: int }
- GameManager: tracks score and spawn points

Edge cases considered

- Missing references (null checks on prefab/transform)
- Projectile hitting non-target objects (SendMessage with DontRequireReceiver)
- Player destroyed (GameManager.OnPlayerDestroyed called)

Testing

- Verify scripts compile in Unity Editor.
- Place a Player GameObject with `PlayerAircraft` attached and a projectile prefab to test firing.
- Place an Enemy prefab with `EnemyAircraft` to verify detection and firing.

Next milestones

1. Add a prototype Scene with camera and UI.
2. Implement a proper health system for enemies.
3. Add simple HUD (health, score, wave timer).
4. Implement more refined flight physics and inertia.
5. Add audio and visual effects for weapons and explosions.
