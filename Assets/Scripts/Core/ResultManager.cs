using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;
using Core;

namespace Logic
{
    public class ResultManager : MonoBehaviour
    {
        [Header("KPI")]
        [SerializeField] private TextMeshProUGUI txtRepsValue;   // 큰 숫자 (count)
        [SerializeField] private TextMeshProUGUI txtRepsUnit;    // reps / 회
        [SerializeField] private TextMeshProUGUI txtTime;        // 00:46 (시간 표시)
        [SerializeField] private TextMeshProUGUI txtKcal;        // 12.3 kcal

        [Header("Rating (1~5)")]
        [SerializeField] private Image[] starImages;             // 5개, 순서대로
        [SerializeField] private Sprite spriteStarFull;
        [SerializeField] private Sprite spriteStarEmpty;
        [SerializeField] private TextMeshProUGUI txtRatingValue; // "4 / 5" (선택)

        [Header("Feedback (3 lines fixed)")]
        [SerializeField] private TextMeshProUGUI txtLine1;
        [SerializeField] private TextMeshProUGUI txtLine2;
        [SerializeField] private TextMeshProUGUI txtLine3;

        [Header("Audio")]
        [SerializeField] private AudioClip sfxFanfare;

        private void Start()
        {
            // 결과 패킷이 늦게 도착하는 경우(씬 전환 직후) 대비: 잠깐 기다렸다가 표시
            StartCoroutine(WaitThenShow());

            if (sfxFanfare != null && Camera.main != null)
                AudioSource.PlayClipAtPoint(sfxFanfare, Camera.main.transform.position, 1.0f);
        }

        private IEnumerator WaitThenShow()
        {
            // 최대 1.5초 정도만 “데이터 들어올 시간” 주고 갱신
            float t = 0f;
            while (t < 1.5f)
            {
                if (GameState.Instance != null)
                {
                    // 점수/칼로리/피드백 중 하나라도 들어오면 바로 렌더
                    if (GameState.Instance.LastSessionScore > 0f ||
                        GameState.Instance.LastCalories > 0f ||
                        !string.IsNullOrEmpty(GameState.Instance.LastAiFeedback))
                        break;
                }
                t += Time.deltaTime;
                yield return null;
            }

            ShowResultData();
        }

        private void ShowResultData()
        {
            if (GameState.Instance == null)
            {
                Debug.LogWarning("[Result] GameState 없음");
                return;
            }

            int count = GameState.Instance.SquatCount;
            float score = GameState.Instance.LastSessionScore;    // 보통 0~5 스케일로 들어오는 구조
            float kcal = GameState.Instance.LastCalories;
            string feedbackRaw = GameState.Instance.LastAiFeedback;

            // 1) KPI
            if (txtRepsValue) txtRepsValue.text = count.ToString();
            if (txtRepsUnit && string.IsNullOrEmpty(txtRepsUnit.text)) txtRepsUnit.text = "reps";

            if (txtKcal) txtKcal.text = $"{kcal:F1} kcal";

            // ★★★ [수정됨] 시간 데이터 연동 부분 ★★★
            // GameState.cs에 있는 public float SessionTime을 가져옵니다.
            float duration = GameState.Instance.SessionTime; 
            
            int m = Mathf.FloorToInt(duration / 60f);
            int s = Mathf.FloorToInt(duration % 60f);

            if (txtTime) txtTime.text = $"{m:00}:{s:00}";
            // ★★★★★★★★★★★★★★★★★★★★★★★★★★

            // 2) Rating: 무조건 1~5 정수
            int rating = Mathf.Clamp(Mathf.RoundToInt(score), 1, 5);
            ApplyStars(rating);

            if (txtRatingValue) txtRatingValue.text = $"{rating} / 5";

            // 3) Feedback: 항상 3줄 템플릿
            var lines = BuildThreeLineFeedback(count, rating, feedbackRaw);
            if (txtLine1) txtLine1.text = lines.line1;
            if (txtLine2) txtLine2.text = lines.line2;
            if (txtLine3) txtLine3.text = lines.line3;

            Debug.Log("[Result] 결과 UI 업데이트 완료");
        }

        private void ApplyStars(int rating)
        {
            if (starImages == null || starImages.Length == 0) return;

            // 1~5 정수 보장
            rating = Mathf.Clamp(rating, 1, 5);

            for (int i = 0; i < starImages.Length; i++)
            {
                var img = starImages[i];
                if (!img) continue;

                // 항상 같은 full 스프라이트만 사용
                if (spriteStarFull != null)
                    img.sprite = spriteStarFull;

                bool on = (i < rating);

                // EMPTY 스프라이트 없이 알파로 "빈 별" 표현
                var c = img.color;
                c.a = on ? 1f : 0.18f;          // 빈 별 투명도(0.15~0.30 취향)
                img.color = c;

                // (선택) 빈 별을 살짝 작게 => 더 '빠진' 느낌
                img.rectTransform.localScale = on ? Vector3.one : Vector3.one * 0.92f;
            }
        }

        private (string line1, string line2, string line3) BuildThreeLineFeedback(int count, int rating, string feedbackRaw)
        {
            // (A) 칭찬 라인: rating 기반
            string line1 =
                rating >= 5 ? "폼이 완벽에 가까웠어요." :
                rating >= 4 ? "리듬이 안정적이었어요." :
                rating >= 3 ? "꾸준히 따라온 게 아주 좋아요." :
                              "시작한 것 자체가 이미 성과예요.";

            // (B) 개선 포커스 1개만: feedbackRaw에서 키워드로 “한 가지만” 잡기
            string focus = PickFocusFromFeedback(feedbackRaw); // "깊이" / "무릎" / "상체"
            string line2 =
                focus == "무릎" ? "무릎이 안쪽으로 모인 순간이 있었어요." :
                focus == "상체" ? "상체가 앞으로 쏠리지 않게 가슴을 살짝 열어봐요." :
                                  "깊이를 한 단계만 더 내려가면 더 좋아져요.";

            // (C) 다음 목표: 숫자 포함(고정 규칙)
            string line3 =
                focus == "깊이" ? "다음엔 ‘깊이 목표’ 5회 연속 도전!" :
                focus == "무릎" ? "다음엔 ‘무릎 정렬’ 5회 연속 도전!" :
                                  "다음엔 ‘상체 세우기’ 5회 연속 도전!";

            // count가 너무 작으면 목표를 “세션 목표”로 바꾸는 것도 자연스러움
            if (count < 5)
                line3 = "다음엔 5회 달성 도전!";

            return (line1, line2, line3);
        }

        private string PickFocusFromFeedback(string feedbackRaw)
        {
            if (string.IsNullOrEmpty(feedbackRaw)) return "깊이";

            // 가장 단순하고 튼튼한 방식: 키워드 우선순위
            if (feedbackRaw.Contains("무릎")) return "무릎";
            if (feedbackRaw.Contains("허리") || feedbackRaw.Contains("상체")) return "상체";
            if (feedbackRaw.Contains("깊이") || feedbackRaw.Contains("더 내려")) return "깊이";
            return "깊이";
        }

        // 기존 ZoneTrigger 연동 유지
        public void OnHomeZoneEnter()
        {
            Debug.Log("🏠 메인으로 복귀");
            if (GameState.Instance != null) GameState.Instance.ResetSessionData();
            SceneManager.LoadScene("02_Profile");
        }

        // (옵션) UI 버튼으로도 쓰고 싶으면 연결
        public void OnRetry()
        {
            Debug.Log("🔁 다시 하기");
            SceneManager.LoadScene("06_Workout"); // 너희 실제 씬 이름으로 교체
        }
    }
}