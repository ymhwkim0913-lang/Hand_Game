# chamcham.py
def detect_hand_orientation(hand_list):
    if not hand_list:
        return "none"

    hand = hand_list[0]  # 첫 번째 손만 기준

    wrist = hand.landmark[0]  # 손목
    tip = hand.landmark[8]    # 검지 끝

    return "left" if tip.x < wrist.x else "right"
