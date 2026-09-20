# Research Plan

Research target: **Graveyard Keeper 1.407**.

Production implementation waits until the assumptions used by production code are verified at the cheapest reliable evidence level.

## Static research already closed

Current installed-runtime evidence now establishes:

- rod IDs are `fishing_rod_0`, `fishing_rod_1`, `fishing_rod_2`;
- rod efficiency is 0.75 / 0.90 / 1.00;
- rod energy use data is -1.0 / -0.5 / -0.3;
- fish definitions own fish/bait-specific `wait_time` values;
- `FishingGUI` has distinct `WaitingForBite` and `WaitingForPulling` states and a `_waiting_for_pulling_time` field;
- the game has a native `buff_fish_catch_time_mltplr` parameter path;
- successful catch handling passes the existing `_fish` object through normal inventory/drop handling;
- `fishing_success` is emitted once per successful fishing cycle;
- in-world fishing render objects include `fishing FX`, `bobber`, and `water_fx`.

## Remaining static/runtime discovery

### 1. Catch construction seam

Verify current 1.407 method bodies for:

- final fish selection;
- `_fish` construction;
- initial item quantity;
- the narrowest reliable point at which quantity can be changed once.

Acceptance target:

`vanilla selects fish -> amount adjusted once -> vanilla reward path continues`

### 2. Bite waiting seam

Verify:

- exact current method that assigns/consumes native bite delay;
- any final random variance after `FishDefinition+BaitData.wait_time`;
- whether the rod should scale the already-resolved wait rather than altering fish data globally.

Preferred architecture: modify the resolved wait for this cast only, after vanilla fish selection.

### 3. Hook reaction-window seam

Verify current method body that sets `_waiting_for_pulling_time`.

Determine exactly how:

- `FishPreset.catch_time`;
- `FISH_CATCH_TIME_MLTPLR`;
- active player buff values

combine.

Preferred architecture: supply a rod-tier modifier through the narrowest native input/parameter path while preserving the vanilla Fast Reflexes interaction.

### 4. Bait/lure lifecycle

Verify:

- consumable bait removal point;
- durability lure decrement point;
- one-cast semantics;
- behavior after failed hook vs successful catch.

x2/x3 catch amount must not silently multiply bait use unless explicitly chosen later.

### 5. Bite visual lifecycle

Inspect active fishing runtime at the bite transition.

Research clue from player reports: vanilla already appears to have a subtle ripple / thin blue ring around the bobber whose disappearance precedes the bite. Treat this as a hypothesis to verify in live runtime, not as an implementation fact.

Required evidence:

- whether the reported vanilla ripple is owned by `water_fx`, `bobber`, another fishing renderer, or an animation state;
- what `bobber`, `fishing FX`, and `water_fx` are doing during `WaitingForBite`;
- what changes exactly at `WaitingForPulling`;
- whether an existing ripple/sprite/animation can be reused;
- whether the cue can be started and stopped without broad `Update()` polling.

Leading visual experiment:

- a fixed-duration pre-bite ring starts shortly before the native bite;
- it contracts to the bobber;
- it reaches center at the native bite;
- vanilla sound remains unchanged.

Do not prototype a full-duration cast countdown first: current fish data proves that native wait carries fish/bait-specific information.

## Research-only harness gate

A small installed-runtime harness is justified only for questions that current static evidence cannot close efficiently.

If needed, one harness should cover all remaining live questions in one fishing session:

- dump exact target method IL/signatures;
- log selected rod/fish/bait;
- log native resolved wait;
- log native/effective hook window;
- log active fishing state transitions;
- snapshot `bobber` / `fishing FX` / `water_fx` components, sprites, transforms and active state around the bite;
- optionally test an isolated visual prototype only after the observation-only run identifies the correct presentation owner.

No save writes, no fish-table changes, no permanent diagnostics, and no broad per-frame scene searches.

## Edge semantics before production acceptance

Verify:

- Simple / Good / Excellent rods;
- common, quality and rare fish;
- no bait, consumable bait and durability lure;
- exact x1/x2/x3 quantity;
- no extra RNG;
- full/nearly full inventory and world drop;
- one achievement/quest success event per fishing cycle;
- vanilla fishing buffs still stack correctly;
- save/load and DLL removal.

## CI policy for this research

Do not run hosted CI merely for these documentation updates.

If a research harness becomes necessary, create one coherent source state first, then use one deliberate build gate for the actual DLL handoff rather than CI on every research commit.
