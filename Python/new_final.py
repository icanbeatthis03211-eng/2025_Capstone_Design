import cv2
import mediapipe as mp
import numpy as np
from collections import deque
import socket, json, time, select, random

# ====== 한글 렌더링용 (Pillow) ======
from PIL import ImageFont, ImageDraw, Image

def draw_hangul(img, text, position, font_size=28, color=(255, 255, 255)):
    img_pil = Image.fromarray(cv2.cvtColor(img, cv2.COLOR_BGR2RGB))
    draw = ImageDraw.Draw(img_pil)
    try:
        font = ImageFont.truetype("C:/Windows/Fonts/malgunbd.ttf", font_size)
    except:
        font = ImageFont.load_default()
    draw.text(position, text, font=font, fill=color)
    return cv2.cvtColor(np.array(img_pil), cv2.COLOR_RGB2BGR)


# ================= 설정 (IP 확인 필수) =================
QUEST_IP = "192.168.137.15"  # ★ 퀘스트 IP 재확인!
QUEST_PORT = 5005
PC_LISTEN_IP = "0.0.0.0"
PC_LISTEN_PORT = 6000

# ================= 🗣️ AI 트레이너 페르소나 대사집 =================
COACH_SCRIPTS = {
    "welcome": [
        "안녕하세요 회원님! 가볍게 몸 좀 풀어볼까요?",
        "반갑습니다! 오늘도 즐겁게 운동해봅시다.",
        "어서오세요! 카메라 앞에 편하게 서주세요."
    ],
    "start_countdown": [
        "자, 준비하시고... 시작합니다!",
        "카운트다운 들어갑니다. 준비!",
        "측정 끝! 바로 시작해볼게요."
    ],
    "knee_bad": [
        "무릎이 안쪽으로 쏠려요. 살짝 벌려주세요.",
        "무릎을 발끝 방향으로! 그래야 안전해요.",
        "무릎 사이를 조금 더 넓혀볼까요?"
    ],
    "spine_bad": [
        "허리가 굽었어요. 가슴을 펴주세요.",
        "상체를 곧게 세워야 운동이 잘 돼요.",
        "시선은 정면! 땅을 보지 마세요."
    ],
    "depth_bad": [
        "조금만 더 앉아볼까요?",
        "자극을 느끼려면 더 깊게!",
        "엉덩이를 조금만 더 내려보세요."
    ],
    "good": [
        "좋아요! 아주 완벽해요.",
        "나이스! 자세가 정말 좋습니다.",
        "훌륭해요! 그 느낌 기억하세요.",
        "좋습니다! 계속 그렇게 해주세요."
    ],
    "fail": [
        "아쉽네요, 조금 더 깊게 앉아보세요.",
        "자세가 무너졌어요. 다시 집중!",
        "천천히 다시 해봅시다."
    ],
    "hold_fail": [
        "버티기 실패! 조금만 더 힘내세요.",
        "시간이 부족했어요. 꽉 버텨야 해요!",
        "허벅지에 힘 꽉!"
    ],
    "mission_complete": [
        "미션 컴플리트! 정말 고생하셨습니다.",
        "목표 달성! 끝까지 해내셨군요. 최고예요!",
        "운동 종료! 아주 멋진 퍼포먼스였습니다."
    ]
}

def get_random_msg(category):
    if category in COACH_SCRIPTS:
        return random.choice(COACH_SCRIPTS[category])
    return ""


# ================= UDP 통신 설정 =================
sock_send = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock_recv = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock_recv.bind((PC_LISTEN_IP, PC_LISTEN_PORT))
sock_recv.setblocking(False)

last_sent_msg = ""
last_sent_time = 0

def send_udp(data_dict):
    try:
        sock_send.sendto(json.dumps(data_dict, ensure_ascii=False).encode(), (QUEST_IP, QUEST_PORT))
    except: pass

def send_depth(v):      send_udp({"type": "depth", "data": float(v)})
def send_count():       send_udp({"type": "count"})
def send_msg(msg):      send_udp({"type": "msg", "data": msg})
def send_hold_time(t):  send_udp({"type": "hold", "data": float(t)})
def send_knee(ok):      send_udp({"type": "knee", "isGood": bool(ok)})
def send_spine(ok):     send_udp({"type": "spine", "isGood": bool(ok)})

