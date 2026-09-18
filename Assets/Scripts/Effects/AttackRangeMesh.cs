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
    
    [Header("近距離圓形範圍")]
    [Tooltip("是否啟用玩家腳下的圓形攻擊範圍")]
    public bool enableCloseRangeCircle = true;
    [Tooltip("圓形範圍半徑（通常設為扇形最窄處的補償值）")]
    public float closeRangeRadius = 1.5f;

    private Mesh mesh;
    private MeshRenderer meshRenderer;
    
    // 用於圓形範圍的獨立物件
    private GameObject circleRangeObject;
    private MeshRenderer circleRenderer;

    void Awake()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        meshRenderer = GetComponent<MeshRenderer>();

        BindSource();
        CreateCloseRangeCircle();
    }
    
    void OnDestroy()
    {
        if (circleRangeObject != null)
        {
            Destroy(circleRangeObject);
        }
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

    void Update()
    {
        // 每幀追蹤來源位置，確保網格跟隨移動
        if (meshRenderer != null && meshRenderer.enabled)
        {
            SyncToSourcePosition();
        }
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
            // 暫時註解掉報錯，目前用不到攻擊範圍
            // Debug.LogError($"AttackRangeMesh [{gameObject.name}]：找不到 IAttackSource！請確認 Inspector 中的 attackSourceBehaviour 是否拖曳了正確的腳本 (例如 PlayerAttack 或 EnemyAI)，或確保其父物件有掛載實作 IAttackSource 的腳本。");
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
        
        if (enableCloseRangeCircle && circleRenderer != null)
        {
            circleRenderer.enabled = true;
        }
    }

    public void Hide()
    {
        meshRenderer.enabled = false;
        
        if (circleRenderer != null)
        {
            circleRenderer.enabled = false;
        }
    }

    public void ShowIdle()
    {
        if (attackSource != null)
            SetShape(attackSource.AttackRange, attackSource.AttackAngle);
        SyncToSourcePosition();
        meshRenderer.enabled = true;
        
        if (enableCloseRangeCircle && circleRenderer != null)
        {
            circleRenderer.enabled = true;
        }
    }

    private void SyncToSourcePosition()
    {
        if (attackSourceComponent != null)
        {
            transform.position = attackSourceComponent.transform.position + positionOffset;
        }
        
        // 透過 IAttackSource 取得方向，由來源端負責提供，保持視覺端單一職責
        if (attackSource != null && attackSource.AttackDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(attackSource.AttackDirection);
        }
        
        // 同步圓形範圍位置（圓形不旋轉）
        if (circleRangeObject != null && attackSourceComponent != null)
        {
            circleRangeObject.transform.position = attackSourceComponent.transform.position + positionOffset;
        }
    }
    
    /// <summary>
    /// 建立腳下的圓形攻擊範圍 Mesh
    /// </summary>
    private void CreateCloseRangeCircle()
    {
        if (!enableCloseRangeCircle)
            return;
        
        // 建立獨立的 GameObject
        circleRangeObject = new GameObject("CloseRangeCircle");
        circleRangeObject.transform.SetParent(transform.parent);
        circleRangeObject.transform.localPosition = positionOffset;
        circleRangeObject.transform.localRotation = Quaternion.identity;
        
        // 新增 MeshFilter 和 MeshRenderer
        MeshFilter circleMeshFilter = circleRangeObject.AddComponent<MeshFilter>();
        circleRenderer = circleRangeObject.AddComponent<MeshRenderer>();
        
        // 使用與扇形相同的材質
        circleRenderer.sharedMaterial = meshRenderer.sharedMaterial;
        circleRenderer.enabled = false;
        
        // 生成圓形 Mesh
        Mesh circleMesh = GenerateCircleMesh(closeRangeRadius, 32);
        circleMeshFilter.mesh = circleMesh;
    }
    
    /// <summary>
    /// 生成圓形 Mesh
    /// </summary>
    private Mesh GenerateCircleMesh(float radius, int segments)
    {
        Mesh circleMesh = new Mesh();
        int vertexCount = segments + 2; // 中心點 + 外圈點 + 閉合點
        
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[segments * 3];
        
        // 抵消父物件的縮放
        float lossyScaleXZ = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
        float displayRadius = lossyScaleXZ > 0.0001f ? radius / lossyScaleXZ : radius;
        
        // 中心點
        vertices[0] = new Vector3(0, heightOffset, 0);
        
        // 外圈頂點
        for (int i = 0; i <= segments; i++)
        {
            float angle = ((float)i / segments) * 2f * Mathf.PI;
            vertices[i + 1] = new Vector3(
                Mathf.Cos(angle) * displayRadius,
                heightOffset,
                Mathf.Sin(angle) * displayRadius
            );
        }
        
        // 三角形索引
        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }
        
        circleMesh.vertices = vertices;
        circleMesh.triangles = triangles;
        circleMesh.RecalculateNormals();
        circleMesh.RecalculateBounds();
        
        return circleMesh;
    }
    
    /// <summary>
    /// 取得圓形範圍半徑（供攻擊判定使用）
    /// </summary>
    public float GetCloseRangeRadius()
    {
        return enableCloseRangeCircle ? closeRangeRadius : 0f;
    }
}