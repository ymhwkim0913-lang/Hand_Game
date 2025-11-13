# zero_game.py — 엄지 UP/DOWN (정면 + 양손 2 인식 강화 버전)

from collections import deque

SMOOTH_N = 5
zero_buf = deque(maxlen=SMOOTH_N)

# 손가락 길이에 대한 비율 기준 (스케일 자동 보정)
L_STRICT = 0.8   # 한 손에서 "엄지 확실히 든" 기준
L_LOOSE  = 0.4   # 양손에서 "엄지 꽤 든" 기준 (2 판정용)


def _thumb_lift_ratio(hand):
    """
    엄지 TIP이 IP, MCP보다 얼마나 위에 있는지
    손가락 길이(|IP-MCP|)로 정규화한 비율을 리턴
    """
    tip = hand.landmark[4]
    ip  = hand.landmark[3]
    mcp = hand.landmark[2]

    finger_len = abs(ip.y - mcp.y) + 1e-6  # 0 나누기 방지

    lift_ip  = (ip.y  - tip.y) / finger_len   # TIP이 IP보다 위면 + 방향
    lift_mcp = (mcp.y - tip.y) / finger_len   # TIP이 MCP보다 위면 + 방향

    return lift_ip, lift_mcp


def _thumb_up_strict(hand):
    lift_ip, lift_mcp = _thumb_lift_ratio(hand)
    return (lift_ip > L_STRICT) and (lift_mcp > L_STRICT)


def _thumb_up_loose(hand):
    lift_ip, lift_mcp = _thumb_lift_ratio(hand)
    return (lift_ip > L_LOOSE) and (lift_mcp > L_LOOSE)


def count_thumbs(hand_list):
    # 손이 아예 없으면 0 유지
    if not hand_list:
        zero_buf.append(0)
        return zero_buf[-1]

    # 각 손 엄지 상태 계산
    strict_flags = [_thumb_up_strict(h) for h in hand_list]
    loose_flags  = [_thumb_up_loose(h)  for h in hand_list]

    strict_count = sum(strict_flags)
    loose_count  = sum(loose_flags)

    # 기본값: 한 손 기준 엄격 카운트
    curr = strict_count

    # 손이 두 개 이상 있을 때,
    # 둘 다 "느슨 기준" 이상이면 2로 강제 판정
    if len(hand_list) >= 2 and loose_count >= 2:
        curr = 2

    zero_buf.append(curr)

    # 스무딩: 최근 N개가 모두 같으면 그 값 확정
    if len(zero_buf) == zero_buf.maxlen and len(set(zero_buf)) == 1:
        return zero_buf[-1]

    return zero_buf[-1]
