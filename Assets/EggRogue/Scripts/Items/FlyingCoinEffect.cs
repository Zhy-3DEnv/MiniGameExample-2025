using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 金币飞向 UI 的视觉效果。从世界坐标起点飞向目标 RectTransform，约 1 秒内完成。
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class FlyingCoinEffect : MonoBehaviour
{
    [Tooltip("飞行时长（秒）")]
    public float duration = 1f;

    [Tooltip("缓动：0=线性，>0 为末端加速")]
    public float easePower = 1.5f;

    private RectTransform _rect;
    private Image _image;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _image = GetComponent<Image>();
    }

    /// <summary>
    /// 创建并播放飞行动画。飞完自动销毁。
    /// </summary>
    /// <param name="worldStart">金币世界坐标</param>
    /// <param name="target">目标 UI RectTransform</param>
    /// <param name="canvas">用于放置飞币的 Canvas</param>
    /// <param name="sprite">飞币图标（可空，用默认圆）</param>
    /// <param name="size">图标大小（像素）</param>
    /// <param name="duration">飞行时长（秒），默认 1 秒</param>
    /// <param name="color">飞币颜色（无 sprite 时使用；有 sprite 时作为 tint）</param>
    public static void Play(Vector3 worldStart, RectTransform target, Canvas canvas, Sprite sprite, float size = 40f, float duration = 1f, Color? color = null)
    {
        if (target == null || canvas == null) return;

        var cam = Camera.main;
        if (cam == null) return;

        var go = new GameObject("FlyingCoin");
        var rect = go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();

        rect.SetParent(canvas.transform, false);
        rect.sizeDelta = new Vector2(size, size);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Color flyColor = color ?? new Color(1f, 0.84f, 0f, 1f);
        if (sprite != null)
        {
            img.sprite = sprite;
            img.color = flyColor;
        }
        else
        {
            img.sprite = CreateDefaultCoinSprite(flyColor);
            img.color = Color.white;
        }

        img.raycastTarget = false;

        var fx = go.AddComponent<FlyingCoinEffect>();
        fx.duration = duration;
        fx.StartCoroutine(fx.FlyRoutine(worldStart, target, canvas, cam));
    }

    private static Sprite CreateDefaultCoinSprite(Color color)
    {
        var tex = new Texture2D(32, 32);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float dx = (x - 15.5f) / 16f;
                float dy = (y - 15.5f) / 16f;
                float d = dx * dx + dy * dy;
                tex.SetPixel(x, y, d <= 1f ? color : Color.clear);
            }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
    }

    private IEnumerator FlyRoutine(Vector3 worldStart, RectTransform target, Canvas canvas, Camera cam)
    {
        RectTransform canvasRect = transform.parent as RectTransform;
        if (canvasRect == null) yield break;

        Vector2 startScreen = RectTransformUtility.WorldToScreenPoint(cam, worldStart);
        Vector2 endScreen;
        if (target != null)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                endScreen = new Vector2(target.position.x, target.position.y);
            else
                endScreen = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera != null ? canvas.worldCamera : cam, target.TransformPoint(Vector3.zero));
        }
        else
            endScreen = startScreen;

        Camera canvasCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : (canvas.worldCamera != null ? canvas.worldCamera : cam);

        Vector2 startLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, startScreen, canvasCam, out startLocal);
        Vector2 endLocal;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, endScreen, canvasCam, out endLocal);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, easePower);

            _rect.anchoredPosition = Vector2.Lerp(startLocal, endLocal, eased);
            yield return null;
        }

        Destroy(gameObject);
    }
}
