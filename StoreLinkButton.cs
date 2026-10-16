using UnityEngine;

[DisallowMultipleComponent]
public class StoreLinkButton : MonoBehaviour
{
    public void OpenStore()
    {
        string storeUrl =
            $"https://play.google.com/store/apps/details?id={Application.identifier}";

        Application.OpenURL(storeUrl);
    }
}
