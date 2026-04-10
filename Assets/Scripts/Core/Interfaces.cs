using UnityEngine;

namespace OutOfBounds.Core
{
    /// <summary>
    /// 可交互对象接口
    /// 用于玩家与物体交互
    /// </summary>
    public interface IInteractable
    {
        bool CanInteract { get; }
        void OnInteractStart();
        void OnInteractEnd();
    }

    /// <summary>
    /// 物理对象接口
    /// 统一物理相关操作
    /// </summary>
    public interface IPhysicsObject
    {
        Vector2 Velocity { get; }
        float Mass { get; }
        bool IsKinematic { get; set; }
        void ApplyForce(Vector2 force);
    }

    /// <summary>
    /// 对象池接口
    /// 用于对象池管理
    /// </summary>
    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
    }

    /// <summary>
    /// 存档接口
    /// 支持数据持久化
    /// </summary>
    public interface ISaveable
    {
        string SaveKey { get; }
        object CaptureState();
        void RestoreState(object state);
    }

    /// <summary>
    /// 状态接口
    /// 用于状态机系统
    /// </summary>
    public interface IState<T>
    {
        void Enter(T owner);
        void Update(T owner);
        void Exit(T owner);
    }
}
