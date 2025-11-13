# detector.py (수정 버전)
import cv2
import mediapipe as mp

mp_hands = mp.solutions.hands

class HandDetector:
    def __init__(self, max_hands=2, min_detection=0.5, min_tracking=0.5):
        self.hands = mp_hands.Hands(
            static_image_mode=False,
            max_num_hands=max_hands,
            min_detection_confidence=min_detection,
            min_tracking_confidence=min_tracking
        )

    def get_landmarks(self, frame):
        """
        return: list of Mediapipe LandmarkList (0~2개)
        """
        image_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        results = self.hands.process(image_rgb)

        if results.multi_hand_landmarks:
            return list(results.multi_hand_landmarks)  # 항상 리스트
        else:
            return []
