# Better Fishing Rods Research Probe 0.1.1

Research-only diagnostic for Graveyard Keeper 1.407.

## Purpose

One normal fishing cast should close the remaining visual/lifecycle evidence gaps without changing gameplay. Version 0.1.1 removes the GetRandomFish patch that contaminated live wait timing in 0.1.0.

The probe:

- observes the intact resolved wait after vanilla selection;
- identifies the native `can_take_out` countdown start;
- correlates that start with `FishingThrowingAnim.OnStateExit`;
- snapshots the in-world `bobber`, `fishing FX`, `water_fx` and fish-shadow objects through the real wait and at the bite transition.

## Safety

The probe does not change fish tables, catch amount, waiting time, hook timing, or save data. It does not add the proposed visual marker.

It only observes runtime state and writes one text log in the BepInEx root.

## Evidence artifact

Expected output:

`BepInEx/BetterFishingRodsResearchProbe-0.1.1.log`

## Runtime test

After a clean build is handed off:

1. install the research-probe DLL;
2. launch the game and load a normal save;
3. perform one complete fishing cast with any convenient rod/bait;
4. exit or return to menu;
5. return the single research-probe log.

No special timing, screenshots, console commands, or manual value reading should be necessary.
