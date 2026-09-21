# Changelog

## 1.0.3

- Fixed horizontal countdown-ring alignment when fishing to the left.
- Reduced the ring's base blue RGB brightness while preserving the accepted alpha curve.
- Added lightweight ambient-aware RGB scaling so the ring is calmer in dark scenes and remains readable in brighter scenes.
- Uses the game's final ambient light rather than a direct Keeper's Lantern integration, so the behavior also works with vanilla lighting.
- No fishing balance, timing, catch, bait, energy, save, or minigame behavior changes.

## 1.0.1

- First stable release under the final **Bite Countdown** identity.
- Added a subtle shrinking ring around the fishing bobber.
- The ring follows the native fish wait and reaches the bobber at the bite.
- Missed hooks correctly re-arm the ring when vanilla starts another wait.
- Uses the accepted visual alignment and opacity.
- Does not change fishing balance, catches, bait, energy, or the reeling minigame.
- Uses clean product identity: `Bite Countdown`, `nikich.bitecountdown`, and `BiteCountdown.dll`.
- Creates no separate diagnostic log file.
