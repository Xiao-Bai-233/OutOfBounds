using UnityEngine;
using System.Collections.Generic;
using OutOfBounds.UI;
using OutOfBounds.DragSystem;

namespace OutOfBounds.UI
{
/// <summary>
/// UI物理系统管理器
/// 管理场景中所有的UI物理元素
/// </summary>
public class UIPhysicsManager : MonoBehaviour
{
    public static UIPhysicsManager Instance { get; private set; }

    [Header("设置")]
    [SerializeField] private bool autoFindElements = true;
    [SerializeField] private bool debugMode;

    [Header("心形掉落预制体")]
    [SerializeField] [Tooltip("受伤时掉落到场景中的心形预制体")]
    public GameObject heartFallPrefab;

    // 所有注册的UI物理元素
    private List<UIPhysicsElement> registeredElements = new List<UIPhysicsElement>();
    private List<UIPhysicsElement> elementsToRemove = new List<UIPhysicsElement>();

    // 当前被拖拽的元素
    private DraggableUI currentDraggingElement;

    // 对象池
    private Stack<UIPhysicsElement> heartPool = new Stack<UIPhysicsElement>();
    private Stack<DialogBubbleElement> bubblePool = new Stack<DialogBubbleElement>();
    private GameObject poolContainer;

    #region Unity 生命周期

    private void Awake()
    {
        // 单例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 创建对象池容器
        poolContainer = new GameObject("PhysicsObjectPool");
        poolContainer.transform.SetParent(transform);
        poolContainer.SetActive(false);
    }

    private void Start()
    {
        if (autoFindElements)
        {
            RegisterAllUIPhysicsElements();
        }
    }

    private void Update()
    {
        // 处理待删除元素
        if (elementsToRemove.Count > 0)
        {
            foreach (var element in elementsToRemove)
            {
                registeredElements.Remove(element);
            }
            elementsToRemove.Clear();
        }
        
        // 检查碰撞
        CheckCollisions();
    }

    #endregion

    #region 元素注册

    /// <summary>
    /// 注册一个UI物理元素
    /// </summary>
    public void RegisterElement(UIPhysicsElement element)
    {
        if (element != null && !registeredElements.Contains(element))
        {
            registeredElements.Add(element);
            element.OnBecameGrounded += HandleElementLanded;
            element.OnLeftGround += HandleElementLeftGround;
        }
    }

    /// <summary>
    /// 注销一个UI物理元素
    /// </summary>
    public void UnregisterElement(UIPhysicsElement element)
    {
        if (element != null)
        {
            elementsToRemove.Add(element);
            element.OnBecameGrounded -= HandleElementLanded;
            element.OnLeftGround -= HandleElementLeftGround;
        }
    }

    /// <summary>
    /// 自动查找并注册所有UIPhysicsElement
    /// </summary>
    public void RegisterAllUIPhysicsElements()
    {
        var elements = FindObjectsOfType<UIPhysicsElement>();
        foreach (var element in elements)
        {
            RegisterElement(element);
        }
    }

    #endregion

    #region 物理交互

    /// <summary>
    /// 检查两个UI元素之间的碰撞
    /// </summary>
    public void CheckCollisions()
    {
        for (int i = 0; i < registeredElements.Count; i++)
        {
            for (int j = i + 1; j < registeredElements.Count; j++)
            {
                if (registeredElements[i] == null || registeredElements[j] == null) continue;
                if (registeredElements[i].IsBeingDragged || registeredElements[j].IsBeingDragged) continue;

                if (CheckOverlap(registeredElements[i], registeredElements[j]))
                {
                    ResolveCollision(registeredElements[i], registeredElements[j]);
                }
            }
        }
    }

    private bool CheckOverlap(UIPhysicsElement a, UIPhysicsElement b)
    {
        Rect rectA = GetWorldRect(a.RectTransform);
        Rect rectB = GetWorldRect(b.RectTransform);
        return rectA.Overlaps(rectB);
    }

