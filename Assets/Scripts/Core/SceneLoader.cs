using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

namespace OutOfBounds.Core
{
    /// <summary>
    /// 场景加载过渡管理器
    /// 淡入黑屏 → 加载场景 → 淡出还原
    ///
    /// 设计特点：
    /// - 零预制体依赖：运行时自动创建 Canvas + 遮罩 Image
    /// - DontDestroyOnLoad：跨场景保持存活
    /// - 异步加载：SceneManager.LoadSceneAsync 后台加载，不卡主线程
    /// - 覆盖所有现有 SceneManager.LoadScene 调用
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        // ─── 单例 ───────────────────────────────────────────────
        private static SceneLoader _instance;
        private static bool _initialized;

        public static SceneLoader Instance
        {
            get
            {
                if (!_initialized)
                {
                    _instance = FindFirstObjectByType<SceneLoader>();
                    if (_instance == null)
                    {
                        var go = new GameObject("[SceneLoader]");
                        _instance = go.AddComponent<SceneLoader>();
                        DontDestroyOnLoad(go);
                    }
                    _initialized = true;
                }
                return _instance;
            }
        }

        // ─── 配置（可在 Inspector 中调整） ───────────────────────

        [Header("过渡时间")]
        [SerializeField] private float fadeInDuration = 0.3f;   // 渐入黑屏
        [SerializeField] private float fadeOutDuration = 0.3f;  // 渐出还原
        [SerializeField] private float minLoadScreenTime = 0.5f; // 最少黑屏时间（防止闪一下）

        [Header("遮罩设置")]
        [SerializeField] private Color overlayColor = Color.black;
        [SerializeField] [Range(0, 10)] private int overlaySortOrder = 999;

        [Header("可选文字（加载中...）")]
        [SerializeField] private bool showLoadingText = true;
        [SerializeField] private string loadingText = "加载中...";
        [SerializeField] private int textFontSize = 36;
        [SerializeField] private Color textColor = Color.white;

        // ─── 内部引用 ───────────────────────────────────────────
        private Canvas overlayCanvas;
        private Image overlayImage;
        private Text loadingLabel;
        private bool isTransitioning;

        // ─── Unity 生命周期 ─────────────────────────────────────

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _initialized = true;
            DontDestroyOnLoad(gameObject);

