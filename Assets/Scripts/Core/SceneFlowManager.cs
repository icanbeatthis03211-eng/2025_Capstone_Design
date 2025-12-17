using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    public class SceneFlowManager : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool showLogs = true;

        // --- 기존 함수들 (유지) ---
        
        public void LoadNextScene()
        {
            int nextIdx = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextIdx < SceneManager.sceneCountInBuildSettings) SceneManager.LoadScene(nextIdx);
        }

        public void LoadSceneByName(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        // --- 🔥 [NEW] 난이도별 씬 라우팅 함수 (여기가 핵심) ---

        /// <summary>
        /// GameState의 Difficulty 설정을 확인하여, 적절한 Workout 씬으로 이동합니다.
        /// (TrainerIntro 씬이 끝날 때 호출하세요)
        /// </summary>
        public void LoadWorkoutSceneByDifficulty()
        {
            string difficulty = GameState.Instance.Difficulty;
            string targetScene = "";

            switch (difficulty)
            {
                case "Easy":
                    targetScene = "04_Workout_Beginner";
                    break;
                case "Hard":
                    targetScene = "06_Workout_Advanced";
                    break;
                case "Normal":
                default:
                    targetScene = "05_Workout_Intermediate";
                    break;
            }

            if (showLogs) Debug.Log($"[SceneFlow] 난이도 '{difficulty}'에 맞춰 '{targetScene}'으로 이동합니다.");
            SceneManager.LoadScene(targetScene);
        }

        public void LoadIntroScene()
        {
            SceneManager.LoadScene(0);
        }
        
        // (QuitApplication 등 기존 코드 유지...)
    }
}