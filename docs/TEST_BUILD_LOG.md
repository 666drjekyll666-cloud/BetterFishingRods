# Test Build Log

Numbered binaries handed to the user are immutable. Record exact identity and requested runtime evidence here.

| Version | Exact commit SHA | Branch / frozen ref | Purpose | Requested test | Result |
|---|---|---|---|---|---|
| Research Probe 0.1.0 | `3bdc35add2610b8445de5f13c47e4c7dbd51f1d3` | `research/fishing-runtime` | Fishing lifecycle / timing / visual evidence | One complete normal fishing cast | Build run `35511927630`, artifact `10605956480`, DLL SHA-256 `481fe917054b5ef8f7aac3890387684df20e71d8386514dc0faa9ef738963177`. Runtime log received. **Diagnostic defect:** generic Harmony `__args` handling overwrote `GetRandomFish(ref waiting_time)` with zero after the method returned. Captured IL is valid; live wait/visual timing after that call is invalid. Superseded by 0.1.1. |
| Research Probe 0.1.1 | pending | `research/fishing-runtime` | Corrected observation-only settling/countdown/visual evidence | One normal complete fishing cast; return `BepInEx/BetterFishingRodsResearchProbe-0.1.1.log` | Build pending |
