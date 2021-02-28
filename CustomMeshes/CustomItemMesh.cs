using UnityEngine;

namespace CustomMeshes
{
    internal class CustomItemMesh
    {
        public string objName;
        public string meshName;
        public Mesh mesh;

        public CustomItemMesh(string dirName, string name, Mesh mesh)
        {
            this.objName = dirName;
            this.meshName = name;
            this.mesh = mesh;
        }
    }
}