using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class DialogTextAnimator : MonoBehaviour
{
    [SerializeField] private ShadowPixelText TargetText;

    private readonly List<DialogTextTag> _tags = new();
    private string _parsedText = string.Empty;

    private void Awake()
    {
        if (TargetText == null)
        {
            TargetText = GetComponent<ShadowPixelText>();
        }
    }

    public string SetText(string text)
    {
        ResolveTargetText();
        _parsedText = DialogTextTagParser.Parse(text, _tags);

        if (TargetText != null)
        {
            TargetText.SetText(_parsedText);
        }

        return _parsedText;
    }

    public void SetMaxVisibleCharacters(int count)
    {
        ResolveTargetText();
        TargetText?.SetMaxVisibleCharacters(count);
    }

    public float GetDelay(int characterIndex, float defaultDelay)
    {
        for (int i = 0; i < _tags.Count; i++)
        {
            DialogTextTag tag = _tags[i];
            if (tag.Type != DialogTextTagType.Speed)
            {
                continue;
            }

            if (tag.Contains(characterIndex))
            {
                return Mathf.Max(0f, tag.Value);
            }
        }

        return defaultDelay;
    }

    public float GetWait(int characterIndex)
    {
        float waitTime = 0f;

        for (int i = 0; i < _tags.Count; i++)
        {
            DialogTextTag tag = _tags[i];
            if (tag.Type == DialogTextTagType.Wait && tag.Start == characterIndex)
            {
                waitTime = Mathf.Max(waitTime, tag.Value);
            }
        }

        return waitTime;
    }

    private void LateUpdate()
    {
        if (TargetText == null || _tags.Count == 0)
        {
            ResolveTargetText();
        }

        if (TargetText == null || _tags.Count == 0)
        {
            return;
        }

        Play(TargetText.BackText);
        Play(TargetText.FrontText);
    }

    private void Play(TextMeshProUGUI text)
    {
        if (text == null || string.IsNullOrEmpty(text.text))
        {
            return;
        }

        text.ForceMeshUpdate();
        TMP_TextInfo textInfo = text.textInfo;

        for (int i = 0; i < _tags.Count; i++)
        {
            DialogTextTag tag = _tags[i];

            switch (tag.Type)
            {
                case DialogTextTagType.Shake:
                    PlayShake(textInfo, tag);
                    break;
                case DialogTextTagType.Wobble:
                    PlayWobble(textInfo, tag);
                    break;
                case DialogTextTagType.Rainbow:
                    PlayRainbow(textInfo, tag);
                    break;
            }
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            TMP_MeshInfo meshInfo = textInfo.meshInfo[i];
            meshInfo.mesh.vertices = meshInfo.vertices;
            meshInfo.mesh.colors32 = meshInfo.colors32;
            text.UpdateGeometry(meshInfo.mesh, i);
        }
    }

    private void PlayShake(TMP_TextInfo textInfo, DialogTextTag tag)
    {
        float power = tag.Value <= 0f ? 2f : tag.Value;

        ForEachVisibleCharacter(textInfo, tag, (charInfo, order) =>
        {
            Vector3 offset = new(
                Mathf.Sin((Time.time + order) * 62.8f) * power,
                Mathf.Cos((Time.time + order) * 40f) * power,
                0f);

            MoveCharacter(textInfo, charInfo, offset);
        });
    }

    private void PlayWobble(TMP_TextInfo textInfo, DialogTextTag tag)
    {
        float power = tag.Value <= 0f ? 4f : tag.Value;

        ForEachVisibleCharacter(textInfo, tag, (charInfo, _) =>
        {
            Vector3[] vertices = textInfo.meshInfo[charInfo.materialReferenceIndex].vertices;

            for (int i = 0; i < 4; i++)
            {
                Vector3 origin = vertices[charInfo.vertexIndex + i];
                float y = Mathf.Sin(Time.time * 8f + origin.x * 0.04f) * power;
                vertices[charInfo.vertexIndex + i] = origin + new Vector3(0f, y, 0f);
            }
        });
    }

    private void PlayRainbow(TMP_TextInfo textInfo, DialogTextTag tag)
    {
        ForEachVisibleCharacter(textInfo, tag, (charInfo, order) =>
        {
            Color color = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.8f + order * 0.06f, 1f), 0.8f, 1f);
            Color32 color32 = color;
            Color32[] colors = textInfo.meshInfo[charInfo.materialReferenceIndex].colors32;

            for (int i = 0; i < 4; i++)
            {
                colors[charInfo.vertexIndex + i] = color32;
            }
        });
    }

    private void MoveCharacter(TMP_TextInfo textInfo, TMP_CharacterInfo charInfo, Vector3 offset)
    {
        Vector3[] vertices = textInfo.meshInfo[charInfo.materialReferenceIndex].vertices;

        for (int i = 0; i < 4; i++)
        {
            vertices[charInfo.vertexIndex + i] += offset;
        }
    }

    private void ForEachVisibleCharacter(TMP_TextInfo textInfo, DialogTextTag tag, Action<TMP_CharacterInfo, int> action)
    {
        int end = Mathf.Min(tag.Start + tag.Length, textInfo.characterCount);

        for (int i = tag.Start; i < end; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

            if (!charInfo.isVisible)
            {
                continue;
            }

            action.Invoke(charInfo, i - tag.Start);
        }
    }

    private void ResolveTargetText()
    {
        if (TargetText == null)
        {
            TargetText = GetComponent<ShadowPixelText>();
        }
    }
}

