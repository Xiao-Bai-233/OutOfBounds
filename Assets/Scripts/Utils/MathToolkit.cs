using UnityEngine;

namespace OutOfBounds.Utils
{
    /// <summary>
    /// 数学工具类
    /// 提供常用的数学计算函数
    /// </summary>
    public static class MathToolkit
    {
        #region 缓动函数 (Easing Functions)

        public static float EaseInQuad(float t) => t * t;
        public static float EaseOutQuad(float t) => 1 - (1 - t) * (1 - t);
        public static float EaseInOutQuad(float t) => t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;

        public static float EaseInCubic(float t) => t * t * t;
        public static float EaseOutCubic(float t) => 1 - Mathf.Pow(1 - t, 3);
        public static float EaseInOutCubic(float t) => t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;

        public static float EaseOutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1 / d1)
            {
                return n1 * t * t;
            }
            else if (t < 2 / d1)
            {
                return n1 * (t -= 1.5f / d1) * t + 0.75f;
            }
            else if (t < 2.5 / d1)
            {
                return n1 * (t -= 2.25f / d1) * t + 0.9375f;
            }
            else
            {
                return n1 * (t -= 2.625f / d1) * t + 0.984375f;
            }
        }

        /// <summary>
        /// 弹性缓动
        /// </summary>
        public static float EaseOutElastic(float t)
        {
            const float c4 = (2 * Mathf.PI) / 3;
            return t == 0 ? 0 : t == 1 ? 1 : Mathf.Pow(2, -10 * t) * Mathf.Sin((t * 10 - 0.75f) * c4) + 1;
        }

        #endregion

        #region 数值工具

        /// <summary>
        /// 平滑阻尼（类似 Mathf.SmoothDamp 但更简单）
        /// </summary>
        public static float SmoothDamp(float current, float target, ref float velocity, float smoothTime, float maxSpeed = float.PositiveInfinity, float deltaTime = -1f)
        {
            if (deltaTime < 0) deltaTime = Time.deltaTime;
            smoothTime = Mathf.Max(0.0001f, smoothTime);
            float num = 2f / smoothTime;
            float num2 = num * deltaTime;
            float num3 = 1f / (1f + num2 + 0.48f * num2 * num2 + 0.235f * num2 * num2 * num2);
            float num4 = current - target;
            float num5 = target;
            float num6 = maxSpeed * smoothTime;
            num4 = Mathf.Clamp(num4, -num6, num6);
            target = current - num4;
            float num7 = (velocity + num * num4) * deltaTime;
            velocity = (velocity - num * num7) * num3;
            float num8 = target + (num4 + num7) * num3;
            if (num5 - current > 0f == num8 > num5)
            {
                num8 = num5;
                velocity = (num8 - num5) / deltaTime;
            }
            return num8;
        }

        /// <summary>
        /// 将值映射到新的范围
        /// </summary>
        public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return toMin + (value - fromMin) * (toMax - toMin) / (fromMax - fromMin);
        }

        /// <summary>
        /// 角度归一化到 -180 ~ 180
        /// </summary>
        public static float NormalizeAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }

        /// <summary>
        /// 检查值是否在范围内
        /// </summary>
        public static bool InRange(float value, float min, float max)
        {
            return value >= min && value <= max;
        }

        #endregion

        #region 物理计算

        /// <summary>
        /// 预测抛物线落点
        /// </summary>
        public static Vector2 PredictLanding(Vector2 startPos, Vector2 velocity, float gravity, float groundY)
        {
            if (gravity == 0) return startPos;

            float timeToGround = Mathf.Sqrt(2 * (startPos.y - groundY) / Mathf.Abs(gravity));
            return startPos + velocity * timeToGround;
        }

        /// <summary>
        /// 计算反射向量
        /// </summary>
        public static Vector2 Reflect(Vector2 direction, Vector2 normal, float bounciness = 1f)
        {
            return Vector2.Reflect(direction, normal) * bounciness;
        }

        /// <summary>
        /// 计算带摩擦的速度
        /// </summary>
        public static Vector2 ApplyFriction(Vector2 velocity, float friction, float deltaTime)
        {
            float speed = velocity.magnitude;
            if (speed < 0.001f) return Vector2.zero;

            float newSpeed = Mathf.Max(0, speed - friction * deltaTime);
            return velocity.normalized * newSpeed;
        }

        #endregion
    }
}
