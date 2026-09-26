using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Member.KYM.Scripts.UI
{
    public class SettingPlayer : MonoBehaviour
    {
        [SerializeField] private float flipDuration;
        [SerializeField] private RectTransform rectTrm;

        private WaitForSeconds _waitForSeconds;
        private bool _isFlip = false;

        private void Awake()
        {
            _waitForSeconds = new WaitForSeconds(flipDuration);
        }

        private void Start()
        {
            StartCoroutine(CycleCor());
        }

        private IEnumerator CycleCor()
        {
            while (true)
            {
                yield return _waitForSeconds;
                if (!_isFlip)
                    rectTrm.localRotation = Quaternion.Euler(0f, 180f, 0f);
                else
                    rectTrm.localRotation = Quaternion.Euler(0f, 0f, 0f);
                
                _isFlip = !_isFlip;
            }
        }
    }
}