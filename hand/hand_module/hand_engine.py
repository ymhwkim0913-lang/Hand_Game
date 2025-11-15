# hand_module/hand_engine.py

import cv2
import numpy as np
import pickle

from hand_module.detector import HandDetector
from hand_module.zero_game import count_thumbs
from hand_module.chamcham import detect_hand_orientation
from feature_extract import extract_features


class HandEngine:
    def __init__(self):
        self.detector = HandDetector()

        # RPS 모델
        try:
            with open("rps_model.pkl", "rb") as f:
                self.rps_model = pickle.load(f)
            print("✅ RPS 모델 로드 완료")
        except Exception as e:
            print("❌ rps_model.pkl 로드 실패:", e)
            self.rps_model = None

        # 🔥 마지막 값 저장 (초기값)
        self.last_rps = 0     # rock
        self.last_zero = 0    # zero=0개
        self.last_cham = 1    # middle

    def _predict_rps(self, hand_list):
        """ rock=0, scissors=1, paper=2 """
        if not hand_list or self.rps_model is None:
            return None   # 실패 표시

        hand = hand_list[0]
        lm = hand.landmark

        coords = [[p.x, p.y, p.z] for p in lm]
        lm_arr = np.array(coords).reshape(21, 3)
        feat = extract_features(lm_arr).reshape(1, -1)

        try:
            pred = int(self.rps_model.predict(feat)[0])  # 0/1/2
            return pred
        except:
            return None

    def process_frame(self, frame):
        hands = self.detector.get_landmarks(frame)

        # ----- RPS -----
        rps_pred = self._predict_rps(hands)
        if rps_pred is not None:
            self.last_rps = rps_pred
        # else: 유지됨

        # ----- Zero Game -----
        zero_val = count_thumbs(hands)  # 0~2
        if zero_val in [0, 1, 2]:
            self.last_zero = zero_val
        # else: 유지됨

        # ----- Cham Cham Cham -----
        cham_str = detect_hand_orientation(hands)
        cham_map = {"left": 0, "middle": 1, "right": 2}

        if cham_str in cham_map:
            self.last_cham = cham_map[cham_str]
        # "none" 등은 유지됨

        # 최종 반환 (항상 0/1/2 보장)
        return [self.last_rps, self.last_zero, self.last_cham]
