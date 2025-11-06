# detector.py
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
        frame: OpenCV frame (BGR)
        return: list of hand landmark objects or empty list
        """
        image_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        results = self.hands.process(image_rgb)
        return results.multi_hand_landmarks if results.multi_hand_landmarks else []