# 《界外操控》(OutOfBounds) 项目总览文档

> 创建时间：2026年4月3日  
> 项目类型：2D平台解谜游戏  
> 开发引擎：Unity 6000.0.60f1 (URP渲染)  
> 比赛：莉莉丝高校游戏设计比赛  
> 团队：2人（1程序员 + 1美术/音效）

---

## 📋 项目基本信息

### 核心概念
将传统游戏的UI元素（血条、设置菜单、按钮等）**物理化**，使其成为游戏世界中可交互的实体。玩家需要利用这些UI元素来解决谜题、通过关卡。

### 核心玩法机制
1. **UI物理化框架** - UI元素拥有刚体属性，可以碰撞、拖拽、抛掷
2. **鼠标交互系统** - 左键拖拽UI，右键切换固定/可移动模式
3. **血条搭桥机制** - 角色受伤时生成心形物理实体，可用来搭桥
4. **设置菜单重力滑块** - 滑块数值与全局重力挂钩，调节游戏物理

---

## ✅ 已实现功能

### 1. 基础框架系统

#### 1.1 UI物理元素基类 (UIPhysicsElement.cs)
- [x] 物理属性系统（质量、阻力、弹性、摩擦力）
- [x] 速度/角速度管理
- [x] **真空环境物理** - 无重力，物体静止悬浮，被抛掷后自然减速
- [x] 子步进碰撞检测（4步/帧）防止穿模
- [x] 边界约束系统（Canvas边界碰撞反弹）
- [x] 旋转物理效果

#### 1.2 拖拽系统 (DraggableUI.cs)
- [x] 左键拖拽UI元素
- [x] 拖拽时计算释放速度（基于最后几帧位移）
- [x] **右键切换固定/可移动模式**
- [x] 固定模式下物体变为灰色，不受物理影响
- [x] World Space / Screen Space 坐标转换支持

#### 1.3 碰撞检测系统
- [x] BoxCast碰撞检测（使用BoxCollider2D实际范围）
- [x] 碰撞反弹物理（可调节弹性系数）
- [x] 碰撞旋转效果（基于切向速度）
- [x] Layer Collision Matrix配置（UI_Physics层与Ground层碰撞）

### 2. 渲染设置
- [x] Canvas设置为 **World Space** 模式（实现UI与场景物体碰撞）
- [x] URP渲染管线配置
- [x] 2D灯光系统基础设置

### 3. 项目结构
```
Assets/
├── Scripts/
│   ├── UI/
│   │   └── UIPhysicsElement.cs      # UI物理元素基类
│   ├── DragSystem/
│   │   └── DraggableUI.cs           # 拖拽系统
│   └── Settings/
│       └── GlobalPhysicsSettings.cs # 全局物理设置
├── Prefabs/
├── Scenes/
└── Resources/
```

---

## 🔧 技术实现细节

### 物理参数配置
```csharp
// UIPhysicsElement.cs 默认参数
mass = 1f;                    // 质量
drag = 1.5f;                  // 阻力（控制减速速度）
angularDrag = 1f;             // 角阻力
gravityScale = 0f;            // 重力缩放（真空环境为0）
bounciness = 0.2f;            // 弹性（低弹性避免弹飞）
friction = 0.3f;              // 摩擦力
collisionCheckDistance = 0.05f; // 碰撞检测距离
```

### 关键代码逻辑

#### 真空环境物理
```csharp
protected virtual void ApplyGravity()
{
    // 真空环境：不应用重力
    // 物体只在被抛掷时有速度，然后自然减速停止
    return;
}
```

#### 固定模式切换
```csharp
public virtual void ToggleFixed()
{
    isFixed = !isFixed;
    if (isFixed)
    {
        velocity = Vector2.zero;
        angularVelocity = 0f;
        SetColor(fixedColor);  // 变灰
    }
    else
    {
        SetColor(normalColor); // 恢复正常
    }
}
```

#### 子步进碰撞检测
```csharp
int subSteps = 4;
Vector2 totalVelocity = velocity * Time.deltaTime;
Vector2 stepVelocity = totalVelocity / subSteps;

for (int i = 0; i < subSteps; i++)
{
    CheckSceneCollisionsBoxCast();  // 检测碰撞
    ApplyVelocitySubStep(stepVelocity);  // 移动一小步
    ApplyBoundaryConstraints();     // 边界约束
}
```

---

## ❌ 已知问题与限制

### 已修复的问题
1. ✅ **穿模问题** - 通过子步进碰撞检测解决
2. ✅ **弹飞问题** - 降低弹性系数(bounciness)至0.2，增加阻力
3. ✅ **坐标空间问题** - Canvas改为World Space模式
4. ✅ **编译器错误** - `fixed`关键字冲突已解决
5. ✅ **重力问题** - 已改为真空环境（无重力）

### 当前限制
1. 边界约束只在Canvas范围内有效
2. 碰撞检测依赖BoxCollider2D组件
3. 固定模式切换没有视觉动画效果

---

## 📝 待实现功能清单

### 高优先级（核心玩法）

#### 1. 血条系统 ❤️
- [ ] 角色受伤时生成心形物理实体
- [ ] 心形实体可以被拖拽、抛掷、固定
- [ ] 多个心形可以堆叠形成桥梁
- [ ] 心形有耐久度，受冲击会消失

