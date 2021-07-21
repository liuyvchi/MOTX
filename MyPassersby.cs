using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class MyPassersby : MonoBehaviour
{
    private Rigidbody rigBody;
    private Animator animator;
    private MyMovePath movePath;
    private Passersby nearPassersby;
    private SemaphorePeople nearSemaphore;
    private Transform nearCar;
    private Transform nearPlayer;
    private AnimationState lastState;
    // [SerializeField] private float curMoveSpeed;
    private float moveSpeed;
    private float startWalkSpeed;
    private float startRunSpeed;
    private bool redSemaphore;
    private bool insideSemaphore;
    private bool tempStop;
    private bool timeToWalk = true;

    [Tooltip("Layers of car, traffic light, pedestrians, player / Слои автомобиля, светофора, пешеходов, игрока")] public LayerMask targetMask;
    [HideInInspector] public LayerMask obstacleMask;
    [SerializeField] private AnimationState animationState;
    [SerializeField] [Tooltip("Walking speed / Скорость ходьбы")] private float walkSpeed;
    [SerializeField] [Tooltip("Running speed / Скорость бега")] private float runSpeed;
    [SerializeField] [Tooltip("Swing speed / Скорость поворота")] private float speedRotation;
    [SerializeField] [Tooltip("delta time")] private float deltaTime;
    [SerializeField] [Tooltip("Viewing Angle / Угол обзора")] private float viewAngle;
    [SerializeField] [Tooltip("Radius of visibility / Радиус видимости")] private float viewRadius;
    [SerializeField] [Tooltip("Distance to pedestrian / Расстояние до пешехода")] private float distToPeople;

    [SerializeField] [Tooltip("Set your animation speed / Установить свою скорость анимации?")] private bool _overrideDefaultAnimationMultiplier;
    [SerializeField] [Tooltip("Speed animation walking / Скорость анимации ходьбы")] private float _customWalkAnimationMultiplier = 1.0f;
    [SerializeField] [Tooltip("Running animation speed / Скорость анимации бега")] private float _customRunAnimationMultiplier = 1.0f;

    public AnimationState ANIMATION_STATE
    {
        get { return animationState; }
        set { animationState = value; }
    }
    public AnimationState LastState
    {
        get{ return lastState;}
        set{lastState = value;}
    }
    public float WALK_SPEED
    {
        get { return walkSpeed; }
        set { walkSpeed = value; }
    }
    public float RUN_SPEED
    {
        get { return runSpeed; }
        set { runSpeed = value; }
    }
    public float SPEED_ROTATION
    {
        get { return speedRotation; }
        set { speedRotation = value; }
    }
    public float VIEW_ANGLE
    {
        get { return viewAngle; }
        set { viewAngle = value; }
    }
    public float DELTA_TIME
    {
        get {return deltaTime; }
        set {deltaTime = value; }
    }
    // public float VIEW_RADIUS
    // {
    //     get { return viewRadius; }
    //     set { viewRadius = value; }
    // }
    // public bool INSIDE
    // {
    //     get { return insideSemaphore; }
    //     set { insideSemaphore = value; }
    // }
    // public bool RED
    // {
    //     get { return redSemaphore; }
    //     set { redSemaphore = value; }
    // }
    // public float DIST_TO_PEOPLE
    // {
    //     get{return distToPeople;}
    //     set{distToPeople = value;}
    // }
    // public float CustomWalkAnimationMultiplier
    // {
    //     get { return _customWalkAnimationMultiplier; }
    //     set { _customWalkAnimationMultiplier = value; }
    // }
    // public float CustomRunAnimationMultiplier
    // {
    //     get {return _customRunAnimationMultiplier;}
    //     set {_customRunAnimationMultiplier = value;}
    // }
    // public bool OverrideDefaultAnimationMultiplier
    // {
    //     get { return _overrideDefaultAnimationMultiplier; }
    //     set { _overrideDefaultAnimationMultiplier = value; }
    // }

    private void Awake()
    {
        rigBody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        movePath = GetComponent<MyMovePath>();
    }

    private void Start()
    {
        lastState = animationState;
        startWalkSpeed = walkSpeed;
        startRunSpeed = runSpeed;

        animator.CrossFade(animationState.ToString(), 0.1f, 0, Random.Range(0.0f, 1.0f));
    }

    private void Update()
    {
        GetPath(); 
        animator.Play(animationState.ToString());
    }

    private void FixedUpdate()
    {
        
    }

    private void GetPath()
    {
        Vector3 targetPos = movePath.finishPos;

        var richPointDistance = Vector3.Distance(Vector3.ProjectOnPlane(rigBody.transform.position, Vector3.up), Vector3.ProjectOnPlane(targetPos, Vector3.up));

        Vector3 direction = targetPos - transform.position;

        if (direction != Vector3.zero)
        {
            Vector3 newDir = Vector3.zero;

            rigBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            newDir = Vector3.RotateTowards(transform.forward, direction, speedRotation * deltaTime, 0.0f);

            transform.rotation = Quaternion.LookRotation(newDir);
            //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), speedRotation * Time.deltaTime);
        }

        if (richPointDistance > movePath._walkPointThreshold)
        {  
            if (Time.deltaTime > 0)
            {
                // curMoveSpeed = moveSpeed;
                moveSpeed = walkSpeed + Random.Range(-0.1f, 0.1f);
                transform.position += transform.forward * moveSpeed * deltaTime;
                //rigBody.MovePosition(transform.position + transform.forward * curMoveSpeed * Time.fixedDeltaTime);
            }
        }
        else if (richPointDistance <= movePath._walkPointThreshold)
        {

            if (movePath.targetPoint != movePath.targetPointsTotal)
            {
                movePath.targetPoint++;

                movePath.finishPos = movePath.walkPath.getNextPoint(movePath.targetPoint);
                
                float newWalkSpeed = startWalkSpeed + Random.Range(-0.5f, 0.5f);

                walkSpeed = newWalkSpeed;
            }
            else if (movePath.targetPoint == movePath.targetPointsTotal)
            {
                DestroyMeshColliders(gameObject);
                Destroy(gameObject);
                // DestroyImmediate(gameObject);
            }
        }
    }
    
    public void DestroyMeshColliders(GameObject instance)
    {
        MeshCollider[] childCollider = instance.transform.GetComponentsInChildren<MeshCollider>();
        foreach (MeshCollider collider in childCollider)
        {
            Destroy(collider.sharedMesh);
        }
    }
}