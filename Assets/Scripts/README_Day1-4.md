# 《界外操控》Day 1-4 程序员开发指南

> Unity 版本：6000.0.60f1
> 渲染：URP

---

## 📁 已创建的文件结构

```
Assets/
├── Scripts/
│   ├── Player/
│   │   └── PlayerController.cs      ← 角色控制器
│   ├── Physics/
│   │   ├── GlobalPhysicsSettings.cs ← 全局物理设置（重力控制）
│   │   ├── GameManager.cs           ← 游戏主管理器
│   │   └── TestSceneSetup.cs       ← 测试场景助手
│   ├── UI/
│   │   ├── UIPhysicsElement.cs     ← UI物理元素基类
│   │   ├── UIPhysicsManager.cs     ← UI物理系统管理器
│   │   └── GravitySlider.cs        ← 重力滑块控制
│   └── DragSystem/
│       └── DraggableUI.cs          ← 可拖拽UI组件
└── Prefabs/                        ← 预设体目录
```

---

## 🚀 快速开始

### 方式一：使用 TestSceneSetup（推荐）

1. 在 Unity 中创建一个空 GameObject
2. 添加 `TestSceneSetup` 组件
3. 在 Inspector 中配置选项
4. 右键点击组件 → "设置测试场景"

✅ 会自动创建：
- 玩家角色（带移动/跳跃）
- 地面平台
- UI物理测试元素
- 必要的管理器

### 方式二：手动创建

1. 创建 `GameObject` → 添加 `PlayerController`
2. 创建地面，添加 `BoxCollider2D`
3. 创建 Canvas（Screen Space Overlay）
4. 在 Canvas 下创建 UI，添加 `UIPhysicsElement` + `DraggableUI`

---

## 🎮 角色控制器使用

### PlayerController.cs

**已实现功能：**
- ✅ WASD / 方向键移动
- ✅ 空格键跳跃
- ✅ 土狼时间（Coyote Time）
- ✅ 跳跃缓冲（Jump Buffer）
- ✅ 跳跃中断（Jump Cut）
- ✅ 角色翻转

**Inspector 参数：**
| 参数 | 说明 | 默认值 |
|------|------|--------|
| Move Speed | 移动速度 | 8 |
| Jump Force | 跳跃力度 | 12 |
| Coyote Time | 土狼时间(秒) | 0.1 |
| Jump Buffer | 跳跃缓冲(秒) | 0.1 |
| Jump Cut | 跳跃中断减速 | 0.5 |
| Ground Layer | 地面层级 | - |

**事件：**
```csharp
playerController.OnJump += () => Debug.Log("跳跃!");
playerController.OnLand += () => Debug.Log("落地!");
```

---

## 🎯 UI 物理化框架

### 核心概念

UI元素拥有物理属性：
- 重力影响
- 碰撞反弹
- 阻力
- 边界约束

### UIPhysicsElement.cs

**已实现功能：**
- ✅ 重力模拟
- ✅ 边界碰撞
- ✅ 速度应用
- ✅ 阻力衰减
- ✅ 着地检测

### DraggableUI.cs

**已实现功能：**
- ✅ 鼠标拖拽
- ✅ 悬停高亮
- ✅ 拖拽缩放
- ✅ 释放速度
- ✅ 音效反馈

**Inspector 参数：**
| 参数 | 说明 |
|------|------|
| Can Be Dragged | 是否可拖拽 |
| Highlight On Hover | 悬停高亮 |
| Drag Speed | 拖拽速度 |
| Release Velocity Mult | 释放速度倍数 |
| Normal/Hover/Dragging Color | 各状态颜色 |

---

## ⚙️ 重力滑块系统

### GlobalPhysicsSettings.cs

**核心功能：**
- 管理全局重力值
- 供设置菜单的滑块控制
- 影响所有UI物理元素

**方法：**
```csharp
// 设置重力倍增（0-1）
GlobalPhysicsSettings.Instance.SetGravityMultiplier(0.5f);

// 直接设置重力值
GlobalPhysicsSettings.Instance.SetGravity(-20f);

// 重置为默认
GlobalPhysicsSettings.Instance.ResetToDefaultGravity();
```

### GravitySlider.cs

配合 UI 的 Slider 使用：
1. 创建 UI → Slider
2. 添加 `GravitySlider` 组件
3. 连接 Text 显示当前值

---

## 🎮 游戏管理器

### GameManager.cs

**已实现功能：**
- ✅ 暂停/恢复
- ✅ 关卡切换
- ✅ 游戏结束
- ✅ 单例模式

**方法：**
```csharp
GameManager.Instance.PauseGame();
GameManager.Instance.ResumeGame();
GameManager.Instance.LoadLevel(0);
GameManager.Instance.RestartLevel();
```

---

## 📝 Day 5 提交检查清单

在提交技术 Demo 给美术前，确保：

- [ ] 玩家可以正常移动和跳跃
- [ ] 角色动画（站立/跑步/跳跃）
- [ ] 至少 3 个可拖拽的UI元素
- [ ] UI元素受重力影响
- [ ] UI元素可以相互碰撞
- [ ] UI元素在边界内反弹
- [ ] 拖拽有视觉反馈
- [ ] 至少一个可用的测试关卡

---

## ❓ 常见问题

**Q: 玩家穿透了地面**
A: 检查 Ground 的 Layer 是否添加到 Player 的 Ground Layer 中

**Q: UI 元素移出屏幕**
A: 检查 UIPhysicsElement 的 "Constrain To Parent" 是否勾选

**Q: 拖拽不工作**
A: 确保 Canvas 有 GraphicRaycaster 组件

**Q: 物理表现奇怪**
A: 检查 GlobalPhysicsSettings 是否存在且唯一

---

## 🔄 Git 提交建议

```bash
# Day 1-2 完成后
git add .
git commit -m "Day 1-2: 完成基础角色控制器和物理系统"

# Day 3-4 完成后
git add .
git commit -m "Day 3-4: 完成UI物理化框架和拖拽系统"
```

---

*有问题随时问我！🌙*
