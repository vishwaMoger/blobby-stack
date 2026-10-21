using UnityEngine;

namespace StackJump
{
    // Follows the top of the tower and frames it for both portrait (mobile) and landscape (desktop).
    [RequireComponent(typeof(Camera))]
    public class CameraRig : MonoBehaviour
    {
        const float Distance = 9.5f;

        public float TargetY;
        Camera cam;
        float y, yVel, shake;

        void Awake() => cam = GetComponent<Camera>();

        public void Snap(float target)
        {
            TargetY = y = target;
            yVel = 0f;
        }

        public void Shake(float amount) => shake = Mathf.Max(shake, amount);

        // Half the visible width at the tower's depth.
        public float HalfWidth => Distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * cam.aspect;

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            y = Mathf.SmoothDamp(y, TargetY, ref yVel, 0.35f);
            float aspect = cam.aspect;
            cam.fieldOfView = aspect < 1f ? Mathf.Lerp(56f, 44f, Mathf.InverseLerp(0.45f, 1f, aspect)) : 42f;

            var pos = new Vector3(0, y + 2.0f, -Distance);
            var look = new Vector3(0, y + 0.55f, 0);
            if (shake > 0f)
            {
                float t = Time.time * 40f;
                pos += new Vector3(Mathf.PerlinNoise(t, 0f) - 0.5f, Mathf.PerlinNoise(0f, t) - 0.5f, 0) * shake * 0.8f;
                shake = Mathf.MoveTowards(shake, 0f, dt * 1.4f);
            }
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(look - pos));
        }
    }
}
