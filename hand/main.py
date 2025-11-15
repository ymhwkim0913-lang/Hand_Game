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
    serverAddress = ("127.0.0.1", 12345)    # InGameManager의 포트와 동일

    print("🔥 Unity UDP 전송 준비됨")

    while True:
        ret, frame = cap.read()
        if not ret:
            continue

        frame = cv2.flip(frame, 1)

        # HandEngine에서 3가지 게임 값 받기
        # (순서 주의!) rps, zero, cham 순서로 값을 반환
        rps, zero, cham = engine.process_frame(frame)

        # ▼▼▼▼▼ [ 여기가 수정되었습니다! (순서 변경) ] ▼▼▼▼▼
        # ------------------------
        # Unity로 UDP 전송
        # ------------------------
        try:
            # C# InGameManager는 [RPS, CHAM, ZERO] 순서를 기대합니다.
            # [rps, zero, cham] -> [rps, cham, zero] 순서로 변경
            data = bytearray([rps, cham, zero]) 
            
            # 3바이트 배열 전송
            sock.sendto(data, serverAddress)
            
        except Exception as e:
            print(f"UDP 전송 오류: {e}")
            print(f"보내려던 값: rps={rps}, cham={cham}, zero={zero}")
        
        # ▲▲▲▲▲ [ 수정 완료 ] ▲▲▲▲▲


        # ------------------------
        # 화면 디버그 표시
        # ------------------------
        text = f"RPS: {rps} | CHAM: {cham} | ZERO: {zero}" # (디버그 텍스트 순서도 변경)
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