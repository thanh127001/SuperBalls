using System.Collections.Generic;
using UnityEngine;

public static class BallGroupFinder
{
    private const string SelectableBallLayerName = "Ball";
    private const int MinimumGroupSize = 3;

    private static readonly Vector2[] Directions =
    {
        Vector2.left,
        Vector2.right,
        Vector2.up,
        Vector2.down
    };

    private static readonly RaycastHit2D[] RaycastResults =
        new RaycastHit2D[16];


    public static bool TryGetValidGroup(
        Ball centerBall,
        IReadOnlyList<Ball> balls,
        List<Ball> targetGroup)
    {
        return TryGetValidGroup(
            centerBall,
            balls,
            null,
            targetGroup
        );
    }


    public static bool TryFindValidGroup(
        IReadOnlyList<Ball> balls,
        List<Ball> targetGroup)
    {
        targetGroup?.Clear();

        if (balls == null ||
            targetGroup == null)
        {
            return false;
        }

        for (int i = 0;
             i < balls.Count;
             i++)
        {
            if (TryGetValidGroup(
                    balls[i],
                    balls,
                    null,
                    targetGroup))
            {
                return true;
            }
        }

        targetGroup.Clear();
        return false;
    }


    public static int GetValidGroupCapacity(
        IReadOnlyList<Ball> balls,
        int maximumCount = int.MaxValue)
    {
        if (balls == null ||
            balls.Count == 0 ||
            maximumCount <= 0)
        {
            return 0;
        }

        HashSet<Ball> excludedBalls =
            new();

        List<Ball> groupBuffer =
            new(5);

        return FindMaximumGroupCount(
            balls,
            excludedBalls,
            groupBuffer,
            maximumCount
        );
    }


    private static int FindMaximumGroupCount(
        IReadOnlyList<Ball> balls,
        HashSet<Ball> excludedBalls,
        List<Ball> groupBuffer,
        int remainingLimit)
    {
        if (remainingLimit <= 0)
        {
            return 0;
        }

        int bestCount = 0;

        for (int i = 0;
             i < balls.Count;
             i++)
        {
            Ball centerBall =
                balls[i];

            if (!TryGetValidGroup(
                    centerBall,
                    balls,
                    excludedBalls,
                    groupBuffer))
            {
                continue;
            }

            Ball[] selectedGroup =
                groupBuffer.ToArray();

            for (int j = 0;
                 j < selectedGroup.Length;
                 j++)
            {
                excludedBalls.Add(
                    selectedGroup[j]
                );
            }

            int count =
                1 +
                FindMaximumGroupCount(
                    balls,
                    excludedBalls,
                    groupBuffer,
                    remainingLimit - 1
                );

            for (int j = 0;
                 j < selectedGroup.Length;
                 j++)
            {
                excludedBalls.Remove(
                    selectedGroup[j]
                );
            }

            if (count > bestCount)
            {
                bestCount = count;
            }

            if (bestCount >= remainingLimit)
            {
                break;
            }
        }

        groupBuffer.Clear();
        return bestCount;
    }


    private static bool TryGetValidGroup(
        Ball centerBall,
        IReadOnlyList<Ball> balls,
        HashSet<Ball> excludedBalls,
        List<Ball> targetGroup)
    {
        targetGroup?.Clear();

        if (targetGroup == null ||
            !CanParticipate(
                centerBall,
                balls,
                excludedBalls))
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
                    balls,
                    excludedBalls
                );

            if (neighbor == null ||
                neighbor.BallType != targetType)
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


    private static Ball FindNearestBall(
        Ball centerBall,
        Vector2 direction,
        float distance,
        IReadOnlyList<Ball> balls,
        HashSet<Ball> excludedBalls)
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
                    balls,
                    excludedBalls) ||
                hit.distance >= nearestDistance)
            {
                continue;
            }

            nearestBall = hitBall;
            nearestDistance = hit.distance;
        }

        ClearRaycastResults(
            hitCount
        );

        return nearestBall;
    }


    private static bool CanParticipate(
        Ball ball,
        IReadOnlyList<Ball> balls,
        HashSet<Ball> excludedBalls)
    {
        if (ball == null ||
            !ball.IsSelectable ||
            ball.IsBouncing ||
            ball.IsDestroyRequested ||
            excludedBalls != null &&
            excludedBalls.Contains(ball))
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
