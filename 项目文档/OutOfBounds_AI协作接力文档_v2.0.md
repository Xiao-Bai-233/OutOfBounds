# OutOfBounds AI 协作接力文档 v2.0 ✨

## 1. 项目核心架构更新

在 v1.0 的基础上，我们对底层物理、交互及性能进行了深度补强，实现了从“UI 物理化展示”到“完整可玩原型”的跨越。

### 📁 核心文件结构

```text
Assets/Scripts/
├── Core/
│   ├── Interfaces.cs          # 核心接口 (IPhysicsObject, IInteractable, IPoolable)
│   └── GameEvents.cs          # 全局事件总线系统
├── Physics/
│   ├── GameManager.cs         # 游戏流程控制
│   └── GlobalPhysicsSettings.cs # 全局物理参数配置
├── Player/
│   └── PlayerController.cs    # 增强版玩家控制器 (支持斜坡、BoxCast检测、动画同步)
├── Puzzle/
│   ├── NPCDialogController.cs # NPC对话触发与气泡生成 (集成对象池)
│   ├── PressureButton.cs      # 压力感应机关 (支持质量检测)
│   ├── ExitGate.cs            # 关卡出口大门逻辑 (动画与碰撞同步)
│   ├── CheckpointManager.cs   # 存档点管理器 (单例持久化)
│   └── CheckpointTrigger.cs   # 存档点触发器 (相机与存档同步)
├── UI/
│   ├── DialogBubbleElement.cs # 物理对话气泡 (动态尺寸、悬挂旋转平台)
│   ├── HealthBarManager.cs    # 增强版血条 (支持Sprite自定义、对象池、Layer设置)
│   ├── UIPhysicsManager.cs    # UI物理管理器 (核心位置修正、对象池管理)
│   └── MainMenuManager.cs     # 主菜单逻辑 (支持场景拖拽加载)
└── Utils/
    ├── MathToolkit.cs         # 数学与物理计算工具
    └── UIToolkit.cs           # UI坐标转换工具
```

---

## 2. 接力新增功能详解

### 💬 NPC 对话与实体气泡系统
- **物理平台化**：NPC 对话框不再是纯视觉元素，而是具有质量的 `UIPhysicsElement`。玩家可以踩踏气泡，或通过连续点击生成多个气泡构建临时路径。
- **动态尺寸适配**：气泡尺寸根据文本量自动伸缩，并实时同步 `BoxCollider2D`，支持 World Space 适配。
- **悬挂旋转逻辑**：模拟真实物理感，拖拽气泡边缘时会产生自然的下垂旋转效果。
- **性能优化**：集成对象池，点击 NPC 产生的气泡在销毁后会回收利用，零 GC 压力。

### 🏗️ 解谜机关链路
- **压力按钮 (Pressure Button)**：
  - 支持检测玩家及所有 `IPhysicsObject`（如气泡、心形）的质量。
  - 支持 `Required Mass` 设定，可设计需要多个物体才能压下的解谜。
- **出口大门 (Exit Gate)**：
  - 响应按钮信号，自动处理 Open/Close 动画与碰撞体的开关。
  - 内置终点触发器，玩家穿过即可触发关卡完成。

### 🎮 玩家控制与物理稳定性
- **斜坡移动优化**：引入动态物理材质切换。静止时高摩擦防止滑坡，移动时零摩擦保证丝滑。
- **着地判定重构**：采用 `BoxCast` 与 `OverlapCircle` 复合检测，完美识别倾斜的气泡表面，解决动画频繁切换（抽风）的问题。
- **碰撞消能 (Depenetration)**：解决了物体碰撞时的高频抖动及“卡飞”Bug，使堆叠物体更加稳固。

### 💾 存档与传送机制
- **全局存档点**：实现 `CheckpointManager` 记录玩家最后位置，支持跨场景持久化。
- **惩罚逻辑重算**：将毒水池从简单的“扣血”改为“传送到上一个存档点”，极大地提升了平台跳跃玩法的重试体验。

### 🎥 摄像机跟随系统重构
- **柔性跟随**：基于 `SmoothDamp` 的全轴平滑移动，彻底消除死区带来的生硬感。
- **动态前瞻 (Look-ahead)**：相机根据玩家移动方向自动偏移视角，开阔前方视野。
- **垂直偏置**：根据玩家上升/下落状态智能调整视角重心。

---

## 3. 开发者配置与使用指南

### 瓦片地图 (Tilemap) 规范
- **地面层**：设置 Layer 为 `Ground`，添加 `Tilemap Collider 2D` 并勾选 `Used By Composite`。
- **陷阱层**：地刺挂载 `SpikeTrap.cs`，毒池挂载 `PoisonPit.cs`，Collider 必须设为 `Is Trigger`。

### 动画状态机 (Player)
- 必须包含以下参数：
  - `Speed` (Float): 水平移动速度。
  - `IsGrounded` (Bool): 是否着地。
  - `VerticalVelocity` (Float): 垂直速度，用于区分跳跃/下落动画。
  - `Hurt` (Trigger): 受伤瞬时触发。

### 音效集成
- **PlayerController**：直接在 Inspector 中分配 `Jump`, `Land`, `Hurt`, `Teleport` 音效。
- **DraggableUI**：分配 `PickUp` 和 `Drop` 音效。

---

## 4. 后续扩展方向建议
1. **多气泡共存解谜**：扩展 `NPCDialogController` 支持同时存在多个物理气泡，设计更复杂的空中路径。
2. **气泡抓取点增强**：目前悬挂效果基于抓取偏移，未来可加入受力点的视觉表现（如绳索或手抓位）。
3. **视觉反馈增强**：为存档点、传送及按钮触发添加更丰富的粒子特效。

---
**接力说明**：本版本文档已将所有新增脚本、逻辑优化及配置规范整合，开发者可直接基于此架构进行关卡设计与内容创作。🎮
