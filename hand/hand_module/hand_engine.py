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
        # 손 검출기
        self.detector = HandDetector()

        # RPS 모델 로드
        try:
            with open("rps_model.pkl", "rb") as f:
                self.rps_model = pickle.load(f)
            print("✅ RPS 모델 로드 완료")
        except Exception as e:
            print("❌ rps_model.pkl 로드 실패:", e)
            self.rps_model = None

    def _predict_rps(self, hand_list):
        """
        hand_list: detector.get_landmarks(frame) 결과
                   -> [NormalizedLandmarkList, ...]
        첫 번째 손만 사용
        """
        if not hand_list or self.rps_model is None:
            return "no_hand"

        hand = hand_list[0]  # 첫 번째 손
        lm = hand.landmark   # 21개 랜드마크

        coords = [[p.x, p.y, p.z] for p in lm]  # (21,3)
        lm_arr = np.array(coords).reshape(21, 3)

        feat = extract_features(lm_arr).reshape(1, -1)
        pred = self.rps_model.predict(feat)[0]

        return {0: "rock", 1: "scissors", 2: "paper"}[pred]

    def process_frame(self, frame):
        """
        frame(BGR)을 받아서
        (rps, zero, cham)을 반환
        """
        # 1) 손 랜드마크 가져오기 (항상 list)
        hands = self.detector.get_landmarks(frame)
        # hands: [] 또는 [NormalizedLandmarkList, NormalizedLandmarkList]

        # 2) 가위바위보
        rps = self._predict_rps(hands)

        # 3) 제로게임 (양손 엄지 개수)
        zero = count_thumbs(hands)

        # 4) 참참참 (손 방향)
        cham = detect_hand_orientation(hands)

        return rps, zero, cham
