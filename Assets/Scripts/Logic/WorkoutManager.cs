using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Core;

namespace Logic
{
    public class WorkoutManager : MonoBehaviour
    {
        // =========================================================
        // 0) Policy
        // =========================================================
        [Header("0) Policy")]
        [Tooltip("데이터가 끊겼을 때(초). 텍스트만 TRACKING…으로 변경 (아이콘/게이지는 마지막 상태 유지)")]
        [SerializeField] private float dataTimeoutSec = 0.8f;

        // =========================================================
        // 1) HUD Layout
        // =========================================================
        [Header("1) HUD Layout")]
        public RectTransform hudCanvasRect;
        [SerializeField] private Camera rigCamera;

        // =========================================================
        // 2) Depth Zone
        // =========================================================
        [Header("2) Depth Zone")]
        public Image barDepthFill;                 // Zone_Depth/Bar_Fill
        [SerializeField] private RectTransform depthBarAreaRect; // Zone_Depth/Bar_BG
        [SerializeField] private RectTransform targetLineRect;   // Zone_Depth/Line_Target

        [Range(0f, 1f)]
        [SerializeField] private float depthTarget = 0.5f;

        [Tooltip("Depth Fill이 아래→위면 true, 위→아래면 false")]
        [SerializeField] private bool depthFillFromBottom = true;

        [Range(0f, 0.5f)]
        [SerializeField] private float depthUiSmoothing = 0.18f;

        [SerializeField] private Color colorDepthGood = new Color(0, 1, 1, 1);
        [SerializeField] private Color colorDepthBad  = new Color(1, 1, 1, 0.5f);

        // =========================================================
        // 3) Safety Zone (아이콘 스왑만)
        // =========================================================
        [Header("3) Safety Zone - Icons Only")]
        public Image imgKneeIcon;   // Group_Knee/Img_KneeIcon
        public Image imgSpineIcon;  // Group_Spine/Img_SpineIcon

        public Sprite spriteKneeOk;    // icon_knee_ok.png
        public Sprite spriteKneeWarn;  // icon_knee_warn.png
        public Sprite spriteSpineOk;   // icon_spine_ok.png
        public Sprite spriteSpineWarn; // icon_spine_warn.png

        // =========================================================
        // 4) Mission Zone (Count / Hold 링 2겹)
        // =========================================================
        [Header("4) Mission Zone")]
        public Image circleCountFill;  // Zone_Mission/Circle_Count_Fill
        public Image circleHoldFill;   // Zone_Mission/Circle_Hold_Fill
        [SerializeField] private bool ringClockwise = true;

        public TextMeshProUGUI txtCount;      // Zone_Mission/Txt_Count
        public TextMeshProUGUI txtTarget;     // Zone_Mission/Txt_Target
        public TextMeshProUGUI txtHoldTimer;  // Zone_Mission/Txt_HoldTimer

        // =========================================================
        // 5) Info Texts
        // =========================================================
        [Header("5) Info Texts")]
        public TextMeshProUGUI txtDifficulty; // Zone_InfoTop/Txt_Difficulty
        public TextMeshProUGUI txtTimer;      // Zone_InfoTop/Txt_Timer
        public TextMeshProUGUI txtFeedback;   // Zone_Feedback/Txt_Feedback  (GOOD/HOLD/TRACKING…/COMPLETE)

        // =========================================================
        // 6) Difficulty Settings
        // =========================================================
        [Header("6) Difficulty")]
        public int targetCountEasy = 5;
        public int targetCountNormal = 10;
        public int targetCountHard = 20;

        [Header("6) Hold Time (Python HOLD_TIME와 동일하게)")]
        [SerializeField] private float holdTimeEasy = 0f;
        [SerializeField] private float holdTimeNormal = 2.0f;
        [SerializeField] private float holdTimeHard = 6.0f;

        // =========================================================
        // Runtime State
        // =========================================================
        private int _currentCount = 0;
        private int _targetCount = 10;

        private float _holdTotal = 0f;
        private float _holdRemain = 0f;

        private float _timer = 0f;

        private float _depth01 = 0f;
        private float _depthUi = 0f;

        private bool _kneeGood = true;
        private bool _spineGood = true;

        private float _lastAnyDataTime = -999f;
        private bool _timeoutNoticeShown = false;

        public bool IsFinished { get; private set; } = false;

