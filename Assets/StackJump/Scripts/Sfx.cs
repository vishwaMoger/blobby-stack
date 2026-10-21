using UnityEngine;

namespace StackJump
{
    // Sound effects synthesized at startup, so the build carries no audio files.
    public class Sfx : MonoBehaviour
    {
        const int Rate = 44100;

        AudioSource source, pitched;
        AudioClip jump, land, chime, hit, click, whoosh;

        public void Init()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            pitched = gameObject.AddComponent<AudioSource>();
            pitched.playOnAwake = false;

            jump = Tone("jump", 0.15f, 330f, 760f, 0f, 0.25f, 0.35f);
            land = Tone("land", 0.12f, 150f, 60f, 0.55f, 0f, 0.5f);
            hit = Tone("hit", 0.38f, 320f, 50f, 0.6f, 0.4f, 0.55f);
            click = Tone("click", 0.05f, 1100f, 900f, 0f, 0.5f, 0.25f);
            whoosh = Tone("whoosh", 0.9f, 200f, 900f, 0.85f, 0f, 0.3f, true);
            chime = Arpeggio("chime", new[] { 1046.5f, 1318.5f, 1568f, 2093f }, 0.065f, 0.3f);
        }

        public void Jump() => source.PlayOneShot(jump, 0.8f);
        public void Land() => source.PlayOneShot(land, 0.9f);
        public void Hit() => source.PlayOneShot(hit, 1f);
        public void Click() => source.PlayOneShot(click, 0.8f);
        public void World() => source.PlayOneShot(whoosh, 0.9f);

        public void Perfect(int streak)
        {
            pitched.pitch = 1f + Mathf.Min(streak - 1, 6) * 0.06f;
            pitched.PlayOneShot(chime, 0.75f);
        }

        static AudioClip Tone(string name, float dur, float f0, float f1, float noise, float square, float vol, bool swell = false)
        {
            int n = (int)(dur * Rate);
            var data = new float[n];
            var rng = new System.Random(name.Length * 7919);
            float phase = 0f, lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate, u = t / dur;
                phase += 2f * Mathf.PI * Mathf.Lerp(f0, f1, u) / Rate;
                float s = Mathf.Sin(phase);
                s = Mathf.Lerp(s, Mathf.Sign(s) * 0.6f, square);
                lp += ((float)rng.NextDouble() * 2f - 1f - lp) * (swell ? 0.08f + u * 0.3f : 0.25f);
                s = Mathf.Lerp(s, lp * 1.6f, noise);
                float env = swell ? Mathf.Sin(u * Mathf.PI) : Mathf.Min(1f, t * 400f) * (1f - u) * (1f - u);
                data[i] = s * env * vol;
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Arpeggio(string name, float[] notes, float step, float vol)
        {
            float tail = 0.25f;
            int n = (int)((notes.Length * step + tail) * Rate);
            var data = new float[n];
            for (int k = 0; k < notes.Length; k++)
            {
                int start = (int)(k * step * Rate);
                for (int i = start; i < n; i++)
                {
                    float t = (i - start) / (float)Rate;
                    float env = Mathf.Min(1f, t * 600f) * Mathf.Exp(-t * 9f);
                    float s = Mathf.Sin(2f * Mathf.PI * notes[k] * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * notes[k] * t);
                    data[i] += s * env * vol * 0.6f;
                }
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
