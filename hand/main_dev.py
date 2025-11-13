import cv2
import numpy as np
import pickle

from hand_module.detector import HandDetector
from hand_module.zero_game import count_thumbs
from hand_module.chamcham import detect_hand_orientation
from feature_extract import extract_features

try:
    with open("rps_model.pkl", "rb") as f:
        rps_model = pickle.load(f)
except:
    rps_model = None


def predict_rps(lm_list):
    lm = np.array(lm_list).reshape(21, 3)
    feature = extract_features(lm).reshape(1, -1)
    pred = rps_model.predict(feature)[0]
    return {0:"rock", 1:"scissors", 2:"paper"}[pred]


def main():
    detector = HandDetector()
    cap = cv2.VideoCapture(0)

    while True:
        ret, frame = cap.read()
        if not ret:
            continue

        frame = cv2.flip(frame, 1)
        raw = detector.get_landmarks(frame)

        if raw is None or raw == []:
            hands = []
        else:
            hands = raw if isinstance(raw, list) else [raw]

        rps_input = None
        if len(hands) > 0:
            h = hands[0]
            rps_input = [[lm.x, lm.y, lm.z] for lm in h.landmark]

        rps  = predict_rps(rps_input) if (rps_input and rps_model) else "no_hand"
        zero = count_thumbs(hands)
        cham = detect_hand_orientation(hands)

        print(f"RPS: {rps} | ZERO: {zero} | CHAM: {cham}")

        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

    cap.release()
    cv2.destroyAllWindows()


if __name__ == "__main__":
    main()
