# Mimic Boss Work Log

## Goal

Complete the Mimic encounter described in the team's Notion design, integrated
with this project's player, robot-arm grabbing, damage, scene and UI systems.
This document records implementation, verification and remaining work. A checked
code item does not mean the complete encounter has been verified in Play Mode.

## References

- Design: https://app.notion.com/p/3c01f080adb08040b6fcd65b1cc2e13d
- Architecture: https://github.com/Team-Hashira/VIRTUS_Source/tree/main/Boss
- Use serializable pattern classes with Initialize, CanStart, OnStart, OnUpdate,
  OnEnd and OnDie. Do not implement Mimic scheduling with coroutines.
- Some reference patterns use coroutines; copy the lifecycle structure, not those
  implementations or their project-specific dependencies.

## Confirmed Requirements

- Phase 1 treasure: 15 ballistic weapons, one every 0.5 seconds. Weapons are
  grabbable and disposable after a player throw. Interrupt after 5 and 10 weapons
  with Tongue, then resume without resetting the emission count.
- Tongue: approximately 0.5 seconds of warning, attack toward the player and pull
  in / consume throwable objects along its path.
- Phase 1 jumps: 10 jumps across three map zones in random order; landing area
  damage plus three falling rocks in the selected zone. Tongue after jump 5,
  then resume. Rocks damage the player and break on the ground.
- Shell game: two decoys plus the boss, trackable shuffle, correct choice causes
  groggy; wrong choice explodes the chests for area damage. Return home afterward.
- Phase transition: five seconds of damaging coins in all directions, explosion,
  broken chest and exposed body.
- Phase 2: 60-second kill deadline, otherwise explosion and game over. Consecutive
  hits within three seconds add combo; each combo increases incoming damage 20%.
- Phase 2 weapon attack: four platform positions in order 1-2-3-4 or 4-3-2-1.
  Incoming weapons hurt the player but can be caught and thrown back.
- Phase 2 laser: five random directions, fired sequentially (not simultaneously).
- Arena: central boss, two platforms on each side, overhead grapple point.

## Decisions Awaiting User

The user chose to supply their own values, so these are NOT approved defaults:

- Phase 2 transition health threshold.
- Groggy duration.
- Combo reset behavior and cap (the three-second hit window itself is specified).

Other timing, damage and distance settings will be exposed in the Inspector and
identified as tuning values, not treated as final balance.

## Current Project Findings (2026-09-19)

- Worktree was clean at inspection. Actual project directory is
  `C:/Unity/26NInza_TeamProject2`; `Assets/Member/YKJ` exists.
- Unity version: 6000.3.16f1. No existing Mimic behavior scripts found.
- `BossTest.unity` contains a bare Mimic object and another tutorial boss; preserve
  this scene until a scoped scene integration is ready.
- KYM's boss uses a Behavior Graph. ODK's PhasedBossController runs coroutines.
  Neither is the requested plain-class pattern architecture.
- Reuse Agent/HealthModule, IDamageable/DamageData, IGrabbable/ThrowData and
  GrabbableRigidbody instead of replacing the shared systems.
- GrabbableProjectile destroys on world contact. Use a separate MimicWeapon based
  on GrabbableRigidbody so treasure survives landing. Do not alter ThrowWeapon.
- PlayerController.TakeDamage is empty. A HealthModule forwarding call is needed
  for encounter damage to affect the actual player.

## Implementation Status

- [x] Pattern runner with interrupt/resume and cancellation coverage.
- [x] Treasure emission and reusable Tongue interruption (BossTest wired; real-scene runtime unverified).
- [x] Land / grab / release / throw / consume weapon lifecycle.
- [x] Jump and rock attack (BossTest wired; real-scene runtime unverified).
- [ ] Shell game and groggy.
- [ ] Phase transition, phase 2 attacks, combo and deadline.
- [ ] Inspector setup, prefabs and playable scene integration.
- [ ] Health / timer / combo / win / loss feedback.
- [ ] Automated checks plus real Play Mode end-to-end verification.

## Verification

2026-09-19:

- Full project's runtime scripts plus the new Mimic runtime files compiled using
  Unity 6000.3.16f1's C# compiler and the project's own Bee response file: success,
  zero errors, three existing unused-member warnings.
- Isolated Unity verification project: `Temp/MimicVerification`. Exact production
  Mimic source and its actual project dependencies were copied (no combat stubs).
  Actual project TagManager and Physics2DSettings were copied for the second run.
