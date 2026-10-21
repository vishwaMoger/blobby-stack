#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace StackJump
{
    // Editor-only: records the CrazyGames preview trailers (landscape 1920x1080 and portrait
    // 1080x1620, 30 fps, no audio) with Unity's built-in H.264 encoder. Started by GameManager
    // in trailer mode; see StackJumpSetup.CaptureTrailer. Every frame is a fixed 1/30 s step,
    // so the footage is smooth regardless of how fast the editor renders.
    //
    // Cut: cover still (1.5 s) -> solo climb through a world change (11 s)
    //      -> 2-player race (5 s) -> cover still (1 s)
    //
    // The game cameras render into `game`; the UI camera draws that as a full-screen backdrop with
    // the HUD on top into `final`, which is what gets encoded.
    public class TrailerDirector : MonoBehaviour
    {
        const int Fps = 30;

        public GameManager Game;
        public Camera Cam, Cam2;
        public GameUI UI;

        string outDir, coverDir;
        Camera uiCam;
        RenderTexture game, final;
        Texture2D frame;
        int frameIndex;

        IEnumerator Start()
        {
            var parts = (System.Environment.GetEnvironmentVariable("SJ_TRAILER") ?? "").Split('|');
            outDir = parts[0];
            coverDir = parts.Length > 1 ? parts[1] : parts[0];
            Directory.CreateDirectory(outDir);
            Time.captureFramerate = Fps;

            var go = new GameObject("UI Capture Camera");
            uiCam = go.AddComponent<Camera>();
            uiCam.clearFlags = CameraClearFlags.SolidColor;
            uiCam.backgroundColor = Color.black;
            uiCam.cullingMask = 1 << 5;
            uiCam.allowHDR = false;
            uiCam.allowMSAA = false;
            var data = uiCam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false;
            data.renderShadows = false;
            uiCam.enabled = false;

            // Opening/closing frames use the final cover art (the portrait from its larger original).
            yield return Record(1920, 1080, "landscape", "art-landscape-1920x1080.png");
            yield return Record(1080, 1620, "portrait", Path.Combine("source", "art-portrait-original.png"));
            UnityEditor.EditorApplication.Exit(0);
        }

        IEnumerator Record(int w, int h, string name, string coverFile)
        {
            game = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            final = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            game.Create();
            final.Create();
            Cam.targetTexture = game;
            Cam2.targetTexture = game;
            uiCam.targetTexture = final;
            UI.SetCaptureCamera(uiCam, game);
            frame = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var cover = LoadCover(Path.Combine(coverDir, coverFile), w, h);
            var preview = Path.Combine(outDir, "frames-" + name);
            Directory.CreateDirectory(preview);
            foreach (var old in Directory.GetFiles(preview)) File.Delete(old);
            frameIndex = 0;

            var attrs = new VideoTrackAttributes
            {
                frameRate = new MediaRational(Fps),
                width = (uint)w,
                height = (uint)h,
                includeAlpha = false,
                bitRateMode = UnityEditor.VideoBitrateMode.High,
            };
            using (var encoder = new MediaEncoder(Path.Combine(outDir, $"trailer-{name}.mp4"), attrs))
            {
                for (int i = 0; i < 45; i++) encoder.AddFrame(cover);

                Game.TrailerSolo(0, 15);
                for (int i = 0; i < 6; i++) yield return null;
                for (int i = 0; i < 330; i++)
                {
                    yield return null;
                    Capture(encoder, preview);
                }

                Game.TrailerVersus(3);
                for (int i = 0; i < 6; i++) yield return null;
                for (int i = 0; i < 150; i++)
                {
                    yield return null;
                    Capture(encoder, preview);
                }

                for (int i = 0; i < 30; i++) encoder.AddFrame(cover);
            }

            Cam.targetTexture = null;
            Cam2.targetTexture = null;
            uiCam.targetTexture = null;
            game.Release();
            final.Release();
            Debug.Log($"[TrailerDirector] Saved trailer-{name}.mp4 ({w}x{h}, {(45 + 330 + 150 + 30) / (float)Fps:F1} s)");
        }

        void Capture(MediaEncoder encoder, string previewDir)
        {
            Cam.Render();
            if (Cam2.enabled) Cam2.Render();
            uiCam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = final;
            frame.ReadPixels(new Rect(0, 0, final.width, final.height), 0, 0);
            frame.Apply();
            RenderTexture.active = prev;
            encoder.AddFrame(frame);
            // A few stills to review the cut without a video player.
            if (frameIndex % 60 == 0) File.WriteAllBytes(Path.Combine(previewDir, $"frame-{frameIndex:D3}.jpg"), frame.EncodeToJPG(85));
            frameIndex++;
        }

        static Texture2D LoadCover(string path, int w, int h)
        {
            var src = new Texture2D(2, 2);
            src.LoadImage(File.ReadAllBytes(path));
            var tmp = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(src, tmp);
            var prev = RenderTexture.active;
            RenderTexture.active = tmp;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(tmp);
            return tex;
        }
    }
}
#endif
