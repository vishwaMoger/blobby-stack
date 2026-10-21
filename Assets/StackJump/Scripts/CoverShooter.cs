#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;

namespace StackJump
{
    // Editor-only: renders the posed cover scene (?shot) into store-cover sizes and quits.
    // Started by GameManager when the SJ_CAPTURE environment variable is set; see
    // StackJumpSetup.CaptureCovers. Portrait and square are rendered at 2x for crisp downscaling.
    public class CoverShooter : MonoBehaviour
    {
        public Camera Cam;
        public string OutDir, Prefix;

        IEnumerator Start()
        {
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude)) canvas.enabled = false;
            Directory.CreateDirectory(OutDir);
            var sizes = new[] { (1920, 1080, "landscape"), (1600, 2400, "portrait"), (1600, 1600, "square") };
            foreach (var (w, h, name) in sizes)
            {
                var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                Cam.targetTexture = rt;
                // Let the camera rig and sky settle at this aspect ratio, then catch a sparkle burst
                // while it's at full size.
                for (int i = 0; i < 40; i++) yield return null;
                SendMessage("ShotBurst");
                yield return new WaitForSecondsRealtime(0.22f);
                Cam.Render();
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                File.WriteAllBytes(Path.Combine(OutDir, $"raw-{Prefix}-{name}.png"), tex.EncodeToPNG());
                Cam.targetTexture = null;
                rt.Release();
                Debug.Log($"[CoverShooter] Saved {Prefix}-{name} {w}x{h}");
            }
            UnityEditor.EditorApplication.Exit(0);
        }
    }
}
#endif
