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

    #region 特殊元素生成

    /// <summary>
    /// 生成心形物理元素（支持对象池）
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
            
            // 更新尺寸
            RectTransform rect = heartElement.GetComponent<RectTransform>();
            if (rect != null) rect.sizeDelta = new Vector2(size, size);

            // 更新碰撞体尺寸
            BoxCollider2D collider = heartElement.GetComponent<BoxCollider2D>();
            if (collider != null) collider.size = new Vector2(size, size);
            
            // 更新 Image
            var image = heartElement.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                image.sprite = sprite;
                image.color = sprite != null ? Color.white : Color.red;
            }

            // 重置物理状态
            heartElement.ResetPhysics();
        }
        else
        {
            // 池中没有，则创建新对象
            GameObject heartObj = CreateHeartUI(size, sprite);
            heartObj.transform.SetParent(parent ?? transform);
            heartObj.transform.position = position;

            heartElement = heartObj.AddComponent<UIPhysicsElement>();
            heartElement.SetUseGravity(true);

            // 添加碰撞器
            var collider = heartObj.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(size, size);
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

    private GameObject CreateHeartUI(float size = 50f, Sprite sprite = null)
    {
        // 创建心形Image
        var go = new GameObject("HeartElement");
        var image = go.AddComponent<UnityEngine.UI.Image>();

        if (sprite != null)
        {
            image.sprite = sprite;
            image.color = Color.white;
        }
        else
        {
            // TODO: 需要美术提供心形Sprite
            // 暂时使用圆形代替
            image.color = Color.red;
        }

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(size, size);

        return go;
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
