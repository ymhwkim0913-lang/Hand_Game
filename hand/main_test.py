import cv2
from hand_module.detector import HandDetector
from rps_ml_svm import predict_rps   # ✅ SVM 기반 RPS 모델 사용
from hand_module.zero_game import count_thumbs
from hand_module.chamcham import detect_hand_orientation

def main():
    detector = HandDetector()
    cap = cv2.VideoCapture(0)

    if not cap.isOpened():
        print("웹캠을 열 수 없습니다.")
        return

    while True:
        ret, frame = cap.read()
        if not ret:
            continue

        # 손 랜드마크 추출
        hand_landmarks_list = detector.get_landmarks(frame)

        # 🎮 RPS (가위바위보) - SVM 모델
        if hand_landmarks_list:
            rps = predict_rps(hand_landmarks_list[0])
        else:
            rps = "no_hand"

        # ✊ 제로게임 (엄지 개수)
        zero = count_thumbs(hand_landmarks_list)

        # 👉 참참참 (손 방향)
        cham = detect_hand_orientation(hand_landmarks_list)

        # 콘솔 출력
        print(f"RPS: {rps} | ZERO: {zero} | CHAM: {cham}")

        # 화면 표시
        cv2.putText(frame, f"RPS: {rps}", (10,40),
                    cv2.FONT_HERSHEY_SIMPLEX, 1, (0,0,255), 2)
        cv2.putText(frame, f"ZERO: {zero}", (10,80),
                    cv2.FONT_HERSHEY_SIMPLEX, 1, (0,255,0), 2)
        cv2.putText(frame, f"CHAM: {cham}", (10,120),
                    cv2.FONT_HERSHEY_SIMPLEX, 1, (255,0,0), 2)

        cv2.imshow("Hand Game Test", frame)

        # 종료: Q 키
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

    cap.release()
    cv2.destroyAllWindows()

if __name__ == "__main__":
    main()