def send_result(count, score, kcal, feedback):
    send_udp({
        "type": "result", "count": count, "score": float(score),
        "kcal": float(kcal), "feedback": feedback
    })

def send_guide_msg(msg, force=False):
    global last_sent_msg, last_sent_time
    now = time.time()
    
    cooldown = 3.0
    if msg == last_sent_msg: cooldown = 8.0
    if force: cooldown = 1.0

    if (now - last_sent_time) < cooldown:
        return

    send_udp({"type": "msg", "data": msg})
    last_sent_msg = msg
    last_sent_time = now
    print(f"📢 AI 트레이너: {msg}")


# ================= Mediapipe Helper =================
mp_pose = mp.solutions.pose
mp_draw = mp.solutions.drawing_utils

def get_point(lm, i, w, h):
    return np.array([lm[i].x * w, lm[i].y * h])

def is_tpose(lm):
    return abs(lm[11].y - lm[15].y) < 0.1 and abs(lm[12].y - lm[16].y) < 0.1


# ================= AI 리포트 생성기 =================
def generate_ai_report(total_attempts, squat_count, avg_score, error_log):
    if total_attempts == 0: return "인식된 동작이 없었습니다."
    lines = []
    
    success_rate = squat_count / total_attempts if total_attempts > 0 else 0
    
    if squat_count == 0: lines.append("성공 횟수가 없네요. 조금 더 연습해볼까요?")
    elif avg_score >= 4 and success_rate > 0.8: lines.append("완벽합니다! 아주 잘하셨어요. 👍")
    elif avg_score >= 3: lines.append("잘하셨어요! 꾸준함이 답입니다.")
    else: lines.append("고생하셨어요! 자세에 조금 더 신경써보세요.")
    
    if error_log["knee"] > 0:
        lines.append(f"- 무릎 쏠림이 {error_log['knee']}회 있었어요.")
    if error_log["back"] > 0:
        lines.append(f"- 허리가 {error_log['back']}회 굽어졌어요.")
    if error_log["depth"] > 0:
        lines.append(f"- 깊이가 부족한 횟수가 {error_log['depth']}회 있었어요.")
    
    return "\n".join(lines)


