using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;//引用此命名空间是用于数据的写入与读取
using System.Text; //引用这个命名空间是用于接下来用可变的字符串的
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;
#endif

public enum AnimationState
{
    idle1, walk, run
}

public class CamGenPath : MonoBehaviour
{
    public GameObject[] walkingPrefabs;

    // private Rigidbody rigBody;
    // private Animator animator;
    private Get_3D_bbox anotator;
    private Camera cam;
    private string camName;
    private float nextPointThreshold = 1;
    [HideInInspector] public GameObject walkingObjects;
    // private Rigidbody rigBody;

    public float walkSpeed =4.0f;
    public int peopleNum = 20;
    public int minDetectedNum;
    public int middlePointNum = 10;
    public int totalFrameNum = 500;
    public float deltaTime = 0.04f;
    public float camSpeed = 1.2f;
    public float speedRotation = 0.8f;
    public float speedRotationCam = 0.5f; 
    public float deltaCamHeight = 0.0f;
    public float deltaXEulerAngles  = 0.0f;
    [Tooltip("Animation of the pedestrian at the start")] public AnimationState animationState = AnimationState.walk;

    private List<GameObject> pfb;
    
    private List<Vector3> camPoint = new List<Vector3>();
    private bool movingCam = false;
    private int pIndex = 0;
    private Vector3 targetPos;
    float camPointThreshold = 0.5f;
    private float moveSpeed;

    // private vars for screenshot
    private Rect rect;
    private RenderTexture rt;
    private Texture2D cameraTexture;

    private Vector3 farLeftPoint = new Vector3(0, 0, 0);
    private Vector3 farRightPoint = new Vector3(0, 0, 0);
    private Vector3 nearLeftPoint = new Vector3(0, 0, 0);
    private Vector3 nearRightPoint = new Vector3(0, 0, 0);
    private Vector3 directFarL;
    private Vector3 directFarR;
    private Vector3 directL2R;
    private string[] BigDirect = {"Left2Right", "Right2Left", "Near2Far", "Far2Near"};

    public string sceneName = "multiviewX";
    private string saveDir = "/Users/yuchiliu/projects/beachtown/Recordings";
    private string saveFolder;
    private bool stopCam = false;
    private int frameNo = 1;

    // private void Awake()
    // {
    //     rigBody = GetComponent<Rigidbody>();
    //     animator = GetComponent<Animator>();
    // }

