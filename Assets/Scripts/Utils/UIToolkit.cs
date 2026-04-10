using UnityEngine;

namespace OutOfBounds.Utils
{
    using Camera = UnityEngine.Camera;

    /// <summary>
    /// UI工具类
    /// 提供常用的UI相关功能
    /// </summary>
    public static class UIToolkit
    {
        /// <summary>
        /// 屏幕坐标转换为UI本地坐标
        /// </summary>
        public static Vector2 ScreenToUILocal(Vector2 screenPosition, RectTransform parent, Canvas canvas)
        {
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parent, 
                screenPosition, 
                canvas.worldCamera ?? Camera.main, 
                out localPos
            );
            return localPos;
        }

        /// <summary>
        /// 世界坐标转换为屏幕坐标
        /// </summary>
        public static Vector2 WorldToScreen(Vector3 worldPosition, Camera camera = null)
        {
            if (camera == null) camera = Camera.main;
            return camera.WorldToScreenPoint(worldPosition);
        }

        /// <summary>
        /// 设置 RectTransform 的锚定位置，保持 z 值
        /// </summary>
        public static void SetAnchoredPosition(RectTransform rect, Vector2 position)
        {
            rect.anchoredPosition = position;
        }

        /// <summary>
        /// 检查点是否在 RectTransform 内
        /// </summary>
        public static bool ContainsPoint(RectTransform rect, Vector2 screenPoint, Camera camera = null)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, camera);
        }

        /// <summary>
        /// 获取 RectTransform 的屏幕空间矩形
        /// </summary>
        public static Rect GetScreenRect(RectTransform rectTransform, Camera camera = null)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 max = min;

            for (int i = 1; i < 4; i++)
            {
                Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);
                min = Vector2.Min(min, screenPos);
                max = Vector2.Max(max, screenPos);
            }

            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        /// <summary>
        /// 计算从 from 到 to 的方向（限制在2D平面）
        /// </summary>
        public static Vector2 GetDirection2D(Vector2 from, Vector2 to)
        {
            return (to - from).normalized;
        }

        /// <summary>
        /// 创建简单的颜色渐变
        /// </summary>
        public static Color LerpColor(Color a, Color b, float t)
        {
            return Color.Lerp(a, b, t);
        }

        /// <summary>
        /// 创建带 Alpha 的颜色
        /// </summary>
        public static Color WithAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        /// <summary>
        /// 检查 Canvas 是否使用 World Space 模式
        /// </summary>
        public static bool IsWorldSpace(this Canvas canvas)
        {
            return canvas.renderMode == RenderMode.WorldSpace;
        }

        /// <summary>
        /// 获取 Canvas 的缩放因子
        /// </summary>
        public static Vector2 GetCanvasScale(Canvas canvas)
        {
            if (canvas == null) return Vector2.one;
            
            if (canvas.IsWorldSpace())
            {
                return canvas.transform.lossyScale;
            }
            
            return Vector2.one * canvas.scaleFactor;
        }
    }
}
