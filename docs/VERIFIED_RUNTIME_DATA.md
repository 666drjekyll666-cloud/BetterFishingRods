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

Historical decompilation identifies `FishingThrowingAnim.OnStateExit` as the owner that sets `can_take_out=true`; Research Probe 0.1.1 will confirm that exact owner on the current runtime before production relies on it.

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

Research Probe 0.1.1 is scoped to:

- the native `can_take_out` transition;
- `FishingThrowingAnim.OnStateExit` correlation;
- visual snapshots across a real, unmodified wait.

## Localization / rod descriptions

Pending:

- exact localization keys for rod names/descriptions;
- whether current rod descriptions can be extended;
- least invasive localization/data path.

## Open runtime evidence

Highest-value remaining questions:

1. current owner/asset for the visible vanilla ripple or equivalent fishing FX;
2. exact production visual mechanism for the full countdown;
3. live amount > 1 inventory/drop behavior;
4. quality/rare fish quantity preservation;
5. rod localization path.
