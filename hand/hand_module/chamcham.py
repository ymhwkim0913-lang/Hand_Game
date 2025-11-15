# chamcham.py

def detect_hand_orientation(hand_list):
    if not hand_list:
        return "none"

    hand = hand_list[0]  # 첫 번째 손 기준

    wrist = hand.landmark[0]  # 손목
    tip = hand.landmark[8]    # 검지 끝

    diff = tip.x - wrist.x   # 양수 → 오른쪽, 음수 → 왼쪽

    threshold = 0.25  # 중앙 판정 임계값

    if abs(diff) < threshold:
        return "middle"
    elif diff < 0:
        return "left"
    else:
        return "right"
