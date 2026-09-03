using UnityEngine;
using UnityEngine.UI;

namespace Member.ODK.Scripts.Enemys.Volcanus
{
    public class VolcanusTestHealthBars : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Volcanus boss;
        [SerializeField] private Transform player;
        [SerializeField] private bool addTestPlayerHealth = true;
        [SerializeField] private float testPlayerMaxHealth = 300f;

        [Header("Optional UI")]
        [SerializeField] private Image bossFill;
        [SerializeField] private Image playerFill;
        [SerializeField] private bool createRuntimeBars = true;

        private VolcanusTestHealth testPlayerHealth;
        private HealthModule playerHealthModule;
        private GameObject runtimeCanvas;

        private void Start()
        {
            if (boss == null) boss = GetComponent<Volcanus>();
            if (player == null && boss != null) player = boss.Target;
            FindPlayerHealth();

            if (createRuntimeBars && (bossFill == null || playerFill == null))
                CreateRuntimeBars();
            RefreshBars();
        }

        private void Update()
        {
            if (player == null && boss != null)
            {
                player = boss.Target;
                FindPlayerHealth();
            }
            RefreshBars();
        }

        private void FindPlayerHealth()
        {
            if (player == null) return;
            testPlayerHealth = player.GetComponentInParent<VolcanusTestHealth>();
            if (testPlayerHealth == null) testPlayerHealth = player.GetComponentInChildren<VolcanusTestHealth>();
            playerHealthModule = player.GetComponentInParent<HealthModule>();
            if (playerHealthModule == null) playerHealthModule = player.GetComponentInChildren<HealthModule>();

            if (testPlayerHealth == null && playerHealthModule == null && addTestPlayerHealth)
            {
                testPlayerHealth = player.gameObject.AddComponent<VolcanusTestHealth>();
                testPlayerHealth.SetMaxHealth(testPlayerMaxHealth);
            }
        }

        private void RefreshBars()
        {
            if (bossFill != null)
            {
                float maxHealth = boss != null ? boss.MaxHealth : 0f;
                bossFill.fillAmount = maxHealth > 0f ? Mathf.Clamp01(boss.CurrentHealth / maxHealth) : 0f;
            }

            if (playerFill == null) return;
            if (testPlayerHealth != null)
            {
                playerFill.fillAmount = testPlayerHealth.MaxHealth > 0f
                    ? Mathf.Clamp01(testPlayerHealth.CurrentHealth / testPlayerHealth.MaxHealth)
                    : 0f;
            }
            else if (playerHealthModule != null)
            {
                playerFill.fillAmount = playerHealthModule.MaxHealth > 0f
                    ? Mathf.Clamp01(playerHealthModule.CurrentHealth / playerHealthModule.MaxHealth)
                    : 0f;
            }
            else
            {
                playerFill.fillAmount = 0f;
            }
        }

        private void CreateRuntimeBars()
        {
            runtimeCanvas = new GameObject("Volcanus Test Health Bars");
            Canvas canvas = runtimeCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            CanvasScaler scaler = runtimeCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            runtimeCanvas.AddComponent<GraphicRaycaster>();

            if (bossFill == null)
                bossFill = CreateBar(runtimeCanvas.transform, "VOLCANUS", new Vector2(0.5f, 1f), new Vector2(620f, 30f), new Vector2(0f, -42f), new Color(0.92f, 0.12f, 0.08f, 1f));
            if (playerFill == null)
                playerFill = CreateBar(runtimeCanvas.transform, "PLAYER", new Vector2(0f, 0f), new Vector2(360f, 24f), new Vector2(32f, 36f), new Color(0.1f, 0.75f, 1f, 1f));
        }

        private Image CreateBar(Transform parent, string barName, Vector2 anchor, Vector2 size, Vector2 position, Color fillColor)
        {
            GameObject backgroundObject = new GameObject($"{barName} Health Background", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(parent, false);
            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = anchor;
            backgroundRect.anchorMax = anchor;
            backgroundRect.pivot = anchor.x > 0f ? new Vector2(0.5f, 1f) : Vector2.zero;
            backgroundRect.sizeDelta = size;
            backgroundRect.anchoredPosition = position;
            backgroundObject.GetComponent<Image>().color = new Color(0.035f, 0.035f, 0.04f, 0.92f);

            GameObject fillObject = new GameObject($"{barName} Health Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(backgroundObject.transform, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0.02f, 0.14f);
            fillRect.anchorMax = new Vector2(0.98f, 0.86f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fill = fillObject.GetComponent<Image>();
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 1f;
            return fill;
        }

        private void OnDestroy()
        {
            if (runtimeCanvas != null) Destroy(runtimeCanvas);
        }
    }
}
