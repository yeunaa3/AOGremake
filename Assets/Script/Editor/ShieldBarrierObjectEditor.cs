#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AOG.Duel
{
    [CustomEditor(typeof(ShieldBarrierObject))]
    public class ShieldBarrierObjectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Shield Immortal Config", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("immortalDuration"), new GUIContent("Thời Gian Bất Tử (s)"));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Vị Trí & Góc Quay Khiên", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("frontOffset"), new GUIContent("Khoảng Cách Trước Mặt (X)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("heightOffset"), new GUIContent("Độ Cao (Y)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationAngle"), new GUIContent("Góc Nghiêng (Độ)"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
