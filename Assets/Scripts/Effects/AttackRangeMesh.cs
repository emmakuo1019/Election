using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AttackRangeMesh : MonoBehaviour
{
    [Header("資料來源")]
    [SerializeField] private MonoBehaviour attackSourceBehaviour;
    private IAttackSource attackSource;
    private Component attackSourceComponent;
    private Vector3 positionOffset;

    [Header("形狀")]
    public int segments = 30;
    public float heightOffset = 0.05f;

    private Mesh mesh;
    private MeshRenderer meshRenderer;

    void Awake()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        meshRenderer = GetComponent<MeshRenderer>();

        BindSource();
    }

    void OnEnable()
    {
        if (attackSource != null)
        {
            attackSource.OnAttackShapeChanged += SetShape;
            SetShape(attackSource.AttackRange, attackSource.AttackAngle);
        }
    }

    void OnDisable()
    {
        if (attackSource != null)
            attackSource.OnAttackShapeChanged -= SetShape;
    }

    private void BindSource()
    {
        // 若你在 Inspector 指定了來源物件，就用它來綁定，避免因為層級不同而找不到 IAttackSource。
        if (attackSourceBehaviour != null)
        {
            attackSource = attackSourceBehaviour as IAttackSource;
            attackSourceComponent = attackSourceBehaviour as Component;
        }

        // 否則就從父物件往上找。
        if (attackSource == null)
        {
            attackSource = GetComponentInParent<IAttackSource>();
            attackSourceComponent = attackSource as Component;
        }

        if (attackSource == null)
        {
            Debug.LogError("AttackRangeMesh：attackSource 必須實作 IAttackSource");
        }

        if (attackSourceComponent != null)
        {
            // 保留 AttackRangeMesh 物件相對攻擊者的既有偏移（避免因為我們強行把中心移到 root 導致「看起來半徑更小」）。
            positionOffset = transform.position - attackSourceComponent.transform.position;
        }
    }

    public void SetShape(float range, float angle)
    {
        GenerateMesh(range, angle);
    }

    private void GenerateMesh(float range, float angle)
    {
        int vertexCount = segments + 2;

        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[segments * 3];

        vertices[0] = new Vector3(0, heightOffset, 0);

        float halfAngle = angle / 2f;

        // 若父物件有 scale，mesh 的局部頂點會被縮放，導致顯示半徑偏差。
        // 用 lossyScale 抵消，讓世界空間的顯示半徑盡量貼近 attackRange。
        float lossyScaleXZ = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
        float displayRange = lossyScaleXZ > 0.0001f ? range / lossyScaleXZ : range;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float rad = Mathf.Lerp(-halfAngle, halfAngle, t) * Mathf.Deg2Rad;

            vertices[i + 1] = new Vector3(
                Mathf.Sin(rad) * displayRange,
                heightOffset,
                Mathf.Cos(rad) * displayRange
            );
        }

        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    public void Show()
    {
        if (attackSource != null)
            SetShape(attackSource.AttackRange, attackSource.AttackAngle);
        SyncToSourcePosition();
        meshRenderer.enabled = true;
    }

    public void Hide()
    {
        meshRenderer.enabled = false;
    }

    public void ShowIdle()
    {
        if (attackSource != null)
            SetShape(attackSource.AttackRange, attackSource.AttackAngle);
        SyncToSourcePosition();
        meshRenderer.enabled = true;
    }

    private void SyncToSourcePosition()
    {
        if (attackSourceComponent != null)
            transform.position = attackSourceComponent.transform.position + positionOffset;
    }
}