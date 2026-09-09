using Unity.Cinemachine;
using KimLIb.EventSystem;
using System.Collections;
using UnityEngine;

public class CameraManager : MonoSingleton<CameraManager>
{
    [SerializeField] private CinemachineCamera targetCamera;
    [SerializeField] private EventChannelSO cameraEventChannel;
    [SerializeField] private Transform defaultTarget;
    [SerializeField] private Transform focusTarget;
    [SerializeField] private Vector3 focusPosition;
    [SerializeField] private float moveDuration = 0.35f;
    [SerializeField] private float focusOrthographicSize = 3.5f;
    [SerializeField] private bool changeOrthographicSize = true;

    private Transform _runtimeFocusTarget;
    private Transform _targetAfterMove;
    private Coroutine _moveCoroutine;
    private float _defaultOrthographicSize;

    protected override void Awake()
    {
        base.Awake();

        if (targetCamera == null)
        {
            targetCamera = FindFirstObjectByType<CinemachineCamera>();
        }

        if (targetCamera != null)
        {
            _defaultOrthographicSize = targetCamera.Lens.OrthographicSize;

            if (defaultTarget == null)
            {
                defaultTarget = targetCamera.Target.TrackingTarget;
            }
        }

        GameObject focusObject = new GameObject("Camera Focus Target");
        focusObject.transform.SetParent(transform);
        _runtimeFocusTarget = focusObject.transform;

        if (defaultTarget != null)
        {
            _runtimeFocusTarget.position = defaultTarget.position;
        }

        cameraEventChannel?.AddListener<FocusCameraTargetEvent>(OnFocusCameraTargetEvent);
        cameraEventChannel?.AddListener<ReturnDefaultCameraTargetEvent>(OnReturnDefaultCameraTargetEvent);
    }

    protected override void OnDestroy()
    {
        cameraEventChannel?.RemoveListener<FocusCameraTargetEvent>(OnFocusCameraTargetEvent);
        cameraEventChannel?.RemoveListener<ReturnDefaultCameraTargetEvent>(OnReturnDefaultCameraTargetEvent);
        base.OnDestroy();
    }

    public void SetTarget(CameraTarget target)
    {
        if (targetCamera == null)
        {
            return;
        }

        targetCamera.Target = target;
    }

    public void SetTarget(Transform target)
    {
        if (target == null)
        {
            return;
        }

        SetTarget(new CameraTarget
        {
            TrackingTarget = target,
            LookAtTarget = target,
            CustomLookAtTarget = false
        });
    }

    public void FocusTarget()
    {
        Focus(focusTarget);
    }

    public void FocusPosition()
    {
        FocusPosition(focusPosition);
    }

    public void SetFocusPosition(Vector3 position)
    {
        focusPosition = position;
    }

    public void Focus(Transform target)
    {
        if (target == null)
        {
            return;
        }

        FocusPosition(target.position);
    }

    public void FocusPosition(Vector3 position)
    {
        if (targetCamera == null || _runtimeFocusTarget == null)
        {
            return;
        }

        StartFocusMove(position, changeOrthographicSize ? focusOrthographicSize : targetCamera.Lens.OrthographicSize);
    }

    public void ReturnDefaultTarget()
    {
        if (defaultTarget == null)
        {
            return;
        }

        StartFocusMove(defaultTarget.position, _defaultOrthographicSize, defaultTarget);
    }

    public void ReturnDefaultTargetInstant()
    {
        SetTarget(defaultTarget);

        if (targetCamera == null)
        {
            return;
        }

        LensSettings lens = targetCamera.Lens;
        lens.OrthographicSize = _defaultOrthographicSize;
        targetCamera.Lens = lens;
    }

    private void StartFocusMove(Vector3 position, float orthographicSize, Transform targetAfterMove = null)
    {
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
        }

        _targetAfterMove = targetAfterMove;
        _moveCoroutine = StartCoroutine(FocusMoveCoroutine(position, orthographicSize));
    }

    private IEnumerator FocusMoveCoroutine(Vector3 position, float orthographicSize)
    {
        SetTarget(_runtimeFocusTarget);

        Vector3 startPosition = _runtimeFocusTarget.position;
        float startSize = targetCamera.Lens.OrthographicSize;
        float time = 0f;

        if (moveDuration <= 0f)
        {
            _runtimeFocusTarget.position = position;
            SetOrthographicSize(orthographicSize);
            if (_targetAfterMove != null)
            {
                SetTarget(_targetAfterMove);
                _targetAfterMove = null;
            }

            _moveCoroutine = null;
            yield break;
        }

        while (time < moveDuration)
        {
            time += Time.deltaTime;
            float percent = Mathf.Clamp01(time / moveDuration);
            percent = Mathf.SmoothStep(0f, 1f, percent);

            _runtimeFocusTarget.position = Vector3.Lerp(startPosition, position, percent);
            SetOrthographicSize(Mathf.Lerp(startSize, orthographicSize, percent));
            yield return null;
        }

        _runtimeFocusTarget.position = position;
        SetOrthographicSize(orthographicSize);
        if (_targetAfterMove != null)
        {
            SetTarget(_targetAfterMove);
            _targetAfterMove = null;
        }

        _moveCoroutine = null;
    }

    private void SetOrthographicSize(float orthographicSize)
    {
        LensSettings lens = targetCamera.Lens;
        lens.OrthographicSize = orthographicSize;
        targetCamera.Lens = lens;
    }

    private void OnFocusCameraTargetEvent(FocusCameraTargetEvent evt)
    {
        Focus(evt.Target);
    }

    private void OnReturnDefaultCameraTargetEvent(ReturnDefaultCameraTargetEvent evt)
    {
        ReturnDefaultTarget();
    }
}
