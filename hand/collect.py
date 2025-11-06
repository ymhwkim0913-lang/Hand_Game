import cv2
import mediapipe as mp
import csv

mp_hands = mp.solutions.hands
hands = mp_hands.Hands(max_num_hands=1)
mp_draw = mp.solutions.drawing_utils

csv_file = open("rps_data.csv", "w", newline="")
csv_writer = csv.writer(csv_file)

# Header: x0,y0,z0,...x20,y20,z20,label
header = []
for i in range(21):
    header += [f"x{i}", f"y{i}", f"z{i}"]
header.append("label")
csv_writer.writerow(header)

cap = cv2.VideoCapture(0)

def save_landmarks(lm, label):
    row = []
    for p in lm.landmark:
        row.extend([p.x, p.y, p.z])
    row.append(label)
    csv_writer.writerow(row)
    print(f"Saved: {label}")

print("Ready. Press r/p/s to record data. q to quit.")

while True:
    ret, frame = cap.read()
    if not ret:
        continue

    img = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
    result = hands.process(img)

    if result.multi_hand_landmarks:
        for lm in result.multi_hand_landmarks:
            mp_draw.draw_landmarks(frame, lm, mp_hands.HAND_CONNECTIONS)

            key = cv2.waitKey(1) & 0xFF
            if key == ord('r'):
                save_landmarks(lm, "rock")
            elif key == ord('p'):
                save_landmarks(lm, "paper")
            elif key == ord('s'):
                save_landmarks(lm, "scissors")

    cv2.putText(frame, "Press r/p/s to record, q to quit", (10,30),
                cv2.FONT_HERSHEY_SIMPLEX, 0.7, (255,255,255), 2)
    cv2.imshow("Collect RPS Data", frame)

    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

cap.release()
csv_file.close()
cv2.destroyAllWindows()