public enum DialogTextTagType
{
    Shake,
    Wobble,
    Rainbow,
    Speed,
    Wait
}

public readonly struct DialogTextTag
{
    public readonly DialogTextTagType Type;
    public readonly int Start;
    public readonly int Length;
    public readonly float Value;

    public DialogTextTag(DialogTextTagType type, int start, int length, float value)
    {
        Type = type;
        Start = start;
        Length = length;
        Value = value;
    }

    public bool Contains(int index) => index >= Start && index < Start + Length;
}

public static class DialogTextTagParser
{
    public static string Parse(string text, List<DialogTextTag> tags)
    {
        tags.Clear();

        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        StringBuilder builder = new();
        ParseRange(text, 0, text.Length, builder, tags);
        return builder.ToString();
    }

    private static void ParseRange(string text, int start, int end, StringBuilder builder, List<DialogTextTag> tags)
    {
        int index = start;

        while (index < end)
        {
            if (text[index] != '<' || !TryReadTag(text, index, end, out DialogTextTagType type, out float value, out int tagEnd))
            {
                builder.Append(text[index]);
                index++;
                continue;
            }

            if (type == DialogTextTagType.Wait)
            {
                tags.Add(new DialogTextTag(type, builder.Length, 0, value));
                index = tagEnd + 1;
                continue;
            }

            string closeTag = $"</{type}>";
            int closeIndex = text.IndexOf(closeTag, tagEnd + 1, StringComparison.OrdinalIgnoreCase);
            int animStart = builder.Length;

            if (closeIndex < 0 || closeIndex >= end)
            {
                ParseRange(text, tagEnd + 1, end, builder, tags);
                tags.Add(new DialogTextTag(type, animStart, builder.Length - animStart, value));
                break;
            }

            ParseRange(text, tagEnd + 1, closeIndex, builder, tags);
            tags.Add(new DialogTextTag(type, animStart, builder.Length - animStart, value));
            index = closeIndex + closeTag.Length;
        }
    }

    private static bool TryReadTag(string text, int start, int maxEnd, out DialogTextTagType type, out float value, out int tagEnd)
    {
        type = default;
        value = 0f;
        tagEnd = -1;

        int close = text.IndexOf('>', start + 1);
        if (close < 0 || close >= maxEnd || start + 1 >= text.Length || text[start + 1] == '/')
        {
            return false;
        }

        string tagText = text.Substring(start + 1, close - start - 1);
        string[] parts = tagText.Split('=');

        if (!Enum.TryParse(parts[0], true, out type))
        {
            return false;
        }

        if (parts.Length > 1)
        {
            float.TryParse(parts[1], out value);
        }

        tagEnd = close;
        return true;
    }
}
