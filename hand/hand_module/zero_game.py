# zero_game.py — Mediapipe 원본(hand.landmark) 기반 + 정확도 향상

import math
from collections import deque

SMOOTH_N = 5
zero_buffer = deque(maxlen=SMOOTH_N)

# 엄지 인식 기준값 (정면 카메라 기준)
UP_Y_T   = 0.018    # TIP이 MCP보다 이 정도 위면 "올림"
CURVE_T  = 0.010    # TIP이 IP보다 위로 구부러진 값
SIDE_T   = 0.030    # 좌/우 흔들림 억제

def _dist(a, b):
    return math.hypot(a.x - b.x, a.y - b.y)

def _thumb_up(hand):
    tip = hand.landmark[4]
    ip  = hand.landmark[3]
    mcp = hand.landmark[2]

    # 1) 세로로 충분히 올라갔는지
    vertical = (tip.y < mcp.y - UP_Y_T)

    # 2) 구부러진 형태(엄지가 실제로 들렸을 때)
    curved = (tip.y < ip.y - CURVE_T)

    # 3) 가로 흔들림에 대한 안정성 → 2손 붙였을 때 매우 중요
    # TIP과 MCP의 x 차이가 너무 크면 정확도 떨어짐 → 손가락 벌림 오판 방지
    stable = abs(tip.x - mcp.x) < SIDE_T

    return (vertical or curved) and stable


def count_thumbs(hand_list):
    if not hand_list:
        zero_buffer.append(0)
        return zero_buffer[-1]

    cnt = 0
    for hand in hand_list:
        try:
            if _thumb_up(hand):
                cnt += 1
        except:
            pass

    zero_buffer.append(cnt)

    # 최근 N개가 모두 같으면 확정
    if len(zero_buffer) == zero_buffer.maxlen and len(set(zero_buffer)) == 1:
        return zero_buffer[-1]

    return zero_buffer[-1]
