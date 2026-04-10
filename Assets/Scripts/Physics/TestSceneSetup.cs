using UnityEngine;
using OutOfBounds.UI;
using OutOfBounds.Player;
using OutOfBounds.DragSystem;

namespace OutOfBounds.Physics
{
/// <summary>
/// 测试场景设置助手
/// 快速创建测试场景的工具
/// </summary>
public class TestSceneSetup : MonoBehaviour
{
    [Header("测试设置")]
    [SerializeField] private bool autoSetupOnStart;
    [SerializeField] private bool createPlayer = true;
    [SerializeField] private bool createGround = true;
    [SerializeField] private bool createUIPhysicsElements = true;
    [SerializeField] private bool createUICanvas = true;

    [Header("预设配置")]
    [SerializeField] private Vector2 groundSize = new Vector2(20f, 1f);
    [SerializeField] private Vector2 groundPosition = new Vector2(0f, -5f);
    [SerializeField] private int numberOfUIElements = 3;

    // Start is called before the first frame update
    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupTestScene();
        }
    }

    /// <summary>
    /// 一键设置测试场景
    /// </summary>
    [ContextMenu("设置测试场景")]
    public void SetupTestScene()
    {
        Debug.Log("[TestSceneSetup] 开始设置测试场景...");

        // 创建GameManager
        EnsureGameManager();

        // 创建全局物理设置
        EnsureGlobalPhysics();

        // 创建地面
        if (createGround)
        {
            CreateGround();
        }

        // 创建玩家
        if (createPlayer)
        {
            CreatePlayer();
        }

        // 创建UI画布和物理元素
        if (createUICanvas || createUIPhysicsElements)
        {
            SetupUIPhysics();
        }

        // 创建摄像机
        EnsureCamera();

        Debug.Log("[TestSceneSetup] 测试场景设置完成！");
    }

    #region 组件创建

    private void EnsureGameManager()
    {
        if (FindObjectOfType<GameManager>() == null)
        {
            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
            Debug.Log("[TestSceneSetup] 已创建 GameManager");
        }
    }

    private void EnsureGlobalPhysics()
    {
        if (FindObjectOfType<GlobalPhysicsSettings>() == null)
        {
            var go = new GameObject("GlobalPhysicsSettings");
            go.AddComponent<GlobalPhysicsSettings>();
            Debug.Log("[TestSceneSetup] 已创建 GlobalPhysicsSettings");
        }
    }

    private void CreatePlayer()
    {
        if (FindObjectOfType<PlayerController>() != null)
        {
            Debug.Log("[TestSceneSetup] 玩家已存在，跳过创建");
            return;
        }

        // 创建玩家对象
        var playerObj = new GameObject("Player");
        playerObj.transform.position = new Vector3(0, 0, 0);

        // 添加 SpriteRenderer（临时用方块代替）
        var sprite = playerObj.AddComponent<SpriteRenderer>();
        sprite.sprite = CreateSquareSprite();
        sprite.color = Color.blue;
        sprite.sortingOrder = 10;

        // 添加刚体
        var rb = playerObj.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // 添加碰撞器
        var col = playerObj.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);

        // 添加角色控制器
        playerObj.AddComponent<PlayerController>();

        Debug.Log("[TestSceneSetup] 已创建玩家角色");
    }

    private void CreateGround()
    {
        var groundObj = new GameObject("Ground");
        groundObj.transform.position = groundPosition;

        var sprite = groundObj.AddComponent<SpriteRenderer>();
        sprite.sprite = CreateSquareSprite();
        sprite.color = Color.gray;
        sprite.sortingOrder = 5;

        var col = groundObj.AddComponent<BoxCollider2D>();
        col.size = groundSize;

        // 添加标签
        groundObj.tag = "Ground";

        Debug.Log($"[TestSceneSetup] 已创建地面 at {groundPosition}");
    }

    private void SetupUIPhysics()
    {
        // 创建UI画布
        var canvasObj = CreateUICanvas();
        var physicsManager = canvasObj.AddComponent<UIPhysicsManager>();

        // 创建UI物理元素
        if (createUIPhysicsElements)
        {
            CreateUIPhysicsElements(canvasObj.transform);
        }

        Debug.Log("[TestSceneSetup] 已创建UI物理系统");
    }

    private GameObject CreateUICanvas()
    {
        // 检查是否已存在画布
        var existingCanvas = FindObjectOfType<Canvas>();
        if (existingCanvas != null)
        {
            return existingCanvas.gameObject;
        }

        var canvasObj = new GameObject("UIPhysicsCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        return canvasObj;
    }

    private void CreateUIPhysicsElements(Transform parent)
    {
        // 创建可拖拽的UI元素
        Vector2[] positions = {
            new Vector2(960, 540),   // 中心
            new Vector2(500, 400),    // 左上
            new Vector2(1420, 400),   // 右上
        };

        Color[] colors = { Color.red, Color.green, Color.yellow };

        for (int i = 0; i < numberOfUIElements && i < positions.Length; i++)
        {
            var elementObj = new GameObject($"UIPhysicsElement_{i + 1}");
            elementObj.transform.SetParent(parent);

            var rect = elementObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 60);
            rect.position = positions[i];

            var image = elementObj.AddComponent<UnityEngine.UI.Image>();
            image.color = colors[i];

            // 添加物理元素组件
            elementObj.AddComponent<UIPhysicsElement>();

            // 添加拖拽组件
            var draggable = elementObj.AddComponent<DraggableUI>();

            // 设置UI物理管理器
            UIPhysicsManager.Instance?.RegisterElement(elementObj.GetComponent<UIPhysicsElement>());
        }

        Debug.Log($"[TestSceneSetup] 已创建 {numberOfUIElements} 个UI物理元素");
    }

    private void EnsureCamera()
    {
        var cam = UnityEngine.Camera.main;
        if (cam == null)
        {
            var camObj = new GameObject("MainCamera");
            camObj.AddComponent<UnityEngine.Camera>();
            camObj.tag = "MainCamera";
            camObj.transform.position = new Vector3(0, 0, -10);
        }
    }

    #endregion

    #region 工具方法

    private Sprite CreateSquareSprite()
    {
        // 创建一个简单的方形Sprite
        Texture2D tex = new Texture2D(64, 64);
        // 修复: 防止 Unity 因 DontSaveInEditor 标志触发断言错误
        tex.hideFlags = HideFlags.HideAndDontSave;

        Color[] colors = new Color[64 * 64];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.white;
        }
        tex.SetPixels(colors);
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
        // 同样设置 Sprite 的 hideFlags
        sprite.hideFlags = HideFlags.HideAndDontSave;

        return sprite;
    }

    #endregion

    #region 编辑器菜单

    [ContextMenu("清理测试场景")]
    public void CleanupTestScene()
    {
        // 清理玩家
        var player = FindObjectOfType<PlayerController>();
        if (player != null) DestroyImmediate(player.gameObject);

        // 清理地面
        var grounds = GameObject.FindGameObjectsWithTag("Ground");
        foreach (var g in grounds)
        {
            DestroyImmediate(g);
        }

        Debug.Log("[TestSceneSetup] 测试场景已清理");
    }

    #endregion
}
}
