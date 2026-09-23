using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class BallColumnGuide : MonoBehaviour
{
    /*
     * ========================================
     * CONFIG
     * ========================================
     */

    private const int ColumnCountValue = 6;

    private const float GuideWidthValue = 6f;

    private const float GuideHeightValue = 18f;


    /*
     * ========================================
     * PUBLIC
     * ========================================
     */

    public int ColumnCount =>
        ColumnCountValue;

    public float GuideWidth =>
        GuideWidthValue;

    public float GuideHeight =>
        GuideHeightValue;

    public float ColumnWidth =>
        GuideWidthValue /
        ColumnCountValue;


    /*
     * ========================================
     * UNITY
     * ========================================
     */

    private void OnEnable()
    {
        UpdateColumns();
    }


#if UNITY_EDITOR

    private void OnValidate()
    {
        UpdateColumns();
    }

#endif


    /*
     * ========================================
     * PUBLIC METHODS
     * ========================================
     */

    public void RefreshColumns()
    {
        UpdateColumns();
    }


    /*
     * ========================================
     * COLUMNS
     * ========================================
     */

    private void UpdateColumns()
    {
        if (ColumnCountValue < 1 ||
            GuideWidthValue <= 0f ||
            GuideHeightValue <= 0f)
        {
            return;
        }


        int requiredColliderCount =
            ColumnCountValue + 1;


        List<EdgeCollider2D> colliders =
            new List<EdgeCollider2D>();


        GetComponents(colliders);


        /*
         * ========================================
         * CREATE MISSING COLLIDERS
         * ========================================
         */

        while (colliders.Count <
               requiredColliderCount)
        {
            EdgeCollider2D edge =
                gameObject.AddComponent<
                    EdgeCollider2D>();


            colliders.Add(edge);
        }


        /*
         * ========================================
         * CALCULATE SIZE
         * ========================================
         */

        float columnWidth =
            GuideWidthValue /
            ColumnCountValue;


        float halfWidth =
            GuideWidthValue * 0.5f;


        float halfHeight =
            GuideHeightValue * 0.5f;


        float left =
            -halfWidth;


        /*
         * ========================================
         * UPDATE COLLIDERS
         * ========================================
         */

        for (int i = 0;
             i < colliders.Count;
             i++)
        {
            EdgeCollider2D edge =
                colliders[i];


            bool required =
                i < requiredColliderCount;


            edge.enabled =
                required;


            if (!required)
            {
                continue;
            }


            float x =
                left +
                columnWidth * i;


            edge.points =
                new[]
                {
                    new Vector2(
                        x,
                        -halfHeight
                    ),

                    new Vector2(
                        x,
                        halfHeight
                    )
                };
        }
    }
}
