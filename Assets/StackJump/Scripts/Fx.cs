using UnityEngine;

namespace StackJump
{
    // Particle bursts: sparkles on perfect landings, dust puffs, and confetti for a new best.
    public class Fx : MonoBehaviour
    {
        ParticleSystem sparkle, dust, confetti;

        public void Init()
        {
            sparkle = Make("Sparkle", Mats.MakeUnlit(UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.One, false, 3100, 0f, Tex.Star), 0.4f);
            dust = Make("Dust", Mats.MakeUnlit(UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha, false, 3050, 0f, Tex.Soft), 0f);
            confetti = Make("Confetti", Mats.MakeUnlit(UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha, false, 3060, 0f, Tex.Soft), 1.2f);
        }

        ParticleSystem Make(string name, Material material, float gravity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;
            main.gravityModifier = gravity;
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0.2f)));

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = material;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        public void Sparkle(Vector3 pos, Color tint, int count = 26)
        {
            var p = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                var dir = Random.onUnitSphere;
                dir.y = Mathf.Abs(dir.y) * 0.8f + 0.2f;
                p.position = pos + dir * 0.3f;
                p.velocity = dir * Random.Range(2f, 4.5f);
                p.startSize = Random.Range(0.18f, 0.42f);
                p.startLifetime = Random.Range(0.5f, 0.9f);
                p.rotation = Random.Range(0f, 360f);
                p.startColor = Color.Lerp(Color.white, tint, Random.Range(0f, 0.6f)) * 1.6f;
                sparkle.Emit(p, 1);
            }
        }

        public void Dust(Vector3 pos, Color tint)
        {
            var p = new ParticleSystem.EmitParams();
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0.15f, Mathf.Sin(a));
                p.position = pos + dir * 0.35f + Vector3.up * 0.05f;
                p.velocity = dir * Random.Range(1.2f, 2f);
                p.startSize = Random.Range(0.25f, 0.4f);
                p.startLifetime = Random.Range(0.3f, 0.45f);
                p.startColor = new Color(tint.r, tint.g, tint.b, 0.55f);
                dust.Emit(p, 1);
            }
        }

        public void Confetti(Vector3 pos, Color[] palette)
        {
            var p = new ParticleSystem.EmitParams();
            for (int i = 0; i < 70; i++)
            {
                var dir = Random.insideUnitSphere;
                dir.y = Mathf.Abs(dir.y) + 0.6f;
                p.position = pos;
                p.velocity = dir * Random.Range(4f, 8f);
                p.startSize = Random.Range(0.12f, 0.24f);
                p.startLifetime = Random.Range(1.2f, 2f);
                p.startColor = palette[Random.Range(0, palette.Length)];
                confetti.Emit(p, 1);
            }
        }
    }
}
