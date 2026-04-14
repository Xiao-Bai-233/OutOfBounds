using UnityEngine;
using UnityEngine.SceneManagement;

namespace OutOfBounds.UI
{
    /// <summary>
    /// 主菜单管理器
    /// 处理开始游戏和退出游戏逻辑
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [Header("设置")]
        [SerializeField] private Object gameSceneAsset; // 使用 Object 类型支持拖拽
        [SerializeField] private string gameSceneName = "TestScene"; // 备用手动输入
        
        /// <summary>
        /// 开始游戏：跳转到游戏场景
        /// </summary>
        public void StartGame()
        {
            string sceneToLoad = gameSceneName;
            
            #if UNITY_EDITOR
            if (gameSceneAsset != null)
            {
                sceneToLoad = gameSceneAsset.name;
            }
            #endif

            Debug.Log("[MainMenu] 正在进入游戏场景: " + sceneToLoad);
            SceneManager.LoadScene(sceneToLoad);
        }

        /// <summary>
        /// 退出游戏
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[MainMenu] 正在退出游戏...");
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }
}