    private Rect GetWorldRect(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        float minX = Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
        float maxX = Mathf.Max(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
        float minY = Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
        float maxY = Mathf.Max(corners[0].y, corners[1].y, corners[2].y, corners[3].y);

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    private void ResolveCollision(UIPhysicsElement a, UIPhysicsElement b)
    {
        // 1. 位置修正 (Depenetration) - 防止重叠导致的抽搐
        Rect rectA = GetWorldRect(a.RectTransform);
        Rect rectB = GetWorldRect(b.RectTransform);
        
        // 计算重叠区域
        float overlapX = Mathf.Min(rectA.xMax, rectB.xMax) - Mathf.Max(rectA.xMin, rectB.xMin);
        float overlapY = Mathf.Min(rectA.yMax, rectB.yMax) - Mathf.Max(rectA.yMin, rectB.yMin);
        
        // 如果重叠极小，不进行修正，防止微小颤动
        if (overlapX < 0.01f && overlapY < 0.01f) return;

        Vector2 separation = Vector2.zero;
        Vector2 direction = a.RectTransform.position - b.RectTransform.position;
        
        // 沿着重叠较小的轴进行分离
        if (overlapX < overlapY)
        {
            separation.x = overlapX * (direction.x > 0 ? 0.5f : -0.5f);
        }
        else
        {
            separation.y = overlapY * (direction.y > 0 ? 0.5f : -0.5f);
        }
        
        // 应用位置修正 (平分到两个物体上)
        // 稍微降低修正力度，增加稳定性
        float relaxation = 0.8f; 
        if (!a.IsBeingDragged && !a.isKinematic) a.RectTransform.position += (Vector3)(separation * relaxation);
        if (!b.IsBeingDragged && !b.isKinematic) b.RectTransform.position -= (Vector3)(separation * relaxation);

        // 2. 物理冲量响应
        direction.Normalize();
        Vector2 relativeVelocity = a.GetVelocity() - b.GetVelocity();
        float velocityAlongNormal = Vector2.Dot(relativeVelocity, direction);

        // 如果物体正在分离，不处理速度响应
        if (velocityAlongNormal > 0) return;

        // 稳定性处理：如果相对速度极小，不再进行冲量计算
        if (Mathf.Abs(velocityAlongNormal) < 0.1f) return;

        // 计算弹性系数
        float restitution = Mathf.Min(a.bounciness, b.bounciness);

        // 计算冲量
        float impulse = -(1 + restitution) * velocityAlongNormal;
        impulse /= (1 / a.mass) + (1 / b.mass);

        // 应用冲量
        a.AddForce(-impulse * direction);
        b.AddForce(impulse * direction);
    }

    #endregion

    #region 辅助检测方法

    /// <summary>
    /// 检查指定 DraggableUI 是否与任何固定物体重叠（固定UI元素 + Tilemap墙壁/地板）
    /// </summary>
    public bool IsOverlappingWithFixed(DraggableUI draggable)
    {
        if (draggable == null) return false;

        // 1. 检查是否与固定 UI 元素重叠（isFixed 的 UIPhysicsElement）
        RectTransform rt = draggable.GetComponent<RectTransform>();
        if (rt != null)
        {
            Rect draggableRect = GetWorldRect(rt);

            foreach (var element in registeredElements)
            {
                if (element == null || element.gameObject == draggable.gameObject) continue;
                if (!element.isFixed) continue;

                Rect fixedRect = GetWorldRect(element.RectTransform);
                if (draggableRect.Overlaps(fixedRect))
                {
                    return true;
                }
            }
        }

        // 2. 检查是否与 Tilemap 墙壁/地板重叠
        // 墙壁层 Layer 15, 地板层 Layer 16
        RectTransform elementRT = draggable.GetComponent<RectTransform>();
        if (elementRT != null)
        {
            int tilemapLayerMask = (1 << 15) | (1 << 16); // Wall | Ground

            // 方法 A：用 OverlapArea 直接检测矩形区域内的碰撞器（最直接）
            Vector3[] corners = new Vector3[4];
            elementRT.GetWorldCorners(corners);
            // corners[0]=左下, corners[1]=左上, corners[2]=右上, corners[3]=右下
            Vector2 areaMin = new Vector2(
                Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x),
                Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y)
            );
            Vector2 areaMax = new Vector2(
                Mathf.Max(corners[0].x, corners[1].x, corners[2].x, corners[3].x),
                Mathf.Max(corners[0].y, corners[1].y, corners[2].y, corners[3].y)
            );

            Collider2D[] tilemapHits = Physics2D.OverlapAreaAll(
                areaMin,
                areaMax,
                tilemapLayerMask
            );

            if (tilemapHits.Length > 0)
            {
                return true;
            }

            // 方法 B：放大检测范围（防止元素刚好卡在边界上的情况）
            Vector2 inflatedMin = areaMin - Vector2.one * 0.1f;
            Vector2 inflatedMax = areaMax + Vector2.one * 0.1f;
            Collider2D[] inflatedHits = Physics2D.OverlapAreaAll(
                inflatedMin,
                inflatedMax,
                tilemapLayerMask
            );

            if (inflatedHits.Length > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 检查指定 DraggableUI 是否超出 Canvas 边界
    /// </summary>
    public bool IsOutsideCanvas(DraggableUI draggable)
    {
        if (draggable == null) return false;

        RectTransform rt = draggable.GetComponent<RectTransform>();
        if (rt == null) return false;

        Canvas canvas = draggable.GetComponentInParent<Canvas>();
        if (canvas == null) return false;

        RectTransform canvasRT = canvas.transform as RectTransform;
        if (canvasRT == null) return false;

        Rect elementRect = GetWorldRect(rt);
        Rect canvasRect = GetWorldRect(canvasRT);

        // 检查元素是否完全在 Canvas 外（不重叠）
        return !elementRect.Overlaps(canvasRect);
    }

    /// <summary>
    /// 基于中心距离查找附近的 DraggableUI
    /// 用于拖拽中重叠检测
    /// </summary>
    public List<DraggableUI> FindDraggablesByDistance(DraggableUI source, float distanceMultiplier = 1.2f)
    {
        List<DraggableUI> results = new List<DraggableUI>();
        if (source == null) return results;

        RectTransform sourceRT = source.GetComponent<RectTransform>();
        if (sourceRT == null) return results;

        Vector3 sourcePos = sourceRT.position;
        float sourceRadius = sourceRT.rect.size.magnitude * 0.5f;

        foreach (var element in registeredElements)
        {
            if (element == null || element.gameObject == source.gameObject) continue;

            DraggableUI otherDraggable = element.GetComponent<DraggableUI>();
            if (otherDraggable == null) continue;
            if (otherDraggable.IsBeingDragged) continue;

            RectTransform otherRT = element.RectTransform;
            float otherRadius = otherRT.rect.size.magnitude * 0.5f;
            float maxDistance = (sourceRadius + otherRadius) * distanceMultiplier;
            float actualDistance = Vector3.Distance(sourcePos, otherRT.position);

            if (actualDistance < maxDistance)
            {
                results.Add(otherDraggable);
            }
        }
        return results;
    }

    #endregion

    #region 特殊元素生成

    /// <summary>
    /// 生成心形物理元素（支持对象池）
    /// 所有物理/拖拽设置取自预制体，不再代码覆盖
    /// </summary>
    public UIPhysicsElement SpawnHeartElement(Vector2 position, float size = 50f, Transform parent = null, Sprite sprite = null)
    {
        UIPhysicsElement heartElement = null;

        // 尝试从对象池获取
        if (heartPool.Count > 0)
        {
            heartElement = heartPool.Pop();
            heartElement.gameObject.SetActive(true);
            heartElement.transform.SetParent(parent ?? transform);
            heartElement.transform.position = position;
            heartElement.ResetPhysics();
        }
        else
        {
            // 池中没有，从预制体实例化（预制体自带所有物理/拖拽配置）
            if (heartFallPrefab == null)
            {
                Debug.LogError("[UIPhysicsManager] heartFallPrefab 未赋值！请将心形掉落预制体拖入 Inspector");
                return null;
            }
            GameObject heartObj = Instantiate(heartFallPrefab, parent ?? transform);
            heartObj.transform.position = position;

            heartElement = heartObj.GetComponent<UIPhysicsElement>();
            if (heartElement == null) heartElement = heartObj.AddComponent<UIPhysicsElement>();

            // 确保有碰撞器
            if (heartObj.GetComponent<BoxCollider2D>() == null)
                heartObj.AddComponent<BoxCollider2D>();
        }

        RegisterElement(heartElement);
        return heartElement;
    }

    /// <summary>
    /// 回收心形物理元素到对象池
    /// </summary>
    public void RecycleHeartElement(UIPhysicsElement element)
    {
        if (element == null) return;

        UnregisterElement(element);
        
        // 如果是心形元素（通过名称或组件判断，这里简单通过名称）
        if (element.name.StartsWith("HeartElement"))
        {
            element.gameObject.SetActive(false);
            element.transform.SetParent(poolContainer.transform);
            heartPool.Push(element);
        }
        else
        {
            Destroy(element.gameObject);
        }
    }

    /// <summary>
    /// 生成对话气泡（支持对象池）
    /// </summary>
    public DialogBubbleElement SpawnBubbleElement(GameObject prefab, Vector2 position, string text, Transform parent = null)
    {
        DialogBubbleElement bubble = null;

        if (bubblePool.Count > 0)
        {
            bubble = bubblePool.Pop();
            bubble.gameObject.SetActive(true);
            bubble.transform.SetParent(parent ?? transform);
            bubble.transform.position = position;
            bubble.ResetPhysics();
        }
        else
        {
            GameObject go = Instantiate(prefab, parent ?? transform);
            go.transform.position = position;
            bubble = go.GetComponent<DialogBubbleElement>();
            if (bubble == null) bubble = go.AddComponent<DialogBubbleElement>();
        }

        bubble.SetText(text);
        RegisterElement(bubble);
        return bubble;
    }

    /// <summary>
    /// 回收对话气泡到对象池
    /// </summary>
    public void RecycleBubbleElement(DialogBubbleElement element)
    {
        if (element == null) return;

        UnregisterElement(element);
        element.gameObject.SetActive(false);
        element.transform.SetParent(poolContainer.transform);
        bubblePool.Push(element);
    }

    #endregion

    #region 事件处理

    private void HandleElementLanded(UIPhysicsElement element)
    {
        // 可以在这里添加落地效果
        if (debugMode)
        {
            Debug.Log($"[UIPhysics] Element landed: {element.name}");
        }
    }

    private void HandleElementLeftGround(UIPhysicsElement element)
    {
        if (debugMode)
        {
            Debug.Log($"[UIPhysics] Element left ground: {element.name}");
        }
    }

    #endregion

    #region 公共属性

    public List<UIPhysicsElement> RegisteredElements => registeredElements;
    public int ElementCount => registeredElements.Count;
    public DraggableUI CurrentDraggingElement => currentDraggingElement;

    #endregion

    #region 编辑器支持

    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!debugMode) return;

        // 绘制已注册元素的调试信息
        if (registeredElements != null)
        {
            Gizmos.color = Color.cyan;
            foreach (var element in registeredElements)
            {
                if (element != null && element.RectTransform != null)
                {
                    Vector3 pos = element.RectTransform.position;
                    Vector3 size = element.RectTransform.rect.size;
                    Gizmos.DrawWireCube(pos, size);
                }
            }
        }
    }
    #endif

    #endregion
}
}
