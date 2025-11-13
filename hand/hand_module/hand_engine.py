# hand_engine.py — 통합 손 분석 엔진
import numpy as np
from hand_module.detector import HandDetector
from hand_module.zero_game import count_thumbs
from hand_module.chamcham import detect_hand_orientation
from feature_extract import extract_features
import pickle


class HandEngine:
    def __init__(self):
        # 손 인식기
        self.detector = HandDetector()

        # RPS 모델 로드
        try:
            with open("rps_model.pkl", "rb") as f:
                self.rps_model = pickle.load(f)
        except:
            print("❌ RPS model load failed")
            self.rps_model = None


    def _predict_rps(self, hand):
        if hand is None or self.rps_model is None:
            return "no_hand"

        lm = np.array([[p.x, p.y, p.z] for p in hand.landmark]).reshape(21,3)
        feature = extract_features(lm).reshape(1,-1)
        pred = self.rps_model.predict(feature)[0]

        return {0:"rock", 1:"scissors", 2:"paper"}[pred]


    def process_frame(self, frame):
        """
        frame → RPS, ZERO, CHAM 결과 반환
        """
        raw = self.detector.get_landmarks(frame)

        # 손을 리스트로 강제 변환
        if raw is None:
            hands = []
        elif isinstance(raw, list):
            hands = raw
        else:
            hands = [raw]

        # ---------------- RPS (첫 번째 손만)
        rps = self._predict_rps(hands[0]) if len(hands) > 0 else "no_hand"

        # ---------------- ZERO GAME (양손)
        zero = count_thumbs(hands)

        # ---------------- CHAMCHAM (첫 번째 손)
        cham = detect_hand_orientation(hands)

        return rps, zero, cham
