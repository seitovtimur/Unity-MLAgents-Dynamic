using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private bool _activated = false;

    public bool TryActivate()
    {
        if (_activated) return false; // уже был активирован
        _activated = true;
        return true;
    }

    public void ResetCheckpoint()
    {
        _activated = false;
    }
}
