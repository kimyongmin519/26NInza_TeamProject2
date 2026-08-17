using KimLIb.AnimatorSystems;
using UnityEngine;

namespace Member.KYM.Scripts.Agents.FSM
{
    [CreateAssetMenu(fileName = "State data", menuName = "FSM/State SO", order = 0)]
    public class StateSO : ScriptableObject
    {
        public string stateName;
        public string className;
        public int stateIndex;
        public AnimParamSO stateParam;
        public AnimParamSO[] additiveParams;
    }
}