- Unity Test Framework: 27 tests passed, zero failed/skipped. Results:
  `Temp/MimicVerification/results.xml`; log: `Temp/MimicVerification/Editor.log`.
- 18 core cases: valid/rejected transitions, nested interruption, correct outgoing
  OnEnd, restart after cancellation, death cleanup, 5/10 checkpoints, no emission
  during Tongue, invalid time, long-frame behavior, ballistic velocity.
- 9 tests enter Play Mode: actual ground contact and grabbability, grab/release/
  throw and one boss hit, held-object retirement, Tongue cancel and consumption,
  10 jumps / 30 rocks / one Tongue interruption, mid-flight cancellation, and
  rock destruction on ground contact, and synchronous cancellation from a damage
  callback preventing any subsequent rock spawns.
- New Inspector and test sources also compile with the project's actual Unity
  editor/test-runner references. Runtime Mimic files contain no coroutine APIs.
- This does NOT verify the existing player prefab, real scene, final visuals,
  game-over flow or a complete two-phase encounter. Those remain required.

## Changes Made (2026-09-19)

- `Scripts/Boss/MimicPattern.cs`, `MimicPatternRunner.cs`: plain C# lifecycle and
  interrupt stack. Child completion calls parent OnResume, never OnStart.
- `MimicEmissionProgress.cs`, `Patterns/MimicTreasurePattern.cs`: timer driven
  emission; no coroutines and no catch-up burst after a slow frame.
- `Patterns/MimicTonguePattern.cs`: locked-direction warning, extension, retraction,
  once-per-attack damage, capture via IGrabbable, consumption, cancellation cleanup.
- `MimicWeapon.cs`: GrabbableRigidbody subclass, ballistic launch and persistent
  treasure on the ground; player throw becomes one-use damage. Cleanup defers
  destruction of a held weapon until the arm releases it.
- `MimicArena.cs`, `MimicHazard.cs`, `MimicCombat.cs`, `Patterns/MimicJumpPattern.cs`:
  three configured zones, landing warning, arc, radius damage, three rocks per
  landing, halfway Tongue and stable-position cancellation.
- `MimicBoss.cs`: Agent/HealthModule integration, serializable pattern list,
  explicit Begin/Stop, spawned-object cleanup. Default autoplay is OFF; this is
  still a phase-one controller until the remaining encounter work is integrated.
- `Editor/MimicBossEditor.cs`: add supported pattern types and runtime Begin/Stop.
- `Tests/Editor`: core tests and isolated Play Mode integration tests.
- `KYM/.../PlayerController.cs`: forward positive damage to existing HealthModule;
  the method was empty. Real player death/input/UI integration still needs testing.
- `ODK/.../PhasedBossController.cs`: remove stale references to the nonexistent
  DeathModule and use existing HealthModule.OnDeath/Revive. The missing type was a
  pre-existing project-wide compile blocker. No unrelated boss patterns changed.
- Existing scenes, prefabs, ThrowWeapon and shared grab implementations untouched.

## Current Inspector Wiring

`Scene/BossTest.unity` now has the following wiring for the implemented phase-one
patterns. Play On Start is enabled on this scene's Mimic instance. No Play Mode
test was performed during setup, as explicitly requested by the user.

1. Boss root: MimicBoss, HealthModule, appropriate boss collider. Assign the actual
   player root to Target. Keep a stable boss root for jump movement.
2. Mouth and world-space LandingLeft/Right markers; one or more MimicWeapon
   prefabs with Rigidbody2D (positive gravity), solid Collider2D, renderer.
3. Tongue: active LineRenderer object with visible material; assign it under Tongue.
4. Arena: exactly three zones. Each zone needs a boss-center LandingPoint and two
   overhead RockLeft/Right spawn markers. Assign Arena and a MimicHazard rock prefab.
5. Jump pattern: assign a separate landing-warning LineRenderer/material.
6. Use the Inspector's Add Pattern menu for Treasure/Jump. Begin Encounter during
   Play Mode after references are wired. A jump with missing required references
   cannot start; do not mistake treasure-only execution for a complete phase.

## BossTest Scene Setup

