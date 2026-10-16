using UnityEngine;

[DisallowMultipleComponent]
public class StoreLinkButton : MonoBehaviour
{
    public void OpenStore()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        string storeUrl =
            $"https://play.google.com/store/apps/details?id={Application.identifier}";

        Application.OpenURL(storeUrl);
#endif
    }
}
