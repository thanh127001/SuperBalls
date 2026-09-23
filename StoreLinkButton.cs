using UnityEngine;

public class StoreLinkButton : MonoBehaviour
{
    private const string StoreUrl ="https://play.google.com/store/apps/details?id=com.king.candycrushsaga";

    public void   OpenStore()
    {
        Application.OpenURL(StoreUrl);
    }
}