    // Start is called before the first frame update
    void Start()
    {
        Scene scene = SceneManager.GetActiveScene();
        sceneName = scene.name;
        cam = GetComponent<Camera>();
        // rigBody = GetComponent<Rigidbody>();
        camName = cam.name;
        int width = cam.pixelWidth;
        int height = cam.pixelHeight;
        int total_num = width * height;

        //对cam的位置做随机扰动
        Vector3 upWard = new Vector3(0, 1, 0);
        cam.transform.position = cam.transform.position + upWard*deltaCamHeight;
        cam.transform.Rotate(deltaXEulerAngles, 0, 0, Space.World);

        // init for cam
        if (cam.transform.Find("path") != null)
        {
            print("find path");
            GameObject path = cam.transform.Find("path").gameObject;
            GameObject points = path.transform.Find("points").gameObject;
            movingCam = true;
            Transform[] pointsTrans = points.transform.GetComponentsInChildren<Transform>();
            print("pointsTrans:"+pointsTrans.Length.ToString());
            foreach (Transform pTrans in pointsTrans)
            {  
                GameObject p = pTrans.gameObject;
                if (p.name != "points")
                {
                    print("camPoint:"+p.name.ToString());
                    print(p.transform.position);
                    float camHight = cam.transform.position.y;
                    Vector3 camPosition = p.transform.position;
                    camPosition.y = camHight + Random.Range(-0.1f, 0.1f);
                    camPoint.Add(camPosition);
                }
            }
            path.transform.parent = cam.transform.parent; //摆脱相机移动对路径点的影响
            Destroy(path);
        }   
        
        // init for persons
        if (walkingObjects == null)
        {
            walkingObjects = new GameObject();
            walkingObjects.transform.parent = gameObject.transform.parent;
            walkingObjects.name = "walkingObjects";
        }

        pfb = new List<GameObject>(walkingPrefabs);
        for (int i = pfb.Count - 1; i >= 0; i--)
        {
            if (pfb[i] == null)
            {
                pfb.RemoveAt(i);
            }
        }
        walkingPrefabs = pfb.ToArray();
        
        GetPathArea(ref farLeftPoint, ref farRightPoint, ref nearLeftPoint, ref nearRightPoint);
        directFarL = (farLeftPoint - nearLeftPoint).normalized;
        directFarR = (farRightPoint - nearRightPoint).normalized;
        directL2R = (nearRightPoint - nearLeftPoint).normalized;
        //scale the position of farLeftPoint and farRightPoint to be closer
        farLeftPoint = nearLeftPoint + directFarL*80f;
        farRightPoint = nearRightPoint + directFarR*80f;
        //scale the distance between nearLeftPoint and nearRightPoint into a bigger value
        nearLeftPoint = nearLeftPoint - (nearRightPoint - nearLeftPoint);
        nearRightPoint = nearRightPoint + (nearRightPoint - nearLeftPoint);

        float metaDistanceFar = Vector3.Distance(farLeftPoint, nearLeftPoint)/10;

        for (int i=1; i<=10; i++)
        {
            farLeftPoint = nearLeftPoint + directFarL*i*metaDistanceFar;
            Vector3 screenPos = cam.WorldToScreenPoint(farLeftPoint);
            if (screenPos.y < cam.pixelHeight && screenPos.y>0)
            {
                farRightPoint = nearRightPoint + directFarR*i*metaDistanceFar;
            }
            else
            {
                break;
            }
        }

        int GenPeopleNum = 0;
        while (GenPeopleNum<peopleNum)
        {

            float random = Random.Range(0.0f, 1.0f);
            if (random<1.0f)
            {
                SpawnPeople(false);
                GenPeopleNum+=1;
            }
            else
            {
                GenPeopleNum += SpawnPeopleGroup(false);
            }
            
        }

        print("GenPeopleNum->"+GenPeopleNum.ToString());
        string parentName = transform.parent.name;
        saveFolder =  saveDir +"/"+ parentName + "/" + parentName+camName+"P"+peopleNum.ToString();
        if (!Directory.Exists(saveFolder))
        {
    　　    Directory.CreateDirectory(saveFolder);
        }
        
        anotator = gameObject.AddComponent<Get_3D_bbox>();
    }

    public void GetPathArea(ref Vector3  farLeftPoint, ref Vector3 farRightPoint, ref Vector3 nearLeftPoint, ref Vector3 nearRightPoint)
    {
        cam = GetComponent<Camera>();
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
        Plane leftPlane = planes[0];
        Plane rightPlane = planes[1];
        Plane downPlane = planes[2];
        Plane nearPlane = planes[4];
        Plane farPlane = planes[5];
        
        float fov = cam.fieldOfView;
        float asp = cam.aspect;
        float yf = Mathf.Tan(fov/2 * Mathf.Deg2Rad);
        float xf = yf * asp;

        Vector3 f0 = cam.transform.forward - cam.transform.right * xf - cam.transform.up * yf;
        Vector3 f1 = cam.transform.forward - cam.transform.right * xf + cam.transform.up * yf;
        Vector3 f2 = cam.transform.forward + cam.transform.right * xf - cam.transform.up * yf;
        Vector3 f3 = cam.transform.forward + cam.transform.right * xf + cam.transform.up * yf;

        float fcp = cam.farClipPlane;
        float ncp = cam.nearClipPlane;
        Vector3 cpt = cam.transform.position;

        //eight coners 
        Vector3 farLeftBottom = cpt + fcp * f0;
        Vector3 farLeftTop = cpt + fcp * f1;
        Vector3 farRightBotoom = cpt + fcp*f2;
        Vector3 farRightTop = cpt + fcp * f3;
        Vector3 nearLeftBottom = cpt + ncp * f0;
        Vector3 nearLeftTop = cpt + ncp * f1;   
        Vector3 nearRightBotoom = cpt + ncp*f2;
        Vector3 nearRightTop = cpt + ncp * f3;

        farLeftPoint = new Vector3(farLeftTop.x, 0.0f, farLeftTop.z);
        farRightPoint = new Vector3(farRightTop.x, 0.0f, farRightTop.z);
        nearLeftPoint = new Vector3(nearLeftBottom.x, 0.0f, nearLeftBottom.z);
        nearRightPoint = new Vector3(nearRightBotoom.x, 0.0f, nearRightBotoom.z);
    }

