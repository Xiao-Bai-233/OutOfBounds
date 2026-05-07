using UnityEngine;

namespace OutOfBounds.Core
{
    /// <summary>
    /// 关卡定义（ScriptableObject）
    /// 在编辑器中配置每个关卡的基本信息
    ///
    /// 极简设计：无需任何 Sprite/Texture 外部资源
    /// </summary>
    [CreateAssetMenu(fileName = "Level_", menuName = "OutOfBounds/关卡定义")]
    public class LevelDefinition : ScriptableObject
    {
        [Header("基本信息")]
        [Tooltip("关卡索引（0 = 第一关）")]
        public int levelIndex;

        [Tooltip("关卡名称（显示在按钮上）")]
        public string levelName = "新关卡";

        [Header("关卡配置")]
        [Tooltip("游戏场景中的关卡标识（GameManager 通过此值初始化）")]
        public int sceneLevelId = 0;

        [Tooltip("该关卡是否是教程关")]
        public bool isTutorial = false;

        [Header("网格位置")]
        [Tooltip("网格中的行索引（0 开始）")]
        public int gridRow = 0;

        [Tooltip("网格中的列索引（0 开始）")]
        public int gridColumn = 0;
    }
}
