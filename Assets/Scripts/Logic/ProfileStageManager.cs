using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using Core;

namespace Logic
{
    public class ProfileStageManager : MonoBehaviour
    {
        [Header("--- 1. 바닥 발판 그룹 (Floor Zones) ---")]
        public GameObject zoneGroupStep1;   // 성별 발판들
        public GameObject zoneGroupStep2;   // 코치 발판들
        public GameObject zoneGroupStep3;   // 난이도 발판들
        public GameObject zoneGroupStep4;   // START 발판

        [Header("--- 2. 벽면/공중 UI 그룹 (UI Groups) ---")]
        // ★ [수정 포인트] 단계별 UI를 모두 여기서 관리합니다.
        public GameObject uiGroupStep1;     // "나의 성별을 선택하세요"
        public GameObject uiGroupStep2;     // "코치 성별을 선택하세요"
        public GameObject uiGroupStep3;     // "난이도를 선택하세요"
        public GameObject uiGroupStep4;     // 🔥 [NEW] "시작하기" 패널 (Start Zone 위에 뜸)

        [Header("--- 3. UI 이미지 (선택 시 색깔 바꿈용) ---")]
        public Image imgUserMale;
        public Image imgUserFemale;
        public Image imgCoachMale;
        public Image imgCoachFemale;
        public Image imgLevelEasy;
        public Image imgLevelNormal;
        public Image imgLevelHard;

        [Header("--- 4. 스프라이트 에셋 (Sprite Assets) ---")]
        [Space(10)]
        public Sprite spriteUserMale_Normal;
        public Sprite spriteUserMale_Selected;
        public Sprite spriteUserFemale_Normal;
        public Sprite spriteUserFemale_Selected;

        [Space(10)]
        public Sprite spriteCoachMale_Normal;
        public Sprite spriteCoachMale_Selected;
        public Sprite spriteCoachFemale_Normal;
        public Sprite spriteCoachFemale_Selected;

        [Space(10)]
        public Sprite spriteLevelEasy_Normal;
        public Sprite spriteLevelEasy_Selected;
        public Sprite spriteLevelNormal_Normal;
        public Sprite spriteLevelNormal_Selected;
        public Sprite spriteLevelHard_Normal;
        public Sprite spriteLevelHard_Selected;

        [Header("--- 5. 설정 ---")]
        public float nextStepDelay = 2.0f;

        private bool _isBusy = false;

        void Start()
        {
            // 초기화: 1단계만 켜고 나머지는 싹 끈다
            SetStageActive(1);
            ResetAllSprites();
        }

        // --- [이벤트 연결용 함수] ---

        public void OnSelectUser(string gender)
        {
            if (_isBusy) return;
            StartCoroutine(ProcessStep1(gender));
        }

        public void OnSelectCoach(string gender)
        {
            if (_isBusy) return;
            StartCoroutine(ProcessStep2(gender));
        }

        public void OnSelectLevel(string level)
        {
            if (_isBusy) return;
            StartCoroutine(ProcessStep3(level));
        }

        public void OnClickStart()
        {
            // 시작 버튼 누르면 트레이너 소개 씬으로 이동
            SceneManager.LoadScene("03_TrainerIntro");
        }

        // --- [내부 로직] ---

        // ★ 스테이지 교체 함수 (UI와 발판을 동시에 껐다 켬)
        void SetStageActive(int step)
        {
            // 1. 발판(Zone) 제어 - 해당 단계만 켜고 나머지 끔
            if(zoneGroupStep1) zoneGroupStep1.SetActive(step == 1);
            if(zoneGroupStep2) zoneGroupStep2.SetActive(step == 2);
            if(zoneGroupStep3) zoneGroupStep3.SetActive(step == 3);
            if(zoneGroupStep4) zoneGroupStep4.SetActive(step == 4);

            // 2. UI 패널 제어 - 해당 단계만 켜고 나머지 끔
            if(uiGroupStep1) uiGroupStep1.SetActive(step == 1);
            if(uiGroupStep2) uiGroupStep2.SetActive(step == 2);
            if(uiGroupStep3) uiGroupStep3.SetActive(step == 3);
            
            // 🔥 [NEW] 4단계(시작) UI 패널 제어 추가
            if(uiGroupStep4) uiGroupStep4.SetActive(step == 4);
        }

