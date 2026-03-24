using UnityEngine;
using UnityEngine.UI;

public class UIFactory
{
    public static GameObject CreateCanvas(string name = "Canvas", RenderMode renderMode = RenderMode.ScreenSpaceOverlay)
    {
        GameObject canvasObj = new GameObject(name);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = renderMode;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        return canvasObj;
    }

    public static GameObject CreatePanel(string name, Transform parent, Vector2 size, Vector2 position, Color bgColor)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = panel.AddComponent<Image>();
        image.color = bgColor;

        return panel;
    }

    public static GameObject CreateButton(string name, Transform parent, Vector2 size, Vector2 position, string text)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = btnObj.AddComponent<Image>();
        Button button = btnObj.AddComponent<Button>();

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.sizeDelta = Vector2.zero;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;

        Text textComponent = textObj.AddComponent<Text>();
        textComponent.text = text;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return btnObj;
    }

    public static GameObject CreateInputField(string name, Transform parent, Vector2 size, Vector2 position, string placeholder)
    {
        GameObject inputObj = new GameObject(name);
        inputObj.transform.SetParent(parent, false);

        RectTransform rect = inputObj.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = inputObj.AddComponent<Image>();
        image.color = new Color(1, 1, 1, 0.8f);

        InputField inputField = inputObj.AddComponent<InputField>();

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(inputObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(size.x - 10, size.y);
        textRect.position = new Vector3(5, 0, 0);

        Text textComponent = textObj.AddComponent<Text>();
        textComponent.text = "";
        textComponent.alignment = TextAnchor.MiddleLeft;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(inputObj.transform, false);

        RectTransform placeholderRect = placeholderObj.AddComponent<RectTransform>();
        placeholderRect.sizeDelta = new Vector2(size.x - 10, size.y);
        placeholderRect.position = new Vector3(5, 0, 0);

        Text placeholderText = placeholderObj.AddComponent<Text>();
        placeholderText.text = placeholder;
        placeholderText.alignment = TextAnchor.MiddleLeft;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        inputField.textComponent = textComponent;
        inputField.placeholder = placeholderText;

        return inputObj;
    }

    public static GameObject CreateText(string name, Transform parent, Vector2 size, Vector2 position, string text, TextAnchor alignment = TextAnchor.MiddleCenter)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Text textComponent = textObj.AddComponent<Text>();
        textComponent.text = text;
        textComponent.alignment = alignment;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return textObj;
    }

    public static GameObject CreateImage(string name, Transform parent, Vector2 size, Vector2 position, Sprite sprite = null)
    {
        GameObject imageObj = new GameObject(name);
        imageObj.transform.SetParent(parent, false);

        RectTransform rect = imageObj.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image = imageObj.AddComponent<Image>();
        if (sprite != null)
        {
            image.sprite = sprite;
        }

        return imageObj;
    }
}
