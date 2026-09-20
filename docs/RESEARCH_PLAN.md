# Research Plan

Research target: **Graveyard Keeper 1.407**.

Production implementation must wait until the assumptions below are verified at the cheapest reliable evidence level.

## Phase 1 — Static runtime discovery

Inspect current 1.407 assemblies/data for:

1. the three fishing-rod IDs and definitions;
2. the fishing state machine and catch-selection path;
3. the final `Item` creation/grant path;
4. the source of waiting-for-bite time;
5. the source of the hook reaction window;
6. the Fast Reflexes effect and its consumer;
7. bait/lure consumption;
8. achievement/statistic calls on successful catch;
9. bobber, water ripple, fishing animation, and bite-cue resources/events;
10. localization/data paths for rod descriptions.

## Phase 2 — Mechanism selection

For each feature choose the least sufficient host-native mechanism:

### Catch amount
Prefer adjusting the final already-selected fish `Item.amount` once, immediately before vanilla reward handling.

### Faster bite
Prefer modifying the native waiting-time input/result after vanilla has selected the fish, without rerolling or bypassing fish selection.

### Longer hook window
Prefer using the same native parameter/effect path consumed by vanilla Fast Reflexes, if verified suitable.

### Bite readability
First preference: reuse or augment an existing bobber/ripple/animation effect.

Leading UX candidate:

- no full cast-to-bite timer by default;
- shortly before the native bite, show a subtle contracting ring/ripple around the bobber;
- ring reaches center at the native bite moment;
- keep vanilla sound unchanged.

Investigate whether the cue can be driven by the same native waiting countdown without creating a separate source of truth.

## Phase 3 — Edge semantics

Verify before implementation is considered complete:

- full and nearly full inventory;
- stacked fish and quality-bearing fish;
- rare/special fish;
- bait and durability-based lures;
- achievements/statistics;
- save/load and DLL removal;
- unsupported/unknown rod fallback;
- interaction with vanilla fishing buffs.

## Phase 4 — Runtime harness only if needed

If static inspection cannot prove several timing/visual questions, create one research-only harness rather than multiple manual tests.

Potential probe outputs:

- selected rod ID;
- selected fish ID;
- native wait time;
- effective modified wait time;
- native hook window;
- effective hook window;
- active Fast Reflexes value;
- catch amount before/after adjustment;
- bite-cue trigger timestamp vs native bite timestamp.

No per-frame verbose logging in production.

## External research clues — not verified project facts

These clues narrow the investigation but do not satisfy the evidence gate:

- A historical decompilation shows fish selection occurring before the wait, with a fish/bait-specific waiting time and small random variance.
- The same historical path shows a dedicated waiting-for-pulling window and a player parameter named like a fishing catch-time multiplier.
- Historical code also shows successful fishing granting one `Item`, then using normal inventory add/drop fallback and firing fishing achievements/events.
- Current third-party mods built for the modern game still reference `FishingGUI`, `GetRandomFish`, and waiting-for-bite state, suggesting the broad architecture remains recognizable.
- Player reports repeatedly describe the small bobber/ripple cue as easy to miss and the hook timing as unusually strict; some also report the passive waiting itself as tedious.

All of the above must be checked against the actual 1.407 runtime before production code relies on it.
