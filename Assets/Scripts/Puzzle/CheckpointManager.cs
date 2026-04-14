using UnityEngine;
using OutOfBounds.Camera;

namespace OutOfBounds.Puzzle
{
    /// <summary>
    /// 全局检查点管理器
    /// 记录玩家最后触发的存档点位置
    /// </summary>
    public class CheckpointManager : MonoBehaviour
    {
        public static CheckpointManager Instance { get; private set; }

        [Header("状态")]
        [SerializeField] private Vector3 lastCheckpointPosition;
        [SerializeField] private bool hasCheckpointSet = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 设置当前检查点位置
        /// </summary>
        public void SetCheckpoint(Vector3 position)
        {
            lastCheckpointPosition = position;
            hasCheckpointSet = true;
            Debug.Log($"[CheckpointManager] 存档点已更新: {position}");
        }

        /// <summary>
        /// 获取最后一次存档的位置
        /// </summary>
        public Vector3 GetLastCheckpointPosition()
        {
            if (!hasCheckpointSet)
            {
                // 如果没有设置检查点，尝试找场景中的初始点或返回当前位置
                return Vector3.zero; 
            }
            return lastCheckpointPosition;
        }

        public bool HasCheckpointSet => hasCheckpointSet;
    }
}
