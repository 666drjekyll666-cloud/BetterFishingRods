# Better Fishing Rods Research Probe 0.1.0

Research-only diagnostic for Graveyard Keeper 1.407.

## Purpose

One normal fishing cast should close the remaining runtime evidence gaps without changing gameplay.

The probe:

- dumps current `FishingGUI` method IL relevant to fish selection, waiting, bait and catch handling;
- observes `GetRandomFish` output and resolved wait;
- logs fishing state transitions and relevant private fields;
- snapshots the in-world `bobber`, `fishing FX`, `water_fx` and fish-shadow objects after selection, shortly before the predicted bite, and at fishing state transitions.

## Safety

The probe does not change fish tables, catch amount, waiting time, hook timing, or save data. It does not add the proposed visual marker.

It only observes runtime state and writes one text log in the BepInEx root.

## Evidence artifact

Expected output:

`BepInEx/BetterFishingRodsResearchProbe-0.1.0.log`

## Runtime test

After a clean build is handed off:

1. install the research-probe DLL;
2. launch the game and load a normal save;
3. perform one complete fishing cast with any convenient rod/bait;
4. exit or return to menu;
5. return the single research-probe log.

No special timing, screenshots, console commands, or manual value reading should be necessary.
