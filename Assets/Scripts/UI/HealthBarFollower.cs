using UnityEngine;

namespace OutOfBounds.UI
{
    /// <summary>
    /// 血条跟随脚本
    /// 使血条跟随角色移动
    /// </summary>
    public class HealthBarFollower : MonoBehaviour
    {
        [Header("跟随设置")]
        [SerializeField] public Transform followTarget; // 要跟随的目标（角色）
        [SerializeField] private Vector3 followOffset = new Vector3(0, 2f, 0); // 相对于跟随目标的偏移

        private void Update()
        {
            if (followTarget != null)
            {
                // 跟随目标位置
                transform.position = followTarget.position + followOffset;
            }
        }
    }
}
