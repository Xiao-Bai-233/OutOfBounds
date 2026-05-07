using UnityEngine;
using System.IO;

namespace OutOfBounds.Core
{
    /// <summary>
    /// JSON 存档系统
    /// 将游戏进度持久化到 Application.persistentDataPath/save.json
    ///
    /// 设计原则：
    /// - 纯静态类，无需挂载到任何 GameObject
    /// - 所有读写操作自动处理文件不存在的情况
    /// - 线程安全（Unity 主线程使用）
    /// - 每次写入立即 Flush 防丢失
    /// </summary>
    public static class SaveSystem
    {
        private const string FILE_NAME = "save.json";
        private const string BACKUP_FILE_NAME = "save.json.bak";

        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, FILE_NAME);

        private static string BackupPath =>
            Path.Combine(Application.persistentDataPath, BACKUP_FILE_NAME);

        // ─── 内存缓存 ───────────────────────────────────────────

        private static SaveData _cache;
        private static bool _loaded;

        // ─── 公共 API ───────────────────────────────────────────

        /// <summary>
        /// 获取存档数据（自动从缓存或文件加载）
        /// </summary>
        public static SaveData GetSaveData()
        {
            if (!_loaded)
            {
                _cache = LoadFromDisk();
                _loaded = true;
            }
            return _cache;
        }

        /// <summary>
        /// 立即将当前数据写入磁盘
        /// </summary>
        public static void Flush()
        {
            if (_cache != null)
            {
                SaveToDisk(_cache);
            }
        }

        /// <summary>
        /// 指定关卡是否已解锁
        /// </summary>
        public static bool IsLevelUnlocked(int levelIndex)
        {
            return GetSaveData().IsLevelUnlocked(levelIndex);
        }

        /// <summary>
        /// 指定关卡是否已完成
        /// </summary>
        public static bool IsLevelCompleted(int levelIndex)
        {
            return GetSaveData().IsLevelCompleted(levelIndex);
        }

        /// <summary>
        /// 标记关卡为已完成，并解锁下一关
        /// 同时更新 lastPlayedLevel
        /// </summary>
        public static void CompleteLevel(int levelIndex)
        {
            var data = GetSaveData();
            data.CompleteLevel(levelIndex);
            Flush();

            Debug.Log($"[SaveSystem] 关卡 {levelIndex} 已完成，" +
                      $"最高解锁关: {data.highestUnlockedLevel}");
        }

        /// <summary>
        /// 重置所有存档数据
        /// </summary>
        public static void ResetAllData()
        {
            _cache = SaveData.CreateDefault();
            Flush();
            Debug.Log("[SaveSystem] 所有存档数据已重置");
        }

        /// <summary>
        /// 强制重新从磁盘加载（丢弃缓存修改）
        /// </summary>
        public static void ReloadFromDisk()
        {
            _cache = LoadFromDisk();
            _loaded = true;
        }

        /// <summary>
        /// 存档文件是否存在
        /// </summary>
        public static bool SaveFileExists()
        {
            return File.Exists(SavePath);
        }

        // ─── 磁盘 I/O ───────────────────────────────────────────

        private static SaveData LoadFromDisk()
        {
            // 优先读取主文件
            if (File.Exists(SavePath))
            {
                try
                {
                    string json = File.ReadAllText(SavePath);
                    var data = JsonUtility.FromJson<SaveData>(json);

                    if (data != null)
                    {
                        Debug.Log($"[SaveSystem] 存档加载成功: {SavePath}");
                        return data;
                    }

                    Debug.LogWarning("[SaveSystem] 存档解析失败，尝试加载备份...");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[SaveSystem] 读取存档异常: {e.Message}");
                }
            }

            // 尝试读取备份文件
            if (File.Exists(BackupPath))
            {
                try
                {
                    string json = File.ReadAllText(BackupPath);
                    var data = JsonUtility.FromJson<SaveData>(json);
                    if (data != null)
                    {
                        Debug.LogWarning($"[SaveSystem] 从备份恢复成功: {BackupPath}");

                        // 恢复后写回主文件
                        string mainJson = JsonUtility.ToJson(data, true);
                        File.WriteAllText(SavePath, mainJson);

                        return data;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[SaveSystem] 读取备份异常: {e.Message}");
                }
            }

            // 没有任何存档，创建默认
            Debug.Log("[SaveSystem] 未找到存档，创建默认存档");
            return SaveData.CreateDefault();
        }

        private static void SaveToDisk(SaveData data)
        {
            try
            {
                // 确保目录存在
                string dir = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // 先备份已有的文件
                if (File.Exists(SavePath))
                {
                    File.Copy(SavePath, BackupPath, overwrite: true);
                }

                // 写入新文件（格式化缩进方便调试）
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SavePath, json);

                Debug.Log($"[SaveSystem] 存档写入成功: {SavePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveSystem] 写入存档失败: {e.Message}");
            }
        }
    }
}
