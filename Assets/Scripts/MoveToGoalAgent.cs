using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Globalization;


public class MoveToGoalAgent : Agent
{
    [Header("References")]
    [SerializeField] private Transform _goal;
    [SerializeField] private Renderer _groundRenderer;
    [SerializeField] private CheckpointsManager _checkpointManager;
    [SerializeField] private SegmentRouteBuilder _segmentRouteBuilder;

    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 180f;
    //[SerializeField] private float _jumpForce = 5f; // jump disabled in current experiments

    private Rigidbody _rb;
    private Vector3 _startPos;
    private bool _isGrounded = true;
    private Renderer _renderer;

    // Checkpoints
    private int _currentCheckpointIndex = 0;
    private float _prevCheckpointDistance = float.MaxValue;

    // Episode & metrics
    [HideInInspector] public int CurrentEpisode = 0;
    [HideInInspector] public float CumulativeReward = 0f;

    private Color _defaultGroundColor;
    private Coroutine _flashGroundCoroutine;

    // Path / distance tracking
    private float episodeDistance = 0f;
    private Vector3 lastPos;

    // File / metrics handling
    private static HashSet<string> initializedRuns = new HashSet<string>();
    private static object fileLock = new object();
    private string runIdForFile = null;
    //private string metricsPath = null;
    private static string metricsPath = $"Results/metrics_run_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
    private readonly CultureInfo _ci = CultureInfo.InvariantCulture;




    // Per-episode tracking
    private bool episodeSuccess = false;
    private float episodeReward = 0f;
    private int episodeStepCount = 0;
    private float pathDistance = 0f;
    private Vector3 startPos;

    public override void Initialize()
    {
        Debug.Log("Initialize()");

        _renderer = GetComponent<Renderer>();

        CurrentEpisode = 0;
        CumulativeReward = 0f;

        _rb = GetComponent<Rigidbody>();
        if (_rb != null)
        {
            // важно: отключаем вращение физикой (чтобы препятствия не крутят тело агента)
            _rb.freezeRotation = true;
        }

        _startPos = transform.position;
        startPos = _startPos;

        _currentCheckpointIndex = 0;
        _prevCheckpointDistance = float.MaxValue;

        if (_groundRenderer != null)
        {
            _defaultGroundColor = _groundRenderer.material.color;
        }

        // Определяем run-id (попытка получить из Academy). Если нет — fallback на timestamp.
        string detectedRunId = null;
        try
        {
            // Попытка получить свойство RunId через рефлексию (чтобы быть совместимым с разными версиями)
            var academyType = typeof(Academy);
            var prop = academyType.GetProperty("RunId");
            if (prop != null)
            {
                var val = prop.GetValue(Academy.Instance, null);
                detectedRunId = val as string;
            }
        }
        catch
        {
            // ignore
        }

        if (string.IsNullOrEmpty(detectedRunId))
        {
            // fallback
            detectedRunId = $"run_{DateTime.Now:yyyyMMdd_HHmmss}";
        }

        runIdForFile = detectedRunId;

        // Путь к Result-папке в корне проекта (папка рядом с Assets)
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string resultsDir = Path.Combine(projectRoot, "Results");
        if (!Directory.Exists(resultsDir))
        {
            Directory.CreateDirectory(resultsDir);
        }

        metricsPath = Path.Combine(resultsDir, $"metrics_{runIdForFile}.csv");
        

        // Инициализируем файл (шапка) только один раз на run-id
        lock (fileLock)
        {
            if (!initializedRuns.Contains(runIdForFile))
            {
                bool writeHeader = true;
                // Если файл уже есть — не перезаписываем шапку
                if (File.Exists(metricsPath))
                {
                    // Проверим не пуст ли файл
                    var fi = new FileInfo(metricsPath);
                    if (fi.Length > 0) writeHeader = false;
                }

                if (writeHeader)
                {
                    try
                    {
                        File.WriteAllText(metricsPath, "Episode,Success,Steps,Reward,PathDistance\n");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to create metrics file '{metricsPath}': {e}");
                    }
                }

                initializedRuns.Add(runIdForFile);
            }
        }
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("OnEpisodeBegin()");

        // Если у нас есть билд-скрипт уровня, можно регенерировать его здесь (по желанию).
        // _segmentRouteBuilder?.RegenerateLevel();

        // Если SegmentRouteBuilder создал цель — используем её
        if (_segmentRouteBuilder != null && _segmentRouteBuilder.GoalTransform != null)
        {
            _goal = _segmentRouteBuilder.GoalTransform;
        }

        if (_groundRenderer != null && CumulativeReward != 0f)
        {
            Color flashColor = (CumulativeReward >= 0f) ? Color.green : Color.red;
            if (_flashGroundCoroutine != null)
            {
                StopCoroutine(_flashGroundCoroutine);
            }
            _flashGroundCoroutine = StartCoroutine(FlashGround(flashColor, 1.5f));
        }

        _currentCheckpointIndex = 0;

        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        transform.position = _startPos;
        transform.rotation = Quaternion.identity;

        _isGrounded = true;

        CurrentEpisode++;
        CumulativeReward = 0f;
        if (_renderer != null) _renderer.material.color = Color.cyan;

        // Reset per-episode metrics
        episodeSuccess = false;
        episodeReward = 0f;
        episodeStepCount = 0;
        pathDistance = 0f;
        episodeDistance = 0f;
        lastPos = transform.position;
        startPos = transform.position;

        // Reset all checkpoint instances in scene (если такие есть)
        var cps = FindObjectsOfType<Checkpoint>();
        foreach (var cp in cps)
        {
            cp.ResetCheckpoint();
        }
    }

