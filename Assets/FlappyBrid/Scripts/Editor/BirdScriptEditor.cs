using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BirdScript))]
public class BirdScriptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 绘制默认Inspector
        DrawDefaultInspector();
        
        BirdScript birdScript = (BirdScript)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("调试工具", EditorStyles.boldLabel);
        
        // 调试可视化开关
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("启用调试可视化", GUILayout.Width(150));
        birdScript.enableDebugVisualization = EditorGUILayout.Toggle(birdScript.enableDebugVisualization);
        EditorGUILayout.EndHorizontal();
        
        if (birdScript.enableDebugVisualization)
        {
            EditorGUILayout.HelpBox("调试模式已启用。在Scene视图中可以看到拖尾预览效果。", MessageType.Info);
            
            EditorGUILayout.Space();
            
            // 调试总分数
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("测试总分数", GUILayout.Width(150));
            birdScript.debugTotalScore = EditorGUILayout.IntSlider(birdScript.debugTotalScore, 0, 1000);
            EditorGUILayout.EndHorizontal();
            
            // 调试Y曲线
            EditorGUILayout.LabelField("测试Y坐标变化曲线（模拟跳跃轨迹）");
            birdScript.debugYCurve = EditorGUILayout.CurveField(birdScript.debugYCurve);
            
            EditorGUILayout.Space();
            
            // 快速预设按钮
            EditorGUILayout.LabelField("快速预设", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("平滑上升"))
            {
                birdScript.debugYCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 2f);
                EditorUtility.SetDirty(birdScript);
            }
            
            if (GUILayout.Button("跳跃轨迹"))
            {
                // 创建一个抛物线轨迹
                Keyframe[] keys = new Keyframe[]
                {
                    new Keyframe(0f, 0f),
                    new Keyframe(0.3f, 2f),
                    new Keyframe(0.7f, 1.5f),
                    new Keyframe(1f, 0f)
                };
                birdScript.debugYCurve = new AnimationCurve(keys);
                EditorUtility.SetDirty(birdScript);
            }
            
            if (GUILayout.Button("波浪轨迹"))
            {
                // 创建一个波浪轨迹
                Keyframe[] keys = new Keyframe[10];
                for (int i = 0; i < 10; i++)
                {
                    float t = (float)i / 9f;
                    float y = Mathf.Sin(t * Mathf.PI * 2f) * 1f;
                    keys[i] = new Keyframe(t, y);
                }
                birdScript.debugYCurve = new AnimationCurve(keys);
                EditorUtility.SetDirty(birdScript);
            }
            
            if (GUILayout.Button("重置曲线"))
            {
                birdScript.debugYCurve = AnimationCurve.Linear(0f, 0f, 1f, 0f);
                EditorUtility.SetDirty(birdScript);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // 刷新Scene视图按钮
            if (GUILayout.Button("刷新Scene视图", GUILayout.Height(30)))
            {
                SceneView.RepaintAll();
                EditorUtility.SetDirty(birdScript);
                if (!Application.isPlaying && birdScript.enableDebugVisualization)
                {
                    birdScript.UpdateDebugTrailRenderer();
                }
            }
        }
        
        // 强制刷新Scene视图（当参数改变时）
        if (GUI.changed)
        {
            EditorUtility.SetDirty(birdScript);
            SceneView.RepaintAll();
            // 在编辑模式下强制更新拖尾
            if (!Application.isPlaying && birdScript.enableDebugVisualization)
            {
                birdScript.UpdateDebugTrailRenderer();
            }
        }
    }
    
    // 在Scene视图中实时更新
    private void OnSceneGUI()
    {
        BirdScript birdScript = (BirdScript)target;
        if (birdScript.enableDebugVisualization && !Application.isPlaying)
        {
            // 强制更新拖尾渲染器
            birdScript.UpdateDebugTrailRenderer();
        }
    }
    
    // 当Inspector更新时调用
    private void OnInspectorUpdate()
    {
        BirdScript birdScript = (BirdScript)target;
        if (birdScript.enableDebugVisualization && !Application.isPlaying)
        {
            // 定期更新拖尾
            birdScript.UpdateDebugTrailRenderer();
            SceneView.RepaintAll();
        }
    }
}