- Added `Scripts/Boss/Editor/MimicBossTestSetup.cs` with the menu
  `Tools > YKJ > Setup Mimic in BossTest`. It targets only the open BossTest scene,
  reuses its existing Mimic and player, and creates owned test prefabs and arena
  markers. It rejects an already-configured Mimic rather than overwriting tuning.
- The setup source compiles against the project's actual Unity references.
- Initial attempt was blocked by the Windows lock screen. After the user's
  follow-up authorization, invoked the setup in Edit Mode and saved BossTest.
- Configured the existing Mimic, actual Player target, HealthModule, collider,
  kinematic body, mouth, tongue renderer, landing-warning renderer and three arena
  zones. Reused project chest/sword/rock sprites as placeholders. New assets live
  in `MimicTestAssets` (two prefabs and one unlit material).
- Ground detected at Y=-2; boss/zone centers at approximately Y=-0.75. Zones are
  at X=-5/0/5, rock spawn lines at Y=6.5, weapon landing bounds at X=-8/8.
- Verified saved scene/prefab references and Inspector wiring. Did NOT enter Play
  Mode or run runtime tests, per the latest user request. Shell game and phase 2
  are not part of this scene's current pattern list.
- Unity reported automatic repair of 13 missing-type nodes in the existing
  `TutoBoss AI.asset`; its generated change was saved and not manually reverted.
  Scene serialization also refreshed existing weapon fields to the current script
  schema. These are editor-generated changes, not new boss behavior edits.
- No further edits to other members' scripts in this scene-setup attempt.

## Next Work

### Variable Treasure Arcs (2026-09-20, Superseded Below)

- User reference: weapons should be spat out with different strengths, with
  trajectories reaching the floor and both platform heights.
- Replaced Treasure's fixed flight time with `upwardSpeedRange` (default 6..15
  world units/sec). Each emission samples its own vertical speed and a ground
  target between the existing landing bounds. Horizontal speed is solved from
  the descending flight time, so taller arcs still stay within the arena's
  intended spread when unobstructed. Actual physics can land them on platforms
  before reaching that ground target. The 15 weapons / 0.5s and 5/10 interruptions
  are unchanged.
- BossTest's serialized Treasure pattern now has `upwardSpeedRange: (6, 15)`.
  Adjust it under Mimic > Phase One Patterns > MimicTreasurePattern. LandingLeft
  and LandingRight still control the horizontal spread.
- Found Grabbable/Flat contacts disabled in the shared physics layer matrix.
  Added Ground/Flat inclusion on MimicWeapon's own collider (and its test prefab),
  without changing the global matrix, shared platform prefab or other members'
  scripts. Existing platforms already use one-way PlatformEffector2D.
- Grounded state now requires an upward support contact. On landing, clear
  velocity so treasure does not slide off the zero-friction platforms. Grabbing,
  throwing and disposable player-thrown damage retain their existing behavior.
- Added seven pure regression cases for scatter velocity, upper-platform reach,
  raised targets and unsupported gravity, plus one Play Mode regression for
  upward passage and landing on a one-way Flat platform without global changes.
- Verification: full runtime + own editor + test sources compile with Unity's
  compiler and current project references; zero errors, three existing unused
  member warnings. Analytic checks at current gravity/mouth position give peaks
  Y=0.82 / 3.00 / 7.25 for upward speeds 6 / 10 / 15. A far-side target permits
  descending crossings over the lower and upper platform footprints.
- No Unity Play Mode or physics regression execution in this turn, per the user's
  earlier restriction. Platform contact and final appearance still need runtime
  verification; compilation and trajectory calculations are not a Play test.

### Three Explicit Landing Heights (2026-09-20)

- Latest correction: choose Ground / FirstPlatform / SecondPlatform first, then
  randomize X within that surface. Do not choose an arbitrary upward speed and
  hope the weapon reaches a platform. The preceding scatter implementation is
  superseded; its upward-speed range and scatter APIs were removed.
- Added `MimicTreasureHeightCycle`: shuffle the three heights, use each once,
  then reshuffle. Fifteen emission targets therefore split 5/5/5. The cycle lives
  in the treasure pattern and survives Tongue interruptions without resetting.
- MimicBoss now references `firstPlatforms` and `secondPlatforms` separately.
  After selecting a height, choose one platform from that level and sample X
  inside only its collider bounds, inset for the weapon width and rounded ends.
  Y is the platform top plus the weapon's half-height and a small clearance.
  Ground uses the existing LandingLeft/Right X bounds and LandingLeft's Y.
