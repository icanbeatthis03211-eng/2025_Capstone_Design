using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    /// <summary>
    /// 씬 전환과 게임 상태(난이도)에 따라 배경음악(BGM)을 자동으로 관리하는 매니저입니다.
    /// Start()에서 초기 실행을 보장하여 앱 시작 시 BGM 누락을 방지합니다.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class GlobalAudioManager : MonoBehaviour
    {
        public static GlobalAudioManager Instance;

        [Header("Audio Source")]
        [SerializeField] private AudioSource bgmPlayer;

        [Header("Scene BGM List")]
        public AudioClip introBGM;        // 01_Intro
        public AudioClip profileBGM;      // 02_Profile
        public AudioClip trainerIntroBGM; // 03_TrainerIntro (NEW! 홀로그램 등장용)
        public AudioClip resultBGM;       // 05_Result

        [Header("Workout BGM by Difficulty")]
        public AudioClip workoutEasy;     // 04_Workout (초급/헬스장)
        public AudioClip workoutNormal;   // 04_Workout (중급/연습실)
        public AudioClip workoutHard;     // 04_Workout (상급/무대)

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // 씬 이동해도 파괴 안 됨
                
                // AudioSource 컴포넌트 가져오기 (없으면 추가)
                bgmPlayer = GetComponent<AudioSource>();
                if (bgmPlayer == null) bgmPlayer = gameObject.AddComponent<AudioSource>();
                
                bgmPlayer.loop = true; // BGM은 무한 반복
                bgmPlayer.playOnAwake = false;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void OnEnable()
        {
            // 씬이 로드될 때마다 호출될 이벤트 등록
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // ✅ 앱 최초 실행 시, 현재 씬(Intro)의 BGM을 강제로 재생 (안전장치)
        void Start()
        {
            PlayBGMByScene(SceneManager.GetActiveScene().name);
        }

        // 씬 로딩이 끝날 때마다 자동으로 실행되는 함수
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PlayBGMByScene(scene.name);
        }

        /// <summary>
        /// 씬 이름과 난이도를 분석해서 적절한 음악을 틉니다.
        /// </summary>
        private void PlayBGMByScene(string sceneName)
        {
            AudioClip targetClip = null;

            // 1. 씬 이름에 따라 BGM 선정
            switch (sceneName)
            {
                case "01_Intro":
                    targetClip = introBGM;
                    break;
                case "02_Profile":
                    targetClip = profileBGM;
                    break;
                case "03_TrainerIntro":
                    // ✅ 트레이너 등장 씬: 전용 BGM 재생 (임팩트!)
                    targetClip = trainerIntroBGM; 
                    break;
                case "04_Workout":
                    // ✅ 운동 씬: 난이도별 BGM 선택
                    targetClip = GetWorkoutBGM();
                    break;
                case "05_Result":
                    targetClip = resultBGM;
                    break;
            }

            // 3. 음악 재생 (이미 같은 음악이 나오고 있으면 다시 틀지 않음)
            if (targetClip != null)
            {
                if (bgmPlayer.clip != targetClip)
                {
                    bgmPlayer.clip = targetClip;
                    bgmPlayer.Play();
                }
            }
        }

        /// <summary>
        /// GameState의 난이도 설정을 확인하여 해당 BGM을 반환합니다.
        /// </summary>
        private AudioClip GetWorkoutBGM()
        {
            // GameState가 아직 없거나 설정이 안됐을 경우 대비
            if (GameState.Instance == null) return workoutNormal;

            switch (GameState.Instance.Difficulty)
            {
                case "Easy": return workoutEasy;
                case "Hard": return workoutHard;
                default: return workoutNormal; // "Normal" 포함
            }
        }
    }
}