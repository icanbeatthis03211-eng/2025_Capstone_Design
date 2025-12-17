using UnityEngine;

namespace Core
{
    /// <summary>
    /// 게임의 전체 생명주기(Lifecycle) 동안 유지되어야 하는 사용자 데이터 및 세션 상태를 관리합니다.
    /// Singleton 패턴을 사용하여 어디서든 접근 가능하며, 씬이 전환되어도 파괴되지 않습니다.
    /// </summary>
    public class GameState : MonoBehaviour
    {
        #region Singleton Pattern
        // 외부에서 접근 가능한 유일한 인스턴스
        public static GameState Instance { get; private set; }

        private void Awake()
        {
            // 인스턴스가 없는 경우 (처음 생성될 때)
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않도록 설정
                Debug.Log("[GameState] 시스템이 초기화되었습니다.");
            }
            // 이미 인스턴스가 존재하는 경우 (씬 이동 후 중복 생성 방지)
            else if (Instance != this)
            {
                Debug.LogWarning($"[GameState] 중복된 인스턴스가 감지되어 '{gameObject.name}'을(를) 파괴합니다.");
                Destroy(gameObject);
            }
        }
        #endregion

        #region User Profile Data (사용자 설정)
        [Header("--- User Profile Setting ---")]
        [Tooltip("사용자의 성별 (Male / Female)")]
        public string UserGender = "Male";

        [Tooltip("코치의 성별 (Male / Female) - 영상 리소스 분기용")]
        public string CoachGender = "Female";

        [Tooltip("운동 난이도 (Easy / Normal / Hard) - 환경 및 로직 분기용")]
        public string Difficulty = "Normal";
        #endregion

        #region Session Runtime Data (현재 운동 세션 데이터)
        [Header("--- Current Session Status ---")]
        [Tooltip("현재 세션에서 수행한 스쿼트 횟수")]
        public int SquatCount = 0;

        [Tooltip("현재 세션 진행 시간 (초 단위)")]
        public float SessionTime = 0f;
        #endregion

        #region Result Data (결과 리포트용 - 파이썬 수신 데이터)
        // ★★★ [추가됨] 결과 화면에 띄울 데이터들 ★★★
        [Header("--- Result Data (From Python) ---")]
        [Tooltip("이번 세션의 평균 점수 (5.0 만점)")]
        public float LastSessionScore = 0f;

        [Tooltip("이번 세션 소모 칼로리")]
        public float LastCalories = 0f;

        [Tooltip("AI 코치의 피드백 텍스트")]
        [TextArea(3, 10)] // 인스펙터에서 여러 줄로 보이게 설정
        public string LastAiFeedback = "";
        #endregion

        #region State Modification Methods (상태 변경 헬퍼)
        
        /// <summary>
        /// 스쿼트 횟수를 안전하게 증가시킵니다.
        /// </summary>
        /// <param name="amount">증가량 (기본 1)</param>
        public void AddSquat(int amount = 1)
        {
            SquatCount += amount;
        }

        /// <summary>
        /// 세션 시간을 누적합니다. (Update 문에서 호출)
        /// </summary>
        /// <param name="delta">Time.deltaTime</param>
        public void AddSessionTime(float delta)
        {
            SessionTime += delta;
        }

        #endregion

        /// <summary>
        /// 새로운 운동 세션을 시작하기 전, 이전 기록을 초기화합니다.
        /// (Intro -> Workout, 또는 Result -> Replay 시 호출)
        /// </summary>
        public void ResetSessionData()
        {
            // 실시간 데이터 초기화
            SquatCount = 0;
            SessionTime = 0f;

            // 결과 데이터 초기화 (이전 기록 남지 않게)
            LastSessionScore = 0f;
            LastCalories = 0f;
            LastAiFeedback = "";

            Debug.Log($"[GameState] 세션 데이터 리셋 완료. (현재 난이도: {Difficulty})");
        }

        /// <summary>
        /// 현재 설정된 상태를 콘솔에 출력합니다. (디버깅용)
        /// </summary>
        public void PrintCurrentState()
        {
            Debug.Log($"[Status] User: {UserGender} | Coach: {CoachGender} | Level: {Difficulty} | Squats: {SquatCount}");
        }
    }
}