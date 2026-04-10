using System;
using OutOfBounds.UI;

namespace OutOfBounds.Core
{
    /// <summary>
    /// 类型安全的事件系统
    /// 替代直接 Action 委托，自动处理清理
    /// </summary>
    public class GameEvent
    {
        private event Action _event;

        public void Subscribe(Action handler)
        {
            _event += handler;
        }

        public void Unsubscribe(Action handler)
        {
            _event -= handler;
        }

        public void Invoke()
        {
            _event?.Invoke();
        }

        public void Clear()
        {
            _event = null;
        }
    }

    /// <summary>
    /// 带参数的事件
    /// </summary>
    public class GameEvent<T>
    {
        private event Action<T> _event;

        public void Subscribe(Action<T> handler)
        {
            _event += handler;
        }

        public void Unsubscribe(Action<T> handler)
        {
            _event -= handler;
        }

        public void Invoke(T param)
        {
            _event?.Invoke(param);
        }

        public void Clear()
        {
            _event = null;
        }
    }

    /// <summary>
    /// 全局事件定义
    /// 集中管理所有游戏事件
    /// </summary>
    public static class Events
    {
        // 游戏状态
        public static readonly GameEvent OnGameStart = new();
        public static readonly GameEvent OnGamePause = new();
        public static readonly GameEvent OnGameResume = new();
        public static readonly GameEvent OnGameOver = new();
        public static readonly GameEvent OnLevelComplete = new();

        // 物理事件
        public static readonly GameEvent<float> OnGravityChanged = new();

        // 玩家事件
        public static readonly GameEvent<int> OnPlayerHealthChanged = new();
        public static readonly GameEvent OnPlayerDamaged = new();
        public static readonly GameEvent OnPlayerDead = new();
        public static readonly GameEvent OnPlayerRespawn = new();

        // UI物理事件
        public static readonly GameEvent<UIPhysicsElement> OnUIElementGrabbed = new();
        public static readonly GameEvent<UIPhysicsElement> OnUIElementReleased = new();
        public static readonly GameEvent<UIPhysicsElement> OnUIElementBounced = new();

        /// <summary>
        /// 清理所有事件订阅
        /// 场景切换时调用
        /// </summary>
        public static void ClearAll()
        {
            OnGameStart.Clear();
            OnGamePause.Clear();
            OnGameResume.Clear();
            OnGameOver.Clear();
            OnLevelComplete.Clear();
            OnGravityChanged.Clear();
            OnPlayerHealthChanged.Clear();
            OnPlayerDamaged.Clear();
            OnPlayerDead.Clear();
            OnPlayerRespawn.Clear();
            OnUIElementGrabbed.Clear();
            OnUIElementReleased.Clear();
            OnUIElementBounced.Clear();
        }
    }
}