            BuildOverlayUI();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _initialized = false;
                _instance = null;
            }
        }

        // ─── 公共 API ───────────────────────────────────────────

        /// <summary>
        /// 按场景名称加载（带淡入淡出过渡）
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (isTransitioning) return;
            StartCoroutine(TransitionRoutine(() =>
                SceneManager.LoadSceneAsync(sceneName)));
        }

        /// <summary>
        /// 按 Build Index 加载（带淡入淡出过渡）
        /// </summary>
        public void LoadScene(int buildIndex)
        {
            if (isTransitioning) return;
            StartCoroutine(TransitionRoutine(() =>
                SceneManager.LoadSceneAsync(buildIndex)));
        }

        /// <summary>
        /// 加载游戏关卡：设置 LevelTransitionData + 加载 TestScene
        /// </summary>
        public void LoadGameLevel(int levelId, string sceneName = "TestScene")
        {
            if (isTransitioning) return;

            LevelTransitionData.NextLevelId = levelId;
            LevelTransitionData.PreviousLevelId = -1;

            StartCoroutine(TransitionRoutine(() =>
                SceneManager.LoadSceneAsync(sceneName)));
        }

        /// <summary>
        /// 返回主菜单（Build Index 0）
        /// </summary>
        public void LoadMainMenu()
        {
            if (isTransitioning) return;

            LevelTransitionData.Reset();
            Events.ClearAll();

            StartCoroutine(TransitionRoutine(() =>
                SceneManager.LoadSceneAsync(0)));
        }

        /// <summary>
        /// 重新加载当前场景（保留 LevelTransitionData 上下文）
        /// </summary>
        public void ReloadCurrentScene(int currentLevelId)
        {
            if (isTransitioning) return;

            LevelTransitionData.NextLevelId = currentLevelId;
            LevelTransitionData.PreviousLevelId = currentLevelId;

            StartCoroutine(TransitionRoutine(() =>
                SceneManager.LoadSceneAsync(
                    SceneManager.GetActiveScene().buildIndex)));
        }

        // ─── 过渡协程 ───────────────────────────────────────────

        private IEnumerator TransitionRoutine(System.Func<AsyncOperation> loadFunc)
        {
            isTransitioning = true;

            // 0. 确保叠加层就绪（如果上次被销毁了则重建）
            EnsureOverlayReady();

            // 1. 淡入黑屏
            yield return StartCoroutine(FadeOverlay(0f, 1f, fadeInDuration));

            // 2. 启动异步场景加载
            AsyncOperation asyncOp = loadFunc();
            asyncOp.allowSceneActivation = false;

            float loadStartTime = Time.realtimeSinceStartup;

            // 3. 等待加载完成 + 最少黑屏时间
            while (!asyncOp.isDone)
            {
                // 加载进度 >= 0.9 表示场景已准备好
                if (asyncOp.progress >= 0.9f)
                {
                    float elapsed = Time.realtimeSinceStartup - loadStartTime;
                    if (elapsed >= minLoadScreenTime)
                    {
                        asyncOp.allowSceneActivation = true;
                    }
                }
                yield return null;
            }

            // 4. 新场景已激活，淡出还原
            yield return StartCoroutine(FadeOverlay(1f, 0f, fadeOutDuration));

            // ★ 5. 过渡完成 → 隐藏叠加 Canvas，避免干扰主场景 UI
            HideOverlay();

            isTransitioning = false;
        }

        // ─── 遮罩动画 ───────────────────────────────────────────

        private IEnumerator FadeOverlay(float fromAlpha, float toAlpha, float duration)
        {
            if (overlayImage == null) yield break;

            float elapsed = 0f;

            // 更新文字显隐：淡入时显示文字，淡出时隐藏
            if (loadingLabel != null)
                loadingLabel.gameObject.SetActive(toAlpha > 0.5f);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float alpha = Mathf.Lerp(fromAlpha, toAlpha, t);

                Color c = overlayImage.color;
                c.a = alpha;
                overlayImage.color = c;

                yield return null;
            }

            // 确保最终值精确
            Color final = overlayImage.color;
            final.a = toAlpha;
            overlayImage.color = final;
        }

        // ─── UI 构建 ────────────────────────────────────────────

        /// <summary>
        /// 过渡结束后销毁叠加 Canvas，避免干扰主场景 UI
        /// 下次需要时自动重建
        /// </summary>
        private void HideOverlay()
        {
            // 直接销毁叠加 UI，完全消除对主场景的影响
            if (overlayCanvas != null)
            {
                Destroy(overlayCanvas.gameObject);
                overlayCanvas = null;
                overlayImage = null;
                loadingLabel = null;
            }
        }

        /// <summary>
        /// 确保叠加层可用
        /// </summary>
        private void EnsureOverlayReady()
        {
            // 如果之前销毁了，自动重建
            BuildOverlayUI();
        }


        private void BuildOverlayUI()
        {
            // 创建 Canvas（如果尚未创建）
            if (overlayCanvas == null)
            {
                GameObject canvasGo = new GameObject("TransitionOverlayCanvas");
                canvasGo.transform.SetParent(transform);

                overlayCanvas = canvasGo.AddComponent<Canvas>();
                overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                overlayCanvas.sortingOrder = overlaySortOrder;

                // 加 CanvasScaler 让 UI 适配分辨率
                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                canvasGo.AddComponent<GraphicRaycaster>();
            }

            // 创建遮罩 Image
            if (overlayImage == null)
            {
                GameObject imgGo = new GameObject("FadeOverlay");
                imgGo.transform.SetParent(overlayCanvas.transform, false);

                var rectTransform = imgGo.AddComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;

                overlayImage = imgGo.AddComponent<Image>();
                overlayImage.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, 0f);
                overlayImage.raycastTarget = false; // 不影响点击
            }

            // 创建"加载中..."文字
            if (showLoadingText && loadingLabel == null)
            {
                GameObject textGo = new GameObject("LoadingText");
                textGo.transform.SetParent(overlayCanvas.transform, false);

                var rectTransform = textGo.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = new Vector2(300, 60);
                rectTransform.anchoredPosition = Vector2.zero;

                loadingLabel = textGo.AddComponent<Text>();
                loadingLabel.text = loadingText;
                loadingLabel.fontSize = textFontSize;
                loadingLabel.color = textColor;
                loadingLabel.alignment = TextAnchor.MiddleCenter;
                loadingLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                loadingLabel.gameObject.SetActive(false);
            }
        }

        // ─── 编辑器调试（在 SceneLoader 组件上右键菜单） ─────

        [ContextMenu("测试淡入淡出")]
        private void DebugFadeTest()
        {
            Debug.Log("[SceneLoader] 测试淡入淡出（无场景加载）");
            StartCoroutine(FadeOverlay(0f, 1f, fadeInDuration));
            StartCoroutine(DebugFadeOut());
        }

        private IEnumerator DebugFadeOut()
        {
            yield return new WaitForSecondsRealtime(1f);
            StartCoroutine(FadeOverlay(1f, 0f, fadeOutDuration));
        }
    }
}
