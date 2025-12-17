# 🏋️ Health-On-Fit VR (헬스온핏 VR)

## 1. 프로젝트 개요

Health-On-Fit VR은 실시간 자세 교정 VR 홈트레이닝 시스템입니다.
Unity(VR 클라이언트)와 Python(AI 서버) 간의 UDP 소켓 통신을 통해 사용자의 자세를 실시간으로 분석하고 시각·청각적 피드백을 제공합니다.

---

## 2. 폴더 구조

```
2025_Capstone_Design/
├─ Assets/                # Unity 핵심 소스 (Scene, Script, Prefab, Model)
│  ├─ Scenes/             # 실행 씬 (01_Intro ~ 07_Result)
│  └─ Scripts/            # 통신, HUD, 판정 로직
├─ Packages/              # Unity 패키지 설정
├─ ProjectSettings/       # 빌드, 레이어, 태그 설정
└─ Python/                # 자세 분석 서버
   ├─ new_final.py
   └─ requirements.txt
```

---

## 3. 필수 실행 환경 (Prerequisites)

본 프로젝트는 네트워크 통신과 VR 빌드 환경이 핵심이므로 아래 조건을 반드시 준수해야 합니다.

* **Unity**: 6000.0.28f1

  * 설치 시 Android Build Support, SDK, JDK 모듈 체크 필수
* **Python**: 3.10.11
* **VR Device**: Meta Quest 3

  * Developer Mode 활성화 필요
* **Network**: PC와 Quest 3가 동일한 핫스팟 네트워크에 연결되어야 함

---

## 4. 빌드 및 실행 방법 (Step-by-Step)

### Step 0. Unity 프로젝트 열기

* Unity Hub 실행 → **Add (추가)** 버튼 클릭
* 폴더 선택 (**중요**)

  * Assets, Packages 폴더가 포함된 **상위 폴더 (Health-On-Fit-VR)** 선택
  * ⚠️ 주의: Assets 폴더 내부로 들어가서 선택하지 마십시오.
* 실행

  * Editor Version **6000.0.28f1** 확인 후 프로젝트 오픈

---

### Step 1. Python 라이브러리 설치 (최초 1회)

빌드 전, Python 필수 라이브러리를 먼저 설치합니다.
Python 폴더에서 터미널을 열고 아래 명령어를 실행하세요.

```bash
pip install -r requirements.txt
```

---

### Step 2. 네트워크 및 IP 설정 (가장 중요)

Unity–Python 간 UDP 통신을 위해 **서로의 IP 주소를 정확히 등록**해야 합니다.

#### 1. 핫스팟 및 방화벽 설정

* **PC 인터넷 연결**
  PC를 모바일 기기(iPhone 등)의 핫스팟에 먼저 연결하여 인터넷을 활성화합니다.
* **PC 모바일 핫스팟 켜기**
  PC 설정에서 핫스팟을 켭니다. (예: SSID `seul_52@@`)
* **Quest 3 연결**
  Quest 3 → Wi-Fi 설정에서 PC가 만든 핫스팟에 연결합니다.
* **Quest IP 확인**
  Quest → Wi-Fi 상세 정보(고급)에서 `192.168.137.xxx` 형태의 IP를 메모합니다.
* **[필수] 방화벽 해제**
  원활한 소켓 통신을 위해 PC의 방화벽을 일시적으로 모두 끕니다. (하단 FAQ 참고)

---

#### 2. IP 주소 교차 등록 (코드 수정)

##### A) Python – Quest IP 등록

`Python/new_final.py` 파일 상단부를 수정합니다.

```python
# Quest 3 IP 주소 입력
QUEST_IP = "192.168.137.xxx"
```

##### B) Unity – PC IP 등록

* **PC IP 확인**
  `cmd` 실행 → `ipconfig` 입력 → 무선 LAN 어댑터 로컬 영역 연결의 **IPv4 주소** 확인
