using UnityEngine;
namespace OrbitDominion
{
    public sealed class OwnedMesh : MonoBehaviour
    {
        public Mesh mesh;
        void OnDestroy(){if(mesh!=null)Destroy(mesh);}
    }
}
