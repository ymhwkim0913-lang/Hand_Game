# rps.py (final upgrade)
from collections import deque

# 최근 판정 저장(5프레임 평균)
rps_buffer = deque(maxlen=5)

def is_palm_facing(hand):
    # 엄지 vs 새끼손가락 깊이 비교
    thumb = hand.landmark[4]
    pinky = hand.landmark[20]

    # threshold 조정 가능
    return (thumb.z - pinky.z) > 0.025

def is_facing_camera(hand):
    # 손바닥 폭 비교 => 측면일수록 폭 좁아짐
    palm_width = abs(hand.landmark[5].x - hand.landmark[17].x)

    return palm_width > 0.08  # 값 작으면 측면

def finger_up(hand, tip, pip, thresh=0.02):
    return hand.landmark[tip].y < hand.landmark[pip].y - thresh

def recognize_rps(hand_landmarks_list):
    if not hand_landmarks_list:
        rps_buffer.append("no_hand")
    else:
        hand = hand_landmarks_list[0]

        # palm facing check
        if not is_palm_facing(hand) or not is_facing_camera(hand):
            rps_buffer.append("unknown")
        else:
            # 엄지 X축
            thumb_up = (hand.landmark[4].x > hand.landmark[3].x + 0.03)

            # 다른 손가락
            fingers = [
                finger_up(hand, 8, 6),   # index
                finger_up(hand, 12, 10), # middle
                finger_up(hand, 16, 14), # ring
                finger_up(hand, 20, 18)  # pinky
            ]
            
            count = sum(fingers) + (1 if thumb_up else 0)

            if count == 0:
                rps_buffer.append("rock")
            elif fingers[0] and fingers[1] and not fingers[2] and not fingers[3]:
                rps_buffer.append("scissors")
            elif count == 5:
                rps_buffer.append("paper")
            else:
                rps_buffer.append("unknown")

    # 버퍼가 가득 차고 동일 값이면 확정
    if len(rps_buffer) == rps_buffer.maxlen and len(set(rps_buffer)) == 1:
        return rps_buffer[-1]
    else:
        return "processing"  # 안정화 중
