<p align="center">
  <img width="100%" src="https://capsule-render.vercel.app/api?type=waving&color=0:5B2EA6,50:B388FF,100:FFC93C&height=220&section=header&text=BLOBBY%20STACK&fontSize=64&fontColor=ffffff&fontAlignY=36&stroke=000000&strokeWidth=2&desc=Tap%20%C2%B7%20Hop%20%C2%B7%20Stack%20to%20the%20stars&descSize=20&descAlignY=58&animation=twinkling" alt="BLOBBY STACK" />
</p>
<p align="center">
  <img width="100%" src="Submission/covers/art-landscape-1920x1080.png" alt="Blobby Stack cover" />
</p>
<p align="center">
  <a href="https://bluelolipop1.itch.io/blobby-stack"><img src="https://img.shields.io/badge/%E2%96%B6%20PLAY%20FREE%20ON%20ITCH.IO-FA5C5C?style=for-the-badge&logo=itchdotio&logoColor=white" alt="▶ PLAY FREE ON ITCH.IO" height="46" /></a>
</p>
<p align="center"><sub>Free in your browser · no download · desktop &amp; mobile, portrait &amp; landscape</sub></p>
<p align="center">
  <img src="https://img.shields.io/badge/Unity-6.4-000000?style=for-the-badge&logo=unity&logoColor=white" alt="Unity: 6.4" />
  <img src="https://img.shields.io/badge/Render-URP-5B2EA6?style=for-the-badge&logo=unity&logoColor=white" alt="Render: URP" />
  <img src="https://img.shields.io/badge/C%23-Gameplay-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt="C#: Gameplay" />
  <img src="https://img.shields.io/badge/Art-100%25%20Procedural-B388FF?style=for-the-badge" alt="Art: 100% Procedural" />
  <img src="https://img.shields.io/badge/Portals-CrazyGames%20%7C%20Poki%20%7C%20GD%20%7C%20itch-FFC93C?style=for-the-badge" alt="Portals: CrazyGames | Poki | GD | itch" />
  <img src="https://img.shields.io/badge/Build-~8%20MB-2ea44f?style=for-the-badge" alt="Build: ~8 MB" />
</p>
<p align="center">
  <img src="https://readme-typing-svg.demolab.com?font=Press+Start+2P&size=16&duration=2600&pause=700&color=FFC93C&center=true&vCenter=true&width=760&height=50&lines=%3E+TAP+AT+THE+PERFECT+MOMENT;%3E+PERFECT%21+x3+STREAK%21;%3E+HOW+HIGH+CAN+YOU+STACK%3F" alt="> TAP AT THE PERFECT MOMENT / > PERFECT! x3 STREAK! / > HOW HIGH CAN YOU STACK?" />
</p>

---

<h2 align="center">🎮 GAMEPLAY</h2>

<p align="center">
  <img width="100%" src="docs/trailer.gif" alt="Blobby Stack gameplay trailer" />
</p>
<p align="center">Blocks slide in from the side: tap at just the right moment to hop over them and land on top.<br/>Nail the timing for a <b>PERFECT</b> landing and chain bonus points, but jump too early or too late and you get bumped off the tower.</p>
<p align="center"><sub>Full-quality trailers: <a href="Submission/trailer/trailer-landscape.mp4">landscape</a> · <a href="Submission/trailer/trailer-portrait.mp4">portrait</a></sub></p>

<p align="center">
  <img width="100%" src="docs/readme/features.svg" alt="Game features: one tap, perfect streaks, 7 worlds, 7 characters, daily tower, 2-player race" />
</p>

---

<h2 align="center">🌌 SEVEN WORLDS</h2>

<p align="center">
  <img width="49%" src="Submission/covers/cosmos-landscape-1920x1080.png" alt="🌌 Cosmos" />
  <img width="49%" src="Submission/covers/beanstalk-landscape-1920x1080.png" alt="🌱 Beanstalk" />
  <br/><sub>🌌 Cosmos &nbsp;·&nbsp; 🌱 Beanstalk</sub>
</p>
<p align="center">
  <img width="49%" src="Submission/covers/farm-landscape-1920x1080.png" alt="🌅 Sunset Farm" />
  <img width="49%" src="Submission/covers/neon-landscape-1920x1080.png" alt="🌃 Neon City" />
  <br/><sub>🌅 Sunset Farm &nbsp;·&nbsp; 🌃 Neon City</sub>
</p>
<p align="center"><sub>Plus Haunted Hill, Hot Kitchen and Golden Castle</sub></p>

---

<h2 align="center">🕹️ CONTROLS</h2>

| Action | Keyboard | Mouse / touch |
|---|---|---|
| Jump | Space, W or Up arrow | Click or tap |
| Pause | Esc or P | Pause button |

---

<h2 align="center">⚙️ UNDER THE HOOD</h2>