- Wired both lower and both upper platform colliders in BossTest. No shared
  platform prefab, global layer matrix or other members' scripts were changed.
- `CalculateArcVelocity` solves a descending trajectory to the chosen target.
  `Arc Height` is now extra apex clearance (0.75), not random landing height.
  With BossTest's current heights it keeps floor arcs below level one, and
  level-one arcs below level two. Applied a half-fixed-step velocity correction
  for Rigidbody2D's gravity integration. Existing landing/grab/throw behavior
  and per-weapon Ground/Flat collision inclusion are retained.
- Updated initial scene setup and test fixtures for both required platform levels.
  Added/replaced regression cases for 5/5/5 height selection, per-platform X
  bounds, fixed target Y, descending arrival and clearance from the next level.
- Verification: runtime/editor/test sources compile against actual Unity project
  references, with no errors. Inspected all four saved scene component references
  and confirmed the old scatter settings/calls are gone. Tests were compiled,
  not executed; no Play Mode or physics simulation was run as requested. The
  5/5/5 guarantee describes chosen targets, not measured final pickup counts
  after collisions, grabbing or the boss consuming weapons.

1. User-defined phase threshold, groggy duration, combo cap/reset decisions.
2. Shell game, trackable chest shuffle, hit selection, success/failure, return home.
3. Phase transition and all phase-two attacks, deadline/combo and terminal states.
4. Scoped prefab/scene setup, player integration, graphics and HUD feedback.
5. Complete encounter tests and real-scene Play Mode verification. Goal stays active.

## Child Pattern Components (2026-09-21)

- MimicPattern now derives from MonoBehaviour. Treasure, Jump and Tongue are
  separate components under BossTest's Mimic/Patterns children, with Skill IDs
  1, 2 and 3. Existing scene tuning and renderer references were preserved.
- MimicBoss registers its own child patterns in Dictionary<int, MimicPattern>.
  Phase One Skill Ids contains execution order only (1, 2); Tongue Skill Id is 3.
  GetPattern(id) and TryStartSkill(id) use the dictionary. Invalid/duplicate IDs
  and missing configured IDs report errors. Interrupt/resume still uses the runner.
- Add Pattern in the boss Inspector now creates child components with unused IDs.
  Initial BossTest setup uses the same component structure.
- Migrated test construction to AddComponent and added ID lookup, duplicate ID,
  and disabled component cases. Runtime/editor/test sources compiled with no
  errors (three existing warnings). Scene object IDs/references checked statically.
  Tests and Play Mode were not run; loaded-editor scene state was not inspected.

## Equal Jump Zones (2026-09-21)

- MimicArena now splits Horizontal Range into three adjacent equal rectangles.
  BossTest uses -12.444445 to 12.444445, matching its fixed orthographic camera
  at size 7 and 16:9. This is an editable world-space range, not dynamic viewport sizing.
- Landing X is the zone center; existing LandingPoint Y/Z are retained. Rock X
  spans the same zone, with existing spawn heights retained.
- Jump warning uses a square-ended filled LineRenderer strip with pale red
  color/alpha 0.25. Warning Vertical Range sets bottom/top (-2 to 8 in BossTest).
  Landing damage uses the same cached rectangle instead of a radius.
- Updated setup and regression sources. Compilation passed (three existing
  warnings). No Play Mode, physics simulation or visual runtime test was run.

## Attack Body Animation (2026-09-21)

- Added MimicBodyAnimator on BossTest's Mimic, targeting only the Visual child.
  Uses core DOTween for configurable squash/stretch; no DOTween Pro dependency.
  Root movement, colliders and attack anchors are unchanged by the animation.
- Jump: anticipation during warning, stretch during flight, squash/recover on
  landing. The fifth-jump tongue interrupt now waits for the existing landing
  delay so the impact animation can complete.
- Treasure: inflate before emission, recoil per weapon, preserve the final
  recoil for up to 0.18 seconds before closing. Tongue: wind-up, extension and
  recovery poses synchronized to existing timings. Sprite switching is retained.
- Renderer bottom is anchored while scaling. Own tweens and pose are cleared
  on interruption, cancellation, death and disable. Added a regression source
  for fixed feet/collider bounds and disable/reset cleanup.
- Runtime/editor/test sources compiled successfully with three existing warnings.
  No Play Mode or visual runtime verification was performed.
