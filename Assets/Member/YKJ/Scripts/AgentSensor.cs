using KimLIb.ModuleSystems;
using Member.KYM.Scripts.Agents;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.InputSystem;

public class AgentSensor : MonoBehaviour, IModule
{

    [SerializeField] private LayerMask interactableLayer;

    [SerializeField] private Vector2 boxSize;
    [SerializeField] private Vector2 offset;

    public Agent Owner { get; private set; }
    public void Initialize(ModuleOwner owner)
    {
        Owner = owner as Agent;
        if (Owner == null)
        {
            Debug.LogError("주인이 Agent가 아님");
        }
    }
    public bool IsInteractableInDirection()
    {

        Collider2D Target = Physics2D.OverlapBox(transform.position, boxSize, 0, interactableLayer);

        if (Target == null)
            return false;



        Target.GetComponent<Interactable>()?.Interaction(Owner);
        return true;

    }

    public float BoxCastInteractable(Vector2 direction, float distance, out RaycastHit2D hit)
    {
        hit = Physics2D.BoxCast((Vector2)transform.position + offset, boxSize, 0,
            direction, distance, interactableLayer);

        distance = hit ? hit.distance : distance;
        return distance;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + (Vector3)offset, boxSize);
    }


}
