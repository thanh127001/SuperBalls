using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AchievementManager))]
public class AchievementManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (GUILayout.Button("Clear Achievement"))
        {
            AchievementManager manager =
                (AchievementManager)target;

            manager.ClearAchievementData();

            Debug.Log(
                "Achievement data cleared."
            );
        }
    }
}
