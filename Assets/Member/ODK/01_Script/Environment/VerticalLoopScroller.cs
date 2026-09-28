using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Member.ODK.Scripts.Environment
{
    [DisallowMultipleComponent]
    public class VerticalLoopScroller : MonoBehaviour
    {
        [SerializeField] private float speed = 2f;
        [SerializeField] private bool playOnStart = true;
        [SerializeField, Min(0f)] private float heightOverride;
        [SerializeField, Min(2)] private int minimumCopies = 3;

        public float Speed
        {
            get => speed;
            set => speed = value;
        }

        public bool IsPlaying { get; set; }

        private static bool spawningCopy;

        private readonly List<Transform> segments = new List<Transform>();
        private Vector3 basePosition;
        private float segmentHeight;
        private float offset;
        private bool isCopy;

        private void Awake()
        {
            isCopy = spawningCopy;
        }

        private void Start()
        {
            if (isCopy)
            {
                enabled = false;
                return;
            }

            basePosition = transform.localPosition;
            segmentHeight = heightOverride > 0f ? heightOverride : MeasureHeight();
            if (segmentHeight <= 0.01f)
            {
                enabled = false;
                return;
            }

            int copies = Mathf.Max(minimumCopies, Mathf.CeilToInt(GetViewHeight() / segmentHeight) + 2);
            segments.Add(transform);
            spawningCopy = true;
            try
            {
                for (int i = 1; i < copies; i++)
                {
                    GameObject copy = Instantiate(gameObject, transform.parent);
                    copy.name = name + " Loop " + i;
                    VerticalLoopScroller copyScroller = copy.GetComponent<VerticalLoopScroller>();
                    if (copyScroller != null) Destroy(copyScroller);
                    segments.Add(copy.transform);
                }
            }
            finally
            {
                spawningCopy = false;
            }

            IsPlaying = playOnStart;
            ApplyOffset();
        }

        private void LateUpdate()
        {
            if (isCopy || !IsPlaying || segments.Count == 0) return;
            offset += speed * Time.deltaTime;
            ApplyOffset();
        }

        private void ApplyOffset()
        {
            float loop = segmentHeight * segments.Count;
            for (int i = 0; i < segments.Count; i++)
            {
                if (segments[i] == null) continue;
                float y = Mathf.Repeat(i * segmentHeight - offset, loop) - segmentHeight;
                segments[i].localPosition = basePosition + Vector3.up * y;
            }
        }

        private float MeasureHeight()
        {
            Tilemap tilemap = GetComponent<Tilemap>();
            if (tilemap != null)
            {
                tilemap.CompressBounds();
                float cell = tilemap.layoutGrid != null ? tilemap.layoutGrid.cellSize.y : 1f;
                return tilemap.cellBounds.size.y * cell * Mathf.Abs(transform.localScale.y);
            }

            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
                return spriteRenderer.sprite.bounds.size.y * Mathf.Abs(transform.localScale.y);
            return 0f;
        }

        private static float GetViewHeight()
        {
            Camera main = Camera.main;
            if (main == null) return 20f;
            return main.orthographic ? main.orthographicSize * 2f : 20f;
        }
    }
}
