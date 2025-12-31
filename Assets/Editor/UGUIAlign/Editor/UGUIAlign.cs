using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public enum AlignType
{
    Top = 1,
    Left = 2,
    Right = 3,
    Bottom = 4,
    HorizontalCenter = 5,       // 水平居中
    VerticalCenter = 6,         // 垂直居中
    Horizontal = 7,             // 横向分布
    Vertical = 8,               // 纵向分布
}

public class UGUIAlign : Editor
{
    [MenuItem("GameObject/UI/Align/Left 【左对齐】", false, 0)]
    static void AlignLeft()
    {
        Align(AlignType.Left);
    }
    [MenuItem("GameObject/UI/Align/HorizontalCenter 【水平居中】", false, 1)]
    static void AlignHorizontalCenter()
    {
        Align(AlignType.HorizontalCenter);
    }
    [MenuItem("GameObject/UI/Align/Right 【右对齐】", false, 2)]
    static void AlignRight()
    {
        Align(AlignType.Right);
    }
    [MenuItem("GameObject/UI/Align/Top 【顶端对齐】", false, 3)]
    static void AlignTop()
    {
        Align(AlignType.Top);
    }
    [MenuItem("GameObject/UI/Align/VerticalCenter 【垂直居中】", false, 4)]
    static void AlignVerticalCenter()
    {
        Align(AlignType.VerticalCenter);
    }
    [MenuItem("GameObject/UI/Align/Bottom 【底端对齐】", false, 5)]
    static void AlignBottom()
    {
        Align(AlignType.Bottom);
    }
    [MenuItem("GameObject/UI/Align/Horizontal 【横向分布】", false, 6)]
    static void AlignHorizontal()
    {
        Align(AlignType.Horizontal);
    }
    [MenuItem("GameObject/UI/Align/Vertical 【纵向分布】", false, 7)]
    static void AlignVertical()
    {
        Align(AlignType.Vertical);
    }

    public static void Align(AlignType type)
    {
        List<RectTransform> rects = new List<RectTransform>();
        GameObject[] objects = Selection.gameObjects;
        if (objects != null && objects.Length > 0)
        {
            for (int i = 0; i < objects.Length; i++)
            {
                RectTransform rect = objects[i].GetComponent<RectTransform>();
                if (rect != null)
                    rects.Add(rect);
            }
        }

        if (rects.Count > 1)
        {
            Align(type, rects);
        }
        else if (rects.Count == 1)
        {
            // 对单个对象执行对齐（例如居中）
            AlignSingle(type, rects[0]);
        }
    }
    
    public static void Align(AlignType type, List<RectTransform> rects)
    {
        // 记录所有需要修改的 RectTransform 的状态，以支持撤销
        Undo.RecordObjects(rects.ToArray(), "Align UI Elements");

        RectTransform tenplate = rects[0];
        float w = tenplate.sizeDelta.x * tenplate.lossyScale.x;
        float h = tenplate.sizeDelta.y * tenplate.localScale.y;

        float x = tenplate.position.x - tenplate.pivot.x * w;
        float y = tenplate.position.y - tenplate.pivot.y * h;

        switch (type)
        {
            case AlignType.Top:
                for (int i = 1; i < rects.Count; i++)
                {
                    RectTransform trans = rects[i];
                    float th = trans.sizeDelta.y * trans.localScale.y;
                    Vector3 pos = trans.position;
                    pos.y = y + h - th + trans.pivot.y * th;
                    trans.position = pos;
                }
                break;
            case AlignType.Left:
                for (int i = 1; i < rects.Count; i++)
                {
                    RectTransform trans = rects[i];
                    float tw = trans.sizeDelta.x * trans.lossyScale.x;
                    Vector3 pos = trans.position;
                    pos.x = x + tw * trans.pivot.x;
                    trans.position = pos;
                }
                break;
            case AlignType.Right:
                for (int i = 1; i < rects.Count; i++)
                {
                    RectTransform trans = rects[i];
                    float tw = trans.sizeDelta.x * trans.lossyScale.x;
                    Vector3 pos = trans.position;
                    pos.x = x + w - tw + tw * trans.pivot.x;
                    trans.position = pos;
                }
                break;
            case AlignType.Bottom:
                for (int i = 1; i < rects.Count; i++)
                {
                    RectTransform trans = rects[i];
                    float th = trans.sizeDelta.y * trans.localScale.y;
                    Vector3 pos = trans.position;
                    pos.y = y + th * trans.pivot.y;
                    trans.position = pos;
                }
                break;
            case AlignType.HorizontalCenter:
                for (int i = 1; i < rects.Count; i++)
                {
                    RectTransform trans = rects[i];
                    float tw = trans.sizeDelta.x * trans.lossyScale.x;
                    Vector3 pos = trans.position;
                    pos.x = x + 0.5f * w - 0.5f * tw + tw * trans.pivot.x;
                    trans.position = pos;
                }
                break;
            case AlignType.VerticalCenter:
                for (int i = 1; i < rects.Count; i++)
                {
                    RectTransform trans = rects[i];
                    float th = trans.sizeDelta.y * trans.localScale.y;
                    Vector3 pos = trans.position;
                    pos.y = y + 0.5f * h - 0.5f * th + th * trans.pivot.y;
                    trans.position = pos;
                }
                break;
            case AlignType.Horizontal:
                float minX = GetMinX(rects);
                float maxX = GetMaxX(rects);
                rects.Sort(SortListRectTransformByX);
                float distance = (maxX - minX)/(rects.Count - 1);
                for (int i = 1; i < rects.Count - 1; i++)
                {
                    RectTransform trans = rects[i];
                    Vector3 pos = trans.position;
                    pos.x = minX + i * distance;
                    trans.position = pos;
                }
                break;
            case AlignType.Vertical:
                float minY = GetMinY(rects);
                float maxY = GetMaxY(rects);
                rects.Sort(SortListRectTransformByY);
                float distanceY = (maxY - minY)/(rects.Count - 1);
                for (int i = 1; i < rects.Count - 1; i++)
                {
                    RectTransform trans = rects[i];
                    Vector3 pos = trans.position;
                    pos.y = minY + i*distanceY;
                    trans.position = pos;
                }
                break;
        }

        // 标记所有修改过的 RectTransform 为脏，以确保更改被保存
        foreach (var rect in rects)
        {
            EditorUtility.SetDirty(rect);
        }
    }

