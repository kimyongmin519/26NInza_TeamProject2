using Member.KYM.Scripts.Players;
using UnityEngine;
using UnityEngine.Events;

namespace Member.KYM.Scripts.CoreSystems.InteractSystems
{
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private LayerMask whatIsPlayer;
        [SerializeField] private float successDuration;
        private float _currentTime;

        public UnityEvent OnSuccess;
        
        
        private PlayerController _player;
        
        private bool _isIn;
        private bool _isHolding;

        public bool IsIn
        {
            get => _isIn;
            set
            {
                bool before = _isIn;
                _isIn = value;
                if (before != _isIn)
                {
                    /*if (_isIn)
                        interactPromtObj.ShowButton();
                    else
                        interactPromtObj.HideButton();*/
                }
            }
        }

        private void Awake()
        {
            Collider[] colliders = new Collider[1];
            Physics.OverlapSphereNonAlloc(transform.position, 999f, colliders, whatIsPlayer);
            _player = colliders[0].GetComponent<PlayerController>();
            Debug.Assert(_player != null, "씬에 플레이어가 없어요!!!");
        }

        private void Start()
        {
            _player.PlayerInput.OnInteractKeyPressed += HandleInteractKeyPressed;
        }

        private void Update()
        {
            if (_isHolding)
            {
                _currentTime += Time.deltaTime; 
            }
            else
            {
                _currentTime = 0;
            }
            float percent = _currentTime / successDuration;
            //holdingImage.fillAmount = percent;
            if (1f <= percent)
            {
                _currentTime = 0;
                OnSuccess?.Invoke();
            }
        }

        private void HandleInteractKeyPressed(bool value)
        {
            _isHolding = value;
        }

        private void OnDestroy()
        {
            _player.PlayerInput.OnInteractKeyPressed -= HandleInteractKeyPressed;
        }

        private void OnTriggerStay(Collider other)
        {
            IsIn = true;
        }

        private void OnTriggerExit(Collider other)
        {
            IsIn = false;
        }
    }
}
