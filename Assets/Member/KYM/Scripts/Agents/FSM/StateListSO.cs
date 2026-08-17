using UnityEngine;

namespace Member.KYM.Scripts.Agents.FSM
{
    [CreateAssetMenu(fileName = "State manager", menuName = "FSM/State list", order = 10)]
    public class StateListSO : ScriptableObject
    {
        public string stateEnum;
        public StateSO[] states;
    }
}