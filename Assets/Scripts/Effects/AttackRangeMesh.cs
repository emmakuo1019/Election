using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class AttackRangeMesh : MonoBehaviour
{
    [Header("資料來源")]
    [SerializeField] private MonoBehaviour attackSourceBehaviour;
    private IAttackSource attackSource;

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
        attackSource = GetComponentInParent<IAttackSource>();

        if (attackSource == null)
        {
            Debug.LogError("AttackRangeMesh：attackSource 必須實作 IAttackSource");
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

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float rad = Mathf.Lerp(-halfAngle, halfAngle, t) * Mathf.Deg2Rad;

            vertices[i + 1] = new Vector3(
                Mathf.Sin(rad) * range,
                heightOffset,
                Mathf.Cos(rad) * range
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
    }

    public void Show()
    {
        meshRenderer.enabled = true;
    }

    public void Hide()
    {
        meshRenderer.enabled = false;
    }

    public void ShowIdle()
    {
        meshRenderer.enabled = true;
    }
}