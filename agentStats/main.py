from tensorboard.backend.event_processing import event_accumulator
import glob, os, numpy as np, pandas as pd

def load_scalars_from_run(event_file, tag):
    ea = event_accumulator.EventAccumulator(event_file, size_guidance={'scalars':0})
    ea.Reload()
    if tag not in ea.Tags().get('scalars', []):
        return []
    vals = ea.Scalars(tag)
    return [v.value for v in vals]

def get_event_files(run_dir):
    return glob.glob(os.path.join(run_dir, "**", "events.*"), recursive=True)

def analyze_runs(base_results_dir, run_ids):
    rows = []
    for run in run_ids:
        paths = get_event_files(os.path.join(base_results_dir, run))
        if not paths:
            print("No events for", run)
            continue
        ev = paths[0]
        succ = load_scalars_from_run(ev, "episode/success")
        length = load_scalars_from_run(ev, "episode/length")
        if len(succ) == 0:
            print("No success data for", run)
            continue
        succ_rate = np.mean(succ)
        mean_len = np.mean(length) if length else None
        rows.append({'run': run, 'succ_rate': succ_rate, 'mean_length': mean_len,
                     'num_episodes': len(succ)})
    df = pd.DataFrame(rows)
    return df

if __name__ == "__main__":
    base = "results"  # путь к папке results
    runs = ["trainPPO01MoveToGoal", "trainPPOTuned01MoveToGoal"]  # сюда впиши свои run-id
    df = analyze_runs(base, runs)
    print(df)
    print("Overall succ mean:", df['succ_rate'].mean(), "std:", df['succ_rate'].std())
