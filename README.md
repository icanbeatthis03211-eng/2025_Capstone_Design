# Health-On-Fit VR (헬스온핏 VR)

## 1. 프로젝트 개요

**실시간 자세 교정 VR 홈트레이닝 시스템**입니다.
Unity(VR 클라이언트)와 Python(AI 서버) 간의 UDP 소켓 통신을 통해
사용자의 자세를 실시간으로 분석하고 **시각·청각적 피드백**을 제공합니다.

---

## 폴더 구조 설명

* **Assets/**: 프로젝트 핵심 소스코드, 씬(Scene), 프리팹, 모델링 데이터

  * `Assets/Scenes/`: 실행에 필요한 씬 파일 (`StartScene`, `WorkoutScene` 등)
  * `Assets/Scripts/`: 통신 및 로직 스크립트
* **Packages/**: Unity 필수 패키지 명세서 (프로젝트 실행 시 자동 설치)
* **ProjectSettings/**: VR 빌드, 레이어, 태그 설정 파일
* **Python/**: AI 자세 분석 서버 (`new_final.py`, `requirements.txt`)

---

## 2. 필수 실행 환경 (Prerequisites)

**[중요]** 본 프로젝트는 네트워크 통신이 핵심이므로, 아래 환경 버전을 반드시 준수해야 합니다.

* **Unity**: 6000.0.28f1 (버전 불일치 시 스크립트 오류 발생 가능)
* **Python**: 3.10.11
* **VR Device**: Meta Quest 3 (Developer Mode 활성화 필수)
* **Network**: PC와 VR 기기가 동일한 핫스팟 네트워크에 연결되어야 함

---

## 3. 네트워크 및 IP 설정

서로의 IP 주소를 코드에 등록해야 통신이 가능합니다.
아래 순서를 **정확히 그대로** 따라주세요.

### Step 1. 핫스팟 구성 (네트워크 통일)

1. **인터넷 연결**
   PC를 iPhone 14(또는 모바일 기기) 핫스팟에 연결하여 인터넷이 가능한 상태로 만듭니다.
2. **PC 핫스팟 켜기**
   PC 설정에서 *모바일 핫스팟* 기능을 켭니다. (예: SSID `seul 52@@`)
3. **Quest 연결**
   Meta Quest 3의 Wi-Fi 설정에서 PC가 만든 핫스팟(`seul 52@@`)에 연결합니다.
4. **Quest IP 확인**
   Quest → Wi-Fi 상세 정보(고급)에서 **IP 주소 (`192.168.137.xxx`)**를 확인합니다.

---

### Step 2. IP 주소 교차 등록 (코드 수정)

#### 1️⃣ Python – Quest IP 등록

`Python/new_final.py` 파일의 **22번째 줄**을 수정합니다.

```python
# 위 Step 1에서 확인한 Quest 3의 IP 주소
QUEST_IP = "192.168.137.xxx"
```

---

#### 2️⃣ Unity – PC IP 등록

1. **PC IP 확인**
   `cmd` 실행 → `ipconfig` 입력 → *무선 LAN 어댑터*의 IPv4 주소 확인

2. **Unity 설정**

   * `Assets/Scenes/WorkoutScene` 열기
   * Hierarchy → **Workout_HUD_Canvas** 선택
   * Inspector → 스크립트 컴포넌트의 **Pc Ip Address** 입력란에 PC IPv4 주소 입력

---

## 4. 빌드 및 실행 방법 (How to Run)

### Step 1. Unity 빌드 (Android)

1. Unity → **File → Build Settings**
2. **Scenes In Build** 목록에 아래 씬이 모두 포함되어 있는지 확인

```
01_Intro
02_Profile
03_TrainerIntro
04_Workout_Beginner
05_Workout_Intermediate
06_Workout_Advanced
07_Result
```

* 목록에 없는 경우:
  `01_Intro` 씬부터 하나씩 열어 **Add Open Scenes**로 전부 추가

3. Platform: **Android**
4. Quest 3를 PC에 연결한 상태에서 **Build And Run** 클릭

빌드 완료 후 Quest 3에서 앱이 자동 실행됩니다.

---

### Step 2. Python 서버 실행

Unity 빌드가 진행되는 동안 Python 폴더로 이동합니다.

```bash
pip install -r requirements.txt
python new_final.py
```

터미널에 아래 메시지가 출력되면 준비 완료입니다.

```
📡 PC 대기 중...
```

---

### Step 3. VR 시작

1. Quest 3에서 앱 실행
2. 메인 화면에서 **조이스틱을 이용해 패널 앞 발판 위에 서면 자동 START**

---

## 5. 트러블슈팅 (FAQ)

**Q. 앱은 켜지는데 게임이 진행되지 않습니다.**
A. IP 주소 설정 오류 또는 방화벽 문제일 가능성이 매우 높습니다.

* `new_final.py`에 입력한 Quest IP 재확인
* Unity에 입력한 PC IP 재확인
* PC 방화벽에서 Python 통신 허용 여부 확인
* Quest가 PC 핫스팟에 정상 연결되어 있는지 확인

---

**Q. Unity에서 패키지 에러가 발생합니다.**
A. 프로젝트 최초 실행 시 패키지 다운로드(Resolving Packages)가 진행 중일 수 있습니다.
인터넷 연결을 유지한 상태로 잠시 기다려 주세요.