    private IEnumerator FlashGround(Color targetColor, float duration)
    {
        float elapsedTime = 0f;
        _groundRenderer.material.color = targetColor;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            _groundRenderer.material.color = Color.Lerp(targetColor, _defaultGroundColor, elapsedTime / duration);
            yield return null;
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (_goal == null)
        {
            // если цели нет, добавляем нули, чтобы размер наблюдений был стабильным
            sensor.AddObservation(0f); // goalX
            sensor.AddObservation(0f); // goalZ
            sensor.AddObservation(0f); // agentX
            sensor.AddObservation(0f); // agentZ
            sensor.AddObservation(0f); // agentRot
            sensor.AddObservation(0f); // velocityX
            sensor.AddObservation(0f); // velocityZ
            sensor.AddObservation(0f); // isGrounded
            return;
        }

        // Нормализация позиций относительно масштаба сцены (подстрой под свои нужды)
        float goalPositionX_normalized = _goal.localPosition.x / 5f;
        float goalPositionZ_normalized = _goal.localPosition.z / 5f;

        float agentPosX_normalized = transform.localPosition.x / 5f;
        float agentPosZ_normalized = transform.localPosition.z / 5f;

        float agentRotation_normalized = (transform.localRotation.eulerAngles.y / 360f) * 2f - 1f;

        float agentVelocityX_normalized = _rb != null ? _rb.linearVelocity.x / 5f : 0f;
        float agentVelocityY_normalized = _rb != null ? _rb.linearVelocity.z / 5f : 0f;

        sensor.AddObservation(goalPositionX_normalized);
        sensor.AddObservation(goalPositionZ_normalized);
        sensor.AddObservation(agentPosX_normalized);
        sensor.AddObservation(agentPosZ_normalized);
        sensor.AddObservation(agentRotation_normalized);

        sensor.AddObservation(agentVelocityX_normalized);
        sensor.AddObservation(agentVelocityY_normalized);

        sensor.AddObservation(_isGrounded ? 1f : 0f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;

        // Move
        discreteActionsOut[0] = Input.GetKey(KeyCode.UpArrow) ? 1 : 0;

        // Rotate
        if (Input.GetKey(KeyCode.LeftArrow))
            discreteActionsOut[1] = 1;
        else if (Input.GetKey(KeyCode.RightArrow))
            discreteActionsOut[1] = 2;
        else
            discreteActionsOut[1] = 0;

        // Jump disabled: always 0 (we removed jump from actions)
        discreteActionsOut[2] = 0;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // движение агента
        MoveAgent(actions);

        // step penalty to encourage faster completion
        //AddReward(-2f / MaxStep);

        //AddReward(-0.01f);

        float dynamicPenalty = Mathf.Lerp(-0.001f, -0.01f, (float)StepCount / MaxStep);
        AddReward(dynamicPenalty);

        // update cumulative reward for display/debug
        CumulativeReward = GetCumulativeReward();

        // track per-episode values
        episodeReward = GetCumulativeReward();
        episodeStepCount = StepCount;
    }

    public void MoveAgent(ActionBuffers actions)
    {
        var moveAction = actions.DiscreteActions[0];
        var rotateAction = actions.DiscreteActions[1];
        //var jumpAction = actions.DiscreteActions[2]; // disabled

        if (moveAction == 1)
        {
            Vector3 moveDir = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            if (_rb != null)
            {
                _rb.MovePosition(_rb.position + moveDir * _moveSpeed * Time.fixedDeltaTime);
            }
            else
            {
                transform.position += moveDir * _moveSpeed * Time.fixedDeltaTime;
            }
        }

        if (rotateAction == 1)
        {
            transform.Rotate(0f, -_rotationSpeed * Time.deltaTime, 0f);
        }
        else if (rotateAction == 2)
        {
            transform.Rotate(0f, _rotationSpeed * Time.deltaTime, 0f);
        }

        // jump removed / commented out
        // if (jumpAction == 1 && _isGrounded) { ... }
    }

    private void FixedUpdate()
    {
        // prevent physics from rotating agent (so obstacles won't "spin" agent)
        if (_rb != null)
        {
            _rb.angularVelocity = Vector3.zero;
        }

        // lock tilt (x and z rotation) so agent doesn't tip over
        var rot = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, rot.y, 0f);

        // track travelled distance
        episodeDistance += Vector3.Distance(transform.position, lastPos);
        lastPos = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Goal"))
        {
            GoalReached();
        }

        if (other.gameObject.CompareTag("FallZone"))
        {
            AddReward(-0.5f);
            FailEpisode();
        }

        if (other.CompareTag("Checkpoint"))
        {
            // Assumes each checkpoint has a Checkpoint component with TryActivate() that returns true once per episode
            var checkpoint = other.GetComponent<Checkpoint>();
            if (checkpoint != null && checkpoint.TryActivate())
            {
                AddReward(0.5f);
                Debug.Log($"Checkpoint reached: {other.name}");
            }
        }
    }

