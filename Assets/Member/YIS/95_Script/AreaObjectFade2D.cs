using UnityEngine;
using System.Collections;

public class AreaObjectFade2D : MonoBehaviour
{
    [Header("페이드할 오브젝트")]
    public GameObject targetObject;

    [Header("페이드 시간")]
    public float fadeDuration = 0.5f;

    private SpriteRenderer[] spriteRenderers;
    private Coroutine fadeCoroutine;

    private void Start()
    {
        spriteRenderers = targetObject.GetComponentsInChildren<SpriteRenderer>(true);

        // 처음에는 투명하게
        SetAlpha(0f);
        targetObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        targetObject.SetActive(true);

        fadeCoroutine = StartCoroutine(Fade(0f, 1f));
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(Fade(1f, 0f, true));
    }

    private IEnumerator Fade(float startAlpha, float endAlpha, bool disableAfter = false)
    {
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;

            float t = time / fadeDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            SetAlpha(Mathf.Lerp(startAlpha, endAlpha, t));

            yield return null;
        }

        SetAlpha(endAlpha);

        if (disableAfter)
            targetObject.SetActive(false);
    }

    private void SetAlpha(float alpha)
    {
        foreach (SpriteRenderer sprite in spriteRenderers)
        {
            Color color = sprite.color;
            color.a = alpha;
            sprite.color = color;
        }
    }
}
