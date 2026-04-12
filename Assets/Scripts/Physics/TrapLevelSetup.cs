using UnityEngine;
using OutOfBounds.UI;
using OutOfBounds.Player;
using OutOfBounds.DragSystem;

namespace OutOfBounds.Physics
{
    /// <summary>
    /// 陷阱关卡设置
    /// 创建包含地刺陷阱、毒水坑和血条系统的测试场景
    /// </summary>
    public class TrapLevelSetup : MonoBehaviour
    {
        [Header("关卡设置")]
        [SerializeField] private bool autoSetupOnStart = true;
        [SerializeField] private bool createPlayer = true;
        [SerializeField] private bool createGround = true;
        [SerializeField] private bool createTraps = true;
        [SerializeField] private bool createPoisonPit = true;
        [SerializeField] private bool createHealthBar = true;
        [SerializeField] private bool createTaskWindow = true;

        [Header("场景配置")]
        [SerializeField] private Vector2 groundSize = new Vector2(30f, 1f);
        [SerializeField] private Vector2 groundPosition = new Vector2(0f, -6f);
        [SerializeField] private Vector2 playerStartPosition = new Vector2(-10f, 0f);

        [Header("陷阱配置")]
        [SerializeField] private Vector2 spikeTrapPosition = new Vector2(-5f, -4.5f);
        [SerializeField] private Vector2 spikeTrapSize = new Vector2(2f, 1f);

        [Header("毒水坑配置")]
        [SerializeField] private Vector2 poisonPitPosition = new Vector2(5f, -5f);
        [SerializeField] private Vector2 poisonPitSize = new Vector2(3f, 1f);

        [Header("血条配置")]
    [SerializeField] private Vector2 healthBarPosition = new Vector2(0f, 0f);

    [Header("任务窗口配置")]
    [SerializeField] private Vector2 taskWindowSize = new Vector2(400f, 200f);
    [SerializeField] private Vector2 taskWindowCollisionSize = new Vector2(4f, 2f); // 碰撞体积大小

