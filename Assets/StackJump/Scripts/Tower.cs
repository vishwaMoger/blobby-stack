using System.Collections.Generic;
using UnityEngine;

namespace StackJump
{
    // The stack of settled blocks. Block origins sit at their bottom center. Blocks keep the
    // small offsets they landed with, and the top of the tower wobbles a little on each landing.
    public class Tower : MonoBehaviour
    {
        public const float BlockH = 0.6f;
        public const float BlockSize = 1.6f;
        const int KeepVisible = 40;
        const int WobbleDepth = 6;

        class Entry
        {
            public GameObject Go;
            public Quaternion BaseRotation;
            public int Index;
        }

        readonly Queue<Entry> blocks = new();
        readonly List<Entry> recent = new();
        float wobble, wobbleVel;

        public int Height { get; private set; }
        public float TopY => Height * BlockH;

        public GameObject CreateBlock(WorldTheme theme, int index) => BlockFactory.Create(theme, index);

        public void Add(GameObject block)
        {
            block.transform.SetParent(transform, true);
            var p = block.transform.position;
            block.transform.position = new Vector3(p.x, TopY, p.z);
            var e = new Entry { Go = block, BaseRotation = block.transform.rotation, Index = Height };
            blocks.Enqueue(e);
            recent.Add(e);
            if (recent.Count > WobbleDepth) recent.RemoveAt(0);
            Height++;
            while (blocks.Count > KeepVisible) Destroy(blocks.Dequeue().Go);
        }

        public void Kick(float amount) => wobbleVel += amount;

        public void Clear()
        {
            while (blocks.Count > 0) Destroy(blocks.Dequeue().Go);
            recent.Clear();
            Height = 0;
            wobble = wobbleVel = 0f;
        }

        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            wobbleVel += (-wobble * 160f - wobbleVel * 9f) * dt;
            wobble += wobbleVel * dt;
            for (int i = 0; i < recent.Count; i++)
            {
                var e = recent[i];
                if (e.Go == null) continue;
                float depth = (Height - 1 - e.Index) / (float)WobbleDepth;
                float k = Mathf.Max(0f, 1f - depth);
                e.Go.transform.rotation = Quaternion.Euler(0, 0, wobble * k * k) * e.BaseRotation;
            }
        }
    }
}
