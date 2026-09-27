import pandas as pd
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
import numpy as np
import os


csv_path = os.path.join(os.path.dirname(__file__), "TestResults.csv")
df = pd.read_csv(csv_path)

df["TimeToEscape"] = pd.to_numeric(df["TimeToEscape"], errors="coerce")
df["Escaped"] = df["Escaped"].astype(str).str.lower() == "true"

scenarios = ["Static", "Random", "Adaptive"]
bots      = ["LootGreedy", "Speedrun", "Neutral"]

colours = {
    "Static":   "#4C72B0",
    "Random":   "#DD8452",
    "Adaptive": "#55A868"
}

output_dir = os.path.dirname(__file__)

fig, ax = plt.subplots(figsize=(10, 6))

x = np.arange(len(bots))
width = 0.25

for i, scenario in enumerate(scenarios):
    means = []
    errors = []
    for bot in bots:
        subset = df[(df["Scenario"] == scenario) & (df["Bot"] == bot) & df["Escaped"]]
        mean = subset["TimeToEscape"].mean()
        std  = subset["TimeToEscape"].std()
        means.append(mean if not np.isnan(mean) else 0)
        errors.append(std  if not np.isnan(std)  else 0)

    bars = ax.bar(x + i * width, means, width,
                  label=scenario, color=colours[scenario],
                  yerr=errors, capsize=4, alpha=0.85)

ax.set_xlabel("Bot Archetype", fontsize=12)
ax.set_ylabel("Mean Time to Escape (seconds)", fontsize=12)
ax.set_title("Mean Time-to-Escape by Bot Archetype and Director Scenario", fontsize=13)
ax.set_xticks(x + width)
ax.set_xticklabels(bots)
ax.legend(title="Scenario")
ax.grid(axis="y", linestyle="--", alpha=0.5)

plt.tight_layout()
plt.savefig(os.path.join(output_dir, "graph1_mean_time_to_escape.png"), dpi=150)
plt.close()
print("Saved graph1_mean_time_to_escape.png")

fig, ax = plt.subplots(figsize=(10, 6))

for i, scenario in enumerate(scenarios):
    rates = []
    for bot in bots:
        subset = df[(df["Scenario"] == scenario) & (df["Bot"] == bot)]
        rate = subset["Escaped"].mean() * 100 if len(subset) > 0 else 0
        rates.append(rate)

    ax.bar(x + i * width, rates, width,
           label=scenario, color=colours[scenario], alpha=0.85)

ax.set_xlabel("Bot Archetype", fontsize=12)
ax.set_ylabel("Escape Rate (%)", fontsize=12)
ax.set_title("Escape Rate by Bot Archetype and Director Scenario", fontsize=13)
ax.set_xticks(x + width)
ax.set_xticklabels(bots)
ax.set_ylim(0, 110)
ax.legend(title="Scenario")
ax.grid(axis="y", linestyle="--", alpha=0.5)

plt.tight_layout()
plt.savefig(os.path.join(output_dir, "graph2_escape_rate.png"), dpi=150)
plt.close()
print("Saved graph2_escape_rate.png")


fig, ax = plt.subplots(figsize=(10, 6))

for i, scenario in enumerate(scenarios):
    means = []
    errors = []
    for bot in bots:
        subset = df[(df["Scenario"] == scenario) & (df["Bot"] == bot)]
        mean = subset["LootCollected"].mean()
        std  = subset["LootCollected"].std()
        means.append(mean if not np.isnan(mean) else 0)
        errors.append(std  if not np.isnan(std)  else 0)

    ax.bar(x + i * width, means, width,
           label=scenario, color=colours[scenario],
           yerr=errors, capsize=4, alpha=0.85)

ax.set_xlabel("Bot Archetype", fontsize=12)
ax.set_ylabel("Mean Loot Collected", fontsize=12)
ax.set_title("Mean Loot Collected by Bot Archetype and Director Scenario", fontsize=13)
ax.set_xticks(x + width)
ax.set_xticklabels(bots)
ax.legend(title="Scenario")
ax.grid(axis="y", linestyle="--", alpha=0.5)

plt.tight_layout()
plt.savefig(os.path.join(output_dir, "graph3_loot_collected.png"), dpi=150)
plt.close()
print("Saved graph3_loot_collected.png")

fig, ax = plt.subplots(figsize=(8, 5))

bot_colours = {
    "LootGreedy": "#4C72B0",
    "Speedrun":   "#DD8452",
    "Neutral":    "#55A868"
}

adaptive_df = df[(df["Scenario"] == "Adaptive") & df["Escaped"]]

for bot in bots:
    subset = adaptive_df[adaptive_df["Bot"] == bot]
    ax.scatter(subset["Run"], subset["TimeToEscape"],
               label=bot, color=bot_colours[bot], s=80, zorder=3)
    if len(subset) > 1:
        ax.plot(subset["Run"], subset["TimeToEscape"],
                color=bot_colours[bot], alpha=0.4, linewidth=1)

ax.set_xlabel("Run Index", fontsize=12)
ax.set_ylabel("Time to Escape (seconds)", fontsize=12)
ax.set_title("Per-Run Escape Times under Adaptive Director", fontsize=13)
ax.legend(title="Bot Archetype")
ax.grid(linestyle="--", alpha=0.5)

plt.tight_layout()
plt.savefig(os.path.join(output_dir, "graph4_adaptive_scatter.png"), dpi=150)
plt.close()
print("Saved graph4_adaptive_scatter.png")