    public bool checkStop()
    {
        return stopCam;
    }

    public void SpawnPeople(bool supply)
    {   
        List<Vector3> pathPoint = new List<Vector3>();
        GenPath(ref pathPoint);

        int prefabNum = Random.Range(0, pfb.Count);
        // var people = gameObject;
        GameObject people;
        int starPointIdx = 0;
        if (!supply)
        {
            starPointIdx = Random.Range(0, pathPoint.Count);
        }
        print("line221->"+starPointIdx.ToString());
        people = Instantiate(pfb[prefabNum], pathPoint[starPointIdx], Quaternion.identity) as GameObject;
        cleanWeapon(people);
        pfb.RemoveAt(prefabNum);
        var walkPath  = people.AddComponent<MyWalkPath>();
        var movePath = people.AddComponent<MyMovePath>();
        var passersby = people.AddComponent<MyPassersby>();
        walkPath.pathPoint = pathPoint;
        movePath.walkPath = walkPath;
        movePath._walkPointThreshold = nextPointThreshold;

        movePath.InitStartPosition(starPointIdx);
        movePath.SetLookPosition();

        InitializePassersby(ref passersby, 0.1f);

        
        people.transform.parent = walkingObjects.transform;

    }

    public int SpawnPeopleGroup(bool supply)
    {   
        List<Vector3> pathPoint = new List<Vector3>();
        GenPath(ref pathPoint);

        int peopleCount = Random.Range(3, 10);
        int starPointIdx = Random.Range((int)(0.3*pathPoint.Count), (int)(0.7*pathPoint.Count));
        

        for(int i=0; i<peopleCount; i++)
        {
            int prefabNum = Random.Range(0, pfb.Count);
            // var people = gameObject;
            GameObject people;
            
            print("line221->"+starPointIdx.ToString());
            Vector3 startPoint = pathPoint[starPointIdx];
            startPoint.x= startPoint.x + Random.Range(-1.0f, 1.0f);
            startPoint.z= startPoint.z + Random.Range(-1.0f, 1.0f);
            people = Instantiate(pfb[prefabNum], startPoint, Quaternion.identity) as GameObject;
            cleanWeapon(people);
            pfb.RemoveAt(prefabNum);
            var walkPath  = people.AddComponent<MyWalkPath>();
            var movePath = people.AddComponent<MyMovePath>();
            var passersby = people.AddComponent<MyPassersby>();
            walkPath.pathPoint = pathPoint;
            movePath.walkPath = walkPath;
            movePath._walkPointThreshold = nextPointThreshold;

            movePath.InitStartPosition(starPointIdx);
            movePath.SetLookPosition();

            InitializePassersby(ref passersby, 1.0f);

            people.transform.parent = walkingObjects.transform;
            
        }
  
        return peopleCount;
    }

    public void cleanWeapon(GameObject instance)
    {
        Transform weaponTrans = instance.transform.Find("WeaponLeft");
        if (weaponTrans != null) 
        {
            Destroy(weaponTrans.gameObject);
        }
        weaponTrans = instance.transform.Find("WeaponRight");
        if (weaponTrans != null) 
        {
            Destroy(weaponTrans.gameObject);
        }
    }

