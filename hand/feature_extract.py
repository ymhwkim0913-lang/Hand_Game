import numpy as np

def extract_features(lm):
    """
    lm: shape (21, 3) numpy array (x,y,z)
    return: 1D feature vector (numpy array)
    """
    lm = np.array(lm, dtype=np.float32)

    # 1) 손목 기준 정규화
    wrist = lm[0].copy()
    lm = lm - wrist

    # 2) 손가락 끝 좌표 (엄지~새끼)
    tips_idx = [4, 8, 12, 16, 20]
    tips = lm[tips_idx]

    # 3) 손가락 간 거리 (엄지-검지, 검지-중지, 중지-약지, 약지-새끼)
    dist = []
    for i in range(4):
        d = np.linalg.norm(tips[i] - tips[i+1])
        dist.append(d)

    # 4) 손 폭 (엄지끝 - 새끼끝)
    width = np.linalg.norm(tips[0] - tips[4])

    # 5) 손바닥 방향 벡터 크기 (검지뿌리 - 새끼뿌리)
    palm_vec = lm[5] - lm[17]
    palm_mag = np.linalg.norm(palm_vec)

    # 6) 각 손가락 굽힘 각도 (검지, 중지, 약지, 새끼)
    def angle(a, b, c):
        ba = a - b
        bc = c - b
        cosang = np.dot(ba, bc) / (np.linalg.norm(ba) * np.linalg.norm(bc) + 1e-6)
        return np.degrees(np.arccos(np.clip(cosang, -1.0, 1.0)))

    bends = [
        angle(lm[5], lm[6], lm[7]),    # 검지
        angle(lm[9], lm[10], lm[11]),  # 중지
        angle(lm[13], lm[14], lm[15]), # 약지
        angle(lm[17], lm[18], lm[19])  # 새끼
    ]

    # 7) z-depth 퍼짐 (앞으로 뻗을 때 중요)
    z_vals = tips[:, 2]
    depth_spread = float(np.max(z_vals) - np.min(z_vals))

    # 최종 feature 벡터
    feature = np.array(dist + [width, palm_mag, depth_spread] + bends, dtype=np.float32)
    return feature
