==========================================
  OutOfBounds 关卡选择系统 - 编辑器配置指南
==========================================

一、新增文件清单（代码已完成，无需修改）
----------------------------------------
  Assets/Scripts/Core/SaveData.cs           - 存档数据结构
  Assets/Scripts/Core/SaveSystem.cs         - JSON 存档读写系统
  Assets/Scripts/Core/LevelDefinition.cs    - 关卡定义 ScriptableObject
  Assets/Scripts/Core/LevelTransitionData.cs - 跨场景传递关卡索引
  Assets/Scripts/UI/LevelButton.cs          - 关卡卡片组件
  Assets/Scripts/UI/LevelSelectionManager.cs - 关卡选择网格管理器

二、修改文件清单（已更新）
----------------------------------------
  Assets/Scripts/UI/MainMenuManager.cs      - 集成关卡选择面板
  Assets/Scripts/Physics/GameManager.cs     - 接收关卡索引 + 存档写入
  Assets/Scripts/Puzzle/LevelStageManager.cs - 阶段全通 => 通知 GameManager
  Assets/Scripts/Core/GameEvents.cs         - 新增关卡相关事件

三、编辑器配置步骤
----------------------------------------

=== 步骤 1: 创建关卡定义资产（ScriptableObject） ===

  1. 在 Project 窗口中右键 -> Create -> OutOfBounds -> 关卡定义
  2. 命名为 "Level_Tutorial"
  3. 配置：
     - Level Index: 0
     - Level Name: "异常苏醒"
     - Level Description: "使用 WASD 移动，Space 跳跃"
     - Scene Level ID: 0
     - Is Tutorial: ✅
     - Grid Row: 0, Grid Column: 0
  
  4. 再次创建 "Level_01"
     - Level Index: 1
     - Level Name: "UI丛林"
     - Scene Level ID: 1
     - Grid Row: 0, Grid Column: 1
  
  5. （可选）继续创建更多关卡...

=== 步骤 2: 配置 SampleScene 中的 UI ===

  Canvas 下创建关卡选择面板层次结构：
  
  Canvas
  ├── MenuManager (GameObject)  ← 已有，挂载 MainMenuManager
  ├── MainMenuPanel              ← 已有主菜单按钮
  │   ├── StartGameBtn           ← "选关" 按钮（绑定 OpenLevelSelection）
  │   ├── ContinueBtn            ← "继续游戏" 按钮（绑定 ContinueGame）
  │   ├── NewGameBtn             ← "新游戏" 按钮（绑定 NewGame）
  │   └── QuitBtn                ← "退出" 按钮
  └── LevelSelectionPanel        ← ★ 新建：关卡选择面板
      ├── TitleText (Text)       ← 标题："选择关卡"
      ├── BackButton (Button)    ← "返回" 按钮（绑定 BackToMainMenu）
      └── GridContent (空对象)    ← ★ 挂载 GridLayoutGroup 组件
                                  └── (运行时自动生成子物体)


  GridLayoutGroup 配置建议：
  - Cell Size: X=160, Y=200
  - Spacing: X=20, Y=20
  - Constraint: Fixed Column Count = 3
  - Start Axis: Horizontal
  - Child Alignment: Middle Center

=== 步骤 3: 创建 LevelButton 预制体（极简 — 零美术资源） ===

  1. Hierarchy 中创建一个 UI → Button (Legacy)
  2. 删除 Button 子物体中的 "Text"（我们重新加）
  3. 在 Button 下新建一个 UI → Text（命名为 "LabelText"）
  4. 配置 LabelText 的 RectTransform：
     - Anchor: stretch/stretch 填满整个按钮
     - Font Size: 18 (根据按钮尺寸调整)
     - Alignment: Center Middle
     - Text 留空（由代码控制）
  5. 给 Button 挂载 LevelButton 组件
  6. 将 LabelText 拖入 Label Text 插槽，Button 拖入 Button 插槽
  7. 拖入 Project 窗口存为预制体

  最终预制体结构：
  LevelButtonPrefab (Button + LevelButton)
  └── LabelText (Text — 自动显示如 "[锁] 1\nUI丛林")

  所需美术资源：零
  Unity 内置的 Button (Legacy) 自带背景 UISprite，无需任何外部图片

