using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Core; // GameState 사용

namespace Core
{
    public class UDPSender : MonoBehaviour
    {
        [Header("PC Connection Settings")]
        [Tooltip("파이썬이 실행 중인 PC의 IP 주소를 입력하세요 (현재 설정: 172.30.1.26)")]
        // ★ 사용자의 현재 PC IP를 기본값으로 설정했습니다.
        // 만약 인스펙터(Inspector) 창에 다른 값이 적혀있다면 그걸 172.30.1.26으로 고쳐주세요.
        public string pcIpAddress = "172.30.1.26"; 
        public int pcPort = 6000; // 파이썬 코드의 PC_LISTEN_PORT와 같아야 함

        private UdpClient udpClient;

        void Start()
        {
            // 1. UDP 소켓 초기화
            udpClient = new UdpClient();

            // =========================================================
            // 🔥 [핵심 수정] 씬 시작 1초 후 자동 신호 전송
            // =========================================================
            // Workout 씬이 로드되고 1초 뒤에 자동으로 PC(파이썬)에게 
            // "나 준비됐어, 카메라 켜!"라는 신호를 보냅니다.
            Invoke("SendStartSignal", 1.0f); 
        }

        // 운동 시작 신호 보내기 (Invoke에 의해 자동 호출됨)
        public void SendStartSignal()
        {
            // GameState에서 현재 난이도 가져오기 (없으면 Normal)
            string difficulty = GameState.Instance != null ? GameState.Instance.Difficulty : "Normal";
            
            // 파이썬이 기다리는 JSON 형식: {"type": "start", "difficulty": "Hard"}
            string json = $"{{\"type\": \"start\", \"difficulty\": \"{difficulty}\"}}";
            
            SendData(json);
            Debug.Log($"[UDP Send] 🚀 파이썬에게 시작 신호 전송 완료: {json}");
        }

        // 종료 신호 보내기 (앱 종료 시 카메라 끄기용)
        public void SendStopSignal()
        {
            string json = "{\"type\": \"stop\"}";
            SendData(json);
            Debug.Log("[UDP Send] 🛑 파이썬에게 종료 신호 전송");
        }

        private void SendData(string message)
        {
            try
            {
                if (string.IsNullOrEmpty(pcIpAddress))
                {
                    Debug.LogWarning("[UDP Sender] ⚠️ PC IP 주소가 비어있습니다! 인스펙터에서 확인해주세요.");
                    return;
                }

                byte[] data = Encoding.UTF8.GetBytes(message);
                
                // 지정된 IP와 포트로 패킷 전송
                udpClient.Send(data, data.Length, pcIpAddress, pcPort);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UDP Send Error] 전송 실패: {e.Message}");
            }
        }

        void OnApplicationQuit()
        {
            SendStopSignal(); // 앱 종료 시 파이썬도 대기 모드로 돌아가게 신호 전송
            if (udpClient != null) 
            {
                udpClient.Close();
                udpClient = null;
            }
        }
    }
}