* **Unity 설정**

  * `Assets/Scenes/` 폴더 내의 **모든 씬(01_Intro ~ 07_Result)**에 대해 아래 작업을 반복하거나, 프리팹을 수정하여 적용합니다.
  * Hierarchy → `Workout_HUD_Canvas` (또는 통신 매니저) 선택
  * Inspector → Script 컴포넌트의 **PC IP Address** 필드에 위에서 확인한 PC IP 입력

---

### Step 3. Unity Android 빌드

* Unity 메뉴 → **File → Build Settings**
* **Scenes In Build** 목록에 아래 씬들이 모두 체크되어 있는지 확인

```
01_Intro
02_Profile
03_TrainerIntro
04_Workout_Beginner
05_Workout_Intermediate
06_Workout_Advanced
07_Result
```

* (목록에 없다면 해당 씬을 열고 **Add Open Scenes**로 추가)
* Platform: **Android** 확인
* Quest 3를 PC에 연결한 상태에서 **Build And Run** 클릭
* 빌드 완료 후 Quest 3에서 앱 자동 실행

---

### Step 4. Python 서버 실행

Unity 빌드가 완료되어 앱이 실행되기 전, Python 폴더에서 서버를 실행하여 대기합니다.

```bash
python new_final.py
```

터미널에 아래 메시지가 출력되면 준비 완료입니다.

```
📡 PC 대기 중...
```

---

### Step 5. VR 시작

* Quest 3에서 앱이 자동 실행된 것을 확인합니다.
* 메인 화면에서 조이스틱으로 이동하여 패널 앞 발판 위에 서면 자동 시작됩니다.
* 오작동 방지를 위해 씬 로드 후 약 **2초 뒤부터** 발판 인식이 활성화됩니다.

---

## 5. FAQ

### Q. 앱은 실행되지만 운동(통신)이 시작되지 않습니다.

99% 확률로 네트워크 문제입니다. 아래 세 가지를 반드시 확인하세요.

1. **PC 방화벽 완전 해제 (필수)**

   * 제어판 → 시스템 및 보안 → Windows Defender 방화벽
   * Windows Defender 방화벽 설정 또는 해제
   * 개인 네트워크 / 공용 네트워크를 모두 **사용 안 함**으로 변경
   * (평가 종료 후 다시 켜는 것을 권장합니다.)

2. **IP 주소 오기입 여부**

   * Python 파일의 `QUEST_IP`
   * Unity Inspector의 **PC IP Address**
   * 두 값이 서로 정확히 교차 등록되었는지 확인

3. **네트워크 일치 여부**

   * PC와 Quest 3가 서로 다른 Wi-Fi에 연결되어 있지 않은지 확인
   * 반드시 동일한 네트워크(핫스팟) 사용

---

### Q. 방화벽을 껐는데도 통신이 되지 않습니다.

Windows 방화벽에서 **python.exe가 차단된 상태**일 수 있습니다.
이는 최초 실행 시 뜨는 "액세스 허용" 팝업에서 **취소**를 눌렀을 경우 발생합니다.

#### 파이썬 예외(Python Exception) 확인 방법

1. Windows 검색창에 **"방화벽에서 앱 허용"** 입력 후 실행
2. 목록에서 `python` 또는 `python.exe` 항목 확인
3. 해당 항목의 **[개인]**, **[공용]** 체크박스가 모두 체크되어 있는지 확인
4. 체크가 되어 있지 않다면

   * 상단의 **[설정 변경]** 클릭
   * [개인], [공용] 모두 체크 후 **확인**

> ⚠️ 이 설정이 되어 있지 않으면 방화벽을 꺼도 UDP 통신이 정상적으로 이루어지지 않습니다.


---

### Q. Unity 패키지 오류가 발생합니다.

* 프로젝트 최초 실행 시 패키지 자동 다운로드(Resolving Packages)가 진행됩니다.
* 인터넷 연결을 유지한 채 잠시 대기하면 자동으로 해결됩니다.
