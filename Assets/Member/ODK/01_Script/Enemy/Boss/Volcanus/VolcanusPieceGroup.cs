using UnityEngine;

public class VolcanusPieceGroup : MonoBehaviour
{
    [Header("Follow")]
    [SerializeField] private float followThreshold = 3f;
    [SerializeField] private float followStrength = 0.4f;
    [SerializeField] private float maxFollowDistance = 3f;

    private VolcanusPiece[] pieces;

    private void Awake()
    {
        pieces = GetComponentsInChildren<VolcanusPiece>(true);
    }

    public Vector3 GetFollowOffset(VolcanusPiece requester)
    {
        if (pieces == null || pieces.Length == 0)
            return Vector3.zero;

        VolcanusPiece farthestPiece = null;

        Vector3 farthestDisplacement = Vector3.zero;
        float farthestDistance = followThreshold;

        foreach (VolcanusPiece piece in pieces)
        {
            if (piece == null)
                continue;

            if (piece == requester)
                continue;

            if (piece.IsDestroyed)
                continue;

            // 중요한 부분:
            // 자기 원래 위치에서 얼마나 벗어났는지를 검사
            Vector3 displacement =
                piece.transform.localPosition -
                piece.OriginLocalPosition;

            float distance = displacement.magnitude;

            if (distance <= farthestDistance)
                continue;

            farthestDistance = distance;
            farthestDisplacement = displacement;
            farthestPiece = piece;
        }

        if (farthestPiece == null)
            return Vector3.zero;

        float excess =
            farthestDistance -
            followThreshold;

        float followDistance =
            Mathf.Min(
                excess * followStrength,
                maxFollowDistance
            );

        return farthestDisplacement.normalized *
               followDistance;
    }
}