        // =========================================================
        // Unity
        // =========================================================
        void Start()
        {
            SetupDifficulty();
            ApplyRingPolicy();

            _currentCount = 0;
            _timer = 0f;
            IsFinished = false;

            UpdateCountUI();
            UpdateTimerUI();

            // 기본은 OK
            SetKneeStatus(true);
            SetSpineStatus(true);

            // Depth/ Hold 초기
            SetDepthUI(0f, force: true);
            SetHoldUI(0f);

            // 홀드 요소는 시작 시 숨김(홀드 들어오면 SetHoldTime에서 켜짐)
            if (circleHoldFill) circleHoldFill.gameObject.SetActive(false);
            if (txtHoldTimer) txtHoldTimer.gameObject.SetActive(false);

            // 텍스트는 무조건 미니멀 상태문구로만
            SetFeedbackStateGood();

            AdjustHUDPosition();
            RepositionTargetLine();

            if (CoachingVoiceManager.Instance != null)
                CoachingVoiceManager.Instance.Speak("자, 운동을 시작해볼까요? 화이팅!");
        }

        void Update()
        {
            if (IsFinished) return;

            _timer += Time.deltaTime;
            UpdateTimerUI();

            if (GameState.Instance != null)
                GameState.Instance.AddSessionTime(Time.deltaTime);

            // 데이터 타임아웃 상태 관리
            bool timeout = (Time.time - _lastAnyDataTime > dataTimeoutSec);

            if (timeout && !_timeoutNoticeShown)
            {
                _timeoutNoticeShown = true;
                RecomputeMinimalFeedback(); // TRACKING…로 전환
            }
            else if (!timeout && _timeoutNoticeShown)
            {
                _timeoutNoticeShown = false;
                RecomputeMinimalFeedback(); // HOLD 또는 GOOD로 복귀
            }
        }

        // =========================================================
        // Public API (UDPReceiver에서 호출)
        // =========================================================
        public void AddSquatCount()
        {
            if (IsFinished) return;

            MarkDataArrived();

            _currentCount++;
            if (GameState.Instance != null) GameState.Instance.AddSquat(1);

            UpdateCountUI();
            if (_currentCount >= _targetCount) FinishWorkout();

            RecomputeMinimalFeedback();
        }

        public void SetDepth(float value01)
        {
            if (IsFinished) return;

            MarkDataArrived();

            _depth01 = Mathf.Clamp01(value01);
            SetDepthUI(_depth01, force: false);

            RecomputeMinimalFeedback();
        }

        public void SetKneeStatus(bool isGood)
        {
            if (IsFinished) return;

            MarkDataArrived();

            _kneeGood = isGood;
            if (imgKneeIcon != null)
                imgKneeIcon.sprite = isGood ? spriteKneeOk : spriteKneeWarn;

            RecomputeMinimalFeedback();
        }

        public void SetSpineStatus(bool isGood)
        {
            if (IsFinished) return;

            MarkDataArrived();

            _spineGood = isGood;
            if (imgSpineIcon != null)
                imgSpineIcon.sprite = isGood ? spriteSpineOk : spriteSpineWarn;

            RecomputeMinimalFeedback();
        }

        public void SetHoldTime(float remainSec)
        {
            if (IsFinished) return;

            MarkDataArrived();

            _holdRemain = Mathf.Max(0f, remainSec);
            SetHoldUI(_holdRemain);

            RecomputeMinimalFeedback();
        }

        // 파이썬 텍스트 코칭은 미니멀 모드에서 비활성화
        public void SetFeedbackText(string msg)
        {
            // intentionally empty
        }

        private void MarkDataArrived()
        {
            _lastAnyDataTime = Time.time;
        }

        // =========================================================
        // Depth UI + Target Line
        // =========================================================
        private void SetDepthUI(float value01, bool force)
        {
            if (barDepthFill == null) return;

            if (force) _depthUi = value01;
            else
            {
                float k = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.001f, depthUiSmoothing));
                _depthUi = Mathf.Lerp(_depthUi, value01, k);
            }

