using UnityEngine;

namespace StackJump
{
    // Frees a renderer's generated mesh and material when its GameObject is destroyed.
    // Blocks, backdrops and grounds are rebuilt all session long; without this, their one-off
    // meshes (vines, ridges, grids) and materials pile up until the WebGL tab runs out of memory.
    public class OwnedAssets : MonoBehaviour
    {
        Mesh mesh;
        Material material;

        public void Init(Mesh m, Material mat)
        {
            mesh = MeshFactory.IsShared(m) ? null : m;
            material = Mats.IsShared(mat) ? null : mat;
        }

        void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            if (material != null) Destroy(material);
        }
    }
}
