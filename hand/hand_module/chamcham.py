# chamcham.py (improved)
def detect_hand_orientation(hand_landmarks_list):
    """참참참: 좌/우 방향만 판정 (기울임/노이즈 개선 & 방향 반전)"""
    if not hand_landmarks_list:
        return "none"

    hand = hand_landmarks_list[0]

    wrist = hand.landmark[0]
    mid = hand.landmark[12]  # 중지 끝 (기준점)

    dx = mid.x - wrist.x

    # noise threshold
    THRESH = 0.04  

    if dx > THRESH:
        return "left"   # 반전: 원래 right → 이제 left
    elif dx < -THRESH:
        return "right"  # 반전: 원래 left → 이제 right
    else:
        return "none"   # 중앙일 때는 판정X
