using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BackGroundAnime : MonoBehaviour
{
    [SerializeField] private List<Sprite> Sprites = new();
    [SerializeField] private float FrameInterval = 0.1f;
    [SerializeField] private bool PlayOnStart = true;
    [SerializeField] private bool Loop = true;

    private Image _image;
    private SpriteRenderer _spriteRenderer;
    private float _timer;
    private int _currentIndex;
    private bool _isPlaying;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (PlayOnStart)
        {
            Play();
        }
    }

    private void Update()
    {
        if (!_isPlaying || Sprites.Count == 0 || FrameInterval <= 0f)
        {
            return;
        }

        _timer += Time.deltaTime;

        if (_timer < FrameInterval)
        {
            return;
        }

        _timer -= FrameInterval;
        _currentIndex++;

        if (_currentIndex >= Sprites.Count)
        {
            if (!Loop)
            {
                _currentIndex = Sprites.Count - 1;
                _isPlaying = false;
                return;
            }

            _currentIndex = 0;
        }

        SetSprite(_currentIndex);
    }

    public void Play()
    {
        if (Sprites.Count == 0)
        {
            return;
        }

        _isPlaying = true;
        _timer = 0f;
        _currentIndex = Mathf.Clamp(_currentIndex, 0, Sprites.Count - 1);
        SetSprite(_currentIndex);
    }

    public void Stop() => _isPlaying = false;

    public void Restart()
    {
        _currentIndex = 0;
        Play();
    }

    public void SetFrame(int index)
    {
        if (Sprites.Count == 0)
        {
            return;
        }

        _currentIndex = Mathf.Clamp(index, 0, Sprites.Count - 1);
        _timer = 0f;
        SetSprite(_currentIndex);
    }

    private void SetSprite(int index)
    {
        Sprite sprite = Sprites[index];

        if (_image != null)
        {
            _image.sprite = sprite;
        }

        if (_spriteRenderer != null)
        {
            _spriteRenderer.sprite = sprite;
        }
    }
}
