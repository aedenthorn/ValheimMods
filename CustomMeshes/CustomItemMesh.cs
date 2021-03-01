using UnityEngine;

namespace CustomMeshes
{
    internal class CustomItemMesh
    {
        public string objName;
        public string meshName;
        public Mesh mesh;
        public SkinnedMeshRenderer renderer;

        public CustomItemMesh(string dirName, string name, Mesh mesh, SkinnedMeshRenderer renderer = null)
        {
            this.objName = dirName;
            this.meshName = name;
            this.mesh = mesh;
            this.renderer = renderer;
        }
    }
}