#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GameManager gameManager =
            (GameManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            "Editor Test",
            EditorStyles.boldLabel
        );

        using (new EditorGUI.DisabledScope(
                   !Application.isPlaying))
        {
            if (GUILayout.Button(
                    "Level Completed"))
            {
                gameManager.CompleteLevel();
            }

            if (GUILayout.Button(
                    "Game Completed"))
            {
                gameManager.CompleteGame();
            }
        }
    }
}
#endif
