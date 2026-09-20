# Better Fishing Rods Anchor Probe 0.2.2

Research-only coordinate probe for Graveyard Keeper 1.407.

## Why this exists

Visual Prototype 0.2.1 used a hand-selected constant local X offset to move the countdown ring toward the visible float. Runtime feedback showed that this was not evidence-backed and remained wrong.

Current runtime evidence already shows that different cast distances use different bobber sprite sets. A production anchor must therefore be derived from current game data, not a guessed constant.

## What 0.2.2 records

For the active bobber during the real waiting phase:

- fishing spot ID, cast distance and fishing direction;
- bobber hierarchy path, local/world position and scale;
- player mirroring state;
- sprite rect, pivot, pixels-per-unit, bounds, packed state and mesh vertex extent;
- renderer world bounds;
- alpha-pixel bounds read from the actual runtime sprite texture;
- connected opaque components;
- left/right edge centroids;
- a compact alpha map for the first waiting sprite at each distance;
- a clearly labelled outer-edge candidate for analysis only.

The candidate is **not** production behavior. Its purpose is to help identify the actual float point from current runtime asset data.

## Safety

The probe renders nothing and changes no fishing behavior. It does not alter:

- fish selection or RNG;
- waiting duration;
- hook window;
- catch amount;
- bait/lure;
- energy;
- inventory;
- saves.

## Runtime test

1. Remove Visual Prototype 0.2.1.
2. Install Anchor Probe 0.2.2.
3. At one fishing spot, perform one cast at each of the three cast distances.
4. The catch does not need to be completed; once the waiting phase has begun for each distance, the required geometry has been captured.
5. Return only:

`BepInEx/BetterFishingRodsAnchorProbe-0.2.2.log`

This one log should be enough to decide the correct production anchor mechanism.