        void ResetAllSprites()
        {
            if(imgUserMale) imgUserMale.sprite = spriteUserMale_Normal;
            if(imgUserFemale) imgUserFemale.sprite = spriteUserFemale_Normal;
            if(imgCoachMale) imgCoachMale.sprite = spriteCoachMale_Normal;
            if(imgCoachFemale) imgCoachFemale.sprite = spriteCoachFemale_Normal;
            if(imgLevelEasy) imgLevelEasy.sprite = spriteLevelEasy_Normal;
            if(imgLevelNormal) imgLevelNormal.sprite = spriteLevelNormal_Normal;
            if(imgLevelHard) imgLevelHard.sprite = spriteLevelHard_Normal;
        }

        IEnumerator ProcessStep1(string gender)
        {
            _isBusy = true;

            // 데이터 저장
            if (GameState.Instance == null) new GameObject("GameState_Temp").AddComponent<GameState>();
            GameState.Instance.UserGender = gender;

            // 이미지 교체
            bool isMale = (gender == "Male");
            imgUserMale.sprite = isMale ? spriteUserMale_Selected : spriteUserMale_Normal;
            imgUserFemale.sprite = !isMale ? spriteUserFemale_Selected : spriteUserFemale_Normal;

            yield return new WaitForSeconds(nextStepDelay);
            
            ResetPlayerPosition(); // 🔥 [NEW] 플레이어 위치 초기화
            
            // 2단계로 전환
            SetStageActive(2);

            _isBusy = false;
        }

        IEnumerator ProcessStep2(string gender)
        {
            _isBusy = true;

            GameState.Instance.CoachGender = gender;

            bool isMale = (gender == "Male");
            imgCoachMale.sprite = isMale ? spriteCoachMale_Selected : spriteCoachMale_Normal;
            imgCoachFemale.sprite = !isMale ? spriteCoachFemale_Selected : spriteCoachFemale_Normal;

            yield return new WaitForSeconds(nextStepDelay);

            ResetPlayerPosition(); // 🔥 [NEW] 플레이어 위치 초기화

            // 3단계로 전환
            SetStageActive(3);

            _isBusy = false;
        }

        IEnumerator ProcessStep3(string level)
        {
            _isBusy = true;

            GameState.Instance.Difficulty = level;
            GameState.Instance.ResetSessionData();

            imgLevelEasy.sprite = (level == "Easy") ? spriteLevelEasy_Selected : spriteLevelEasy_Normal;
            imgLevelNormal.sprite = (level == "Normal") ? spriteLevelNormal_Selected : spriteLevelNormal_Normal;
            imgLevelHard.sprite = (level == "Hard") ? spriteLevelHard_Selected : spriteLevelHard_Normal;

            yield return new WaitForSeconds(nextStepDelay);

            ResetPlayerPosition(); // 🔥 [NEW] 플레이어 위치 초기화

            // 4단계(START)로 전환 -> 이때 uiGroupStep4도 같이 켜짐
            SetStageActive(4);

            _isBusy = false;
        }

        // 🔥 [NEW] 플레이어 위치를 리셋하는 함수
        void ResetPlayerPosition()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                // XR Rig을 사용하는 경우, Rig 자체를 옮겨야 합니다.
                // 플레이어의 최상위 오브젝트를 찾아서 위치를 변경하는 것이 더 안정적일 수 있습니다.
                // 여기서는 간단히 찾은 'Player' 태그 오브젝트의 위치를 변경합니다.
                // player.transform.position = Vector3.zero;
                Debug.Log("[Profile] 플레이어 위치를 (0, 0, 0)으로 초기화했습니다.");
            }
            else
            {
                Debug.LogWarning("[Profile] 'Player' 태그를 가진 오브젝트를 찾지 못했습니다!");
            }
        }
    }
}