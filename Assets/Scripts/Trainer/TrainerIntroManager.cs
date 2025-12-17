using UnityEngine;
using UnityEngine.Video; // 비디오 재생용
using Core; // GameState 접근용

namespace Logic
{
    public class TrainerIntroManager : MonoBehaviour
    {
        [Header("Video Player Component")]
        public VideoPlayer hologramVideoPlayer;

        [Header("Coach Video Clips")]
        public VideoClip maleCoachClip;   // 남성 코치 영상 파일
        public VideoClip femaleCoachClip; // 여성 코치 영상 파일

        [Header("Settings")]
        [Tooltip("영상이 끝나면 자동으로 운동 씬으로 넘어갈까요?")]
        public bool autoSkipWhenFinished = true;

        void Start()
        {
            // 1. GameState에서 선택된 코치 성별 가져오기
            string selectedGender = GameState.Instance.CoachGender;
            Debug.Log($"[TrainerIntro] 선택된 코치: {selectedGender}");

            // 2. 성별에 따라 비디오 클립 교체
            if (selectedGender == "Male")
            {
                hologramVideoPlayer.clip = maleCoachClip;
            }
            else
            {
                // Female 또는 기본값
                hologramVideoPlayer.clip = femaleCoachClip;
            }

            // 3. 비디오 재생 시작
            hologramVideoPlayer.Play();

            // 4. 영상 종료 이벤트 연결 (영상이 끝나면 운동 씬으로 이동)
            if (autoSkipWhenFinished)
            {
                hologramVideoPlayer.loopPointReached += OnVideoFinished;
            }
        }

        // 영상이 끝났을 때 호출되는 함수
        void OnVideoFinished(VideoPlayer vp)
        {
            Debug.Log("[TrainerIntro] 영상 종료. 운동 씬으로 이동합니다.");
            
            // 아까 만들어둔 SceneFlowManager의 '난이도별 이동' 함수 호출
            // (씬에 SceneFlowManager가 없으면 찾아서 호출)
            var flowManager = FindAnyObjectByType<SceneFlowManager>();
            if (flowManager != null)
            {
                flowManager.LoadWorkoutSceneByDifficulty();
            }
            else
            {
                Debug.LogError("SceneFlowManager를 찾을 수 없습니다! 씬에 추가해주세요.");
            }
        }
    }
}