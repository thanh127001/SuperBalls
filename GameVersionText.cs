using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public class GameVersionText : MonoBehaviour
{
    private void Awake()
    {
        TMP_Text versionText = GetComponent<TMP_Text>();

        versionText.text = $"Version {Application.version}";
    }
}
