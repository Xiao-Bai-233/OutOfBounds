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
        // 简化的碰撞响应
        Vector2 direction = a.RectTransform.position - b.RectTransform.position;
        direction.Normalize();

        // 计算相对速度
        Vector2 relativeVelocity = a.GetVelocity() - b.GetVelocity();
        float velocityAlongNormal = Vector2.Dot(relativeVelocity, direction);

        // 如果物体正在分离，不处理
        if (velocityAlongNormal > 0) return;

        // 计算弹性系数
        float restitution = 0.5f;

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
    /// 生成心形物理元素（用于血条搭桥机制）
    /// </summary>
    public UIPhysicsElement SpawnHeartElement(Vector2 position, float size = 50f, Transform parent = null)
    {
        // 创建一个心形UI元素
        GameObject heartObj = CreateHeartUI(size);
        heartObj.transform.SetParent(parent ?? transform);
        heartObj.transform.position = position;

        var physicsElement = heartObj.AddComponent<UIPhysicsElement>();
        physicsElement.SetUseGravity(true);

        // 添加碰撞器组件，使心形元素可以碰撞
        var collider = heartObj.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(size, size);

        RegisterElement(physicsElement);

        return physicsElement;
    }

    private GameObject CreateHeartUI(float size = 50f)
    {
        // 创建心形Image
        var go = new GameObject("HeartElement");
        var image = go.AddComponent<UnityEngine.UI.Image>();

        // TODO: 需要美术提供心形Sprite
        // 暂时使用圆形代替
        image.color = Color.red;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(size, size);

        return go;
    }

    /// <summary>
    /// 生成一个通用的可拖拽UI物理元素
    /// </summary>
    public DraggableUI SpawnDraggableElement(Vector2 position, Vector2 size, Color color, Transform parent = null)
    {
        var go = new GameObject("DraggableUIElement");
        go.transform.SetParent(parent ?? transform);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.position = position;

        var image = go.AddComponent<UnityEngine.UI.Image>();
        image.color = color;

        var physicsElement = go.AddComponent<UIPhysicsElement>();
        var draggable = go.AddComponent<DraggableUI>();

        RegisterElement(physicsElement);

        return draggable;
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
