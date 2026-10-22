# Blobby Stack: Poki and GameDistribution

The same game ships to three portals from one project. Each portal gets its own build with only that
portal's SDK inside (the CrazyGames SDK's site-lock would crash the game anywhere else).

| Portal | Build command (`-executeMethod StackJump.EditorTools.StackJumpSetup.…`) | Output | Upload |
|---|---|---|---|
| CrazyGames | `BuildRelease` (Basic Launch) / `BuildReleaseFull` (Full Launch) | `Builds/WebGL`, `Builds/WebGL-full` | see `SUBMISSION.md` |
| Poki | `BuildPoki` | `Builds/Poki` | `upload/blobby-stack-poki.zip` |
| GameDistribution | `BuildGameDistribution` | `Builds/GameDistribution` | `upload/blobby-stack-gamedistribution.zip` |

Full headless command (Unity must be closed):

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe" -batchmode -projectPath C:\Crazygames\StackJump -buildTarget WebGL -executeMethod StackJump.EditorTools.StackJumpSetup.BuildGameDistribution -logFile Logs\BuildGD.log
```

Every portal build puts the project back into its CrazyGames configuration when it finishes.

---

## Poki

Upload the zip in **Poki for Developers** (developers.poki.com) and use their in-browser QA tool (Poki Inspector) before
requesting review.

**How the game follows Poki's rules**

| Rule | What the game does |
|---|---|
| `gameLoadingFinished` once loaded | Sent as soon as the SDK is ready |
| `gameplayStart` on the first input, `gameplayStop` on any break | Sent on the player's first tap of each run; stopped on death, pause and ads |
| `commercialBreak` at natural breaks | Before every PLAY AGAIN / rematch and when unpausing. Poki decides whether an ad actually shows |
| No ad cooldowns of our own | The ad revive is offered every good run (no "every 3rd run" limit) |
| Rewarded buttons: not green, video icon, normal option same size or larger | REVIVE and x2 COINS are orange with a clapperboard icon; PLAY AGAIN sits above them at the same size |
| No custom "ad blocked / not available" messages | None shown; rewarded buttons simply don't appear when no ad can play |
| Pause | Pause button, Esc or P; game freezes and the sound stops |
| Mute and freeze during ads | Audio volume 0 and time frozen until Poki calls back |
| No external links or requests | None; saves use local storage only |
| Mobile pill | Moved below the coin and best-score badges |

**Size:** 8.2 MB in total (Brotli). Poki prefers smaller; the remaining bulk is the engine code (5.8 MB).

---

## GameDistribution

1. Sign in at **developer.gamedistribution.com**, create the game and copy its **Game ID**.
2. Put the ID in `Submission/gamedistribution/game-id.txt` and run `BuildGameDistribution`; the build writes
   it into `index.html`. (Or replace `__GD_GAME_ID__` in `Builds/GameDistribution/index.html` by hand.)
3. Tick **Rewarded ads** for the game in the control panel. Without that flag the SDK never offers rewarded
   ads and the REVIVE / x2 COINS buttons stay hidden.
4. Zip the *contents* of `Builds/GameDistribution` (index.html at the zip root) and upload it.
5. Open the game page in the control panel and watch (or skip) one video ad there; GameDistribution only
   marks the SDK as active after that.
6. Request publication. Review takes up to 2 days.

**How the game follows GameDistribution's rules**

| Rule | What the game does |
|---|---|
| Load the SDK once while the game loads | The page template loads it before the game starts loading |
| Pre-roll before the player starts, on user input | The first PLAY press of a session shows an ad. New players start from the menu on this portal (not dropped straight into a run) so that the first ad is always triggered by a tap |
| Mid-roll between sessions, on user input | `showAd()` before every PLAY AGAIN, rematch and unpause; the SDK limits how often an ad really plays |
| Pause and mute on `SDK_GAME_PAUSE`, resume on `SDK_GAME_START` | Handled, including pauses the SDK starts by itself |
| Reward only on `SDK_REWARDED_WATCH_COMPLETE` | The revive / double coins are granted only when that event fired before the ad closed |
| Rewarded ad preloaded | `preloadAd('rewarded')` at start and after every rewarded ad; the buttons only show when one is ready |
| Ad blocker or SDK missing | The game starts after at most 8 s without ads; it never waits on the SDK forever |

**Size:** 8.1 MB in total (Brotli with decompression fallback, so it loads whatever headers the host sends).

**Testing locally:** ads don't serve on localhost. Open the browser console and run `gdsdk.openConsole()` to get
GameDistribution's test ads, then check: PLAY shows an ad first, the game is silent and frozen during it, and
the REVIVE ad grants the revive only when watched to the end.

---

## Listing text (both portals)

Use the same texts as `SUBMISSION.md` section 2 (title, short description, description, controls, tags).
Two changes:

- Neither portal shows the CrazyGames "Basic / Full Launch" distinction; describe the game as is.
- On Poki the pause key is **Esc or P**; on GameDistribution too. Add it to the controls: "Esc / P: pause".

## Images

**GameDistribution:** ready-made in `covers/gamedistribution/` (made by `Tools/gd_covers.py` from the art set):

| File | Assets tab slot |
|---|---|
| `blobby-stack-512x384.jpg` | Required (main thumbnail) |
| `blobby-stack-512x512.jpg` | Required (main thumbnail) |
| `blobby-stack-200x120.jpg` | Required (main thumbnail) |
| `blobby-stack-1280x720.jpg` | Helpful for marketing |
| `blobby-stack-1280x550.jpg` | Helpful for marketing |

The Assets tab only accepts .jpg/.jpeg at exactly these sizes.

**Poki and anything else:** use the **art** cover set from `covers/` (`art-landscape-1920x1080.png`, `art-portrait-800x1200.png`,
`art-square-800x800.png`) and resize to whatever sizes each dashboard asks for. The originals are in
`covers/source/` if a dashboard wants a larger size. The gameplay trailers in `trailer/` work for both portals.

## Quick QA (each build)

- [ ] Loads without errors in the console; no request to crazygames.com
- [ ] First tap starts the run; the tutorial hint disappears after the first block
- [ ] Die without tapping: the game over screen works and PLAY AGAIN starts a new run immediately (no freeze)
- [ ] PLAY AGAIN triggers the portal's ad call, then the new run starts
- [ ] Pause with the button, Esc and P; RESUME and MENU both work
- [ ] REVIVE ad: watched to the end → revive; closed early → no revive, game continues normally
- [ ] Sound mutes during ads and comes back afterwards
- [ ] Phone in portrait and landscape: every button on screen and tappable
