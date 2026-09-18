using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Member.KYM.Scripts.EffectSystems
{
    public class Rotator : MonoBehaviour
    {
        [SerializeField] private Vector3 rotationSpeedAndAxis;

        private void Update()
        {
            transform.Rotate(rotationSpeedAndAxis * Time.deltaTime);
        }
    }
}