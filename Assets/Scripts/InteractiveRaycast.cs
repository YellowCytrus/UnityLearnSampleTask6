using UnityEngine;

[DisallowMultipleComponent]
public class InteractiveRaycast : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private GameObject prefab;

    [Tooltip("Camera used for click raycasts. If empty, Camera.main is used.")]
    [SerializeField] private Camera raycastCamera;

    [Tooltip("Extra push from the surface so spawned objects do not visually clip into it.")]
    [SerializeField, Min(0f)] private float surfacePadding = 0.01f;

    [Header("Raycast")]
    [SerializeField] private LayerMask clickMask = Physics.DefaultRaycastLayers;

    [SerializeField, Min(0f)] private float maxDistance = 1000f;

    private InteractiveBox _selectedBox;

    private void Awake()
    {
        if (raycastCamera == null)
            raycastCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            HandleLeftClick();

        if (Input.GetMouseButtonDown(1))
            HandleRightClick();

        // If the selected object has been destroyed, clear stale reference.
        if (!_selectedBox)
            _selectedBox = null;
    }

    private void HandleLeftClick()
    {
        if (!TryGetHit(out RaycastHit hit))
            return;

        InteractiveBox hitBox = hit.collider.GetComponentInParent<InteractiveBox>();
        if (hitBox != null)
        {
            if (_selectedBox == null)
            {
                _selectedBox = hitBox;
                return;
            }

            if (_selectedBox != hitBox)
            {
                _selectedBox.AddNext(hitBox);
                return;
            }

            return;
        }

        if (!hit.collider.CompareTag("InteractivePlane"))
            return;

        SpawnPrefabOnSurface(hit);
    }

    private void HandleRightClick()
    {
        if (!TryGetHit(out RaycastHit hit))
            return;

        InteractiveBox hitBox = hit.collider.GetComponentInParent<InteractiveBox>();
        if (hitBox == null)
            return;

        if (_selectedBox == hitBox)
            _selectedBox = null;

        Destroy(hitBox.gameObject);
    }

    private bool TryGetHit(out RaycastHit hit)
    {
        hit = default;

        if (raycastCamera == null)
            return false;

        Ray ray = raycastCamera.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out hit, maxDistance, clickMask, QueryTriggerInteraction.Ignore);
    }

    private void SpawnPrefabOnSurface(RaycastHit hit)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"{nameof(InteractiveRaycast)}: prefab is not assigned.", this);
            return;
        }

        GameObject spawned = Instantiate(prefab, hit.point, prefab.transform.rotation);

        float offset = ComputeSurfaceOffset(spawned, hit.normal) + surfacePadding;
        spawned.transform.position = hit.point + hit.normal * offset;
    }

    private static float ComputeSurfaceOffset(GameObject instance, Vector3 normal)
    {
        if (!TryGetWorldBounds(instance, out Bounds bounds))
            return 0.5f;

        Vector3 n = normal.normalized;
        Vector3 e = bounds.extents;

        // Projection of world-axis AABB extents onto the placement normal.
        float support = Mathf.Abs(n.x) * e.x + Mathf.Abs(n.y) * e.y + Mathf.Abs(n.z) * e.z;

        // Distance from the hit point to the lowest AABB point along normal direction.
        float minAlongNormal = Vector3.Dot(n, bounds.center) - support;
        return Mathf.Max(0f, Vector3.Dot(n, instance.transform.position) - minAlongNormal);
    }

    private static bool TryGetWorldBounds(GameObject obj, out Bounds bounds)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
                bounds.Encapsulate(colliders[i].bounds);
            return true;
        }

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return true;
        }

        bounds = default;
        return false;
    }
}
