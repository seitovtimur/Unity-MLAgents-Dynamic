using System.Collections.Generic;
using UnityEngine;

public class CheckpointsManager : MonoBehaviour
{
    private List<Transform> checkpoints = new List<Transform>();
    private HashSet<Transform> visitedCheckpoints = new HashSet<Transform>();

    void Awake()
    {
        checkpoints.Clear();
        foreach (Transform child in transform)
        {
            checkpoints.Add(child);
        }
    }

    /// <summary>
    /// Проверяет, был ли этот чекпоинт уже достигнут.
    /// Если нет, добавляет его в список достигнутых и возвращает true.
    /// </summary>
    public bool TryReachCheckpoint(Transform checkpoint)
    {
        if (checkpoints.Contains(checkpoint) && !visitedCheckpoints.Contains(checkpoint))
        {
            visitedCheckpoints.Add(checkpoint);
            return true; // первый раз — засчитываем
        }
        return false; // уже был или не наш чекпоинт
    }

    /// <summary>
    /// Сброс прогресса — вызывать в OnEpisodeBegin у агента.
    /// </summary>
    public void ResetCheckpoints()
    {
        visitedCheckpoints.Clear();
    }
}
