using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 常驻场景引导。挂在 PersistentRoot 上，Start 时以附加模式加载主菜单。
/// PersistentScene 保持常驻（不卸载），因此无需 DontDestroyOnLoad，可避免 Unity 内部断言错误。
/// 仅在 PersistentScene 中运行；若从其他场景启动则不引导。
/// </summary>
[DefaultExecutionOrder(-1000)]
public class PersistentBootstrap : MonoBehaviour
{
    [Tooltip("常驻场景名称（用于判断是否执行引导）")]
    public string persistentSceneName = "PersistentScene";

    [Tooltip("主菜单场景名称（引导后加载）")]
    public string mainMenuSceneName = "MainMenu";

    private static bool s_bootstrapped = false;
    private bool shouldLoadMainMenu = false;

    private void Awake()
    {
        Scene active = SceneManager.GetActiveScene();
        if (active.name != persistentSceneName)
            return;

        if (s_bootstrapped)
        {
            Destroy(gameObject);
            return;
        }

        s_bootstrapped = true;
        shouldLoadMainMenu = true;
    }

    private void Start()
    {
        if (!shouldLoadMainMenu || string.IsNullOrEmpty(mainMenuSceneName))
            return;

        // 附加加载主菜单，PersistentScene 保持常驻，无需 DontDestroyOnLoad
        SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Additive);
        Scene mainMenuScene = SceneManager.GetSceneByName(mainMenuSceneName);
        if (mainMenuScene.isLoaded)
            SceneManager.SetActiveScene(mainMenuScene);
    }
}
