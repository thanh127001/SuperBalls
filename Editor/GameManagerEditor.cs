#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    private static readonly string[] TestModeLabels =
    {
        "Bình thường",
        "Level Completed",
        "Game Completed"
    };

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

        EditorGUI.BeginChangeCheck();

        int selectedIndex =
            GUILayout.SelectionGrid(
                (int)gameManager.CurrentEditorTestMode,
                TestModeLabels,
                TestModeLabels.Length
            );

        if (!EditorGUI.EndChangeCheck())
        {
            return;
        }

        Undo.RecordObject(
            gameManager,
            "Change Game Test Mode"
        );

        gameManager.SetEditorTestMode(
            (GameManager.EditorTestMode)selectedIndex
        );

        EditorUtility.SetDirty(
            gameManager
        );
    }
}
#endif
