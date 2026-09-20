# Verified Runtime Data

This document stores verified Graveyard Keeper runtime facts relevant to Better Fishing Rods.

Target: **Graveyard Keeper 1.407**.

Current verified Assembly-CSharp identity:

- version: `11.0.0.0`;
- MVID: `6f50b8e7-156b-49ac-bbe8-7505894b2364`.

Evidence comes from the user's installed runtime, current GameBalance data, and direct method IL captured by Better Fishing Rods Research Probe 0.1.0. Live timing after `GetRandomFish` in probe 0.1.0 is not trusted because that probe accidentally overwrote the by-ref waiting-time argument after observing it; the captured method IL itself remains valid.

## Rod identities and vanilla data

Current runtime GameBalance data confirms:

| Rod | Item ID | Tier | Base price (raw data) | Efficiency | Energy on use |
|---|---|---:|---:|---:|---:|
| Simple Fishing Rod | `fishing_rod_0` | 1 | 2 | 0.75 | -1.0 |
| Good Fishing Rod | `fishing_rod_1` | 2 | 3 | 0.90 | -0.5 |
| Excellent Fishing Rod | `fishing_rod_2` | 3 | 5 | 1.00 | -0.3 |

Vanilla rod progression already improves energy efficiency. Better Fishing Rods should not casually add proportional energy penalties for x2/x3 catches.

## Fish selection and resolved waiting time

Current 1.407 `FishingGUI.ChangeState(WaitingForBite)` calls:

`GetRandomFish(ref _waiting_for_bite_delay)`

and stores the returned `FishDefinition` as `_fish_def`.

Current `GetRandomFish(ref float waiting_time)`:

1. refreshes the currently eligible weighted fish set;
2. performs one weighted random fish selection;
3. chooses the selected fish's no-bait or matching-bait `wait_time`;
4. multiplies that wait by `Random.Range(0.9f, 1.1f)`;
5. returns the already-selected `FishDefinition`.

Therefore the resolved native wait for one bite attempt is:

`selected fish/bait wait_time * random factor [0.9, 1.1]`

The mod must not reroll this selection.

## Native settling gate and wait consumption

Current `FishingGUI.UpdateWaitingForBite()` is explicit:

- if `can_take_out == false`, it returns immediately;
- otherwise it subtracts `Time.deltaTime` from `_waiting_for_bite_delay`;
- when the timer reaches zero, the bite occurs.

This establishes a native separation between cast/landing/settling and the actual fish-wait countdown.

For the accepted full-countdown visual, the preferred start point is the native transition to `can_take_out == true`, not the initial `WaitingForBite` state change.

Research Probe 0.1.1 confirms this owner directly on the current runtime. During one Excellent Fishing Rod cast, `WaitingForBite` began at t=65.699 with `can_take_out=false` and resolved wait 8.363159 s. `FishingThrowingAnim.OnStateExit` logged `can_take_out=false` before the call at t=69.689 and `can_take_out=true` after it at t=69.729, with the wait still 8.363159 s. The native settling phase was therefore about 4.03 s in that cast, and the actual fish-wait timer remained untouched until the animation exit.

## Catch construction

When the wait reaches zero, current `UpdateWaitingForBite()` constructs:

`new Item(_fish_def.item_id, 1)`

stores it in `_fish`, loads the selected fish preset, and enters `WaitingForPulling`.

This closes the construction question:

- vanilla fish identity is already fixed;
- vanilla initializes quantity to exactly 1;
- the item exists before the `WaitingForPulling` transition.

Leading least-sufficient amount seam: adjust `_fish.value` once when entering `WaitingForPulling`, based only on a recognized equipped rod. Unknown rods remain x1.

## Hook reaction window

Current `FishingGUI.ChangeState(WaitingForPulling)` reads:

`buff_fish_catch_time_mltplr`

Then:

- if its absolute value is <= 0.01, `_waiting_for_pulling_time = _fish_preset.catch_time`;
- otherwise, `_waiting_for_pulling_time = _fish_preset.catch_time * buff_fish_catch_time_mltplr`.

Current GameBalance contains vanilla `buff_fishing` with:

`buff_fish_catch_time_mltplr = 2`.

The vanilla reaction-time buff path is therefore directly verified.

Preferred mod architecture: scale the already-resolved `_waiting_for_pulling_time` by rod tier after vanilla has applied its own buff, preserving vanilla buff semantics.

A separate `buff_pulling_fish_mltplr` affects a different phase and must not be conflated with the bite reaction window.

## Bait / lure consumption

Current `ChangeState(TakingOut)` calls `RemoveBait(selectedBait)` once when bait is selected, before branching on fishing success.

Current `RemoveBait(Item bait)`:

- for a durability item with enough durability, subtracts exactly one `durability_decrease_on_use_speed`;
- otherwise removes exactly one bait item from player inventory.

Better Fishing Rods catch quantity must not multiply this vanilla bait/lure consumption.

## Successful catch / reward path

Current `FishingGUI.UpdateTakingOut()` confirms that on successful fishing the game:

1. calls `AchievementsSystem.CheckKeyQuests("fishing_success", 1)`;
2. passes the existing `_fish` to `MainGame.player.AddToInventory`;
3. if inventory insertion fails, passes that same `_fish` to `MainGame.player.DropItem`;
4. records fishing design/discovery state.

Consequences:

- one successful fishing cycle remains one success/quest event;
- changing only `_fish.value` does not inherently multiply that event;
- normal inventory/drop behavior remains host-owned.

Still requires live edge verification: amount > 1 through full/nearly-full inventory and world-drop fallback.

## Fishing visual objects

Current installed runtime confirms player fishing render objects including:

- `fishing FX`;
- `bobber`;
- `water_fx`;
- fish/fish-shadow render objects.

