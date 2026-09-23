using UnityEngine;

public class BallGlow : MonoBehaviour
{
    private Transform parentTransform;

    private void Awake()
    {
        parentTransform = transform.parent;
    }

    private void LateUpdate()
    {
        if (parentTransform == null)
        {
            return;
        }

        transform.localRotation =
            Quaternion.Inverse(parentTransform.rotation);
    }
}