import numpy as np
import joblib

model = joblib.load("rps_model_svm.pkl")
THRESH = 0.55

def predict_rps(hand_landmarks):
    row = []
    for lm in hand_landmarks.landmark:
        row.extend([lm.x, lm.y, lm.z])
    X = np.array(row).reshape(1, -1)

    proba = model.predict_proba(X)[0]
    label = model.classes_[np.argmax(proba)]
    confidence = np.max(proba)

    if confidence < THRESH:
        return "unknown"
    return label
