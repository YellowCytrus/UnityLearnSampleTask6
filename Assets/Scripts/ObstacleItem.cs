using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class ObstacleItem : MonoBehaviour
{
    [Header("State")]
    [SerializeField, Range(0f, 1f)] private float currentValue = 1f;

    [Header("Events")]
    public UnityEvent onDestroyObstacle;

    [Header("Visuals")]
    [Tooltip("Renderer to tint. If empty, the first Renderer in children will be used.")]
    [SerializeField] private Renderer targetRenderer;

    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private MaterialPropertyBlock _mpb;
    private bool _destroyed;

    public float CurrentValue => currentValue;

    private void Awake()
    {
        if (onDestroyObstacle == null)
            onDestroyObstacle = new UnityEvent();

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        _mpb ??= new MaterialPropertyBlock();
        ApplyVisual();
    }

    private void OnValidate()
    {
        currentValue = Mathf.Clamp01(currentValue);
        if (!Application.isPlaying)
        {
            if (targetRenderer == null)
                targetRenderer = GetComponentInChildren<Renderer>();
            _mpb ??= new MaterialPropertyBlock();
            ApplyVisual();
        }
    }

    public void GetDamage(float value)
    {
        if (_destroyed)
            return;

        if (value <= 0f)
            return;

        currentValue = Mathf.Clamp01(currentValue - value);
        ApplyVisual();

        if (currentValue <= 0f)
        {
            _destroyed = true;
            onDestroyObstacle?.Invoke();
            Destroy(gameObject);
        }
    }

    private void ApplyVisual()
    {
        if (targetRenderer == null)
            return;

        // 1 -> white, 0 -> red
        Color c = Color.Lerp(Color.red, Color.white, currentValue);

        targetRenderer.GetPropertyBlock(_mpb);

        // Support both Built-in and SRP shaders.
        _mpb.SetColor(ColorId, c);
        _mpb.SetColor(BaseColorId, c);

        targetRenderer.SetPropertyBlock(_mpb);
    }
}

