import cv2
from hand_module.hand_engine import HandEngine


def main():
    engine = HandEngine()
    cap = cv2.VideoCapture(0)

    if not cap.isOpened():
        print("❌ 웹캠을 열 수 없습니다.")
        return

    print("▶ Hand Engine Started (NO CAMERA WINDOW)")
    print("▶ 종료하려면 q를 누르세요.\n")

    while True:
        ret, frame = cap.read()
        if not ret:
            continue

        frame = cv2.flip(frame, 1)

        # hand_engine 처리
        rps, zero, cham = engine.process_frame(frame)

        # 콘솔 출력
        print(f"RPS: {rps} | ZERO: {zero} | CHAM: {cham}")

        # 종료 조건
        if cv2.waitKey(1) & 0xFF == ord('q'):
            print("▶ 종료합니다.")
            break

    cap.release()
    cv2.destroyAllWindows()


if __name__ == "__main__":
    main()
