using System.Collections.Generic;
using UnityEngine;

namespace StackJump
{
    // The stack of settled blocks. Block origins sit at their bottom center.
    public class Tower : MonoBehaviour
    {
        public const float BlockH = 0.6f;
        public const float BlockSize = 1.6f;
        const int KeepVisible = 40;

        readonly Queue<GameObject> blocks = new();
        public int Height { get; private set; }
        public float TopY => Height * BlockH;

        public GameObject CreateBlock(WorldTheme theme, int index)
        {
            var root = new GameObject("Block " + index);
            var rng = new System.Random(index * 7919 + theme.Seed);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            var color = theme.BlockColor(index);

            if (theme.BlockLayers >= 2)
            {
                // Two uneven slabs, like stacked rocks.
                var slab = MeshFactory.RoundedBox(new Vector3(BlockSize, 0.32f, BlockSize), 0.11f);
                for (int i = 0; i < 2; i++)
                {
                    var go = new GameObject("Slab");
                    go.transform.SetParent(root.transform, false);
                    go.transform.localPosition = new Vector3(R(-0.05f, 0.05f), 0.16f + i * 0.285f, R(-0.05f, 0.05f));
                    go.transform.localRotation = Quaternion.Euler(0, R(-5f, 5f), 0);
                    float sc = R(0.95f, 1.02f);
                    go.transform.localScale = new Vector3(sc, 1, sc);
                    var r = Mats.AddRenderer(go, slab, Mats.Toon);
                    var c = Color.Lerp(color, Color.white, i * 0.08f + R(-0.03f, 0.03f));
                    Mats.Set(r, c, 0.15f, 0f, 0.12f, 0f, 0.18f);
                }
            }
            else
            {
                var go = new GameObject("Slab");
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = new Vector3(0, BlockH * 0.5f, 0);
                go.transform.localRotation = Quaternion.Euler(0, R(-2f, 2f), 0);
                var r = Mats.AddRenderer(go, MeshFactory.RoundedBox(new Vector3(BlockSize, BlockH, BlockSize), 0.12f), Mats.Toon);
                Mats.Set(r, color, 0.2f, 0f, 0.14f, 0f, 0.16f);
            }
            return root;
        }

        public void Add(GameObject block)
        {
            block.transform.SetParent(transform, true);
            block.transform.position = new Vector3(0, TopY, 0);
            blocks.Enqueue(block);
            Height++;
            while (blocks.Count > KeepVisible) Destroy(blocks.Dequeue());
        }

        public void Clear()
        {
            while (blocks.Count > 0) Destroy(blocks.Dequeue());
            Height = 0;
        }
    }
}
