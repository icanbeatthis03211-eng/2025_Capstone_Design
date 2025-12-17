using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace Logic
{
    public class ZoneTrigger : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("밟고 얼마나 있어야 실행될까요? (초)")]
        public float waitTime = 2.0f;
        
        [Tooltip("씬 시작 후 몇 초 동안 작동을 막을까요? (오작동 방지)")]
        public float initialDelay = 2.0f; // 🔥 [추가됨] 안전장치

        [Tooltip("작동 시 바뀔 색상")]
        public Color activeColor = Color.green;
        private Color _originalColor;
        private Renderer _renderer;

        [Header("Events")]
        public UnityEvent onTriggerActivated; 

        private Coroutine _activationRoutine;
        private bool _isReady = false; // 안전장치 플래그

        void Start()
        {
            _renderer = GetComponent<Renderer>();
            if (_renderer) _originalColor = _renderer.material.color;

            // 씬 시작 후 일정 시간 대기 후 작동 허용
            StartCoroutine(EnableTriggerRoutine());
        }

        IEnumerator EnableTriggerRoutine()
        {
            _isReady = false;
            yield return new WaitForSeconds(initialDelay);
            _isReady = true; // 이제부터 밟아도 됨
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_isReady) return; // 아직 준비 안 됨
            if (!other.CompareTag("Player")) return;

            Debug.Log("[Zone] 플레이어 감지! 카운트다운 시작...");
            if (_activationRoutine != null) StopCoroutine(_activationRoutine);
            _activationRoutine = StartCoroutine(ProcessActivation());
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            Debug.Log("[Zone] 플레이어 나감. 취소.");
            if (_activationRoutine != null) StopCoroutine(_activationRoutine);
            
            if (_renderer) _renderer.material.color = _originalColor;
        }

        IEnumerator ProcessActivation()
        {
            if (_renderer) _renderer.material.color = Color.yellow; 
            yield return new WaitForSeconds(waitTime);

            if (_renderer) _renderer.material.color = activeColor; 
            Debug.Log("[Zone] 실행!");
            onTriggerActivated.Invoke();
        }
    }
}