#### 2. 设置菜单物理化 ⚙️
- [ ] 设置面板可以像普通UI一样被拖拽
- [ ] **重力滑块** - 调节滑块改变全局重力（甚至可以让重力反向）
- [ ] 音量滑块物理化
- [ ] 设置菜单可以被固定作为平台

#### 3. 角色控制器 🎮
- [ ] 基础移动（左右移动、跳跃）
- [ ] 与UI物理元素的交互（站在固定的UI上）
- [ ] 受伤系统（生成心形）
- [ ] 死亡/重生机制

#### 4. 关卡元素 🚧
- [ ] 地面/墙壁（带碰撞器）
- [ ] 陷阱（尖刺、激光等）
- [ ] 目标点（关卡出口）
- [ ] 可收集物品

### 中优先级（游戏体验）

#### 5. 音效系统 🔊
- [ ] UI拖拽音效
- [ ] 碰撞音效
- [ ] 固定/解除固定音效
- [ ] 背景音乐

#### 6. 视觉反馈 ✨
- [ ] 拖拽时UI高亮/放大效果
- [ ] 固定模式切换动画
- [ ] 碰撞时的粒子效果
- [ ] 轨迹拖尾效果

#### 7. 教程系统 📚
- [ ] 第一关：基础拖拽教学
- [ ] 第二关：固定模式教学
- [ ] 第三关：血条搭桥教学
- [ ] 第四关：重力滑块教学

### 低优先级（ polish ）

#### 8. 存档系统 💾
- [ ] 关卡进度保存
- [ ] 设置保存

#### 9. UI界面 🖼️
- [ ] 主菜单
- [ ] 关卡选择
- [ ] 暂停菜单
- [ ] 胜利/失败画面

#### 10. 优化 🔧
- [ ] 对象池（心形实体）
- [ ] 性能优化
- [ ] 内存管理

---

## 📅 18天开发计划回顾

### 第一阶段：技术原型（Day 1-5）
- ✅ Day 1-4: 基础框架和UI物理化系统 **已完成**
- ⏳ Day 5: 技术原型整合与测试

### 第二阶段：教程关卡（Day 6-9）
- ⏳ Day 6-7: 角色控制器 + 基础关卡元素
- ⏳ Day 8-9: 血条系统 + 教程关卡搭建

### 第三阶段：第一关（Day 10-14）
- ⏳ Day 10-11: 设置菜单物理化
- ⏳ Day 12-14: 第一关完整设计与实现

### 第四阶段：打磨与发布（Day 15-18）
- ⏳ Day 15-16: 音效、视觉 polish
- ⏳ Day 17: 测试与Bug修复
- ⏳ Day 18: 打包发布

---

## 🔗 重要文件路径

### 项目路径
- 项目根目录：`D:\UintyGroup\AI-NPC\`
- Unity版本：6000.0.60f1

### 关键脚本
- `Assets\Scripts\UI\UIPhysicsElement.cs`
- `Assets\Scripts\DragSystem\DraggableUI.cs`
- `Assets\Scripts\Settings\GlobalPhysicsSettings.cs`

### 文档位置
- `项目文档\OutOfBounds_18_Day_Plan.docx` - 原始开发计划
- `项目文档\OutOfBounds_项目总览.md` - 本文档

---

## 💡 设计要点与注意事项

### 物理设计
1. **真空环境** - 没有重力，物体不会自然下落
2. **阻力系统** - 物体运动后会在几秒内自然停止
3. **弹性控制** - 保持低弹性(0.2)避免物体弹飞

### 交互设计
1. **左键拖拽** - 移动UI元素
2. **右键点击** - 切换固定/可移动模式
3. **固定后的UI** - 变为灰色，可以作为平台使用

### 技术注意事项
1. Canvas必须使用 **World Space** 模式才能与场景物体碰撞
2. UI元素需要添加 **BoxCollider2D** 组件
3. 需要配置 **Layer Collision Matrix**（UI_Physics层与Ground层）
4. 使用子步进碰撞检测防止高速穿模

---

## 🐛 调试技巧

### 常见问题排查
1. **物体不碰撞** - 检查Layer设置和Collision Matrix
2. **拖拽不工作** - 确认Canvas是World Space模式
3. **物体弹飞** - 降低bounciness，增加drag
4. **穿模** - 检查BoxCollider2D是否正确设置

### 调试工具
- 开启Gizmos查看碰撞范围
- 使用Debug.Log输出速度/状态信息
- 在Scene视图中观察射线检测

---

## 📚 参考资源

### Unity相关
- Physics2D.BoxCast 文档
- Canvas Render Mode 说明
- EventTrigger 使用指南

### 游戏设计参考
- 《Baba Is You》 - 规则操控
- 《Patrick's Parabox》 - 递归解谜
- 《Moncage》 - 视角解谜

---

## 📝 更新日志

### 2026-04-03
- ✅ 完成基础UI物理化框架
- ✅ 实现拖拽系统
- ✅ 实现固定/可移动模式切换
- ✅ 配置真空环境物理（无重力）
- ✅ 修复穿模、弹飞等问题
- ✅ 创建项目总览文档

---

*本文档将持续更新，记录项目进度和重要决策。*
