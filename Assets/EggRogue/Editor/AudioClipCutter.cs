using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Text;
#endif

public static class AudioClipCutter
{
    public static AudioClip CutClip(AudioClip clip, float start, float end)
    {
        if (clip == null)
        {
            Debug.LogError("AudioClipCutter.CutClip: 源 AudioClip 为空。");
            return null;
        }

        if (end <= start)
        {
            Debug.LogError("AudioClipCutter.CutClip: 结束时间必须大于开始时间。");
            return null;
        }

        int frequency = clip.frequency;
        int channels = clip.channels;

        float clipLength = clip.length;
        float clampedStart = Mathf.Clamp(start, 0f, clipLength);
        float clampedEnd = Mathf.Clamp(end, 0f, clipLength);

        if (clampedEnd <= clampedStart)
        {
            Debug.LogError("AudioClipCutter.CutClip: 截取区间无效。");
            return null;
        }

        int startSample = Mathf.FloorToInt(clampedStart * frequency) * channels;
        int endSample = Mathf.FloorToInt(clampedEnd * frequency) * channels;

        float[] data = new float[clip.samples * channels];
        clip.GetData(data, 0);

        int length = endSample - startSample;
        float[] newData = new float[length];

        System.Array.Copy(data, startSample, newData, 0, length);

        AudioClip newClip = AudioClip.Create(
            clip.name + "_cut",
            length / channels,
            channels,
            frequency,
            false
        );

        newClip.SetData(newData, 0);

        return newClip;
    }
}

#if UNITY_EDITOR
public class AudioClipCutterWindow : EditorWindow
{
    private AudioClip _sourceClip;
    private float _startTime;
    private float _endTime;
    private string _newClipName = string.Empty;

    private const int WaveformResolution = 1024;
    private float[] _waveformSamples;
    private AudioClip _waveformClip;

    [MenuItem("Tools/Audio/音效截断工具")]
    public static void ShowWindow()
    {
        AudioClipCutterWindow window = GetWindow<AudioClipCutterWindow>();
        window.titleContent = new GUIContent("音效截断工具");
        window.minSize = new Vector2(420f, 160f);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("将一段音频截取为新的 AudioClip 资源", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();

        _sourceClip = (AudioClip)EditorGUILayout.ObjectField("源音效", _sourceClip, typeof(AudioClip), false);

        if (_sourceClip == null)
        {
            EditorGUILayout.HelpBox("请先在上方选择一个源 AudioClip。", MessageType.Info);
            return;
        }

        UpdateWaveformCache();

        EditorGUILayout.LabelField("波形预览", EditorStyles.boldLabel);
        Rect waveformRect = GUILayoutUtility.GetRect(0, 100, GUILayout.ExpandWidth(true));
        DrawWaveform(waveformRect);
        EditorGUILayout.Space();

        if (Mathf.Approximately(_endTime, 0f))
        {
            _endTime = _sourceClip.length;
        }

        float clipLength = _sourceClip.length;

        EditorGUILayout.LabelField($"音效总时长: {clipLength:F3} 秒");
        EditorGUILayout.Space();

        _startTime = Mathf.Clamp(_startTime, 0f, clipLength);
        _endTime = Mathf.Clamp(_endTime, 0f, clipLength);

        if (_endTime < _startTime)
        {
            _endTime = _startTime;
        }

        EditorGUILayout.MinMaxSlider(new GUIContent("截取区间 (秒)"), ref _startTime, ref _endTime, 0f, clipLength);
        _startTime = EditorGUILayout.FloatField("开始时间 (秒)", _startTime);
        _endTime = EditorGUILayout.FloatField("结束时间 (秒)", _endTime);

        EditorGUILayout.Space();

        string defaultName = _sourceClip != null ? $"{_sourceClip.name}_cut" : "NewAudioClip";
        if (string.IsNullOrEmpty(_newClipName))
        {
            _newClipName = defaultName;
        }
        _newClipName = EditorGUILayout.TextField("新音效名称", _newClipName);

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(_endTime <= _startTime))
        {
            if (GUILayout.Button("生成并保存新音效"))
            {
                CreateCutClipAsset();
            }
        }
    }

    private void CreateCutClipAsset()
    {
        if (_sourceClip == null)
        {
            Debug.LogError("AudioClipCutterWindow: 源音效为空，无法截取。");
            return;
        }

        if (_endTime <= _startTime)
        {
            Debug.LogError("AudioClipCutterWindow: 结束时间必须大于开始时间。");
            return;
        }

        AudioClip newClip = AudioClipCutter.CutClip(_sourceClip, _startTime, _endTime);
        if (newClip == null)
        {
            Debug.LogError("AudioClipCutterWindow: 截取失败，返回的 AudioClip 为空。");
            return;
        }

        string sourcePath = AssetDatabase.GetAssetPath(_sourceClip);
        string directory = string.IsNullOrEmpty(sourcePath)
            ? "Assets"
            : Path.GetDirectoryName(sourcePath);

        string safeName = string.IsNullOrEmpty(_newClipName) ? $"{_sourceClip.name}_cut" : _newClipName;
        string assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(directory, $"{safeName}.wav"));

        SaveClipAsWav(newClip, assetPath);

