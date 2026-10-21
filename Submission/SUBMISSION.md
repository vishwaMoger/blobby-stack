# Blobby Stack: CrazyGames submission pack

Everything needed to fill in the CrazyGames Developer Portal (https://developer.crazygames.com/games),
plus a QA checklist built from the CrazyGames requirements docs (docs.crazygames.com/requirements).

---

## 1. What to upload

| Launch phase | Folder to upload | Ads | Size |
|---|---|---|---|
| **Basic Launch** (first submission) | `Builds/WebGL` | None. CrazyGames disables ads in Basic Launch, so this build has no ad buttons at all | 11.0 MB, 5 files |
| **Full Launch** (once CrazyGames selects the game) | `Builds/WebGL-full` | Midgame between runs, rewarded revive / x2 coins / x2 daily reward | 11.0 MB, 5 files |

- Both are Brotli-compressed (CrazyGames only accepts compressed builds) and use relative paths.
- Initial download is 11 MB: under the 50 MB limit, and under the **20 MB limit for the mobile homepage**.
- Rebuild either one headlessly with `-executeMethod StackJump.EditorTools.StackJumpSetup.BuildRelease`
  or `...BuildReleaseFull` (Unity Editor closed).
- **Orientation:** Landscape (desktop). Mobile: supported in both orientations, safe to enable.

---

## 2. Listing text

**Title:** Blobby Stack

**Short description (one line):**
Tap to hop over sliding blocks, stack the tallest tower and climb through 7 colorful worlds!

**Description:**

> Blobby Stack is a cute one-tap stacking game. Blocks slide in from the side: tap at just the right
> moment to hop over them and land on top. Nail the timing for a PERFECT landing and chain bonus points,
> but jump too early or too late and you'll get bumped off the tower!
>
> Climb through 7 colorful worlds, each with its own blocks: moon rocks in the Cosmos, vine-wrapped stalks
> on the Beanstalk, coffins on Haunted Hill, hay bales on the Sunset Farm, ovens in the Hot Kitchen,
> bricks at the Golden Castle and glowing chips in Neon City.
>
> **Features**
> - One-button controls that anyone can learn in seconds
> - Perfect-timing streaks worth up to 3 points per block
> - 7 worlds to climb through, with new scenery every 20 blocks
> - 7 adorable characters to unlock with coins
> - Daily rewards and a new Daily Tower challenge every day
> - Local 2 Player race: share one keyboard or one screen with a friend

**Controls:**

| | Solo | 2 Player |
|---|---|---|
| Desktop | Space, W, Up arrow or left click to jump | Player 1: W · Player 2: Up arrow |
| Mobile | Tap anywhere to jump | Tap your half of the screen |

**Category:** Casual (alternatives: Arcade, 2 Player)

**Tags:** stacking, tower, one button, timing, jumping, cute, casual, arcade, 2 player, hyper casual

---

## 3. Covers

All three required sizes, rendered from the game itself and titled with the in-game font (Lilita One, SIL OFL).
They follow the cover rules: only the title as text, no borders, icons, store logos or "Play" words, and
the same look across all three.

Four sets, one per world. Each set has the same three files, named `<set>-landscape-1920x1080.png`,
`<set>-portrait-800x1200.png` and `<set>-square-800x800.png` in `covers/`:

| Set | Look |
|---|---|
| **cosmos** (recommended) | Purple space, ringed planet; matches the game's UI and the trailer's opening frame |
| farm | Pink sunset sky, hay bales, red tractors |
| neon | Synthwave grid, glowing chips |
| beanstalk | Bright blue sky, clouds, giant beanstalks |

Upload one set only; the three sizes must match each other. If you pick a set other than cosmos,
re-render the trailer so its opening frame matches (see section 4).
The `raw-*.png` files are untitled renders used to build the covers; don't upload those.

**Regenerating covers** (other worlds, other characters):

```powershell
$env:SJ_ARGS = "shot&world=5&char=0"   # world 0-6, character 0-6
$env:SJ_CAPTURE = "C:\Crazygames\StackJump\Submission\covers|castle"
& "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe" -batchmode -projectPath C:\Crazygames\StackJump -buildTarget WebGL -executeMethod StackJump.EditorTools.StackJumpSetup.CaptureCovers
python Tools\compose_covers.py castle
```

---

## 4. Preview trailer

| File | Format |
|---|---|
| `trailer/trailer-landscape.mp4` | 1920×1080, 30 fps, H.264, 18.5 s, no audio |
| `trailer/trailer-portrait.mp4` | 1080×1620 (2:3), 30 fps, H.264, 18.5 s, no audio |

Both meet the spec: 15–20 s, under 50 MB, no sound, no black bars, no logo intro, no cursor, no promo text,
real-time speed (not fast-forwarded), and they open on the static cover.

**Cut:** cover (1.5 s) → solo climb with HUD, PERFECT streaks and coins, crossing from Cosmos into the Beanstalk
world (11 s) → 2 Player split-screen race on the farm (5 s) → cover (1 s).

The trailers are rendered from the real game in the Unity Editor, frame by frame at a fixed 1/30 s step, and
encoded with Unity's built-in H.264 encoder. The `frames-*` folders hold preview stills of each clip.

**Re-rendering** (for example after gameplay changes, or with another cover set):

```powershell
$env:SJ_ARGS = "trailer"
$env:SJ_TRAILER = "C:\Crazygames\StackJump\Submission\trailer|C:\Crazygames\StackJump\Submission\covers"
& "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe" -batchmode -projectPath C:\Crazygames\StackJump -buildTarget WebGL -executeMethod StackJump.EditorTools.StackJumpSetup.CaptureTrailer
```

To open on a different cover set, change the cover file names in `TrailerDirector.Start`.

---

## 5. QA checklist

Legend: ✅ done and verified here · 🟡 done, needs your check on a real device or on CrazyGames · ⬜ to do

### Technical

| Check | Status | Notes |
|---|---|---|
| Initial download ≤ 50 MB (≤ 20 MB for mobile homepage) | ✅ | 11.0 MB |
| Total files ≤ 1,500 | ✅ | 5 files |
| Compressed build (Brotli/Gzip) | ✅ | Brotli |
| Relative paths only | ✅ | `index.html` loads `Build/...` relatively |
| SDK integrated, `gameplayStart` / `gameplayStop` | ✅ | Start on run start, stop on death/menu/ads |
| Loads with no console errors | ✅ | Fresh load: 0 errors |
| No freeze if the SDK is blocked or slow | ✅ | 8 s init timeout, then plays offline |
| Progress saving via SDK Data module | ✅ | Best, coins, characters, worlds, daily state; verified in the cloud-save store |
| Late SDK can't overwrite cloud progress | ✅ | Data source fixed at first read, local mirror |
| No memory growth in long sessions | ✅ | Soak test: score 83 across several worlds, memory flat at 97 MB |
| 60 fps on desktop | ✅ | Steady 60 fps, worst frame 19 ms (heaviest world, and with 2 cameras in 2P) |
| `user-select: none` on the page | ✅ | In the WebGL template |
| Physics independent of refresh rate | ✅ | Exact constant-gravity jump arc (fixed: at 30 fps the old integration lost height and made jumps fail), time-based movement, substepped springs |
| Runs smoothly on a 4 GB Chromebook | 🟡 | Untested. Shadows and bloom are the heaviest parts |
| Mobile: touch, safe areas, performance | 🟡 | Touch input is built in; test on a real phone (iOS is disabled by default for Unity games) |
| Works in Chrome and Edge | 🟡 | Developed against Chromium; check Edge once |

### Gameplay

| Check | Status | Notes |
|---|---|---|
| New players land straight in gameplay (Full Launch) | 🟡 | First-time players start a run immediately with a tap hint; returning players get the menu (one click to play). Logic is in place but not yet tested in a fresh browser profile. |
| English, readable at 800×450 | 🟡 | Smallest text raised to stay readable at 800×450; check on the CrazyGames QA tool's smallest size |
| No custom fullscreen button | ✅ | None |
| No external links or cross-promotion | ✅ | None |
| PEGI 12 content | ✅ | Cartoon characters, no violence beyond a comic knock-off |
| Avoids Escape and Ctrl+W | ✅ | Uses Space, W, Up arrow, Enter |
| Original name and assets | ✅ | Renamed from "Stack Hop" (an existing Devpost project). All art is procedural, and the font is OFL |
| Mute option | ✅ | Sound button; also follows the CrazyGames mute setting |

### Ads (Full Launch build only)

| Check | Status | Notes |
|---|---|---|
| Basic Launch build has no ad buttons | ✅ | Verified in the compiled code: rewarded checks are hard-wired off |
| Midgame only at logical points (after death → play again) | ✅ | Never on menu/navigation buttons |
| No midgame paired with a "keep playing" offer on the same death | ✅ | Skipped when revive was offered or a rewarded ad was watched |
| Pause and mute only while an ad plays; resume on error | ✅ | Plus an ad timeout so the game can't stay paused |
| No reward on ad error | ✅ | Error → no reward, ad buttons hide for the session |
| Rewarded offers aren't on every death | ✅ | Revive only after good runs (score ≥ 10 and ≥ half your best); ad revive at most every 3rd run |
| Rewarded buttons show the video icon; skip matches the reward button | ✅ | REVIVE and PLAY AGAIN share size, font and color |
| Coin alternative to the rewarded offer | ✅ | Revive for 100 coins |
| Ad blockers: nothing blocked, no dead buttons | ✅ | Ad blocker detected → rewarded buttons hidden, game unaffected |
| Ads play correctly on CrazyGames | 🟡 | Only tested with the SDK's local simulator; recheck in the QA tool |

### Covers and listing

| Check | Status | Notes |
|---|---|---|
| Landscape 1920×1080, portrait 800×1200, square 800×800 | ✅ | `covers/` |
| Only the title as text; no borders or icons | ✅ | |
| Description, controls, tags | ✅ | Section 2 |
| Preview video, landscape and portrait | ✅ | `trailer/`, section 4 |

### Before you press submit

1. Upload `Builds/WebGL` to the CrazyGames **QA tool** and play through: first-run flow (use a private
   window), menu, character shop, daily reward, Daily Tower, 2 Player, game over → play again.
2. Check it at the smallest preview size (800×450) and on a phone.
3. Pick the cover set (Cosmos recommended) and fill in the text from section 2.
4. Upload both trailers from `trailer/`.
