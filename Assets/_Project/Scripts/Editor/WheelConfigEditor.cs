using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Wof.Data;

namespace Wof.EditorTools
{
    /// <summary>
    /// Custom inspector for WheelConfig (R2: "content of slices of each wheel should be
    /// changeable from the editor"). Gives designers a reorderable slice list with a
    /// reward picker, weight and bomb toggle per row, plus a live total-weight readout.
    /// </summary>
    [CustomEditor(typeof(WheelConfig))]
    public sealed class WheelConfigEditor : Editor
    {
        private ReorderableList _list;
        private SerializedProperty _slices;

        private void OnEnable()
        {
            _slices = serializedObject.FindProperty("slices");
            _list = new ReorderableList(serializedObject, _slices, true, true, true, true)
            {
                drawHeaderCallback = r => EditorGUI.LabelField(r, "Slices  (reward · weight · bomb)"),
                drawElementCallback = (rect, i, _, __) =>
                {
                    var el = _slices.GetArrayElementAtIndex(i);
                    rect.height = EditorGUIUtility.singleLineHeight;
                    rect.y += 2f;
                    float w = rect.width;

                    EditorGUI.PropertyField(
                        new Rect(rect.x, rect.y, w * 0.50f, rect.height),
                        el.FindPropertyRelative("reward"), GUIContent.none);
                    EditorGUI.PropertyField(
                        new Rect(rect.x + w * 0.52f, rect.y, w * 0.28f, rect.height),
                        el.FindPropertyRelative("weight"), GUIContent.none);
                    EditorGUI.PropertyField(
                        new Rect(rect.x + w * 0.84f, rect.y, w * 0.16f, rect.height),
                        el.FindPropertyRelative("isBomb"), GUIContent.none);
                }
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, "slices", "m_Script");
            _list.DoLayoutList();

            float total = 0f;
            for (int i = 0; i < _slices.arraySize; i++)
                total += _slices.GetArrayElementAtIndex(i).FindPropertyRelative("weight").floatValue;
            EditorGUILayout.HelpBox($"Total weight: {total:0.##}", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