    public void GenPath(ref List<Vector3> pathPoint)
    {
        int bigDirIdx = (int)Random.Range(0, 4);
        string bigDir = BigDirect[bigDirIdx];
        float metaDistanceFar = Vector3.Distance(farLeftPoint, nearLeftPoint)/10;
        float metaDistanceL2RNear = Vector3.Distance(nearLeftPoint, nearRightPoint)/10;
        float metaDistanceL2RFar = Vector3.Distance(farLeftPoint, farRightPoint)/10;
        Vector3 starPosition;
        Vector3 endPosition;
        List<Vector3> middlePositions; 
        Vector3 directMove;
        float metaDistanceMove;
        float starMetaNum = Random.Range(0, 10);
        float endMetaNum = Random.Range(0, 10);
        Vector3 yAxis = new Vector3(0.0f, 1.0f, 0.0f);
        Vector3 directRandom = new Vector3(0.0f, 0.0f, 0.0f);
        
        middlePointNum = Random.Range(10, 50);
        switch (bigDir)
        {    
            case "Left2Right":
                starPosition = nearLeftPoint + directFarL*starMetaNum*metaDistanceFar;
                pathPoint.Add(starPosition);
                endPosition = nearRightPoint  + directFarR*endMetaNum*metaDistanceFar;
                directMove = (endPosition - starPosition).normalized;
                directRandom = Vector3.Normalize(Vector3.Cross(directMove, yAxis));
                metaDistanceMove = Vector3.Distance(endPosition, starPosition)/middlePointNum;
                // randomMove = Random.Range(0.0f, 10.0f);
                for (int i=1; i<=middlePointNum; i++)
                {
                    Vector3 middleP = starPosition + directMove*i*metaDistanceMove;
                    float randomP = Random.Range(0,1.0f);
                    float random = Random.Range(0,1);
                    if (random < 0.5)
                    {
                        middleP += directRandom*metaDistanceMove;
                    }
                    else
                    {
                        middleP += -directRandom*metaDistanceMove;
                    }
                    pathPoint.Add(middleP);
                }
                pathPoint.Add(endPosition);
                break;
            case "Right2Left":
                starPosition = nearRightPoint + directFarR*starMetaNum*metaDistanceFar;
                pathPoint.Add(starPosition);
                endPosition = nearLeftPoint  + directFarL*endMetaNum*metaDistanceFar;
                directMove = (endPosition - starPosition).normalized;
                directRandom = Vector3.Normalize(Vector3.Cross(directMove, yAxis));
                metaDistanceMove = Vector3.Distance(endPosition, starPosition)/middlePointNum;
                for (int i=1; i<=middlePointNum; i++)
                {
                    Vector3 middleP = starPosition + directMove*i*metaDistanceMove;
                    float randomP = Random.Range(0,1.0f);
                    float random = Random.Range(0,1);
                    if (random < 0.5)
                    {
                        middleP += directRandom*metaDistanceMove;
                    }
                    else
                    {
                        middleP += -directRandom*metaDistanceMove;
                    }
                    pathPoint.Add(middleP);
                }
                pathPoint.Add(endPosition);
                break;
     
            case "Near2Far":
                if (Random.Range(0,1) < 0.5)    
                {
                    starPosition = nearLeftPoint - directL2R*starMetaNum*metaDistanceL2RNear;
                }
                else
                {
                    starPosition = nearRightPoint + directL2R*starMetaNum*metaDistanceL2RNear;
                }
                
                pathPoint.Add(starPosition);
                endPosition = farLeftPoint  + directL2R*endMetaNum*metaDistanceL2RFar;
                directMove = (endPosition - starPosition).normalized;
                directRandom = Vector3.Normalize(Vector3.Cross(directMove, yAxis));
                metaDistanceMove = Vector3.Distance(endPosition, starPosition)/middlePointNum;
                for (int i=1; i<=middlePointNum; i++)
                {
                    Vector3 middleP = starPosition + directMove*i*metaDistanceMove;
                    float randomP = Random.Range(0,1.0f);
                    float random = Random.Range(0,1);
                    if (random < 0.5)
                    {
                        middleP += directRandom*metaDistanceMove;
                    }
                    else
                    {
                        middleP += -directRandom*metaDistanceMove;
                    }
                    pathPoint.Add(middleP);
                }
                pathPoint.Add(endPosition);
                break;

            case "Far2Near":
                starPosition = farLeftPoint  + directL2R*endMetaNum*metaDistanceL2RFar;
                pathPoint.Add(starPosition);
                if (Random.Range(0,1) < 0.5)    
                {
                    endPosition = nearLeftPoint - directL2R*starMetaNum*metaDistanceL2RNear;
                }
                else
                {
                    endPosition = nearRightPoint + directL2R*starMetaNum*metaDistanceL2RNear;
                }
                    
                directMove = (endPosition - starPosition).normalized;
                directRandom = Vector3.Normalize(Vector3.Cross(directMove, yAxis));
                metaDistanceMove = Vector3.Distance(endPosition, starPosition)/middlePointNum;
                for (int i=1; i<=middlePointNum; i++)
                {
                    Vector3 middleP = starPosition + directMove*i*metaDistanceMove;
                    float randomP = Random.Range(0,1.0f);
                    float random = Random.Range(0,1);
                    if (random < 0.5)
                    {
                        middleP += directRandom*metaDistanceMove;
                    }
                    else
                    {
                        middleP += -directRandom*metaDistanceMove;
                    }
                    pathPoint.Add(middleP);
                }
                pathPoint.Add(endPosition);
                break;
        }
    }