            barDepthFill.fillAmount = _depthUi;
            barDepthFill.color = (_depthUi >= depthTarget) ? colorDepthGood : colorDepthBad;
        }

        public void RepositionTargetLine()
        {
            if (depthBarAreaRect == null || targetLineRect == null) return;

            Rect r = depthBarAreaRect.rect;
            float t = Mathf.Clamp01(depthTarget);

            // Fill 방향에 맞춰 목표선 위치를 변환
            float mapped = depthFillFromBottom ? t : (1f - t);
            float y = Mathf.Lerp(r.yMin, r.yMax, mapped);

            Vector2 p = targetLineRect.anchoredPosition;
            p.y = y;
            targetLineRect.anchoredPosition = p;
        }

        // =========================================================
        // Hold UI (2겹 링)
        // =========================================================
        private void SetHoldUI(float remainSec)
        {
            bool hasHold = _holdTotal > 0.01f;
            bool isHolding = hasHold && (remainSec > 0.1f);

            if (circleHoldFill != null)
            {
                circleHoldFill.gameObject.SetActive(isHolding);
                if (isHolding)
                    circleHoldFill.fillAmount = Mathf.Clamp01(remainSec / _holdTotal);
            }

            if (txtHoldTimer != null)
            {
                txtHoldTimer.gameObject.SetActive(isHolding);
                if (isHolding) txtHoldTimer.text = $"{remainSec:F1}s";
            }
        }

        private void ApplyRingPolicy()
        {
            if (circleCountFill != null) circleCountFill.fillClockwise = ringClockwise;
            if (circleHoldFill != null) circleHoldFill.fillClockwise = ringClockwise;
        }

        // =========================================================
        // Minimal Feedback (GOOD / HOLD / TRACKING… / COMPLETE)
        // =========================================================
        private void RecomputeMinimalFeedback()
        {
            if (IsFinished || txtFeedback == null) return;

            if (_timeoutNoticeShown)
            {
                txtFeedback.text = "TRACKING…";
                return;
            }

            bool hasHold = _holdTotal > 0.01f;
            bool isHolding = hasHold && (_holdRemain > 0.1f);

            if (isHolding)
            {
                txtFeedback.text = "HOLD";
                return;
            }

            txtFeedback.text = "GOOD";
        }

        private void SetFeedbackStateGood()
        {
            if (txtFeedback) txtFeedback.text = "GOOD";
        }

        // =========================================================
        // Difficulty / Count / Timer
        // =========================================================
        private void SetupDifficulty()
        {
            string diff = GameState.Instance != null ? GameState.Instance.Difficulty : "Normal";

            switch (diff)
            {
                case "Easy":
                    _targetCount = targetCountEasy;
                    _holdTotal = holdTimeEasy;
                    if (txtDifficulty) { txtDifficulty.text = "BEGINNER"; txtDifficulty.color = Color.green; }
                    break;

                case "Hard":
                    _targetCount = targetCountHard;
                    _holdTotal = holdTimeHard;
                    if (txtDifficulty) { txtDifficulty.text = "ADVANCED"; txtDifficulty.color = Color.red; }
                    break;

                default:
                    _targetCount = targetCountNormal;
                    _holdTotal = holdTimeNormal;
                    if (txtDifficulty) { txtDifficulty.text = "INTERMEDIATE"; txtDifficulty.color = Color.yellow; }
                    break;
            }

            if (txtTarget) txtTarget.text = $"/ {_targetCount}";
        }

        private void UpdateCountUI()
        {
            if (txtCount) txtCount.text = _currentCount.ToString();

            if (circleCountFill != null)
                circleCountFill.fillAmount = (float)_currentCount / Mathf.Max(1, _targetCount);
        }

        private void UpdateTimerUI()
        {
            if (!txtTimer) return;

            int m = Mathf.FloorToInt(_timer / 60f);
            int s = Mathf.FloorToInt(_timer % 60f);
            txtTimer.text = $"{m:00}:{s:00}";
        }

        // =========================================================
        // HUD Position
        // =========================================================
        private void AdjustHUDPosition()
        {
            var cam = rigCamera != null ? rigCamera : (Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>());
            if (cam == null || hudCanvasRect == null) return;

            float eyeHeight = cam.transform.position.y;
            float targetY = Mathf.Clamp(eyeHeight, 1.0f, 1.8f) - 0.15f;

            Vector3 pos = hudCanvasRect.position;
            pos.y = targetY;
            hudCanvasRect.position = pos;
        }

        // =========================================================
        // Finish
        // =========================================================
        private void FinishWorkout()
        {
            if (IsFinished) return;
            IsFinished = true;

            if (CoachingVoiceManager.Instance != null)
                CoachingVoiceManager.Instance.Speak("목표 달성! 정말 고생하셨어요.");

            if (txtFeedback) txtFeedback.text = "COMPLETE";

            var sender = Object.FindFirstObjectByType<Core.UDPSender>();
            if (sender != null)
            {
                sender.SendStopSignal();
                Debug.Log("🛑 운동 종료! 파이썬에게 결과 요청.");
            }

            Invoke(nameof(GoToResultScene), 3.0f);
        }

        private void GoToResultScene()
        {
            SceneManager.LoadScene("07_Result");
        }
    }
}
