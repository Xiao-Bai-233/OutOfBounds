using System;
using System.Collections.Generic;

namespace OutOfBounds.Core
{
    /// <summary>
    /// 存档数据结构
    /// 使用 JSON 序列化保存到 Application.persistentDataPath
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int saveVersion = 1;

        /// <summary>
        /// 已解锁的最高关卡索引（0 = 第一关始终解锁）
        /// </summary>
        public int highestUnlockedLevel = 0;

        /// <summary>
        /// 已完成通过的关卡索引列表
        /// </summary>
        public List<int> completedLevels = new List<int>();

        /// <summary>
        /// 最后一次玩的关卡索引（用于继续游戏）
        /// </summary>
        public int lastPlayedLevel = 0;

        /// <summary>
        /// 关卡星级评分（可选扩展）
        /// </summary>
        public Dictionary<int, int> levelStars = new Dictionary<int, int>();

        /// <summary>
        /// 创建新的空白存档
        /// </summary>
        public static SaveData CreateDefault()
        {
            return new SaveData
            {
                saveVersion = 1,
                highestUnlockedLevel = 0,       // 第一关默认解锁
                completedLevels = new List<int>(),
                lastPlayedLevel = 0,
                levelStars = new Dictionary<int, int>()
            };
        }

        /// <summary>
        /// 指定关卡是否已解锁
        /// </summary>
        public bool IsLevelUnlocked(int levelIndex)
        {
            return levelIndex <= highestUnlockedLevel;
        }

        /// <summary>
        /// 指定关卡是否已完成
        /// </summary>
        public bool IsLevelCompleted(int levelIndex)
        {
            return completedLevels.Contains(levelIndex);
        }

        /// <summary>
        /// 通关当前关卡，解锁下一关
        /// </summary>
        public void CompleteLevel(int levelIndex)
        {
            if (!completedLevels.Contains(levelIndex))
            {
                completedLevels.Add(levelIndex);
            }

            // 解锁下一关
            int nextLevel = levelIndex + 1;
            if (nextLevel > highestUnlockedLevel)
            {
                highestUnlockedLevel = nextLevel;
            }

            lastPlayedLevel = levelIndex;
        }
    }
}
