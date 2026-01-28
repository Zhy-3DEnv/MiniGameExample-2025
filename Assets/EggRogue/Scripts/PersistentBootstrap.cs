using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 常驻场景引导。挂在 PersistentRoot 上，Awake 时 DontDestroyOnLoad 并加载主菜单。
/// 仅在 PersistentScene 中运行；若从其他场景启动则不引导。
/// </summary>
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

        // 防止重复引导（比如 PersistentScene 被重复加载 / 场景里放了多个 PersistentRoot）
        if (s_bootstrapped)
        {
            Destroy(gameObject);
            return;
        }

        s_bootstrapped = true;

        // 不要在 Awake 里立刻 LoadScene（某些 Unity 版本/平台会触发内部层级断言）
        // 另外，这个 Bootstrap 本身不需要常驻：真正需要常驻的是各个 Manager（它们自己会 DontDestroyOnLoad）。
        shouldLoadMainMenu = true;
    }

    private void Start()
    {
        if (!shouldLoadMainMenu)
            return;

        // 进入主菜单（单场景加载）
        if (!string.IsNullOrEmpty(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
    }
}
