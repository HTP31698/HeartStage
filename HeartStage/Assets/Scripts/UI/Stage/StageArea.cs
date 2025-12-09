using UnityEngine;

public class StageArea : MonoBehaviour
{
    public static StageArea Instance;
    private Bounds stageBounds;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        CalculateBounds();
    }

    private void CalculateBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        stageBounds = renderers[0].bounds;

        foreach (Renderer r in renderers)
            stageBounds.Encapsulate(r.bounds);

        // 위/아래 0.5씩 확장 (Y축만)
        stageBounds.Expand(new Vector3(0f, 1f, 0f));
    }

    public bool IsInside(Vector3 worldPos)
    {
        return stageBounds.Contains(worldPos);
    }
}