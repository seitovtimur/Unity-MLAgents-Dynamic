using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;


public class MoveToGoalAgent : Agent
{
    [SerializeField] private Transform _goal;
    [SerializeField] private Renderer _groundRenderer;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 180f;
    [SerializeField] private float _jumpForce = 5f;
    
    [SerializeField] private CheckpointsManager _checkpointManager;
    [SerializeField] private SegmentRouteBuilder _segmentRouteBuilder;


    private Rigidbody _rb;
    private Vector3 _startPos;
    private bool _isGrounded = true;
    private Renderer _renderer;


    //CHECKPOINTS
    
    private int _currentCheckpointIndex = 0;
    private float _prevCheckpointDistance = float.MaxValue;



    [HideInInspector] public int CurrentEpisode = 0;
    [HideInInspector] public float CumulativeRewared = 0f;

    private Color _defaultGroundColor;
    private Coroutine _flashGroundCoroutine;

    public override void Initialize()
    {
        Debug.Log("Initialize()");

        _renderer = GetComponent<Renderer>();

        CurrentEpisode = 0;
        CumulativeRewared = 0f;

        _rb = GetComponent<Rigidbody>();
        _startPos = transform.position;

        
        _currentCheckpointIndex = 0;
        _prevCheckpointDistance = float.MaxValue;
        

        if (_groundRenderer != null)
        {
            _defaultGroundColor = _groundRenderer.material.color;
        }
    }


    public override void OnEpisodeBegin()
    {
        Debug.Log("OnEpisodeBegin()");

        //ГЕНЕРАЦИЯ УРОВНЯ
        if (_segmentRouteBuilder == null)
        {
            Debug.LogError("SegmentRouteBuilder is not assigned. Please assign it in the inspector.");
            return;
        }

        _segmentRouteBuilder?.RegenerateLevel();
        

        if (_segmentRouteBuilder != null && _segmentRouteBuilder.GoalTransform != null)
        {
            _goal = _segmentRouteBuilder.GoalTransform;
        }
        else
        {
            // Эта ошибка теперь будет означать серьезную проблему в логике
            Debug.LogError("SegmentRouteBuilder не смог предоставить ссылку на новую цель!");
        }

        if (_groundRenderer != null && CumulativeRewared != 0f)
        {
            Color flashColor = (CumulativeRewared >= 0f) ? Color.green : Color.red;

            if (_flashGroundCoroutine != null)
            {
                StopCoroutine(_flashGroundCoroutine);
            }

            _flashGroundCoroutine = StartCoroutine(FlashGround(flashColor, 1.5f));
        }

        _currentCheckpointIndex = 0;


        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        transform.position = _startPos;
        transform.rotation = Quaternion.identity;

        _isGrounded = true;


        CurrentEpisode++;
        CumulativeRewared = 0f;
        _renderer.material.color = Color.cyan;
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
            // Если цели по какой-то причине нет, мы не можем собрать наблюдения.
            // Отправляем 8 нулевых значений, чтобы вектор наблюдений имел правильный размер.
            sensor.AddObservation(0f); // goalX
            sensor.AddObservation(0f); // goalZ
            sensor.AddObservation(0f); // agentX
            sensor.AddObservation(0f); // agentZ
            sensor.AddObservation(0f); // agentRot
            sensor.AddObservation(0f); // velocityX
            sensor.AddObservation(0f); // velocityZ
            sensor.AddObservation(0f); // isGrounded
            return; // Выходим из метода, чтобы избежать ошибки
        }
        

        float goalPositionX_normalized = _goal.localPosition.x / 5f;
        float goalPositionZ_normalized = _goal.localPosition.z / 5f;

        float agentPosX_normalized = transform.localPosition.x / 5f;
        float agentPosZ_normalized = transform.localPosition.z / 5f;

        float agentRotation_normalized = (transform.localRotation.eulerAngles.y / 360f) * 2f - 1f;

        float agentVelocityX_normalized = _rb.linearVelocity.x / 5f;
        float agentVelocityY_normalized = _rb.linearVelocity.z / 5f;


        

        sensor.AddObservation(goalPositionX_normalized);
        sensor.AddObservation(goalPositionZ_normalized);
        sensor.AddObservation(agentPosX_normalized);
        sensor.AddObservation(agentPosZ_normalized);
        sensor.AddObservation(agentRotation_normalized);

        sensor.AddObservation(agentVelocityX_normalized);
        sensor.AddObservation(agentVelocityY_normalized);

        sensor.AddObservation(_isGrounded ? 1f : 0f);

        //sensor.AddObservation(_goal.localPosition);

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

        // Jump
        discreteActionsOut[2] = Input.GetKey(KeyCode.Space) ? 1 : 0;

    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        //Move the agent using the action
        MoveAgent(actions);

        //Penalty given each step to encourage agent to finish quickly
        AddReward(-2f / MaxStep);

        //if (_rb.linearVelocity.magnitude < 0.1f)
        //{
        //    AddReward(-0.01f);
        //}

        //Updating reward
        CumulativeRewared = GetCumulativeReward();
    }



    public void MoveAgent(ActionBuffers actions)
    {

        var moveAction = actions.DiscreteActions[0];
        var rotateAction = actions.DiscreteActions[1];
        var jumpAction = actions.DiscreteActions[2];

        if (moveAction == 1)
        {
            Vector3 moveDir = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            _rb.MovePosition(_rb.position + moveDir * _moveSpeed * Time.fixedDeltaTime);
        }
        // �������� else! Rigidbody ��� ����� ��������� ����� Drag.


        if (rotateAction == 1)
        {
            transform.Rotate(0f, -_rotationSpeed * Time.deltaTime, 0f);
        }
        else if (rotateAction == 2)
        {
            transform.Rotate(0f, _rotationSpeed * Time.deltaTime, 0f);
        }

        if (jumpAction == 1 && _isGrounded)
        {
            _rb.AddForce(Vector3.up * _jumpForce, ForceMode.VelocityChange);
            _isGrounded = false;
        }
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

            EndEpisode();
        }


        if (other.CompareTag("Checkpoint"))
        {
            if (_checkpointManager != null && _checkpointManager.GetCheckpoint(_currentCheckpointIndex) == other.transform)
            {
                AddReward(0.5f); // ������� ������ �� ���������� ��������
                _currentCheckpointIndex++;
            }
        }

    }

 

    private void GoalReached()
    {
        AddReward(1.0f); 
        //AddReward(MaxStep / (StepCount * 100.0f));
        AddReward(1f - StepCount / MaxStep);

        CumulativeRewared = GetCumulativeReward();

        //_segmentRouteBuilder?.RegenerateLevel();

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

        //if (collision.gameObject.CompareTag("Checkpoint"))
        //{
        //    AddReward(0.5f);
        //    _isGrounded = true;
        //}

        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.25f);

            //EndEpisode();
        }

        if (collision.gameObject.CompareTag("Obstacle"))
        {
            AddReward(-0.1f);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            AddReward(-0.03f * Time.fixedDeltaTime);
        }

        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.03f * Time.fixedDeltaTime);

            //EndEpisode();
        }
    }



}
