import cv2
from hand_module.hand_engine import HandEngine

def main():
    cap = cv2.VideoCapture(0)

    if not cap.isOpened():
        print("웹캠을 열 수 없습니다.")
        return

    engine = HandEngine()

    while True:
        ret, frame = cap.read()
        if not ret:
            continue

        frame = cv2.flip(frame, 1)

        rps, zero, cham = engine.process_frame(frame)

        text = f"RPS: {rps} | ZERO: {zero} | CHAM: {cham}"
        cv2.putText(
            frame, text,
            (10, 40),
            cv2.FONT_HERSHEY_SIMPLEX,
            0.9,
            (0, 255, 255),
            2
        )

        cv2.imshow("Hand Engine", frame)

        if cv2.waitKey(1) & 0xFF == ord("q"):
            break

    cap.release()
    cv2.destroyAllWindows()


if __name__ == "__main__":
    main()
