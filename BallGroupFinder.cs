using System.Collections.Generic;
using UnityEngine;

public static class BallGroupFinder
{
    private const string SelectableBallLayerName =
        "Ball";

    private const int MinimumGroupSize =
        3;

    private static readonly Vector2[] Directions =
    {
        Vector2.left,
        Vector2.right,
        Vector2.up,
        Vector2.down
    };

    private static readonly RaycastHit2D[]
        RaycastResults =
            new RaycastHit2D[16];


    /*
     * ========================================
     * FIND VALID GROUP
     * ========================================
     */

    public static bool TryGetValidGroup(
        Ball centerBall,
        IReadOnlyList<Ball> balls,
        List<Ball> targetGroup)
    {
        targetGroup?.Clear();

        if (targetGroup == null ||
            !CanParticipate(
                centerBall,
                balls))
        {
            return false;
        }

        targetGroup.Add(
            centerBall
        );

        float searchDistance =
            GetWorldDiameter(
                centerBall
            );

        if (searchDistance <= 0f)
        {
            targetGroup.Clear();
            return false;
        }

        BallType targetType =
            centerBall.BallType;

        for (int i = 0;
             i < Directions.Length;
             i++)
        {
            Ball neighbor =
                FindNearestBall(
                    centerBall,
                    Directions[i],
                    searchDistance,
                    balls
                );

            if (neighbor == null ||
                neighbor.BallType !=
                targetType)
            {
                continue;
            }

            targetGroup.Add(
                neighbor
            );
        }

        if (targetGroup.Count >=
            MinimumGroupSize)
        {
            return true;
        }

        targetGroup.Clear();

        return false;
    }


    /*
     * ========================================
     * NEIGHBOR
     * ========================================
     */

    private static Ball FindNearestBall(
        Ball centerBall,
        Vector2 direction,
        float distance,
        IReadOnlyList<Ball> balls)
    {
        int layer =
            LayerMask.NameToLayer(
                SelectableBallLayerName
            );

        if (layer < 0)
        {
            return null;
        }

        ContactFilter2D filter =
            new()
            {
                useLayerMask = true,
                layerMask = 1 << layer,
                useTriggers = false
            };

        int hitCount =
            Physics2D.Raycast(
                centerBall.transform.position,
                direction,
                filter,
                RaycastResults,
                distance
            );

        Ball nearestBall = null;

        float nearestDistance =
            float.PositiveInfinity;

        for (int i = 0;
             i < hitCount;
             i++)
        {
            RaycastHit2D hit =
                RaycastResults[i];

            if (hit.collider == null)
            {
                continue;
            }

            Ball hitBall =
                GetBallFromCollider(
                    hit.collider
                );

            if (hitBall == centerBall ||
                !CanParticipate(
                    hitBall,
                    balls) ||
                hit.distance >=
                    nearestDistance)
            {
                continue;
            }

            nearestBall =
                hitBall;

            nearestDistance =
                hit.distance;
        }

        ClearRaycastResults(
            hitCount
        );

        return nearestBall;
    }


    /*
     * ========================================
     * VALIDATION
     * ========================================
     */

    private static bool CanParticipate(
        Ball ball,
        IReadOnlyList<Ball> balls)
    {
        if (ball == null ||
            !ball.IsSelectable ||
            ball.IsBouncing ||
            ball.IsDestroyRequested)
        {
            return false;
        }

        int layer =
            LayerMask.NameToLayer(
                SelectableBallLayerName
            );

        if (layer < 0 ||
            ball.gameObject.layer != layer)
        {
            return false;
        }

        if (balls == null)
        {
            return true;
        }

        for (int i = 0;
             i < balls.Count;
             i++)
        {
            if (balls[i] == ball)
            {
                return true;
            }
        }

        return false;
    }


    /*
     * ========================================
     * COLLIDER
     * ========================================
     */

    private static float GetWorldDiameter(
        Ball ball)
    {
        if (ball == null ||
            ball.CircleCollider == null)
        {
            return 0f;
        }

        Vector3 scale =
            ball.transform.lossyScale;

        float scaleFactor =
            Mathf.Max(
                Mathf.Abs(scale.x),
                Mathf.Abs(scale.y)
            );

        return
            ball.CircleCollider.radius *
            scaleFactor *
            2f;
    }


    private static Ball GetBallFromCollider(
        Collider2D ballCollider)
    {
        if (ballCollider == null)
        {
            return null;
        }

        if (ballCollider.TryGetComponent(
                out Ball ball))
        {
            return ball;
        }

        return ballCollider
            .GetComponentInParent<Ball>();
    }


    private static void ClearRaycastResults(
        int hitCount)
    {
        int clearCount =
            Mathf.Min(
                hitCount,
                RaycastResults.Length
            );

        for (int i = 0;
             i < clearCount;
             i++)
        {
            RaycastResults[i] =
                default;
        }
    }
}
