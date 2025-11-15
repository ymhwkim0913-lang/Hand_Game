import math
from collections import deque

SMOOTH_N = 5
zero_buffer = deque(maxlen=SMOOTH_N)

def _dist(a, b):
    return math.hypot(a.x - b.x, a.y - b.y)

def _palm_center(hand):
    # 손바닥 중심 추정 (wrist + middle_mcp 평균)
    wrist = hand.landmark[0]
    mid   = hand.landmark[9]
    return ((wrist.x + mid.x) * 0.5, (wrist.y + mid.y) * 0.5)

def _thumb_up_single(hand):
    """
    일반 한 손 엄지 판단
    (손 크기 기반)
    """
    tip = hand.landmark[4]
    ip  = hand.landmark[3]
    mcp = hand.landmark[2]

    wrist = hand.landmark[0]
    mid   = hand.landmark[9]
    hand_size = _dist(wrist, mid) + 1e-6

    vertical = (mcp.y - tip.y) > 0.30 * hand_size
    curve    = (ip.y - tip.y)  > 0.15 * hand_size
    stable   = abs(tip.x - mcp.x) < 0.60 * hand_size

    return vertical and curve and stable


def _thumb_up_two_hands(left, right):
    """
    양손 붙어 있을 때 전용 판정
    왼손 엄지: tip.x < palm_center.x
    오른손 엄지: tip.x > palm_center.x
    """
    # palm center 구하기
    lc_x, lc_y = _palm_center(left)
    rc_x, rc_y = _palm_center(right)

    l_tip = left.landmark[4]
    r_tip = right.landmark[4]

    left_up  = (l_tip.x < lc_x)
    right_up = (r_tip.x > rc_x)

    cnt = int(left_up) + int(right_up)
    return cnt


def count_thumbs(hand_list):
    # 손 없음
    if not hand_list:
        return None

    # 한 손인 경우
    if len(hand_list) == 1:
        hand = hand_list[0]
        cnt = 1 if _thumb_up_single(hand) else 0

    # 두 손인 경우
    elif len(hand_list) >= 2:
        hand_list = hand_list[:2]  # 첫 두 손만
        left, right = hand_list

        # 두 엄지가 가까우면 → 붙임 모드
        dist_thumb_mcp = _dist(left.landmark[2], right.landmark[2])

        if dist_thumb_mcp < 0.15:  
            # ← 세환이 영상 기준 최적 값 (멀리 있어도 잘 맞음)
            cnt = _thumb_up_two_hands(left, right)
        else:
            # 일반 모드
            cnt = 0
            if _thumb_up_single(left):
                cnt += 1
            if _thumb_up_single(right):
                cnt += 1

    # 스무딩
    zero_buffer.append(cnt)

    if len(zero_buffer) == zero_buffer.maxlen:
        # 최근 값 중 가장 많이 나온 값 반환
        return max(set(zero_buffer), key=zero_buffer.count)

    return cnt
