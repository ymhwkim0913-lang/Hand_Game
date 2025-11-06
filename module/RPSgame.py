# hand_game_capture_stable.py (네트워크 전송 기능 추가됨)
import cv2
import mediapipe as mp
import socket  # 1. <<< [추가됨] UDP 통신을 위해 import

# ----------------------------
# Mediapipe 초기화
mp_hands = mp.solutions.hands
mp_drawing = mp.solutions.drawing_utils
hands = mp_hands.Hands(
    static_image_mode=False,
    max_num_hands=2,
    min_detection_confidence=0.5,
    min_tracking_confidence=0.5
)

# ----------------------------
# 헬퍼 함수 (수정 없음)
def get_hand_landmarks(frame):
    """손 랜드마크 반환 (최대 2개), 없으면 빈 리스트 반환"""
    image_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
    results = hands.process(image_rgb)
    if results.multi_hand_landmarks:
        return results.multi_hand_landmarks
    else:
        return []

def count_thumbs(hand_landmarks_list):
    """제로게임: 양손 엄지만 세기"""
    count = 0
    for hand_landmarks in hand_landmarks_list:
        # 엄지 끝 4번, 손바닥 기준으로 y 좌표 확인
        if hand_landmarks.landmark[4].y < hand_landmarks.landmark[2].y:
            count += 1
    return count

def detect_hand_orientation(hand_landmarks_list):
    """참참참: 손날 방향 인식"""
    if not hand_landmarks_list:
        return "none"
    for hand_landmarks in hand_landmarks_list:
        wrist = hand_landmarks.landmark[0]
        middle_finger = hand_landmarks.landmark[12]

        dx = middle_finger.x - wrist.x
        dy = middle_finger.y - wrist.y

        if abs(dx) > abs(dy):
            return "right" if dx > 0 else "left"
        else:
            return "up" if dy < 0 else "down"
    return "none"

def recognize_rps(hand_landmarks_list):
    """가위바위보: 손가락 개수 기준"""
    if not hand_landmarks_list:
        return "no_hand"

    hand = hand_landmarks_list[0]  # 첫 손 기준
    fingers = [4, 8, 12, 16, 20]  # 엄지, 검지, 중지, 약지, 새끼
    count = 0
    for tip in fingers:
        if hand.landmark[tip].y < hand.landmark[tip - 2].y:
            count += 1

    if count == 0:
        return "rock"
    elif count == 2:
        return "scissors"
    elif count == 5:
        return "paper"
    else:
        return "unknown"

# ----------------------------
# 메인 실행
def main():
    # 2. <<< [추가됨] Unity로 보낼 UDP 소켓 설정
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    serverAddress = ("127.0.0.1", 12345) # IP, 포트

    cap = cv2.VideoCapture(0)
    if not cap.isOpened():
        print("웹캠을 열 수 없습니다.")
        return

    while True:
        ret, frame = cap.read()
        if not ret:
            continue

        hand_landmarks_list = get_hand_landmarks(frame)

        # 게임 모듈별 결과 계산
        zero_result = count_thumbs(hand_landmarks_list)        # 제로게임
        cham_result = detect_hand_orientation(hand_landmarks_list) # 참참참
        rps_result = recognize_rps(hand_landmarks_list)        # 가위바위보

        # 3. <<< [추가됨] Unity C# 스크립트가 받을 수 있도록
        # 'rock' -> '0', 'scissors' -> '1', 'paper' -> '2'로 변환
        
        data_to_send = "" # 보낼 데이터
        
        if rps_result == "rock":
            data_to_send = "0"
        elif rps_result == "scissors":
            data_to_send = "1"
        elif rps_result == "paper":
            data_to_send = "2"
            
        # 4. <<< [추가됨] '0', '1', '2' 중 하나일 때만 UDP로 전송
        if data_to_send:
            try:
                sock.sendto(data_to_send.encode(), serverAddress)
            except Exception as e:
                print(f"UDP 전송 오류: {e}")

        # --- 이 아래는 원래 코드 (표시용) ---

        # 화면에 랜드마크 표시
        for hand_landmarks in hand_landmarks_list:
            mp_drawing.draw_landmarks(frame, hand_landmarks, mp_hands.HAND_CONNECTIONS)

        # 화면 텍스트 표시
        cv2.putText(frame, f"ZERO: {zero_result}", (10, 30),
                    cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)
        cv2.putText(frame, f"CHAM: {cham_result}", (10, 70),
                    cv2.FONT_HERSHEY_SIMPLEX, 1, (255, 0, 0), 2)
        cv2.putText(frame, f"RPS: {rps_result}", (10, 110),
                    cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 0, 255), 2)

        # 콘솔 출력 (이건 그대로 둠)
        print(f"ZERO: {zero_result}, CHAM: {cham_result}, RPS: {rps_result}")

        # 화면 표시
        cv2.imshow("Hand Game Capture", frame)

        # 종료
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

    cap.release()
    cv2.destroyAllWindows()
    sock.close() # 5. <<< [추가됨] 프로그램 종료 시 소켓 닫기

if __name__ == "__main__":
    main()