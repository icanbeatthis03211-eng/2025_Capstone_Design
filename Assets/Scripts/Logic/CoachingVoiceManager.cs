using UnityEngine;
using TMPro;
using System.Collections;
using Core; 

namespace Logic
{
    [RequireComponent(typeof(AudioSource))]
    public class CoachingVoiceManager : MonoBehaviour
    {
        public static CoachingVoiceManager Instance; 

        [Header("UI References")]
        public TextMeshProUGUI txtSubtitle;  

        [Header("Audio Settings")]
        public AudioSource audioSource; 
        
        void Awake()
        {
            Instance = this;
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            // 시작 시엔 비워둠
            if (txtSubtitle) txtSubtitle.text = "";
        }

        public void Speak(string message, AudioClip voiceClip = null, float duration = 3.0f)
        {
            // 🔥 [핵심] 자막 유지 기능
            // 타이머로 지우지 않고, 새로운 메시지가 올 때까지 계속 띄워둡니다.
            if (txtSubtitle != null)
            {
                txtSubtitle.text = message;
            }

            // 오디오 처리
            if (voiceClip != null && audioSource != null)
            {
                // TTS가 말하고 있다면 끊기 (우선순위: 녹음된 음성 > TTS)
                if (GoogleTTSManager.Instance) GoogleTTSManager.Instance.Stop();
                
                audioSource.Stop();
                audioSource.PlayOneShot(voiceClip);
            }
            else
            {
                // 구글 TTS 호출
                if (GoogleTTSManager.Instance != null)
                {
                    GoogleTTSManager.Instance.Speak(message);
                }
            }
            
            Debug.Log($"[AI Coach] {message}");
        }
        
        public void SpeakTextOnly(string message)
        {
            Speak(message, null);
        }
    }
}