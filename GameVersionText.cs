using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public class GameVersionText : MonoBehaviour
{
    [SerializeField] private string prefix = "Version\\n";

    private void Awake()
    {
        TMP_Text versionText = GetComponent<TMP_Text>();
        versionText.text = prefix.Replace("\\n", "\n") + Application.version;
    }
}
