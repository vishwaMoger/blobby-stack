using System;
using UnityEngine;

namespace StackJump
{
    // The hopping character: vertical jump physics, squash-and-stretch, blinking and knockout.
    public class Player : MonoBehaviour
    {
        public const float Radius = 0.41f;
        const float CharacterScale = 1.2f;
        const float JumpHeight = 1.45f;
        const float Gravity = 34f;

        public CharacterRig Rig { get; private set; }
        public bool Grounded { get; private set; } = true;
        public bool Dead { get; private set; }
        public float FeetY => transform.position.y;
        public float TimeSinceJump => Time.time - jumpTime;

        Func<float> support;
        Action onLanded;
        float vy, jumpTime;
        float squash, squashVel;
        float blinkTimer = 2f, blink = 1f;
        float look, lookTarget;
        Vector3 deathVel;
        Vector3 deathSpin;

        public void Init(Func<float> supportHeight, Action landed)
        {
            support = supportHeight;
            onLanded = landed;
        }

        public void SetCharacter(CharacterDef def, bool silhouette)
        {
            if (Rig != null) Destroy(Rig.Root.gameObject);
            Rig = CharacterFactory.Build(def, transform, silhouette);
            Rig.Root.localScale = Vector3.one * CharacterScale;
            Kick(0.4f);
        }

        public void ResetTo(float y)
        {
            Dead = false;
            Grounded = true;
            vy = 0;
            transform.SetPositionAndRotation(new Vector3(0, y, 0), Quaternion.identity);
            Rig?.SetDead(false);
        }

        public bool Jump()
        {
            if (!Grounded || Dead) return false;
            vy = Mathf.Sqrt(2f * Gravity * JumpHeight);
            Grounded = false;
            jumpTime = Time.time;
            Kick(0.35f);
            return true;
        }

        public void Die(float dir)
        {
            Dead = true;
            Grounded = false;
            deathVel = new Vector3(dir * 5.5f, 7.5f, -2.5f);
            deathSpin = new Vector3(UnityEngine.Random.Range(-200f, 200f), 0, -dir * 540f);
            Rig.SetDead(true);
            Kick(-0.4f);
        }

        public void LookAt(float x) => lookTarget = Mathf.Clamp(x, -1f, 1f);

        public void Kick(float amount) => squashVel += amount * 12f;

        void Update()
        {
            float dt = Time.deltaTime;

            if (Dead)
            {
                deathVel.y -= Gravity * 0.7f * dt;
                transform.position += deathVel * dt;
                transform.Rotate(deathSpin * dt, Space.World);
            }
            else if (!Grounded)
            {
                vy -= Gravity * dt;
                var p = transform.position;
                p.y += vy * dt;
                float ground = support();
                if (vy < 0f && p.y <= ground)
                {
                    p.y = ground;
                    vy = 0;
                    Grounded = true;
                    Kick(-0.55f);
                    onLanded?.Invoke();
                }
                transform.position = p;
            }

            AnimateRig(dt);
        }

        void AnimateRig(float dt)
        {
            if (Rig == null) return;

            // Spring-driven squash and stretch, stretched by vertical speed while airborne.
            float target = Grounded ? Mathf.Sin(Time.time * 3.2f) * 0.025f : Mathf.Clamp(vy * 0.018f, -0.12f, 0.18f);
            // Substepped so the spring stays stable on slow devices.
            int steps = Mathf.CeilToInt(dt / 0.008f);
            float h = dt / Mathf.Max(1, steps);
            for (int i = 0; i < steps; i++)
            {
                squashVel += (-(squash - target) * 320f - squashVel * 16f) * h;
                squash += squashVel * h;
            }
            float s = Mathf.Clamp(squash, -0.45f, 0.45f);
            Rig.Squash.localScale = new Vector3(1f - s * 0.5f, 1f + s, 1f - s * 0.5f);

            blinkTimer -= dt;
            if (blinkTimer <= 0f)
            {
                blink = 0f;
                blinkTimer = UnityEngine.Random.Range(1.8f, 4.2f);
            }
            blink = Mathf.MoveTowards(blink, 1f, dt * 9f);
            Rig.Blink(blink < 0.5f ? 0.1f : 1f);

            look = Mathf.Lerp(look, lookTarget, 1f - Mathf.Exp(-dt * 10f));
            Rig.Look(look);
        }
    }
}
