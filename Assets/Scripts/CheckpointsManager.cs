using System.Collections.Generic;
using UnityEngine;

public class CheckpointsManager : MonoBehaviour
{
    public List<Transform> checkpoints = new List<Transform>();

    public Transform GetCheckpoint(int index)
    {
        if (index >= 0 && index < checkpoints.Count)
            return checkpoints[index];
        return null;
    }

    public int TotalCheckpoints => checkpoints.Count;
}
