using System;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public class GameVersionText : MonoBehaviour
{
    [Header("Display")]
    [Tooltip("Leave empty to show only the version. Use {0} for the version and \\n for a new line.")]
    [SerializeField] private string format = "";

    private void Awake()
    {
        TMP_Text versionText = GetComponent<TMP_Text>();
        string version = Application.version;

        if (string.IsNullOrEmpty(format))
        {
            versionText.text = version;
            return;
        }

        try
        {
            versionText.text = string.Format(format.Replace("\\n", "\n"), version);
        }
        catch (FormatException)
        {
            Debug.LogWarning("GameVersionText: Invalid format string. Use {0} for the version.", this);
            versionText.text = version;
        }
    }
}