<p align="center">
  <img width="100%" src="docs/readme/tech.svg" alt="Under the hood: fully procedural art, one project for four portals, clean SDK layer, custom shaders, 8 MB builds, any screen" />
</p>

---

<h2 align="center">🛠️ FOR DEVELOPERS</h2>


<details>
<summary><b>🛠️ For developers: covers, requirements, portal builds and project layout</b></summary>

### Covers

The `art-*` set is the one used on the portals.

| Landscape (1920×1080) | Portrait (800×1200) | Square (800×800) |
|---|---|---|
| <img src="Submission/covers/art-landscape-1920x1080.png" width="420"> | <img src="Submission/covers/art-portrait-800x1200.png" width="200"> | <img src="Submission/covers/art-square-800x800.png" width="260"> |

### Requirements

- Unity **6000.4.5f1** with the WebGL build module
- Python 3 with Pillow, only for the cover scripts in `Tools/`

### Build

One project builds for several web portals. Each build contains only its own portal's SDK. Close the Unity
Editor first (a headless build can't open a project that is already open).

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe"
& $unity -batchmode -projectPath . -buildTarget WebGL -executeMethod StackJump.EditorTools.StackJumpSetup.<Method> -logFile Logs\build.log
```

| `<Method>` | Output | For |
|---|---|---|
| `BuildRelease` | `Builds/WebGL` | CrazyGames Basic Launch (no ads) |
| `BuildReleaseFull` | `Builds/WebGL-full` | CrazyGames Full Launch (ads) |
| `BuildPoki` | `Builds/Poki` | Poki |
| `BuildGameDistribution` | `Builds/GameDistribution` | GameDistribution |
| `BuildItch` | `Builds/Itch` | itch.io (no SDK, no ads); page text and images in [`Submission/itch/`](Submission/itch) |
| `BuildDev` | `Builds/WebGL-dev` | Local testing: uncompressed, with ads |

Serve a build locally with any static server, for example
`python -m http.server 8765 --directory Builds/WebGL-dev`, then open http://localhost:8765. Portal SDKs only
serve ads on their own sites; on localhost the game runs without them.

For GameDistribution, put your game ID in `Submission/gamedistribution/game-id.txt` (or set `SJ_GD_GAME_ID`)
before building. Builds are about 8 MB with Brotli compression.

### How the portals are handled

`Assets/StackJump/Scripts/Platform.cs` is the only code that talks to a portal SDK; the rest of the game only
sees its API. The portal is chosen by a scripting define (`SJ_POKI`, `SJ_GD`, or neither for CrazyGames, plus
`SJ_ADS` for ads), and the build methods also switch the matching JavaScript bridge and WebGL page template on
and off. The CrazyGames SDK has a site-lock that crashes the game on other domains, so the other builds leave
it out completely (assembly define constraints, plugin toggles, and its `Resources` folder parked during the
build).

Each portal's rules are followed in the game itself: when ads may play, how rewarded buttons look, pause
behaviour, and when gameplay start and stop are reported. The details and each portal's submission steps are in
[`Submission/SUBMISSION.md`](Submission/SUBMISSION.md) (CrazyGames) and
[`Submission/OTHER-PORTALS.md`](Submission/OTHER-PORTALS.md) (Poki and GameDistribution).

### Project layout

| Path | What's in it |
|---|---|
| `Assets/StackJump/Scripts` | Game code: lanes, tower, characters, UI, themes, economy, platform layer |
| `Assets/StackJump/Shaders` | Toon, unlit and sky shaders |
| `Assets/StackJump/Editor` | Project setup and the portal build methods |
| `Assets/WebGLTemplates` | Page templates for the default build, Poki and GameDistribution |
| `Assets/CrazySDK`, `Assets/PokiSDK` | The portals' official SDKs (see below) |
| `Tools/` | Cover scripts (`prepare_covers.py`, `compose_covers.py`, `gd_covers.py`) |
| `Submission/` | Store texts, covers, trailers and QA checklists |

### Third-party code and assets

- **CrazyGames SDK** and **Poki Unity SDK**: included unmodified in `Assets/CrazySDK` and `Assets/PokiSDK`
  under their own terms; they are not covered by this project's license.
- **Lilita One** font by Juan Montoreano: SIL Open Font License, see `Assets/StackJump/Licenses`.

</details>

### 📄 License

No license has been chosen yet, so all rights are reserved by the author.

---

<p align="center">
  <b>Made with ❤️ and 🫧 by <a href="https://github.com/vishwaMoger">Vishwa Moger</a></b><br/>
  <sub>Game Developer · 3+ years · Bangalore, India · <a href="https://bluelolipop1.itch.io">more games on itch.io</a> · <a href="https://vishwa-moger.vercel.app">portfolio</a></sub>
</p>

<img width="100%" src="https://capsule-render.vercel.app/api?type=waving&color=0:FFC93C,50:B388FF,100:5B2EA6&height=110&section=footer" />
