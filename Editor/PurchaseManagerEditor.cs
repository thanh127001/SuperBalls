#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PurchaseManager))]
public class PurchaseManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PurchaseManager purchaseManager =
            (PurchaseManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            "Editor Test",
            EditorStyles.boldLabel
        );

        bool currentPurchased =
            Application.isPlaying &&
            purchaseManager.IsInitialized
                ? purchaseManager.IsGamePurchased
                : PurchaseManager.EditorPurchased;

        bool purchased =
            EditorGUILayout.Toggle(
                "Purchased",
                currentPurchased
            );

        if (purchased !=
            currentPurchased)
        {
            purchaseManager.EditorSetPurchased(
                purchased
            );
        }
    }
}
#endif