        AssetDatabase.ImportAsset(assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        AudioClip importedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        Selection.activeObject = importedClip != null ? importedClip : (Object)newClip;
        EditorGUIUtility.PingObject(Selection.activeObject);

        Debug.Log($"AudioClipCutterWindow: 新音效已生成并保存为 WAV 文件，路径: {assetPath}");
    }

    private void UpdateWaveformCache()
    {
        if (_sourceClip == null)
        {
            _waveformSamples = null;
            _waveformClip = null;
            return;
        }

        if (_waveformClip == _sourceClip && _waveformSamples != null)
        {
            return;
        }

        int channels = _sourceClip.channels;
        int totalSamples = _sourceClip.samples * channels;

        if (totalSamples <= 0)
        {
            _waveformSamples = null;
            _waveformClip = _sourceClip;
            return;
        }

        float[] rawData = new float[totalSamples];
        _sourceClip.GetData(rawData, 0);

        int resolution = Mathf.Min(WaveformResolution, totalSamples);
        _waveformSamples = new float[resolution];

        int samplesPerPoint = Mathf.Max(1, totalSamples / resolution);

        for (int i = 0; i < resolution; i++)
        {
            int startIndex = i * samplesPerPoint;
            int endIndex = Mathf.Min(startIndex + samplesPerPoint, totalSamples);

            float maxValue = 0f;

            for (int j = startIndex; j < endIndex; j += channels)
            {
                float sampleValue = Mathf.Abs(rawData[j]);
                if (sampleValue > maxValue)
                {
                    maxValue = sampleValue;
                }
            }

            _waveformSamples[i] = maxValue;
        }

        _waveformClip = _sourceClip;
    }

    private void DrawWaveform(Rect rect)
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));

        if (_waveformSamples == null || _waveformSamples.Length == 0)
        {
            return;
        }

        Handles.BeginGUI();

        float midY = rect.y + rect.height * 0.5f;
        float halfHeight = rect.height * 0.45f;

        Handles.color = new Color(0.25f, 0.25f, 0.25f);
        Handles.DrawLine(new Vector3(rect.x, midY), new Vector3(rect.xMax, midY));

        Handles.color = new Color(0.2f, 0.8f, 0.2f);

        int length = _waveformSamples.Length;
        for (int i = 0; i < length; i++)
        {
            float t = (float)i / (length - 1);
            float x = rect.x + t * rect.width;
            float value = _waveformSamples[i];
            float y0 = midY - value * halfHeight;
            float y1 = midY + value * halfHeight;

            Handles.DrawLine(new Vector3(x, y0), new Vector3(x, y1));
        }

        if (_sourceClip != null)
        {
            float clipLength = _sourceClip.length;
            if (clipLength > 0f)
            {
                float startNorm = Mathf.Clamp01(_startTime / clipLength);
                float endNorm = Mathf.Clamp01(_endTime / clipLength);

                float startX = rect.x + startNorm * rect.width;
                float endX = rect.x + endNorm * rect.width;

                Handles.color = Color.yellow;
                Handles.DrawLine(new Vector3(startX, rect.y), new Vector3(startX, rect.yMax));

                Handles.color = Color.red;
                Handles.DrawLine(new Vector3(endX, rect.y), new Vector3(endX, rect.yMax));
            }
        }

        Handles.EndGUI();
    }

    private void SaveClipAsWav(AudioClip clip, string assetPath)
    {
        if (clip == null)
        {
            Debug.LogError("AudioClipCutterWindow: SaveClipAsWav 失败，clip 为空。");
            return;
        }

        string projectPath = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
        string fullPath = Path.Combine(projectPath, assetPath);

        try
        {
            int channels = clip.channels;
            int sampleRate = clip.frequency;
            int sampleCountPerChannel = clip.samples;
            int totalSampleCount = sampleCountPerChannel * channels;

            if (totalSampleCount <= 0)
            {
                Debug.LogError("AudioClipCutterWindow: SaveClipAsWav 失败，采样数为 0。");
                return;
            }

            float[] samples = new float[totalSampleCount];
            clip.GetData(samples, 0);

            // 转成 16 位 PCM 小端字节
            const int bitsPerSample = 16;
            const int headerSize = 44;
            byte[] bytes = new byte[totalSampleCount * (bitsPerSample / 8)];

            int offset = 0;
            for (int i = 0; i < totalSampleCount; i++)
            {
                float sample = Mathf.Clamp(samples[i], -1f, 1f);
                short intSample = (short)(sample * short.MaxValue);

                bytes[offset++] = (byte)(intSample & 0xFF);
                bytes[offset++] = (byte)((intSample >> 8) & 0xFF);
            }

            int fileSize = headerSize + bytes.Length;
            int byteRate = sampleRate * channels * (bitsPerSample / 8);
            short blockAlign = (short)(channels * (bitsPerSample / 8));

            using (FileStream fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(fileStream))
            {
                // RIFF 头
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(fileSize - 8);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));

                // fmt 子块
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(byteRate);
                writer.Write(blockAlign);
                writer.Write((short)bitsPerSample);

                // data 子块
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AudioClipCutterWindow: 保存 WAV 文件失败。路径: {fullPath}\n{e}");
        }
    }
}
#endif
