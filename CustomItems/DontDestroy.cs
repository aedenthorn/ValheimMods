using UnityEngine;

namespace CustomItems
{
    public class DontDestroy: MonoBehaviour
    {
        void OnDestroy()
        {
            Debug.Log("XYZ Destroyed");
        }
    }
}