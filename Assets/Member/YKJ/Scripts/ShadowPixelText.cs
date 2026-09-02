using TMPro;
using UnityEngine;

public enum TextHorizontalAlignment
{
    Left,
    Center,
    Right,
    Justified,
    Flush
}

public enum TextVerticalAlignment
{
    Top,
    Middle,
    Bottom,
    Baseline,
    Geometry,
    Capline
}

public class ShadowPixelText : MonoBehaviour
{
    [SerializeField] private float size = 36f;

    [Header("Alignment")]
    [SerializeField] private TextHorizontalAlignment horizontalAlignment;
    [SerializeField] private TextVerticalAlignment verticalAlignment;

    [Header("References")]
    [SerializeField] private TextMeshProUGUI frontText;
    [SerializeField] private TextMeshProUGUI backText;

    private void OnValidate()
    {
        Refresh();
    }

    public void SetText(string text)
    {
        if (frontText != null)
            frontText.text = text;

        if (backText != null)
            backText.text = text;
    }
    public string GetText() => frontText != null ? frontText.text : string.Empty;
    private void Refresh()
    {
        if (frontText == null || backText == null)
            return;

        backText.text = frontText.text;

        frontText.fontSize = size;
        backText.fontSize = size;

        ApplyAlignment(frontText);
        ApplyAlignment(backText);
    }

    private void ApplyAlignment(TextMeshProUGUI target)
    {
        target.horizontalAlignment = horizontalAlignment switch
        {
            TextHorizontalAlignment.Left
                => HorizontalAlignmentOptions.Left,

            TextHorizontalAlignment.Center
                => HorizontalAlignmentOptions.Center,

            TextHorizontalAlignment.Right
                => HorizontalAlignmentOptions.Right,

            TextHorizontalAlignment.Justified
                => HorizontalAlignmentOptions.Justified,

            TextHorizontalAlignment.Flush
                => HorizontalAlignmentOptions.Flush,

            _ => HorizontalAlignmentOptions.Center
        };

        target.verticalAlignment = verticalAlignment switch
        {
            TextVerticalAlignment.Top
                => VerticalAlignmentOptions.Top,

            TextVerticalAlignment.Middle
                => VerticalAlignmentOptions.Middle,

            TextVerticalAlignment.Bottom
                => VerticalAlignmentOptions.Bottom,

            TextVerticalAlignment.Baseline
                => VerticalAlignmentOptions.Baseline,

            TextVerticalAlignment.Geometry
                => VerticalAlignmentOptions.Geometry,

            TextVerticalAlignment.Capline
                => VerticalAlignmentOptions.Capline,

            _ => VerticalAlignmentOptions.Middle
        };
    }
}