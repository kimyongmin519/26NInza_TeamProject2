using System.Collections;
using KimLIb.AnimatorSystems;
using Member.KYM.Scripts.Agents;
using Member.KYM.Scripts.CoreSystems;
using UnityEngine;
using UnityEngine.U2D.IK;

namespace Member.KYM.Scripts.Players.RobotArm
{
    public class RobotArmTakeOutController : MonoBehaviour
    {
        [SerializeField] private PlayerInputSO playerInput;
        [SerializeField] private Animator animator;
        [SerializeField] private AnimatorTrigger animatorTrigger;
        [SerializeField] private RobotArm robotArm;
        [SerializeField] private RobotArmGrabber grabber;
        [SerializeField] private RobotArmGrappler grappler;
        [SerializeField] private IKManager2D ikManager;
        [SerializeField] private AgentRenderer agentRenderer;
        [SerializeField] private GameObject[] propPrefabs;
        [SerializeField] private AnimParamSO takeOutParam;
        [SerializeField, Min(0f)] private float controlBlendDuration = 0.15f;

        public bool IsTakingOut { get; private set; }

        private bool _robotArmWasEnabled;
        private bool _ikManagerWasEnabled;
        private float _ikWeightBeforeTakeOut;
        private Vector3 _visualScaleBeforeTakeOut;
        private Coroutine _blendRoutine;
        private bool _isFinishing;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
            
            animator.enabled = false;

            if (animatorTrigger == null)
                animatorTrigger = GetComponent<AnimatorTrigger>();

            if (agentRenderer == null)
                agentRenderer = transform.root.GetComponentInChildren<AgentRenderer>(true);

            if (ikManager == null)
                ikManager = GetComponentInParent<IKManager2D>(true);
        }

        private void OnEnable()
        {
            if (playerInput != null)
                playerInput.OnTakeOutKeyPressed += HandleTakeOut;
        }

        private void OnDisable()
        {
            if (playerInput != null)
                playerInput.OnTakeOutKeyPressed -= HandleTakeOut;

            UnsubscribeAnimationEnd();
            ForceRestoreArmControl();
        }

        private void HandleTakeOut()
        {
            if (IsTakingOut ||
                animator == null ||
                animator.runtimeAnimatorController == null ||
                animatorTrigger == null ||
                takeOutParam == null ||
                robotArm == null ||
                !robotArm.gameObject.activeInHierarchy ||
                grabber == null ||
                grabber.IsHolding ||
                grabber.IsBusy ||
                (grappler != null && grappler.IsGrappling) ||
                !HasValidPrefab())
            {
                return;
            }

            IsTakingOut = true;
            _isFinishing = false;
            grabber.SetActionLocked(true);

            _robotArmWasEnabled = robotArm.enabled;
            robotArm.enabled = false;

            if (ikManager != null)
            {
                _ikManagerWasEnabled = ikManager.enabled;
                _ikWeightBeforeTakeOut = ikManager.weight;
            }

            _visualScaleBeforeTakeOut = transform.localScale;
            float facingDirection = agentRenderer != null
                ? agentRenderer.FacingDirection
                : 1f;
            transform.localScale = new Vector3(
                Mathf.Abs(_visualScaleBeforeTakeOut.x) * facingDirection,
                _visualScaleBeforeTakeOut.y,
                _visualScaleBeforeTakeOut.z
            );

            animatorTrigger.OnAnimationEnd -= HandleAnimationEnd;
            animatorTrigger.OnAnimationEnd += HandleAnimationEnd;
            animator.enabled = true;
            animator.Play(takeOutParam.ParamHash, 0, 0f);

            StartIkBlend(0f, false);
        }

