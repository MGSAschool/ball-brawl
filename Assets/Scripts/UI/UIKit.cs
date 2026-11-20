using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class UIKit
{
    public static RectTransform MakeOverlayCanvas(string name, Transform parent, int sortingOrder, Vector2 referenceResolution, bool clickable)
    {
        GameObject canvasObject = new GameObject(name, typeof(Canvas), typeof(CanvasScaler));
        if (clickable) canvasObject.AddComponent<GraphicRaycaster>();
        canvasObject.transform.SetParent(parent, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        return (RectTransform)canvasObject.transform;
    }

    public static RectTransform NewRect(string name, Transform parent)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    public static Image NewImage(string name, Transform parent)
    {
        Image image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.rectTransform.SetParent(parent, false);
        image.raycastTarget = false;
        return image;
    }

    public static GameObject NewPanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        Image image = NewImage(name, parent);
        image.color = HUDStyle.Panel;
        Place(image.rectTransform, anchorMin, anchorMax, pivot, position, size);
        return image.gameObject;
    }

    public static Image NewIcon(Transform parent, Sprite sprite, Rect anchors)
    {
        Image icon = NewImage("Icon", parent);
        Stretch(icon.rectTransform, anchors);
        icon.preserveAspect = true;
        icon.sprite = sprite;
        icon.enabled = sprite != null;
        return icon;
    }

    public static Outline AddOutline(GameObject target, Color color, float distance)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(distance, -distance);
        return outline;
    }

    public static void Stretch(RectTransform rect, Rect anchors)
    {
        rect.anchorMin = anchors.min;
        rect.anchorMax = anchors.max;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static void StretchToArt(RectTransform rect, Rect area, Rect artArea)
    {
        float width = area.width / artArea.width;
        float height = area.height / artArea.height;
        float left = area.xMin - artArea.xMin * width;
        float bottom = area.yMin - artArea.yMin * height;
        Stretch(rect, new Rect(left, bottom, width, height));
    }

    public static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    public static void PlaceAt(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        Place(rect, anchor, anchor, anchor, position, size);
    }

    public static Rect Inset(Rect r, float fraction)
    {
        float dx = r.width * fraction;
        float dy = r.height * fraction;
        return Rect.MinMaxRect(r.xMin + dx, r.yMin + dy, r.xMax - dx, r.yMax - dy);
    }

    public static TextMeshProUGUI NewAutoText(Transform parent, string name, float minSize, float maxSize,
        TextAlignmentOptions alignment, float padding)
    {
        TextMeshProUGUI text = NewText(parent, name);
        RectTransform rect = text.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);

        text.enableAutoSizing = true;
        text.fontSizeMin = minSize;
        text.fontSizeMax = maxSize;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = Color.white;
        text.outlineWidth = 0.2f;
        return text;
    }

    public static TextMeshProUGUI NewLabel(Transform parent, string name, float fontSize, Color color, float outlineWidth)
    {
        TextMeshProUGUI text = NewText(parent, name);
        text.fontSize = fontSize;
        text.color = color;
        text.outlineWidth = outlineWidth;
        GameArt art = GameArt.Instance;
        if (art == null || art.font == null) text.fontStyle = FontStyles.Bold;
        return text;
    }

    public static string Hex(Color color)
    {
        return ColorUtility.ToHtmlStringRGB(color);
    }

    public static string Colored(string text, Color color)
    {
        return "<color=#" + Hex(color) + ">" + text + "</color>";
    }

    public static string Colored(string text, string hex)
    {
        return "<color=#" + hex + ">" + text + "</color>";
    }

    public static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
        int minutes = total / 60;
        int rest = total % 60;
        return minutes + ":" + rest.ToString("00");
    }

    private static TextMeshProUGUI NewText(Transform parent, string name)
    {
        TextMeshProUGUI text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.rectTransform.SetParent(parent, false);

        GameArt art = GameArt.Instance;
        if (art != null && art.font != null) text.font = art.font;
        text.richText = true;
        text.raycastTarget = false;
        text.outlineColor = HUDStyle.Outline;
        return text;
    }
}
