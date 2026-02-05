#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace EggRogue
{
    /// <summary>
    /// 在 GameScene 中为环境光修复组件做一键挂载（解决编辑器中从 PersistentScene 进入 GameScene 后环境光不生效的问题）。
    /// </summary>
    public static class SceneEnvironmentLightingFixSetup
    {
        private const string GameSceneName = "GameScene";

        [MenuItem("EggRogue/GameScene 添加环境光修复组件")]
        public static void AddEnvironmentLightingFix()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.name != GameSceneName)
            {
                Debug.LogWarning($"[SceneEnvironmentLightingFix] 当前场景为 {active.name}，请先打开 {GameSceneName} 再执行此菜单。");
                return;
            }

            var fix = Object.FindObjectOfType<SceneEnvironmentLightingFix>();
            if (fix != null)
            {
                Debug.Log("[SceneEnvironmentLightingFix] 场景中已存在该组件，无需重复添加。");
                Selection.activeGameObject = fix.gameObject;
                return;
            }

            // 优先挂在 LevelFlowManager 上，否则挂在第一个根物体上
            LevelFlowManager levelFlow = Object.FindObjectOfType<LevelFlowManager>();
            GameObject target = levelFlow != null ? levelFlow.gameObject : null;
            if (target == null)
            {
                var roots = active.GetRootGameObjects();
                foreach (GameObject go in roots)
                {
                    if (go.GetComponent<Camera>() == null && go.GetComponent<Light>() == null)
                    {
                        target = go;
                        break;
                    }
                }
            }

            if (target == null)
            {
                target = new GameObject("SceneEnvironmentLightingFix");
            }

            var added = target.AddComponent<SceneEnvironmentLightingFix>();
            EditorUtility.SetDirty(target);
            EditorSceneManager.MarkSceneDirty(active);
            Selection.activeGameObject = target;
            Debug.Log($"[SceneEnvironmentLightingFix] 已在 {target.name} 上添加环境光修复组件，请保存场景 (Ctrl+S)。");
        }
    }
}
#endif