# ================= 메인 운동 로직 =================
def run_workout(difficulty):
    print(f"🚀 {difficulty} 모드 시작")
    send_guide_msg(get_random_msg("welcome"), force=True)

    # 설정
    TARGET_COUNT = 10
    if difficulty == "Easy": TARGET_COUNT = 5
    elif difficulty == "Hard": TARGET_COUNT = 20
    
    # 난이도 대폭 완화
    HOLD_TIME = 6.0 if difficulty == "Hard" else (2.0 if difficulty == "Normal" else 0.0)
    
    # 파라미터 설정
    DEPTH_DOWN = 0.50  # 50% 정도만 앉아도 인정
    DEPTH_UP   = 0.20  # 20% 정도 굽혀져 있어도 선 것으로 인정
    
    # 🔥 [수정됨] 정면 허리 인식 임계값 (Baseline 비율)
    # 서 있을 때 상체 길이의 65% 미만으로 짧아지면(앞으로 많이 숙이면) 경고
    # 이 값을 높이면(0.70) 더 엄격해지고, 낮추면(0.55) 더 관대해집니다.
    SPINE_LIMIT_RATIO = 0.55  
    
    VALGUS_LIMIT = 0.25 

    # === Depth 및 Baseline 파라미터 ===
    BASELINE_FRAMES = 45   # 서있는 baseline 샘플 수
    EMA_ALPHA = 0.20       

    cap = cv2.VideoCapture(0, cv2.CAP_DSHOW)
    if not cap.isOpened(): cap = cv2.VideoCapture(1, cv2.CAP_DSHOW)
    if not cap.isOpened():
        send_guide_msg("카메라 연결 실패! PC 확인", force=True)
        return

    # 변수 초기화
    calibrated = False
    t_cnt = 0
    t_loss_cnt = 0
    
    squat_count = 0; total_attempts = 0
    squat_scores = []
    error_log = {"depth": 0, "knee": 0, "back": 0}

    state = "UP"
    current_hold = 0.0
    hold_success = False
    last_time = time.time()
    last_coach_time = time.time()
    ui_lines = []

    is_counting_down = False
    countdown_start_time = 0

    # === Baseline (서있음) 자동 캘리브레이션 변수 ===
    baseline_vthigh = 0.0   # 서 있을 때 허벅지 수직 길이
    baseline_torso = 0.0    # 서 있을 때 상체 길이 (NEW)
    baseline_cnt = 0
    baseline_ready = False
    ema_ratio = None

    with mp_pose.Pose(min_detection_confidence=0.5, min_tracking_confidence=0.5) as pose:
        while True:
            try:
                data, _ = sock_recv.recvfrom(1024)
                if json.loads(data.decode()).get("type") == "stop":
                    print("🛑 VR 종료 신호 수신.")
                    break
            except: pass

            ret, frame = cap.read()
            if not ret: break
            frame = cv2.flip(frame, 1)
            h, w, _ = frame.shape
            rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            res = pose.process(rgb)

            now = time.time(); dt = now - last_time; last_time = now

            if not res.pose_landmarks:
                if not is_counting_down:
                    send_guide_msg("회원님, 카메라 중앙에 서주세요!")
                frame = draw_hangul(frame, "사람 찾는 중...", (20,40), 30, (0,0,255))
                cv2.imshow("HealthOnFit PC", frame)
                if cv2.waitKey(1) == ord('q'): break
                continue

            lm = res.pose_landmarks.landmark
            mp_draw.draw_landmarks(frame, res.pose_landmarks, mp_pose.POSE_CONNECTIONS)

            # --- 1. 캘리브레이션 (T-Pose) ---
            if not calibrated:
                if is_tpose(lm):
                    t_loss_cnt = 0
                    t_cnt += 1
                    if t_cnt % 20 == 0: 
                        send_guide_msg(f"측정 중입니다... {min(100, int(t_cnt/60*100))}%", force=True)
                    frame = draw_hangul(frame, f"측정 중... {t_cnt}/60", (20, 40), 30, (0,255,255))
                else:
                    t_loss_cnt += 1
                    if t_loss_cnt > 15:
                        if t_cnt > 15: send_guide_msg("측정이 끊겼어요. 다시 T자를!", force=True)
                        elif t_cnt == 0: send_guide_msg("양팔을 벌려 T자를 만들어주세요.")
                        t_cnt = 0
                        frame = draw_hangul(frame, "T-포즈 필요", (20, 40), 30, (0,0,255))
                    else:
                        frame = draw_hangul(frame, f"측정 중... {t_cnt}/60", (20, 40), 30, (0,255,255))

                if t_cnt >= 60:
                    calibrated = True
                    is_counting_down = True
                    countdown_start_time = time.time()
                    send_guide_msg("측정 완료! 잠시 후 시작합니다.", force=True)
                    send_guide_msg(get_random_msg("start_countdown"), force=True)
                
                cv2.imshow("HealthOnFit PC", frame)
                if cv2.waitKey(1) == ord('q'): break
                continue

            # --- 2. 카운트다운 ---
            if is_counting_down:
                elapsed = time.time() - countdown_start_time
                if elapsed < 1.0: txt = "3"; send_guide_msg("3", force=True)
                elif elapsed < 2.0: txt = "2"; send_guide_msg("2", force=True)
                elif elapsed < 3.0: txt = "1"; send_guide_msg("1", force=True)
                elif elapsed < 4.0: txt = "START!"; send_guide_msg("시작하세요!", force=True)
                else: is_counting_down = False; txt = ""

                if txt:
                    frame = draw_hangul(frame, txt, (w//2 - 50, h//2), 100, (0, 0, 255))
                
                cv2.imshow("HealthOnFit PC", frame)
                if cv2.waitKey(1) == ord('q'): break
                continue

            # --- 3. 운동 분석 (핵심 수정 부분) ---
            
            hip = (get_point(lm, 23, w, h) + get_point(lm, 24, w, h)) / 2
            knee = (get_point(lm, 25, w, h) + get_point(lm, 26, w, h)) / 2
            ankle = (get_point(lm, 27, w, h) + get_point(lm, 28, w, h)) / 2
            shoulder = (get_point(lm, 11, w, h) + get_point(lm, 12, w, h)) / 2

            # [수정] 상체 길이 측정 (어깨-골반 거리)
            current_torso_len = np.linalg.norm(shoulder - hip)
            
            # [수정] 허벅지 수직 길이 (Depth 측정용)
            vthigh = abs(knee[1] - hip[1])

            # --- 0) Baseline (서있음) 데이터 수집 ---
            if not baseline_ready:
                baseline_cnt += 1
                if baseline_cnt == 1:
                    baseline_vthigh = vthigh
                    baseline_torso = current_torso_len
                    send_guide_msg("서있는 기준값 측정 중... 잠시만요!", force=True)
                else:
                    # 평균 누적 (Online Mean)
                    baseline_vthigh = baseline_vthigh + (vthigh - baseline_vthigh) / baseline_cnt
                    baseline_torso = baseline_torso + (current_torso_len - baseline_torso) / baseline_cnt

                progress = int(min(100, (baseline_cnt / BASELINE_FRAMES) * 100))
                ui_lines = [f"서있는 기준값 측정 중... {progress}%"]

                # 기준 측정 중에는 모든 상태 OK로 전송
                send_depth(0.0)
                send_hold_time(0.0)
                send_spine(True)
                send_knee(True)

                frame = draw_hangul(frame, ui_lines[0], (20, 40), 30, (0,255,255))
                cv2.imshow("HealthOnFit PC", frame)
                if cv2.waitKey(1) == ord('q'):
                    break

                if baseline_cnt >= BASELINE_FRAMES:
                    baseline_vthigh = max(1.0, baseline_vthigh)
                    baseline_torso = max(1.0, baseline_torso)
                    ema_ratio = 1.0
                    baseline_ready = True
                    
                    state = "UP"
                    current_hold = 0.0
                    hold_success = False

                    send_guide_msg("기준 설정 완료! 스쿼트를 시작하세요.", force=True)
                continue

            # --- 1) Depth 계산 ---
            ratio = vthigh / baseline_vthigh
            ratio = max(0.0, min(1.5, ratio))

            if ema_ratio is None: ema_ratio = ratio
            else: ema_ratio = EMA_ALPHA * ratio + (1.0 - EMA_ALPHA) * ema_ratio

            depth_val = 1.0 - ema_ratio
            depth_val = max(0.0, min(1.2, depth_val))

            # --- 2) Spine(허리) 판정: [Baseline 비율 방식] ---
            # 원리: 상체를 앞으로 숙이면 카메라 상에서 상체 길이가 짧아짐(Foreshortening)
            # 서 있을 때 길이 대비 55% 이하로 짧아지면 허리를 숙였다고 판단
            torso_ratio = current_torso_len / baseline_torso
            back_ok = torso_ratio >= SPINE_LIMIT_RATIO

            # --- 3) Knee(무릎) 판정 ---
            thigh_len = np.linalg.norm(hip - knee)
            valgus = abs((knee[0] - ankle[0]) / (thigh_len + 1e-6))
            knee_ok = valgus <= VALGUS_LIMIT 

            # 게이지 및 전송
            gauge_fill = max(0.0, min(1.0, depth_val / max(1e-6, DEPTH_DOWN)))
            send_depth(gauge_fill)
            send_spine(back_ok)
            send_knee(knee_ok)

            # AI 코칭
            if (time.time() - last_coach_time) > 4.0:
                coach_msg = ""
                if not knee_ok:
                    coach_msg = get_random_msg("knee_bad")
                    error_log["knee"] += 1
                elif not back_ok:
                    coach_msg = get_random_msg("spine_bad")
                    error_log["back"] += 1
                elif state == "DOWN" and not depth_ok and (time.time() - last_coach_time) > 6.0:
                     coach_msg = get_random_msg("depth_bad")
                     error_log["depth"] += 1
                
                if coach_msg:
                    send_guide_msg(coach_msg)
                    ui_lines = [coach_msg]
                    last_coach_time = time.time()

            # 상태 머신
            depth_ok = depth_val >= DEPTH_DOWN 

            if state == "UP":
                if depth_ok: 
                    total_attempts += 1
                    state = "DOWN"
                    current_hold = 0.0
                    hold_success = False
            
            elif state == "DOWN":
                all_ok = depth_ok and back_ok and knee_ok
                
                if HOLD_TIME > 0:
                    if all_ok: current_hold += dt
                    
                    remain = max(0.0, HOLD_TIME - current_hold)
                    send_hold_time(remain)
                    if current_hold >= (HOLD_TIME - 1e-3): hold_success = True
                else:
                    hold_success = True 

                # 일어남 감지
                if depth_val < DEPTH_UP:
                    score = 1
                    if hold_success: score += 2
                    if knee_ok: score += 1
                    if back_ok: score += 1
                    squat_scores.append(score)

                    if hold_success:
                        squat_count += 1
                        send_count()
                        
                        if squat_count >= TARGET_COUNT:
                            send_guide_msg(get_random_msg("mission_complete"), force=True)
                            print("✅ 목표 달성! 루프 종료.")
                            break 
                        else:
                            msg = get_random_msg("good")
                            send_guide_msg(msg)
                            ui_lines = [msg]
                    else:
                        msg = get_random_msg("hold_fail" if HOLD_TIME > 0 else "fail")
                        send_guide_msg(msg)
                        ui_lines = [msg]
                    
                    last_coach_time = time.time()
                    state = "UP"
                    current_hold = 0.0

            # 화면 표시
            cv2.putText(frame, f"Count: {squat_count}", (20, 80), cv2.FONT_HERSHEY_SIMPLEX, 0.8, (0,255,0), 2)
            
            # 디버깅 정보: TR(Torso Ratio)가 0.65 밑으로 떨어지면 Back Fail
            debug_info = f"State:{state} | D:{depth_val:.2f} | TR:{torso_ratio:.2f}"
            color_d = (0, 255, 0) if back_ok else (0, 0, 255)
            cv2.putText(frame, debug_info, (20, 120), cv2.FONT_HERSHEY_SIMPLEX, 0.6, color_d, 2)

            for i, line in enumerate(ui_lines):
                frame = draw_hangul(frame, line, (20, 160 + i*35), 26, (255, 255, 255))
            
            if state == "DOWN" and HOLD_TIME > 0:
                remain = max(0.0, HOLD_TIME - current_hold)
                frame = draw_hangul(frame, f"버티기: {remain:.1f}초", (20, 220), 26, (0,255,255))

            cv2.imshow("HealthOnFit PC", frame)
            if cv2.waitKey(1) == ord('q'): break

    cap.release()
    cv2.destroyAllWindows()

    avg = round(sum(squat_scores)/len(squat_scores), 1) if squat_scores else 0
    kcal = round(squat_count * 0.8, 2)
    ai_feedback = generate_ai_report(total_attempts, squat_count, avg, error_log)
    
    print("📤 유니티로 결과 데이터 전송 중...")
    send_result(squat_count, avg, kcal, ai_feedback)

    print("\n" + "="*30)
    print(f"✅ 운동 종료 리포트")
    print(f"- 총 횟수: {squat_count}")
    print(ai_feedback)
    print("="*30 + "\n")


# ================= 대기 모드 Loop =================
print(f"📡 PC 대기 중... (Quest IP: {QUEST_IP})")
print(f"👂 VR 신호 대기 중 (Port: {PC_LISTEN_PORT})")

while True:
    try:
        readable, _, _ = select.select([sock_recv], [], [], 1.0)
        if readable:
            data, addr = sock_recv.recvfrom(1024)
            msg = json.loads(data.decode())
            if msg.get("type") == "start":
                diff = msg.get("difficulty", "Normal")
                print(f"✅ VR 신호 수신! [{diff}] 모드 실행")
                run_workout(diff)
                print("📡 다시 대기 모드로 돌아갑니다...")
    except KeyboardInterrupt:
        print("\n프로그램을 종료합니다.")
        break
    except Exception as e:
        print(f"Error: {e}")
        time.sleep(1)