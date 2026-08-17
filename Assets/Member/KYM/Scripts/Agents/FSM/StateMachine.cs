using System;
using System.Collections.Generic;
using UnityEngine;

namespace Member.KYM.Scripts.Agents.FSM
{
    public class StateMachine
    {
        public AgentState CurrentState { get; private set; }
        private Dictionary<int, AgentState> _stateDict;

        public StateMachine(Agent agent, StateSO[] stateList)
        {
            _stateDict = new Dictionary<int, AgentState>();
            foreach (StateSO stateData in stateList)
            {
                Type type = Type.GetType(stateData.className);
                Debug.Assert(type != null, $"찾고자 하는 타입이 없습니다. : {stateData.className}");
                int paramHash = stateData.stateParam != null ? stateData.stateParam.ParamHash : 0;
                AgentState state = (AgentState)Activator.CreateInstance(type, agent, paramHash);
                
                _stateDict.Add(stateData.stateIndex, state);
            }
        }

        public void ChangeState(int newStateIndex)
        {
            CurrentState?.Exit();
            AgentState newState = _stateDict.GetValueOrDefault(newStateIndex);
            Debug.Assert(newState != null, $"new State is null {newStateIndex}");
            
            CurrentState = newState;
            CurrentState.Enter();
        }
        
        public void UpdateMachine() => CurrentState?.Update();

        public AgentState GetCurrentState() => CurrentState;
        public AgentState GetState(int stateIndex)
        {
            return _stateDict.GetValueOrDefault(stateIndex);
        }
    }
}