Probe 0.1.0 could not provide trustworthy mid-countdown visual snapshots because of its by-ref timing bug.

Research Probe 0.1.1 closed the lifecycle question with an unmodified wait:

- `FishingThrowingAnim.OnStateExit` is the current 1.407 transition that flips `can_take_out` from false to true;
- countdown observation began immediately afterward with 8.354834 s remaining;
- the game entered `WaitingForPulling` at t=78.013 and the observer ended at -0.000505 s remaining, confirming that this transition is aligned with the native timer reaching zero;
- `water_fx` remained inactive at countdown start, 75%, 50%, 25%, 10%, 3%, and the bite snapshot;
- `fishing FX` was active at throwing-animation exit but inactive throughout the actual wait and bite snapshot;
- `fishadow` remained inactive throughout those sampled waiting states;
- the active `bobber` renderer changed among `hero_fishing_idle_bobber_frm_*` sprites during the wait.

Therefore the production start/end lifecycle for a full countdown is now verified. The sampled objects do **not** prove where the player-visible vanilla ripple graphic is authored. If such a ripple is visible during this phase, it is not represented by an active `water_fx` or `fishing FX` object at the sampled moments; it may be part of the bobber sprite/animation or another uncaptured renderer.


### Bobber anchor geometry

Visual Prototype 0.2.1 is rejected anchor evidence: it used a hand-selected constant `1.90f` local X offset rather than host data.

Anchor Probe 0.2.2 then measured the actual waiting-state bobber geometry in the user's current 1.407 runtime at `sea_fishing_spot` for all three cast distances:

- distance 1: bobber local position X = `-0.875`;
- distance 2: bobber local position X = `-1.583`;
- distance 3: bobber local position X = `-2.938`;
- all sampled `hero_fishing_idle_bobber_frm_01..10` frames had the same sprite rect `240x144`, pivot `(120,72)`, PPU `48`, and tight mesh extent X = `-1.500..-1.146`, Y = `-0.271..0`;
- the sampled alpha crop was likewise identical across the three distances.

This closes the main coordinate question: cast distance is already represented by the native bobber transform, while the visible float graphic is offset inside that transform by the native sprite mesh. No location/distance coordinate table is needed.

The least-sufficient visual anchor is therefore the center of the **current waiting sprite's tight mesh**, derived at runtime from `Sprite.vertices` after the throwing animation has yielded to the idle/waiting sprite. For the measured frames this is approximately `(-1.323, -0.1355)` in bobber-local coordinates. This value is evidence, not a production constant: production should calculate it from the current sprite geometry. Parent/player mirroring and any renderer flip must remain host-owned.

Visual Prototype 0.2.3 runtime feedback accepts the **global** data-derived anchor behavior across the tested location/distances: the ring follows the correct cast target. The remaining mismatch is local presentation, not host positioning.

The supplied close-up shows the ring convergence point approximately 4 rendered pixels left of the visible red float center, with no material vertical mismatch. Visual Prototype 0.2.4 therefore keeps the current tight-mesh anchor and adds a measured **-4 sprite-pixel X correction** in bobber-local space, converted through the current sprite PPU. Because the bobber remains under the host's mirrored player hierarchy, this correction mirrors with the host rather than introducing location/distance tables.

The user also requested lower opacity because 0.2.3 is too bright at night. This is a presentation-only acceptance item; 0.2.4 reduces alpha while leaving the countdown timing and geometry unchanged.

## Localization / rod descriptions

Pending:

- exact localization keys for rod names/descriptions;
- whether current rod descriptions can be extended;
- least invasive localization/data path.

## Open runtime evidence

Highest-value remaining questions:

1. visual acceptance of the data-derived tight-mesh bobber anchor (including an opposite-facing edge if available);
2. live amount > 1 inventory/drop behavior;
3. quality/rare fish quantity preservation;
4. rod localization path.

### Temporary visual calibration gate

Visual Prototype 0.2.4 confirmed that a fixed art correction can overshoot even when the global host-derived anchor is correct. For the next research-only calibration build, the hard-coded art correction is removed and replaced by two temporary BepInEx integer settings visible in Configuration Manager:

- `Position X (pixels)`: -5..+5, integer, positive = screen-right;
- `Position Y (pixels)`: -5..+5, integer, positive = screen-up.

The values are applied live while the countdown ring is active. They are layered only over the verified current-sprite tight-mesh anchor and do not affect fishing gameplay. Initial calibration defaults are X=+2, Y=-2 based on the 0.2.3/0.2.4 visual feedback. Once the user accepts exact values, production should freeze the accepted art offset and remove these temporary calibration settings unless a separate user-facing configuration requirement is established.

### Accepted countdown visual values and missed-hook lifecycle

User runtime acceptance for Visual Prototype 0.2.5:

- accepted visual offset: **X=+2, Y=-2 game pixels** over the verified current-sprite tight-mesh anchor;
- accepted opacity: the 0.2.4/0.2.5 alpha profile is good enough to freeze; further reduction would be taste-only;
- temporary F1 calibration is no longer required in production.

The same runtime log exposed one lifecycle gap: after a bite reaches `WaitingForPulling`, a missed hook can cause vanilla to transition back to `WaitingForBite` and resolve a new fish/wait without replaying `FishingThrowingAnim.OnStateExit`. The 0.2.5 ring correctly stopped at the first `WaitingForPulling` but did not restart for that retry wait.

Least-sufficient correction: keep `FishingThrowingAnim.OnStateExit` as the initial-cast start owner, and additionally re-arm the ring when `FishingGUI.ChangeState` finishes in `WaitingForBite` **only if** `can_take_out == true`. On an initial cast this condition is false until the throw animation exits; on a missed-hook retry it remains true, allowing the ring to restart without introducing polling or a second gameplay timer.
