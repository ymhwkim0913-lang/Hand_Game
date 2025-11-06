# zero_game.py (robust thumbs)
import math
from collections import deque

SMOOTH_N = 5
UP_Y_T   = 0.020
RATIO_T  = 0.75  # TIP-IP 길이 / IP-MCP 길이 비

zero_buffer = deque(maxlen=SMOOTH_N)

def _dist(a, b):
    return math.hypot(a.x - b.x, a.y - b.y)

def _thumb_extended(hand):
    tip = hand.landmark[4]
    ip  = hand.landmark[3]
    mcp = hand.landmark[2]
    # 길이 비 + y 방향 동시 확인 → 기울기에도 강함
    ratio_ok = (_dist(tip, ip) / max(_dist(ip, mcp), 1e-6)) > RATIO_T
    up_y = tip.y < mcp.y - UP_Y_T
    return ratio_ok and up_y

def count_thumbs(hand_list):
    if not hand_list:
        zero_buffer.append(0)
    else:
        cnt = 0
        for hand in hand_list:
            if _thumb_extended(hand):
                cnt += 1
        zero_buffer.append(cnt)

    # 스무딩
    if len(zero_buffer) == zero_buffer.maxlen and len(set(zero_buffer)) == 1:
        return zero_buffer[-1]
    return zero_buffer[-1]  # 직전값
