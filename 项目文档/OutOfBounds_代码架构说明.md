# 《界外操控》代码架构说明

> 详细说明核心类设计和交互关系

---

## 🏗️ 核心类架构

```
┌─────────────────────────────────────────────────────────────┐
│                      UIPhysicsElement                       │
│                    (UI物理元素基类)                          │
├─────────────────────────────────────────────────────────────┤
│  属性：                                                       │
│  - mass, drag, angularDrag    // 物理属性                    │
│  - velocity, angularVelocity  // 速度状态                    │
│  - isFixed, isKinematic       // 状态标记                    │
│  - useGravity, gravityScale   // 重力设置（当前禁用）         │
├─────────────────────────────────────────────────────────────┤
│  方法：                                                       │
│  + ApplyGravity()             // 真空环境（空实现）           │
│  + ApplyDrag()                // 应用阻力减速                 │
│  + CheckSceneCollisionsBoxCast()  // BoxCast碰撞检测         │
│  + ToggleFixed()              // 切换固定模式                 │
│  + StartDrag()/Drag()/EndDrag()   // 拖拽接口                │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ 继承/使用
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      DraggableUI                            │
│                    (拖拽交互组件)                            │
├─────────────────────────────────────────────────────────────┤
│  实现接口：                                                   │
│  - IBeginDragHandler                                          │
│  - IDragHandler                                               │
│  - IEndDragHandler                                            │
│  - IPointerClickHandler   // 右键固定                        │
├─────────────────────────────────────────────────────────────┤
│  功能：                                                       │
│  - 左键拖拽计算速度                                           │
│  - 右键切换固定模式                                           │
│  - 坐标转换（World/Screen Space）                            │
└─────────────────────────────────────────────────────────────┘
```

---

## 📋 UIPhysicsElement 详细说明

### 物理模拟流程

```
FixedUpdate (每固定时间步长)
    │
    ├── ApplyGravity()      // 真空环境，不执行
    │
    └── ApplyDrag()         // 应用阻力减速
            velocity *= (1 - drag * deltaTime)

Update (每帧)
    │
    ├── ApplyRotation()     // 应用旋转
    │
    └── 子步进循环 (4次)
            │
            ├── CheckSceneCollisionsBoxCast()  // 碰撞检测
            │       使用 Physics2D.BoxCastAll()
            │       检测与场景物体的碰撞
            │
            ├── ApplyVelocitySubStep()         // 移动一小步
            │
            └── ApplyBoundaryConstraints()     // 边界约束
```

### 关键方法详解

#### 1. 碰撞检测 CheckSceneCollisionsBoxCast()
```csharp
// 使用 BoxCast 进行精确碰撞检测
RaycastHit2D[] hits = Physics2D.BoxCastAll(
    worldPos,           // 起始位置（Collider中心）
    size * 0.95f,       // 检测盒子大小
    rotation,           // 旋转角度
    moveDirection,      // 运动方向
    checkDistance,      // 检测距离
    collisionLayers     // 碰撞层
);
```

**为什么用 BoxCast 而不是简单的Collider？**
- 可以提前预测碰撞，防止高速穿模
- 子步进检测确保每步都检查
- 能获取碰撞法线用于反弹计算

#### 2. 碰撞处理 HandleCollisionPhysics()
```csharp
// 1. 速度反射（反弹）
velocity = Vector2.Reflect(velocity, hit.normal) * bounciness;

// 2. 添加旋转（基于切向速度）
angularVelocity += tangentVelocity * 2f / mass;

// 3. 应用摩擦力
velocity *= (1f - friction * 0.2f);

// 4. 防止穿透
rectTransform.position += hit.normal * penetration;
```

#### 3. 固定模式 ToggleFixed()
```csharp
public virtual void ToggleFixed()
{
    isFixed = !isFixed;
    
    if (isFixed)
    {
        // 停止所有运动
        velocity = Vector2.zero;
        angularVelocity = 0f;
        // 变灰表示固定
        SetColor(fixedColor);
    }
    else
    {
        // 恢复正常颜色
        SetColor(originalColor);
    }
}
```

---

## 📋 DraggableUI 详细说明

### 拖拽流程

```
OnBeginDrag (开始拖拽)
    │
    ├── 记录起始位置
    ├── physicsElement.StartDrag()  // 通知物理组件
    └── velocitySamples.Clear()     // 清空速度样本

OnDrag (拖拽中)
    │
    ├── 计算新位置（World Space坐标转换）
    ├── physicsElement.Drag(newPosition)
    └── 记录位置样本（用于计算释放速度）

OnEndDrag (结束拖拽)
    │
    ├── 计算释放速度（基于最后几帧位移）
    ├── physicsElement.EndDrag(releaseVelocity)
    └── 应用速度到物理系统
```