    // private void GoalReached()
    // {
    //     AddReward(2.0f);
    //     AddReward(2f - (float)StepCount / MaxStep);

    //     CumulativeReward = GetCumulativeReward();

    //     episodeSuccess = true;
    //     episodeReward = GetCumulativeReward();
    //     episodeStepCount = StepCount;
    //     pathDistance = episodeDistance;

    //     // regenerate level if builder is present
    //     if (_segmentRouteBuilder != null)
    //     {
    //         _segmentRouteBuilder?.RegenerateLevel();
    //     }

    //     EndEpisodeWithStats(true);
    // }


    private void GoalReached()
    {
        // float baseReward = 2.0f;
        // float speedBonus = 2f * (1f - (float)StepCount / MaxStep); // от 0 до 2


        float t = (float)StepCount / MaxStep;
        float speedBonus = 2f * (1f - Mathf.Pow(t, 2)); // квадратичное усиление для быстрых


        AddReward(baseReward);
        AddReward(speedBonus);

        CumulativeReward = GetCumulativeReward();

        episodeSuccess = true;
        episodeReward = GetCumulativeReward();
        episodeStepCount = StepCount;
        pathDistance = episodeDistance;

        if (_segmentRouteBuilder != null)
            _segmentRouteBuilder.RegenerateLevel();

        EndEpisodeWithStats(true);
    }


    private void FailEpisode()
    {
        AddReward(-0.5f);
        episodeSuccess = false;
        episodeReward = GetCumulativeReward();
        episodeStepCount = StepCount;
        pathDistance = episodeDistance;

        EndEpisodeWithStats(false);
    }

    private void EndEpisodeWithStats(bool success)
    {
        // Write CSV line
        string line = $"{CurrentEpisode},{(success ? 1 : 0)},{episodeStepCount},{episodeReward:F3},{pathDistance:F3}\n";
        try
        {
            lock (fileLock)
            {
                // File.AppendAllText(metricsPath, line);
                File.AppendAllText(metricsPath, $"{CompletedEpisodes.ToString(_ci)},{(success ? 1 : 0)},{StepCount.ToString(_ci)},{CumulativeReward.ToString(_ci)},{episodeDistance.ToString(_ci)}\n");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write metrics to '{metricsPath}': {e}");
        }

        // Log to TensorBoard via StatsRecorder
        try
        {
            var stats = Academy.Instance.StatsRecorder;
            stats.Add("episode/success", success ? 1f : 0f);
            stats.Add("episode/length", episodeStepCount);
            stats.Add("episode/distance", pathDistance);
            stats.Add("episode/reward", episodeReward);
        }
        catch (Exception e)
        {
            // don't break training if StatsRecorder not available
            Debug.LogWarning($"Failed to write stats to StatsRecorder: {e.Message}");
        }

        EndEpisode();
    }

    private bool IsGapAhead()
    {
        Vector3 origin = transform.position + transform.forward * 0.5f + Vector3.up * 0.1f;
        return !Physics.Raycast(origin, Vector3.down, 1.0f, LayerMask.GetMask("Ground"));
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            _isGrounded = true;
        }

        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.1f);
        }

        if (collision.gameObject.CompareTag("Obstacle"))
        {
            AddReward(-0.1f);
        }

        if (collision.gameObject.CompareTag("RotatingObstacle"))
        {
            AddReward(-0.1f);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            AddReward(-0.1f * Time.fixedDeltaTime);
        }

        if (collision.gameObject.CompareTag("RotatingObstacle"))
        {
            AddReward(-0.1f * Time.fixedDeltaTime);
        }

        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.03f * Time.fixedDeltaTime);
        }
    }
}
