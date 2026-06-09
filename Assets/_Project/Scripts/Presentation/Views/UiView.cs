using UnityEngine;

namespace Wof.Presentation
{
    /// <summary>
    /// Base for every View. Child references are auto-wired in OnValidate by name (brief:
    /// "Button references should be automatically set from OnValidate"), so nothing is
    /// dragged by hand and no OnClick is wired in the editor.
    /// </summary>
    public abstract class UiView : MonoBehaviour
    {
        /// <summary>Find a descendant by name and grab a component into <paramref name="field"/> if unset.</summary>
        protected T Bind<T>(ref T field, string childName) where T : Component
        {
            if (field == null)
            {
                var t = transform.FindDeep(childName);
                if (t != null) field = t.GetComponent<T>();
            }
            return field;
        }

#if UNITY_EDITOR
        protected virtual void OnValidate() => AutoWire();

        /// <summary>Each View binds its own children here.</summary>
        protected abstract void AutoWire();
#endif
    }
}