    private static void AlignSingle(AlignType type, RectTransform rect)
    {
        // 记录单个 RectTransform 的状态，以支持撤销
        Undo.RecordObject(rect, "Align UI Element");

        // 示例：根据类型对单个对象执行居中对齐
        switch (type)
        {
            case AlignType.HorizontalCenter:
                // 获取父对象的 RectTransform
                RectTransform parent = rect.parent as RectTransform;
                if (parent != null)
                {
                    Vector3 pos = rect.localPosition;
                    pos.x = 0;
                    rect.localPosition = pos;
                }
                break;
            case AlignType.VerticalCenter:
                RectTransform parentV = rect.parent as RectTransform;
                if (parentV != null)
                {
                    Vector3 pos = rect.localPosition;
                    pos.y = 0;
                    rect.localPosition = pos;
                }
                break;
            // 根据需要添加其他类型的单个对象对齐
            default:
                break;
        }

        // 标记对象为脏
        EditorUtility.SetDirty(rect);
    }

    private static int SortListRectTransformByX(RectTransform r1, RectTransform r2)
    {
        float w = r1.sizeDelta.x * r1.lossyScale.x;
        float x1 = r1.position.x - r1.pivot.x * w;
        w = r2.sizeDelta.x * r2.lossyScale.x;
        float x2 = r2.position.x - r2.pivot.x * w;
        if (x1 >= x2)
            return 1;
        else
            return -1;
    }

    private static int SortListRectTransformByY(RectTransform r1, RectTransform r2)
    {
        float w = r1.sizeDelta.y * r1.lossyScale.y;
        float y1 = r1.position.y - r1.pivot.y * w;
        w = r2.sizeDelta.y * r2.lossyScale.y;
        float y2 = r2.position.y - r2.pivot.y * w;
        if (y1 >= y2)
            return 1;
        else
            return -1;
    }

    private static float GetMinX(List<RectTransform> rects)
    {
        if (null == rects || rects.Count == 0)
            return 0;
        RectTransform tenplate = rects[0];
        float minx = tenplate.position.x;
        float tempX = 0;
        for (int i = 1; i < rects.Count; i++)
        {
            tempX = rects[i].position.x;
            if (tempX < minx)
                minx = tempX;
        }
        return minx;
    }

    private static float GetMaxX(List<RectTransform> rects)
    {
        if (null == rects || rects.Count == 0)
            return 0;
        RectTransform tenplate = rects[0];
        float maxX = tenplate.position.x;
        float tempX = 0;
        for (int i = 1; i < rects.Count; i++)
        {
            tempX = rects[i].position.x;
            if (tempX > maxX)
                maxX = tempX;
        }
        return maxX;
    }

    private static float GetMinY(List<RectTransform> rects)
    {
        if (null == rects || rects.Count == 0)
            return 0;
        RectTransform tenplate = rects[0];
        float minY = tenplate.position.y;
        float tempX = 0;
        for (int i = 1; i < rects.Count; i++)
        {
            tempX = rects[i].position.y;
            if (tempX < minY)
                minY = tempX;
        }
        return minY;
    }

    private static float GetMaxY(List<RectTransform> rects)
    {
        if (null == rects || rects.Count == 0)
            return 0;
        RectTransform tenplate = rects[0];
        float maxY = tenplate.position.y;
        float tempX = 0;
        for (int i = 1; i < rects.Count; i++)
        {
            tempX = rects[i].position.y;
            if (tempX > maxY)
                maxY = tempX;
        }
        return maxY;
    }
}
