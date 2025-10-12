import pandas as pd
import matplotlib.pyplot as plt
import glob
import os

# Путь к Result-файлам
results_path = "results/metrics_run_20251010_114601"
files = glob.glob(results_path)

if not files:
    print("❌ No metrics files found in Results/")
    exit()

for file in files:
    print(f"\n📂 Analyzing: {file}")
    df = pd.read_csv(file, sep=",")

    # --- Основные статистики ---
    success_rate = df["Success"].mean() * 100
    avg_steps = df["Steps"].mean()
    avg_reward = df["Reward"].mean()
    avg_path = df["PathDistance"].mean()

    print(f"✅ Success Rate: {success_rate:.2f}%")
    print(f"📏 Avg Steps: {avg_steps:.1f}")
    print(f"💰 Avg Reward: {avg_reward:.3f}")
    print(f"🧭 Avg Path Distance: {avg_path:.2f}")

    # --- График успеха ---
    plt.figure(figsize=(10, 4))
    plt.plot(df["Episode"], df["Success"].rolling(50).mean(), label="Success Rate (Smoothed)")
    plt.title(f"Success over Episodes — {os.path.basename(file)}")
    plt.xlabel("Episode")
    plt.ylabel("Success (0/1)")
    plt.grid(True)
    plt.legend()
    plt.show()

    # --- Reward тренд ---
    plt.figure(figsize=(10, 4))
    plt.plot(df["Episode"], df["Reward"].rolling(50).mean(), label="Reward (Smoothed)", color="orange")
    plt.title(f"Reward Trend — {os.path.basename(file)}")
    plt.xlabel("Episode")
    plt.ylabel("Reward")
    plt.grid(True)
    plt.legend()
    plt.show()