### 速度计算逻辑
```csharp
// 收集最近5帧的位置样本
velocitySamples.Enqueue(new VelocitySample 
{ 
    position = currentPosition, 
    time = Time.time 
});

// 释放时计算平均速度
if (velocitySamples.Count >= 2)
{
    var first = velocitySamples.Peek();
    var last = velocitySamples.Last();
    
    float deltaTime = last.time - first.time;
    Vector2 deltaPos = last.position - first.position;
    
    releaseVelocity = deltaPos / deltaTime * releaseVelocityScale;
}
```

### 坐标转换处理
```csharp
// World Space 模式下直接使用世界坐标
if (canvas.renderMode == RenderMode.WorldSpace)
{
    rectTransform.position = position;
}
// Screen Space 模式下使用 anchoredPosition
else
{
    rectTransform.anchoredPosition = position;
}
```

---

## 🔌 扩展接口设计

### 如何创建新的UI物理元素类型

```csharp
// 继承 UIPhysicsElement
public class HeartPhysicsElement : UIPhysicsElement
{
    [Header("心形特殊属性")]
    [SerializeField] private float durability = 100f;
    [SerializeField] private ParticleSystem breakEffect;
    
    protected override void HandleCollisionPhysics(RaycastHit2D hit)
    {
        // 调用父类方法
        base.HandleCollisionPhysics(hit);
        
        // 添加心形特有的逻辑
        float impactForce = hit.relativeVelocity.magnitude;
        durability -= impactForce;
        
        if (durability <= 0)
        {
            BreakHeart();
        }
    }
    
    void BreakHeart()
    {
        if (breakEffect != null)
            Instantiate(breakEffect, transform.position, Quaternion.identity);
        
        Destroy(gameObject);
    }
}
```

### 事件系统

```csharp
// UIPhysicsElement 提供的事件
public System.Action<UIPhysicsElement, Collision2D> OnCollisionEnter2D;
public System.Action<UIPhysicsElement, Collision2D> OnCollisionStay2D;
public System.Action<UIPhysicsElement, Collision2D> OnCollisionExit2D;
public System.Action<UIPhysicsElement> OnBecameGrounded;
public System.Action<UIPhysicsElement> OnLeftGround;

// 使用示例
void Start()
{
    var physicsElement = GetComponent<UIPhysicsElement>();
    physicsElement.OnCollisionEnter2D += OnHitSomething;
    physicsElement.OnBecameGrounded += OnLanded;
}

void OnHitSomething(UIPhysicsElement element, Collision2D collision)
{
    Debug.Log($"{element.name} 撞到了 {collision.gameObject.name}");
}
```

---

## 🎯 设计模式应用

### 1. 组件模式 (Component Pattern)
- `UIPhysicsElement` 负责物理模拟
- `DraggableUI` 负责输入处理
- 两者通过组合而非继承关联

### 2. 观察者模式 (Observer Pattern)
- 使用 C# 的 `System.Action` 实现事件系统
- 碰撞、着地等事件可以被外部订阅

### 3. 模板方法模式 (Template Method)
- `ApplyGravity()` 等方法是虚方法
- 子类可以重写以改变行为（如特殊重力效果）

---

## 📊 性能考虑

### 优化点
1. **子步进数量可调** - 当前4步，可根据需要调整
2. **碰撞检测距离限制** - 只在运动时检测
3. **速度阈值优化** - 速度很小时归零，避免持续微小计算

### 潜在性能瓶颈
1. 大量UI元素同时物理模拟
2. 复杂的碰撞场景
3. 建议：使用对象池管理频繁生成/销毁的物体（如心形）

---

## 🔮 未来扩展方向

### 1. 更多物理元素类型
```csharp
public class GravitySliderElement : UIPhysicsElement
{
    // 重力滑块 - 调节全局重力
    public override void OnValueChanged(float value)
    {
        GlobalPhysicsSettings.Instance.SetGravity(value);
    }
}

public class HealthBarElement : UIPhysicsElement
{
    // 血条 - 受伤时掉落心形
    public void TakeDamage(float damage)
    {
        SpawnHeartPieces(damage);
    }
}
```

### 2. 物理效果增强
- 磁力系统（吸引/排斥）
- 风力/气流影响
- 浮力（特定区域）

### 3. 联动物理
- 多个UI元素连接（如链条）
- 弹簧约束
- 铰链关节

---

*本文档描述了当前代码架构，随着项目进展会持续更新。*
