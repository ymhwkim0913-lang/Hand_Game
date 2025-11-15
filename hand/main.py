import cv2
import socket
from hand_module.hand_engine import HandEngine

def main():
    engine = HandEngine()
    cap = cv2.VideoCapture(0)

    if not cap.isOpened():
        print("❌ 웹캠을 열 수 없습니다.")
        return

    # ------------------------
    # UDP 소켓 (Unity로 전송)
    # ------------------------
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    serverAddress = ("127.0.0.1", 12345)   # Unity가 받는 포트와 동일해야 함

    print("🔥 Unity UDP 전송 준비됨")

    while True:
        ret, frame = cap.read()
        if not ret:
            continue

        frame = cv2.flip(frame, 1)

        # HandEngine에서 3가지 게임 값 받기
        rps, zero, cham = engine.process_frame(frame)

        # ------------------------
        # Unity로 UDP 전송
        # ------------------------
        message = f"{rps},{zero},{cham}"
        sock.sendto(message.encode("utf-8"), serverAddress)

        # ------------------------
        # 화면 디버그 표시
        # ------------------------
        text = f"RPS: {rps} | ZERO: {zero} | CHAM: {cham}"
        cv2.putText(frame, text, (10, 40),
                    cv2.FONT_HERSHEY_SIMPLEX,
                    0.9, (0, 255, 255), 2)

        cv2.imshow("Hand Engine", frame)

        if cv2.waitKey(1) & 0xFF == ord("q"):
            break

    cap.release()
    cv2.destroyAllWindows()
    print("🟢 종료됨")


if __name__ == "__main__":
    main()
