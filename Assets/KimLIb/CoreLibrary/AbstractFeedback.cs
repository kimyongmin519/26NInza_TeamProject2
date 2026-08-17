using UnityEngine;

namespace GGMLib.CoreLibrary
{
    public abstract class AbstractFeedback : MonoBehaviour
    {
        public abstract void PlayFeedback();
        public virtual void StopFeedback() { }
    }
}