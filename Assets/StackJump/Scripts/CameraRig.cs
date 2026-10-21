using UnityEngine;

namespace StackJump
{
    // Follows the top of the tower and frames it for both portrait (mobile) and landscape (desktop).
    [RequireComponent(typeof(Camera))]
    public class CameraRig : MonoBehaviour
    {
        public float Distance = 8.2f;

        public float TargetY;
        public float CenterX; // x of the tower this camera follows (split screen)
        Camera cam;
        float y, yVel, shake;

        void Awake() => cam = GetComponent<Camera>();

        public Camera Cam => cam != null ? cam : cam = GetComponent<Camera>();

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
            cam.fieldOfView = aspect < 1f ? Mathf.Lerp(58f, 44f, Mathf.InverseLerp(0.45f, 1f, aspect)) : 38f;

            var pos = new Vector3(CenterX, y + 2.3f, -Distance);
            var look = new Vector3(CenterX, y + 0.95f, 0);
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
