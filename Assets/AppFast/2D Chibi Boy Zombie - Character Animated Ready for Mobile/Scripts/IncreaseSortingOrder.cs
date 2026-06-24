using UnityEngine;
using System.Collections;

namespace NetDinamica.AppFast
{
    public class IncreaseSortingOrder : MonoBehaviour
    {
        private static int renderOrderOffset = 0;

        // Use this for initialization
        void Start()
        {
            // Safely wrap sorting order offset to prevent short integer overflow
            renderOrderOffset = (renderOrderOffset + 30) % 5000;

            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                renderer.sortingOrder += renderOrderOffset;
            }
        }
    }
}