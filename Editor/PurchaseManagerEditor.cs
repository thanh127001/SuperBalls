#if UNITY_EDITOR
using UnityEditor;

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

        using (new EditorGUI.DisabledScope(
                   !Application.isPlaying ||
                   !purchaseManager.IsInitialized))
        {
            bool purchased =
                EditorGUILayout.Toggle(
                    "Purchased",
                    purchaseManager.IsGamePurchased
                );

            if (purchased !=
                purchaseManager.IsGamePurchased)
            {
                purchaseManager.EditorSetPurchased(
                    purchased
                );
            }
        }
    }
}
#endif
