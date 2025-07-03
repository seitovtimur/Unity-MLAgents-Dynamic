using Unity.VisualScripting;
using UnityEngine;

public class GUI_Agent : MonoBehaviour
{
    [SerializeField] private MoveToGoalAgent _agent;

    private GUIStyle _defaultStyle = new GUIStyle();
    private GUIStyle _positiveStyle = new GUIStyle();
    private GUIStyle _negativeStyle = new GUIStyle();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _defaultStyle.fontSize = 40;
        _defaultStyle.normal.textColor = Color.yellow;

        _positiveStyle.fontSize = 40;
        _positiveStyle.normal.textColor = Color.green;

        _negativeStyle.fontSize = 40;
        _negativeStyle.normal.textColor = Color.red;
    }


    private void OnGUI()
    {
        string debugEpisode = "Episode: " + _agent.CurrentEpisode + " - Step: " + _agent.StepCount;
        string debugReward = "Reward: " + _agent.CumulativeRewared.ToString();

        // Select style based on reward value
        GUIStyle rewardStyle = _agent.CumulativeRewared < 0 ? _negativeStyle : _positiveStyle;

        // Display the debug text
        GUI.Label(new Rect(20, 20, 500, 50), debugEpisode, _defaultStyle);
        GUI.Label(new Rect(20, 60, 500, 50), debugReward, rewardStyle);
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
