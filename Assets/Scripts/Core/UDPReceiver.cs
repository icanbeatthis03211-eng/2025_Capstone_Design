using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using Logic; 

namespace Core
{
    public class UDPReceiver : MonoBehaviour
    {
        [Header("Settings")]
        public int port = 5005; 
        public bool showDebugLog = true;

        private UdpClient udpClient;
        private Thread receiveThread;
        private bool isRunning = true;
        private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

        void Start() { StartUDPServer(); }

        void Update()
        {
            while (messageQueue.TryDequeue(out string json))
            {
                ProcessReceivedData(json);
            }
        }

        private void StartUDPServer()
        {
            try
            {
                udpClient = new UdpClient(port);
                receiveThread = new Thread(new ThreadStart(ReceiveUDPData));
                receiveThread.IsBackground = true;
                receiveThread.Start();
                Debug.Log($"[UDP] 수신 대기 시작 (Port: {port})");
            }
            catch (System.Exception e) { Debug.LogError($"[UDP] 서버 실패: {e.Message}"); }
        }

        private void ReceiveUDPData()
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            while (isRunning)
            {
                try
                {
                    if (udpClient != null)
                    {
                        byte[] data = udpClient.Receive(ref remoteEndPoint);
                        string json = Encoding.UTF8.GetString(data);
                        messageQueue.Enqueue(json);
                    }
                }
                catch (System.Exception) { }
            }
        }

        // 🔥 [최종] 파이썬 데이터를 처리하는 핵심 함수
        private void ProcessReceivedData(string json)
        {
            if (showDebugLog) Debug.Log($"[Recv] {json}");

            var workoutMgr = Object.FindFirstObjectByType<WorkoutManager>();

            try
            {
                // 1. 결과 리포트 수신 (Python -> Unity)
                if (json.Contains("\"type\": \"result\""))
                {
                    Debug.Log("📄 성적표 도착! 데이터를 저장합니다.");

                    float score = ExtractFloatValue(json, "score");
                    float kcal = ExtractFloatValue(json, "kcal");
                    string feedback = ExtractStringValue(json, "feedback");

                    if (GameState.Instance != null)
                    {
                        GameState.Instance.LastSessionScore = score;
                        GameState.Instance.LastCalories = kcal;
                        GameState.Instance.LastAiFeedback = feedback;
                    }
                }
                // 2. 메시지 및 가이드 수신 (텍스트 + 음성 동기화) 🔥
                else if (json.Contains("\"type\": \"msg\""))
                {
                    string msg = ExtractStringValue(json, "data");
                    
                    // ★ [수정 1] 중요한 메시지인지 판단 (시작, 완료 등)
                    bool isUrgent = msg.Contains("시작") || msg.Contains("완료") || msg.Contains("Start") || msg.Contains("Count");

                    // ★ [수정 2] GoogleTTSManager 직접 호출 (긴급 여부 전달)
                    // (CoachingVoiceManager가 GoogleTTSManager를 감싸고 있다면 그쪽을 수정해야 함. 
                    //  여기서는 확실하게 GoogleTTSManager를 부르도록 작성함)
                    if (GoogleTTSManager.Instance != null) 
                        GoogleTTSManager.Instance.Speak(msg, isUrgent);

                    // (2) HUD 텍스트 업데이트
                    if (workoutMgr != null) 
                    {
                        workoutMgr.SetFeedbackText(msg);
                    }
                }
                // 3. 운동 중 실시간 데이터
                else if (workoutMgr != null)
                {
                    if (json.Contains("\"type\": \"depth\""))
                        workoutMgr.SetDepth(ExtractFloatValue(json, "data"));
                    
                    else if (json.Contains("\"type\": \"count\""))
                        workoutMgr.AddSquatCount();
                    
                    else if (json.Contains("\"type\": \"knee\""))
                        workoutMgr.SetKneeStatus(json.Contains("true"));
                    
                    else if (json.Contains("\"type\": \"spine\""))
                    {
                         bool isGood = json.Contains("true");
                         
                         // ★ [수정 3] 여기서 독단적으로 말하던 코드 삭제함!
                         // 파이썬이 이미 쿨다운 계산해서 'msg'로 보내주므로, 
                         // 여기서 또 말하면 중복 + 무한반복의 원인이 됩니다.
                         
                         workoutMgr.SetSpineStatus(isGood);
                    }
                    else if (json.Contains("\"type\": \"hold\""))
                    {
                         float time = ExtractFloatValue(json, "data");
                         workoutMgr.SetHoldTime(time);
                    }
                }
            }
            catch (System.Exception e) { Debug.LogError($"파싱 에러: {e.Message}"); }
        }

        private float ExtractFloatValue(string json, string key)
        {
            string pattern = $"\"{key}\":";
            int start = json.IndexOf(pattern);
            if (start == -1) return 0f;
            start += pattern.Length;
            
            int end = json.IndexOf(",", start);
            if (end == -1) end = json.IndexOf("}", start);
            
            string valStr = json.Substring(start, end - start).Replace("\"", "").Trim();
            return float.Parse(valStr);
        }

        private string ExtractStringValue(string json, string key)
        {
            string pattern = $"\"{key}\": \"";
            int start = json.IndexOf(pattern);
            if (start == -1) return "";
            start += pattern.Length;

            int end = json.LastIndexOf("\""); 
            if (end <= start) end = json.IndexOf("\"", start); 

            return json.Substring(start, end - start);
        }

        void OnApplicationQuit()
        {
            isRunning = false;
            if (udpClient != null) udpClient.Close();
            if (receiveThread != null) receiveThread.Abort();
        }
    }
}