=== 步骤 4: 组装 LevelSelectionManager ===

  1. 将 LevelSelectionPanel 挂载 LevelSelectionManager 组件
  2. 配置各个引用：
     - Level Definitions: 拖入步骤1创建的 ScriptableObject 数组
     - Level Button Prefab: 拖入步骤3创建的预制体
     - Grid Content: 拖入 GridContent 对象
     - Selection Panel: LevelSelectionPanel 自身
     - Back Button: 返回按钮
     - Panel Title Text: 标题文本
     - Game Scene Name: "TestScene"

=== 步骤 5: 配置 MainMenuManager ===

  1. 选中 MenuManager 对象
  2. 将 LevelSelectionManager 拖入 Level Selection Manager 插槽
  3. 将主菜单面板拖入 Main Menu Panel
  4. 将 LevelSelectionPanel 拖入 Level Selection Panel
  5. 将 Continue 按钮拖入 Continue Button

=== 步骤 6: 配置 GameManager（TestScene 中） ===

  GameManager 已自动从 LevelTransitionData 读取关卡索引
  无需额外配置，确保 TotalLevels 与定义的关卡数量一致即可

=== 步骤 7: 配置 LevelStageManager（TestScene 中） ===

  LevelStageManager 新增了配置项：
  - Auto Notify Game Manager: ✅（默认开启）
  开启后，所有 7 个阶段完成时自动调用 GameManager.CompleteLevel()

四、测试方法
----------------------------------------
  1. 运行游戏
  2. 点击"选关"按钮
  3. 观察网格：第一关应亮起（未锁定），后续关卡应灰色+锁图标
  4. 点击解锁的关卡 → 加载游戏场景
  5. 完成所有阶段 → 自动保存进度 → 下一关解锁 → 或返回主菜单
  6. 返回主菜单 → "继续游戏"按钮可见
  7. 再次打开"选关" → 第一关显示✅完成，第二关变为可点击

五、场景过渡动画（SceneLoader — 新增）
----------------------------------------
  
  系统会自动工作，无需任何手动配置！
  
  SceneLoader 是一个"DontDestroyOnLoad"单例，首次调用时：
  1. 自动创建 Canvas + 黑色遮罩 Image + "加载中..." 文字
  2. 淡入黑屏（0.3秒）→ 异步加载场景 → 淡出还原（0.3秒）
  3. 所有场景切换自动附带此过渡效果

  可在 Inspector 中调整的参数（选中场景中的 [SceneLoader] 对象）：
  - Fade In Duration: 渐入黑屏速度
  - Fade Out Duration: 渐出还原速度  
  - Min Load Screen Time: 最少黑屏时间（防止太快闪一下）
  - Show Loading Text: 是否显示"加载中..."文字
  
  调试方法：
  在 [SceneLoader] 组件右键 → "测试淡入淡出" 可不加载场景只测试动画

六、调试手段
----------------------------------------
  在 LevelSelectionManager 的 Inspector 中有两个调试按钮：
  - "重置所有存档" → 清空 PlayerPrefs 级别的存档
  - "解锁所有关卡" → 调试全部解锁效果
  
  在 SceneLoader 组件上右键 → "测试淡入淡出"

七、常见问题
----------------------------------------
  Q: 锁图标不显示？
  A: 检查 LockIcon 引用和初始 active 状态

  Q: 点击已锁定关卡仍能进入？
  A: 检查 LevelButton.button.interactable 是否正确设为 false

  Q: 过关后没解锁下一关？
  A: 检查 LevelStageManager.AutoNotifyGameManager 是否勾选
     检查 GameManager.TotalLevels 是否大于当前关卡索引
