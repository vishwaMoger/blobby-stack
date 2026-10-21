using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StackJump
{
    // Thick, even outline plus a chunky drop shadow for game-style text.
    // uGUI's Outline only offsets diagonally, which looks thin and gappy at large sizes.
    public class GameOutline : BaseMeshEffect
    {
        public Color OutlineColor = new(0.17f, 0.06f, 0.32f, 1f);
        public Color ShadowColor = new(0.08f, 0.02f, 0.18f, 0.55f);
        public float Size = 5f;
        public Vector2 ShadowOffset = new(0f, -8f);

        static readonly List<UIVertex> source = new();
        static readonly List<UIVertex> output = new();
        static readonly Vector2[] Directions =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
            new(0.7071f, 0.7071f), new(-0.7071f, 0.7071f), new(0.7071f, -0.7071f), new(-0.7071f, -0.7071f),
        };

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;
            source.Clear();
            output.Clear();
            vh.GetUIVertexStream(source);

            foreach (var d in Directions) Append(ShadowColor, ShadowOffset + d * Size);
            foreach (var d in Directions) Append(OutlineColor, d * Size);
            output.AddRange(source);

            vh.Clear();
            vh.AddUIVertexTriangleStream(output);
        }

        void Append(Color color, Vector2 offset)
        {
            Color32 c = color;
            foreach (var v in source)
            {
                var copy = v;
                copy.position += (Vector3)offset;
                copy.color = new Color32(c.r, c.g, c.b, (byte)(c.a * v.color.a / 255));
                output.Add(copy);
            }
        }
    }
}
