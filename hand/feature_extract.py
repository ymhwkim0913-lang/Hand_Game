import numpy as np

def extract_features(landmarks):
    lm = landmarks.copy().astype(np.float32)

    # 1) 손목 기준 상대좌표
    wrist = lm[0].copy()
    lm -= wrist

    # 2) 손 크기 정규화
    hand_size = np.linalg.norm(lm[9]) + 1e-6
    lm /= hand_size

    # 3) feature vector로 변환
    feat = lm.reshape(-1)
    return feat