        public void GrabTakeOutProp()
        {
            if (!IsTakingOut || grabber.IsHolding)
                return;

            GameObject prefab = GetRandomPrefab();
            if (prefab == null)
                return;

            GameObject instance = Instantiate(
                prefab,
                grabber.GrabPoint.position,
                grabber.GrabPoint.rotation
            );

            IGrabbable grabbable = FindGrabbable(instance);
            if (grabbable == null || !grabber.TryGrab(grabbable))
                Destroy(instance);
        }

        public void FinishTakeOut()
        {
            UnsubscribeAnimationEnd();

            if (!IsTakingOut || _isFinishing)
                return;

            _isFinishing = true;
            GrabTakeOutProp();

            if (robotArm != null)
                robotArm.enabled = _robotArmWasEnabled;

            StartIkBlend(_ikWeightBeforeTakeOut, true);
        }

        private void StartIkBlend(float targetWeight, bool restoreAfterBlend)
        {
            if (_blendRoutine != null)
            {
                StopCoroutine(_blendRoutine);
                _blendRoutine = null;
            }

            if (ikManager == null ||
                !_ikManagerWasEnabled ||
                controlBlendDuration <= 0f)
            {
                if (ikManager != null && _ikManagerWasEnabled)
                    ikManager.weight = targetWeight;

                if (restoreAfterBlend)
                    CompleteRestoreArmControl();

                return;
            }

            _blendRoutine = StartCoroutine(
                BlendIkWeightRoutine(targetWeight, restoreAfterBlend)
            );
        }

        private IEnumerator BlendIkWeightRoutine(
            float targetWeight,
            bool restoreAfterBlend
        )
        {
            float startWeight = ikManager.weight;
            float elapsed = 0f;

            while (elapsed < controlBlendDuration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(
                    elapsed / controlBlendDuration
                );
                float easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
                ikManager.weight = Mathf.Lerp(
                    startWeight,
                    targetWeight,
                    easedTime
                );
                yield return null;
            }

            ikManager.weight = targetWeight;
            _blendRoutine = null;

            if (restoreAfterBlend)
                CompleteRestoreArmControl();
        }

        private void CompleteRestoreArmControl()
        {
            IsTakingOut = false;
            _isFinishing = false;
            grabber?.SetActionLocked(false);

            if (robotArm != null)
                robotArm.enabled = _robotArmWasEnabled;

            if (ikManager != null)
            {
                ikManager.weight = _ikWeightBeforeTakeOut;
                ikManager.enabled = _ikManagerWasEnabled;
            }

            if (animator != null)
                animator.enabled = false;

            transform.localScale = _visualScaleBeforeTakeOut;
        }

        private void ForceRestoreArmControl()
        {
            if (_blendRoutine != null)
            {
                StopCoroutine(_blendRoutine);
                _blendRoutine = null;
            }

            if (!IsTakingOut)
            {
                if (animator != null)
                    animator.enabled = false;

                return;
            }

            CompleteRestoreArmControl();
        }

        private void HandleAnimationEnd()
        {
            FinishTakeOut();
        }

        private void UnsubscribeAnimationEnd()
        {
            if (animatorTrigger != null)
                animatorTrigger.OnAnimationEnd -= HandleAnimationEnd;
        }

        private bool HasValidPrefab()
        {
            if (propPrefabs == null)
                return false;

            foreach (GameObject prefab in propPrefabs)
            {
                if (prefab != null)
                    return true;
            }

            return false;
        }

        private GameObject GetRandomPrefab()
        {
            int startIndex = Random.Range(0, propPrefabs.Length);

            for (int i = 0; i < propPrefabs.Length; i++)
            {
                GameObject prefab = propPrefabs[(startIndex + i) % propPrefabs.Length];
                if (prefab != null)
                    return prefab;
            }

            return null;
        }

        private static IGrabbable FindGrabbable(GameObject instance)
        {
            MonoBehaviour[] behaviours = instance.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IGrabbable grabbable)
                    return grabbable;
            }

            return null;
        }

        private void OnValidate()
        {
            controlBlendDuration = Mathf.Max(0f, controlBlendDuration);
        }
    }
}
