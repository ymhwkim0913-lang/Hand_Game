import pandas as pd
from sklearn.ensemble import RandomForestClassifier
import joblib

df = pd.read_csv("rps_data.csv")

X = df.drop("label", axis=1)
y = df["label"]

model = RandomForestClassifier(n_estimators=400)
model.fit(X, y)

joblib.dump(model, "rps_model.pkl")
print("✅ Model saved as rps_model.pkl")
