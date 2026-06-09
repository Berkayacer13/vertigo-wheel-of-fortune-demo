using UnityEngine;

namespace Wof.Presentation
{
    public static class TransformExtensions
    {
        /// <summary>Recursive depth-first find of a descendant by exact name (used by OnValidate auto-wiring).</summary>
        public static Transform FindDeep(this Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var r = root.GetChild(i).FindDeep(name);
                if (r != null) return r;
            }
            return null;
        }
    }
}
