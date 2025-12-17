using UnityEngine;
using System.Diagnostics;
using System.IO;
using Core;

namespace Logic
{
    public class PythonLauncher : MonoBehaviour
    {
        [Header("Python Path Settings")]
        public string pythonExePath = @"C:\Users\SEUL\AppData\Local\Programs\Python\Python310\python.exe";
        public string scriptFolderPath = @"C:\Users\SEUL\Documents\1.0.0_Capstone_Design";

        [Header("File Names")]
        public string scriptBeginner = "Final_Beginner.py";
        public string scriptInter = "Final_Inter.py";
        public string scriptAdvanced = "Final_advanced.py";

        private Process _pythonProcess;

        void Start()
        {
            // ★ 핵심: 유니티 에디터(PC)에서만 실행되도록 설정
#if UNITY_EDITOR
            string difficulty = GameState.Instance != null ? GameState.Instance.Difficulty : "Normal";
            string scriptToRun = scriptInter;

            switch (difficulty)
            {
                case "Easy": scriptToRun = scriptBeginner; break;
                case "Hard": scriptToRun = scriptAdvanced; break;
                case "Normal": default: scriptToRun = scriptInter; break;
            }

            UnityEngine.Debug.Log($"[PythonLauncher] (PC 모드) 파이썬 자동 실행 준비: {scriptToRun}");
            RunPythonScript(scriptToRun);
#else
            // 퀘스트(빌드된 앱)에서는 아무것도 하지 않음 (로그만 남김)
            UnityEngine.Debug.Log("[PythonLauncher] 퀘스트(Android) 환경이므로 파이썬을 자동 실행하지 않습니다.");
#endif
        }

        void RunPythonScript(string fileName)
        {
#if UNITY_EDITOR
            string fullScriptPath = Path.Combine(scriptFolderPath, fileName);
            string winPythonPath = pythonExePath.Replace("/", "\\");
            string winScriptPath = fullScriptPath.Replace("/", "\\");

            if (!File.Exists(fullScriptPath)) return;

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = $"/K \"\"{winPythonPath}\" \"{winScriptPath}\"\"";
                startInfo.UseShellExecute = true; 
                startInfo.CreateNoWindow = false; 
                startInfo.WindowStyle = ProcessWindowStyle.Normal;

                _pythonProcess = Process.Start(startInfo);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"실행 실패: {e.Message}");
            }
#endif
        }

        void OnDestroy() { KillPythonProcess(); }
        void OnApplicationQuit() { KillPythonProcess(); }

        private void KillPythonProcess()
        {
#if UNITY_EDITOR
            if (_pythonProcess != null && !_pythonProcess.HasExited)
            {
                try { _pythonProcess.Kill(); _pythonProcess.Dispose(); } catch {}
            }
#endif
        }
    }
}