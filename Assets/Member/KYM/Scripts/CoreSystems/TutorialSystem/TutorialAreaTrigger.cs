using Member.KYM.Scripts.Players;
using UnityEngine;

namespace Member.KYM.Scripts.CoreSystems.TutorialSystem
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class TutorialAreaTrigger : MonoBehaviour
    {
        [SerializeField] private TutorialStep step;

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryComplete(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            // 단계가 시작되기 전에 이미 영역 안에 들어온 경우도 처리한다.
            TryComplete(other);
        }

        private void TryComplete(Collider2D other)
        {
            if (step == null || !step.IsActive ||
                other.GetComponentInParent<PlayerController>() == null)
                return;

            step.Complete();
        }

        private void OnValidate()
        {
            Collider2D area = GetComponent<Collider2D>();
            if (area != null)
                area.isTrigger = true;
        }
    }
}
