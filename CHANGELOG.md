# Changelog

## 1.0.2

- Fixed horizontal countdown-ring alignment when fishing to the left.
- Preserves the accepted right-facing alignment.
- The residual bobber-art correction now lives in sprite-local space, so the game's native left/right mirroring also mirrors that correction.
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
