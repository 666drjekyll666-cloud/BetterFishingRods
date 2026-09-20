# Better Fishing Rods

A small quality-of-life mod for **Graveyard Keeper 1.407**.

Better Fishing Rods adds a subtle shrinking ring around the fishing bobber while you wait for a bite. The ring follows the game's real waiting time and reaches the bobber when the native bite happens.

## What it changes

- Adds an in-world countdown ring around the bobber.
- Works across the full native bite-wait interval.
- Appears again if you miss a hook and the game starts another wait.

## What it does not change

Version 1.x does **not** change fishing balance:

- no faster bites;
- no longer hook window;
- no extra fish;
- no changes to fish selection, rarity, quality, bait, energy, or the reeling minigame.

The game still owns the entire fishing result. The mod only visualizes the waiting phase.

## Installation

1. Install BepInEx 5 for Graveyard Keeper.
2. Put `BetterFishingRods.dll` into `Graveyard Keeper/BepInEx/plugins/`.
3. Start the game.

No configuration is required.

## Compatibility

Targeted and tested against **Graveyard Keeper 1.407**.

The mod stores no custom save data. Removing `BetterFishingRods.dll` restores vanilla behavior without save migration.
