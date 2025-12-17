using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using Logic;

namespace Core
{
    [Serializable] public class GoogleTTSRequest { public Input input; public VoiceSelectionParams voice; public AudioConfig audioConfig; }
    [Serializable] public class Input { public string text; }
    [Serializable] public class VoiceSelectionParams { public string languageCode; public string name; }
    [Serializable] public class AudioConfig { public string audioEncoding; public double pitch; public double speakingRate; }
    [Serializable] public class GoogleTTSResponse { public string audioContent; }

    public class GoogleTTSManager : MonoBehaviour
    {
        public static GoogleTTSManager Instance;

        [Header("Google Cloud API Key")]
        public string apiKey = "AIzaSyDAIJyQQgm8Rj7bX9ks00T0MdlPOaN3zIE"; 

        [Header("Voice Settings")]
        private string femaleVoice = "ko-KR-Neural2-A"; 
        private string maleVoice = "ko-KR-Neural2-C";   

        private const string API_URL = "https://texttospeech.googleapis.com/v1/text:synthesize";
        private AudioSource audioSource;

        private Queue<string> _speechQueue = new Queue<string>();
        private bool _isSpeaking = false;
        private string _lastAddedText = ""; 

        // ★ [Pro] 오디오 캐시 저장소 (같은 말 또 다운받지 않기 위해)
        private Dictionary<string, AudioClip> _audioCache = new Dictionary<string, AudioClip>();

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            }
            else Destroy(gameObject);
        }

        // ★ [Pro] 파라미터 추가: isImmediate (기본값 false)
        // 급한 메시지(시작, 완료 등)일 때만 true로 호출하세요.
        public void Speak(string text, bool isImmediate = false)
        {
            if (string.IsNullOrEmpty(text)) return;

            // 1. 중복 방지 (급한 메시지는 중복이어도 말해야 할 수 있음 -> isImmediate면 통과)
            if (!isImmediate && text == _lastAddedText) 
            {
                if (_speechQueue.Count > 2) return; 
            }

            // 2. 긴급 메시지 처리 (매직 스트링 제거됨)
            if (isImmediate)
            {
                Stop(); // 하던 말 끊고
                _lastAddedText = text;
                // 바로 코루틴 시작 (큐 무시하고 단독 실행)
                StartCoroutine(ProcessSingleImmediate(text));
                return;
            }

            // 3. 일반 메시지 큐에 추가
            _speechQueue.Enqueue(text);
            _lastAddedText = text;

            if (!_isSpeaking)
            {
                StartCoroutine(ProcessQueue());
            }
        }

        // ★ [Pro] 긴급 메시지용 단독 처리기
        IEnumerator ProcessSingleImmediate(string text)
        {
            _isSpeaking = true;
            yield return StartCoroutine(RequestAndPlayTTS(text));
            // 긴급 메시지 후 큐가 비어있다면 상태 해제, 아니면 다시 큐 처리 시작
            if (_speechQueue.Count > 0) StartCoroutine(ProcessQueue());
            else _isSpeaking = false;
        }

        IEnumerator ProcessQueue()
        {
            _isSpeaking = true;

            while (_speechQueue.Count > 0)
            {
                string textToSpeak = _speechQueue.Dequeue();
                
                yield return StartCoroutine(RequestAndPlayTTS(textToSpeak));

                while (audioSource.isPlaying) { yield return null; }
                yield return new WaitForSeconds(0.1f);
            }

            _isSpeaking = false;
            _lastAddedText = ""; 
        }

        public void Stop()
        {
            StopAllCoroutines(); 
            _speechQueue.Clear(); 
            if(audioSource) audioSource.Stop();
            _isSpeaking = false;
            _lastAddedText = "";
        }

        IEnumerator RequestAndPlayTTS(string text)
        {
            // ★ [Pro] 캐싱 확인: 이미 다운받은 적 있는 말인가?
            if (_audioCache.ContainsKey(text))
            {
                audioSource.clip = _audioCache[text];
                audioSource.Play();
                yield break; // 서버 요청 안 하고 끝냄
            }

            // ... (서버 요청 준비 코드는 동일) ...
            string gender = "Female"; 
            if (GameState.Instance != null) gender = GameState.Instance.CoachGender;
            string selectedVoice = (gender == "Male") ? maleVoice : femaleVoice;
            
            GoogleTTSRequest req = new GoogleTTSRequest
            {
                input = new Input { text = text },
                voice = new VoiceSelectionParams { languageCode = "ko-KR", name = selectedVoice },
                audioConfig = new AudioConfig { audioEncoding = "MP3", speakingRate = 1.25, pitch = 0.0 }
            };

            string json = JsonUtility.ToJson(req);

            using (UnityWebRequest www = new UnityWebRequest($"{API_URL}?key={apiKey}", "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");

                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    GoogleTTSResponse response = JsonUtility.FromJson<GoogleTTSResponse>(www.downloadHandler.text);
                    if (!string.IsNullOrEmpty(response.audioContent))
                    {
                        byte[] audioBytes = Convert.FromBase64String(response.audioContent);
                        yield return StartCoroutine(PlayAudio(audioBytes, text)); // text 전달
                    }
                }
            }
        }

        // ★ [Pro] 캐싱을 위해 text 파라미터 추가
        IEnumerator PlayAudio(byte[] bytes, string originalText)
        {
            // 임시 파일 쓰기 (여전히 필요하다면 유지, 아니면 메모리 로드 방식 추천)
            string tempPath = System.IO.Path.Combine(Application.persistentDataPath, "tts_temp.mp3");
            System.IO.File.WriteAllBytes(tempPath, bytes);

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, AudioType.MPEG))
            {
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    if(clip) 
                    { 
                        // ★ [Pro] 캐시에 저장
                        if (!_audioCache.ContainsKey(originalText))
                        {
                            _audioCache.Add(originalText, clip);
                        }

                        if(audioSource) 
                        {
                            audioSource.clip = clip;
                            audioSource.Play(); 
                        }
                    }
                }
            }
        }
    }
}