        #region Unity 生命周期

        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupTrapLevel();
            }
        }

        #endregion

        #region 场景设置

        /// <summary>
        /// 设置陷阱关卡
        /// </summary>
        [ContextMenu("设置陷阱关卡")]
        public void SetupTrapLevel()
        {
            Debug.Log("[TrapLevelSetup] 开始设置陷阱关卡...");

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

            // 创建陷阱
            if (createTraps)
            {
                CreateSpikeTrap();
            }

            // 创建毒水坑
            if (createPoisonPit)
            {
                CreatePoisonPit();
            }

            // 创建血条
            if (createHealthBar)
            {
                CreateHealthBar();
            }

            // 创建任务目标窗口
            if (createTaskWindow)
            {
                CreateTaskWindow();
            }

            // 创建摄像机
            EnsureCamera();

            Debug.Log("[TrapLevelSetup] 陷阱关卡设置完成！");
        }

        #endregion

        #region 组件创建

        private void EnsureGameManager()
        {
            if (FindObjectOfType<GameManager>() == null)
            {
                var go = new GameObject("GameManager");
                go.AddComponent<GameManager>();
                Debug.Log("[TrapLevelSetup] 已创建 GameManager");
            }
        }

        private void EnsureGlobalPhysics()
        {
            if (FindObjectOfType<GlobalPhysicsSettings>() == null)
            {
                var go = new GameObject("GlobalPhysicsSettings");
                go.AddComponent<GlobalPhysicsSettings>();
                Debug.Log("[TrapLevelSetup] 已创建 GlobalPhysicsSettings");
            }
        }

        private void CreatePlayer()
        {
            if (FindObjectOfType<PlayerController>() != null)
            {
                Debug.Log("[TrapLevelSetup] 玩家已存在，跳过创建");
                return;
            }

            // 创建玩家对象
            var playerObj = new GameObject("Player");
            playerObj.transform.position = playerStartPosition;

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

            // 添加标签
            playerObj.tag = "Player";

            // 添加角色控制器
            playerObj.AddComponent<PlayerController>();

            Debug.Log("[TrapLevelSetup] 已创建玩家角色");
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

            Debug.Log($"[TrapLevelSetup] 已创建地面 at {groundPosition}");
        }

        private void CreateSpikeTrap()
        {
            var spikeObj = new GameObject("SpikeTrap");
            spikeObj.transform.position = spikeTrapPosition;

            var sprite = spikeObj.AddComponent<SpriteRenderer>();
            sprite.sprite = CreateSquareSprite();
            sprite.color = Color.red;
            sprite.sortingOrder = 5;

            var col = spikeObj.AddComponent<BoxCollider2D>();
            col.size = spikeTrapSize;
            col.isTrigger = true;

            // 添加地刺陷阱组件
            spikeObj.AddComponent<SpikeTrap>();

            Debug.Log($"[TrapLevelSetup] 已创建地刺陷阱 at {spikeTrapPosition}");
        }

        private void CreatePoisonPit()
        {
            var pitObj = new GameObject("PoisonPit");
            pitObj.transform.position = poisonPitPosition;

            var sprite = pitObj.AddComponent<SpriteRenderer>();
            sprite.sprite = CreateSquareSprite();
            sprite.color = Color.green;
            sprite.sortingOrder = 5;

            var col = pitObj.AddComponent<BoxCollider2D>();
            col.size = poisonPitSize;
            col.isTrigger = true;

            // 添加毒水坑组件
            var poisonPit = pitObj.AddComponent<PoisonPit>();
            // 设置需要2颗心填充
            Debug.Log($"[TrapLevelSetup] 已创建毒水坑 at {poisonPitPosition}");
        }

        private void CreateHealthBar()
        {
            // 创建UI画布
            var canvasObj = CreateUICanvas();

            // 检查是否已经存在HealthBar物体
            var existingHealthBar = canvasObj.transform.Find("HealthBar");
            GameObject healthBarObj;
            if (existingHealthBar != null)
            {
                // 复用现有的HealthBar物体
                healthBarObj = existingHealthBar.gameObject;
                Debug.Log("[TrapLevelSetup] 复用现有的血条系统");
            }
            else
            {
                // 创建血条容器
                healthBarObj = new GameObject("HealthBar");
                healthBarObj.transform.SetParent(canvasObj.transform);

                var rect = healthBarObj.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(200f, 50f);
                rect.anchoredPosition = healthBarPosition;

                // 添加血条管理器
                healthBarObj.AddComponent<HealthBarManager>();

                // 添加血条跟随脚本
                var follower = healthBarObj.AddComponent<OutOfBounds.UI.HealthBarFollower>();
                var player = FindObjectOfType<OutOfBounds.Player.PlayerController>();
                if (player != null)
                {
                    follower.followTarget = player.transform;
                }

                Debug.Log("[TrapLevelSetup] 已创建血条系统");
            }

            // 确保UIPhysicsManager存在
            if (canvasObj.GetComponent<UIPhysicsManager>() == null)
            {
                canvasObj.AddComponent<UIPhysicsManager>();
            }
        }

        private void CreateTaskWindow()
        {
            // 使用现有的UIPhysicsCanvas（World Space模式）
            var canvasObj = CreateUICanvas();
            Canvas taskCanvas = canvasObj.GetComponent<Canvas>();

            // 检查是否已经存在TaskWindow物体
            var existingTaskWindow = canvasObj.transform.Find("TaskWindow");
            GameObject taskWindowObj;
            if (existingTaskWindow != null)
            {
                // 复用现有的TaskWindow物体
                taskWindowObj = existingTaskWindow.gameObject;
                Debug.Log("[TrapLevelSetup] 复用现有的任务目标窗口");
            }
            else
            {
                // 创建任务窗口
                taskWindowObj = new GameObject("TaskWindow");
                taskWindowObj.transform.SetParent(canvasObj.transform);

                var rect = taskWindowObj.AddComponent<RectTransform>();
                rect.sizeDelta = taskWindowSize;
                rect.anchoredPosition = new Vector2(0f, 0f);
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);

                // 设置TaskWindow的世界位置
                taskWindowObj.transform.position = new Vector3(0f, 3f, 0f); // 显示在屏幕上方

                // 添加背景
                var background = new GameObject("Background");
                background.transform.SetParent(taskWindowObj.transform);
                background.transform.localPosition = Vector3.zero;
                background.transform.localScale = Vector3.one;

                var bgImage = background.AddComponent<UnityEngine.UI.Image>();
                bgImage.color = new Color(0f, 0f, 0f, 0.8f);

                var bgRect = background.GetComponent<RectTransform>();
                bgRect.sizeDelta = taskWindowSize;

                // 添加任务文本
                var textObj = new GameObject("TaskText");
                textObj.transform.SetParent(taskWindowObj.transform);
                textObj.transform.localPosition = Vector3.zero;

                var text = textObj.AddComponent<UnityEngine.UI.Text>();
                text.color = Color.white;
                text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                text.fontSize = 20;
                text.alignment = TextAnchor.MiddleCenter;

                var textRect = textObj.GetComponent<RectTransform>();
                textRect.sizeDelta = new Vector2(380f, 180f);

                // 添加任务窗口脚本
                var taskWindowScript = taskWindowObj.AddComponent<OutOfBounds.UI.TaskWindow>();
                taskWindowScript.taskText = text;

                // 创建任务窗口管理器
                var managerObj = new GameObject("TaskWindowManager");
                managerObj.transform.SetParent(canvasObj.transform);
                var manager = managerObj.AddComponent<OutOfBounds.UI.TaskWindowManager>();
                manager.taskWindow = taskWindowObj;

                // 添加物理碰撞和拖拽功能
                // 添加UIPhysicsElement组件
                var physicsElement = taskWindowObj.AddComponent<OutOfBounds.UI.UIPhysicsElement>();
                physicsElement.mass = 5f; // 增加质量，使其更稳定
                physicsElement.drag = 2f;
                physicsElement.useGravity = true;
                physicsElement.isPlatform = true; // 允许玩家站在上面
                physicsElement.isFloating = false; // 不漂浮，受重力影响

                // 添加DraggableUI组件
                var draggable = taskWindowObj.AddComponent<OutOfBounds.DragSystem.DraggableUI>();
                draggable.canBeDragged = true;
                draggable.enableRKeyReset = true; // 允许按R键重置位置

                // 添加BoxCollider2D，用于物理碰撞
                var collider = taskWindowObj.AddComponent<BoxCollider2D>();
                collider.size = taskWindowCollisionSize; // 调整碰撞器大小，与窗口匹配

                // 注册到UIPhysicsManager
                if (OutOfBounds.UI.UIPhysicsManager.Instance != null)
                {
                    OutOfBounds.UI.UIPhysicsManager.Instance.RegisterElement(physicsElement);
                }

                Debug.Log("[TrapLevelSetup] 已创建任务目标窗口和管理器，并添加了物理碰撞和拖拽功能");
            }
        }

        private GameObject CreateUICanvas()
        {
            // 检查是否已存在画布
            var existingCanvas = FindObjectOfType<Canvas>();
            if (existingCanvas != null)
            {
                // 确保已存在的Canvas有UIPhysicsManager组件
                if (existingCanvas.GetComponent<OutOfBounds.UI.UIPhysicsManager>() == null)
                {
                    existingCanvas.gameObject.AddComponent<OutOfBounds.UI.UIPhysicsManager>();
                }
                return existingCanvas.gameObject;
            }

            var canvasObj = new GameObject("UIPhysicsCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;

            var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // 添加UIPhysicsManager组件
            canvasObj.AddComponent<OutOfBounds.UI.UIPhysicsManager>();

            // 检查是否已存在Event System
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // 设置Canvas的大小和位置
            RectTransform rect = canvasObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1000f, 1000f); // 增大Canvas大小，确保能容纳TaskWindow
            rect.position = new Vector3(0, 0, -1f);

            return canvasObj;
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

            Color[] colors = new Color[64 * 64];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.white;
            }
            tex.SetPixels(colors);
            tex.Apply();

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);

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

            // 清理陷阱
            var spikeTraps = FindObjectsOfType<SpikeTrap>();
            foreach (var trap in spikeTraps)
            {
                DestroyImmediate(trap.gameObject);
            }

            // 清理毒水坑
            var poisonPits = FindObjectsOfType<PoisonPit>();
            foreach (var pit in poisonPits)
            {
                DestroyImmediate(pit.gameObject);
            }

            // 清理血条
            var healthBars = FindObjectsOfType<HealthBarManager>();
            foreach (var bar in healthBars)
            {
                DestroyImmediate(bar.gameObject);
            }

            Debug.Log("[TrapLevelSetup] 测试场景已清理");
        }

        #endregion
    }
}