    private void InitializePassersby(ref MyPassersby _passersby, float staticRatio)
    {
       
        float ifStatic = Random.Range(0.0f, 1.0f);
        if (ifStatic <= staticRatio)
        {
            _passersby.WALK_SPEED = 0;
             _passersby.ANIMATION_STATE = AnimationState.idle1;
        }
        else
        {
            _passersby.WALK_SPEED = walkSpeed+Random.Range(-0.5f, 0.5f);
            // _passersby.WALK_SPEED = walkSpeed+Random.Range(-0.1f, 0.1f);
             _passersby.ANIMATION_STATE = AnimationState.walk;
        }
        
        // _passersby.RUN_SPEED = runSpeed;
        _passersby.SPEED_ROTATION = speedRotation;
        _passersby.DELTA_TIME = deltaTime;


        // _passersby.VIEW_ANGLE = viewAngle;
        // _passersby.VIEW_RADIUS = viewRadius;
        // _passersby.targetMask = targetMask;
        // _passersby.obstacleMask = obstacleMask;
        // _passersby.DIST_TO_PEOPLE = distToPeople;

        // _passersby.OverrideDefaultAnimationMultiplier = _overrideDefaultAnimationMultiplier;
        // _passersby.CustomWalkAnimationMultiplier = _customWalkAnimationMultiplier;
        // _passersby.CustomRunAnimationMultiplier = _customRunAnimationMultiplier;
    }

    public void SavePicture(string saveFolder, int frameNo)
    {
        //等待所有的摄像机和GUI被渲染完成
        // yield return new WaitForEndOfFrame();
        GetCameraTexture(saveFolder, frameNo);
    }

    private void GetCameraTexture(string saveFolder, int frameNo)
    {
        if (rt == null)
        {
            rect = new Rect(0, 0, Screen.width, Screen.height);
            rt = new RenderTexture(Screen.width, Screen.height, 16);
            cameraTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        }
        cam = GetComponent<Camera>();
        // RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 16);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        cameraTexture.ReadPixels(rect, 0, 0);
        cameraTexture.Apply();

        string cameraTexPath = saveFolder + "/" + frameNo.ToString() + ".jpg";
        System.IO.File.WriteAllBytes(cameraTexPath, cameraTexture.EncodeToJPG());
        
        // reset active camera texture and render texture
        cam.targetTexture = null;
        RenderTexture.active = null;
    }

