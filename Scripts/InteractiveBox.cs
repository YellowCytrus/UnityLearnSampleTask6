// InteractiveBox

using UnityEngine;

public class InteractiveBox : MonoBehaviour
{
    [SerializeField] private InteractiveBox next;

    [Header("Beam visualization")]
    [Tooltip("Line color when the beam reaches the next box with no collider in between (enable Gizmos on the Game view to see Debug lines in Play Mode).")]
    [SerializeField] private Color beamColorClear = Color.cyan;

    [Tooltip("Line color when something blocks the ray before the next position.")]
    [SerializeField] private Color beamColorBlocked = Color.red;

    [Header("Beam physics")]
    [Tooltip("Layers considered by the beam raycast.")]
    [SerializeField] private LayerMask beamHitLayers = Physics.DefaultRaycastLayers;

    [Tooltip("Moves the ray origin along the beam so colliders on this object are less likely to be reported as the first hit.")]
    [SerializeField] private float rayOriginForwardOffset = 0.02f;

    [Tooltip("Ignore uses default trigger rules from Physics settings; Collide/UseGlobal behave as in Physics.Raycast documentation.")]
    [SerializeField] private QueryTriggerInteraction beamTriggerInteraction = QueryTriggerInteraction.Ignore;

    [Tooltip("Max ray distance as a fraction of the remaining span after forward offset (1 = exactly up to the next transform position).")]
    [SerializeField] [Min(0f)] private float maxDistanceScaleToNext = 1f;

    public void OnMouseDown()
    {
        Debug.Log("InteractiveBox clicked");
    }

    /// <summary>Sets the single successor in the chain, or clears it when <paramref name="box"/> is null.</summary>
    public void AddNext(InteractiveBox box)
    {
        if (box == this)
        {
            Debug.LogWarning($"{nameof(InteractiveBox)}.{nameof(AddNext)}: ignored self-reference.", this);
            return;
        }

        next = box;
    }

    private void Update()
    {
        if (!TryResolveNext(out InteractiveBox target))
            return;

        Vector3 beamStart = transform.position;
        Vector3 beamEnd = target.transform.position;
        Vector3 toNext = beamEnd - beamStart;
        float distanceToNext = toNext.magnitude;
        if (distanceToNext <= Mathf.Epsilon)
            return;

        Vector3 direction = toNext / distanceToNext;

        bool hitObstacle = TryGetObstacleAlongBeam(
            beamStart,
            direction,
            distanceToNext,
            out ObstacleItem obstacle,
            out bool lineOfSightClear);

        Debug.DrawLine(beamStart, beamEnd, lineOfSightClear ? beamColorClear : beamColorBlocked, 0f);

        if (hitObstacle && obstacle != null)
            obstacle.GetDamage(Time.deltaTime);
    }

    private bool TryResolveNext(out InteractiveBox target)
    {
        target = null;
        if (next == null)
            return false;

        if (!next)
        {
            next = null;
            return false;
        }

        target = next;
        return true;
    }

    /// <summary>
    /// Casts one ray along the segment toward <paramref name="distanceWorldToNext"/>. Sets <paramref name="lineOfSightClear"/> when nothing is hit along the cast.
    /// Returns true when the first hit has an <see cref="ObstacleItem"/> in its parent chain.
    /// </summary>
    private bool TryGetObstacleAlongBeam(
        Vector3 beamStartWorld,
        Vector3 beamDirectionNormalized,
        float distanceWorldToNext,
        out ObstacleItem obstacle,
        out bool lineOfSightClear)
    {
        obstacle = null;
        lineOfSightClear = false;

        float forward = Mathf.Max(0f, rayOriginForwardOffset);
        Vector3 origin = beamStartWorld + beamDirectionNormalized * forward;
        float remaining = distanceWorldToNext - forward;
        if (remaining <= 0f)
            return false;

        float maxDistance = remaining * maxDistanceScaleToNext;
        if (maxDistance <= 0f)
            return false;

        if (!Physics.Raycast(origin, beamDirectionNormalized, out RaycastHit hit, maxDistance, beamHitLayers, beamTriggerInteraction))
        {
            lineOfSightClear = true;
            return false;
        }

        lineOfSightClear = false;
        obstacle = hit.collider.GetComponentInParent<ObstacleItem>();
        return obstacle != null;
    }
}