    public void moveCam()
    {
        Vector3 targetPos = camPoint[pIndex];
        cam = GetComponent<Camera>();
        var richPointDistance = Vector3.Distance(Vector3.ProjectOnPlane(transform.position, Vector3.up), Vector3.ProjectOnPlane(targetPos, Vector3.up));
        Vector3 direction = targetPos - transform.position;

        if (direction != Vector3.zero)
        {
            Vector3 newDir = Vector3.zero;
            float xAngle = transform.eulerAngles.x;
            // rigBody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            newDir = Vector3.RotateTowards(transform.forward, direction, speedRotationCam * deltaTime, 0.0f);
            transform.rotation = Quaternion.LookRotation(newDir);
            
            //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), speedRotation * Time.deltaTime);
        }

        // judge if came reach the camera destination
        if (richPointDistance > camPointThreshold)
        { 
            if (Time.deltaTime > 0)
            {
                // curMoveSpeed = moveSpeed;
                //相机运动
                moveSpeed = camSpeed + Random.Range(-0.1f, 0.1f);
                transform.position += direction * moveSpeed * deltaTime;
                //相机抖动
                Vector3 shake = Vector3.zero;
                shake = new Vector3(Random.Range(-0.02f, 0.02f), 0, Random.Range(-0.02f, 0.02f));
                transform.position = transform.position + shake;
                //rigBody.MovePosition(transform.position + transform.forward * curMoveSpeed * Time.fixedDeltaTime);
            }
        }
        else if (richPointDistance <= camPointThreshold)
        {
            if (pIndex != camPoint.Count -1)
            {
                targetPos = camPoint[pIndex];
                pIndex+=1;
            }
            else if (pIndex == camPoint.Count -1)
            {
                stopCam = true;
                // UnityEditor.EditorApplication.isPlaying = false;
            }
        }

        GetPathArea(ref farLeftPoint, ref farRightPoint, ref nearLeftPoint, ref nearRightPoint);
        directFarL = (farLeftPoint - nearLeftPoint).normalized;
        directFarR = (farRightPoint - nearRightPoint).normalized;
        directL2R = (nearRightPoint - nearLeftPoint).normalized;
        //scale the position of farLeftPoint and farRightPoint to be closer
        farLeftPoint = nearLeftPoint + directFarL*80f;
        farRightPoint = nearRightPoint + directFarR*80f;
        //scale the distance between nearLeftPoint and nearRightPoint into a bigger value
        nearLeftPoint = nearLeftPoint - (nearRightPoint - nearLeftPoint);
        nearRightPoint = nearRightPoint + (nearRightPoint - nearLeftPoint);
        
        
        float metaDistanceFar = Vector3.Distance(farLeftPoint, nearLeftPoint)/10;

        for (int i=1; i<=10; i++)
        {
            farLeftPoint = nearLeftPoint + directFarL*i*metaDistanceFar;
            Vector3 screenPos = cam.WorldToScreenPoint(farLeftPoint);
            if (screenPos.y < cam.pixelHeight && screenPos.y>0)
            {
                farRightPoint = nearRightPoint + directFarR*i*metaDistanceFar;
            }
            else
            {
                break;
            }
        }

    }

    static public bool IsAPointInACamera(Camera cam, Vector3 wordPos)
    {
        // 是否在视野内
        bool result1 = false;
        Vector3 posViewport = cam.WorldToViewportPoint(wordPos);
        Rect rect = new Rect(0, 0, 1, 1);
        result1 = rect.Contains(posViewport);
        // 是否在远近平面内
        bool result2 = false;
        if(posViewport.z >= cam.nearClipPlane && posViewport.z<=cam.farClipPlane)
        {
            result2 = true;
        }
        // 综合判断
        bool result = result1 && result2;
        Debug.Log("result:" + result.ToString());
        return result;
    }

    public void Doupdate()
    {
        if (pfb.Count == 0)
        {
            print("person prefabs are not enough to generate new people");
            // Application.Quit();
            stopCam = true;
        }
        
        

        // update the camera position
        if (movingCam)
        {
            moveCam();
            
        }
        //相机抖动
        Vector3 shake = Vector3.zero;
        shake = new Vector3(Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f));
        transform.position = transform.position + shake;

        // get the bbox of objects by anotator
        
        if (frameNo <= totalFrameNum)
        {
            SavePicture(saveFolder, frameNo);

            // anotator = GetComponent<Get_3D_bbox>();
            anotator.DoUpdate(sceneName, frameNo);
        }
        else
        {
            // UnityEditor.EditorApplication.isPlaying = false;
            stopCam = true;
        }

        // add people if peopeles in current camera is not enough
        if (anotator.detectedNum < peopleNum)
        {
            print("generate new people");
            SpawnPeople(true);
            
                
        }
        frameNo+=1;
        // //clear perosn who are not in this camera
        // Transform[] walkTrans = walkingObjects.transform.GetComponentsInChildren<Transform>();
        // foreach (Transform walkTran in walkTrans)
        // {
        //     GameObject w = walkTran.gameObject;
        //     if (w.name != "walkingObjects")
        //     {
        //         if (!IsAPointInACamera(cam, walkTran.position))
        //         {
        //             Destroy(w);
        //         }
        //     }
        // }

    }

}
