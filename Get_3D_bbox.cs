using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using UnityEditor.Recorder;
// using UnityEditor.Recorder.Input;
#if UNITY_EDITOR
using UnityEditor;
#endif


public static class RendererExtensions
{
	public static bool IsVisibleFrom(this Renderer renderer, Camera camera)
	{
		Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
		return GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
	}
}

public class Get_3D_bbox : MonoBehaviour
{
    public Renderer rend;
    private float timePass = 0.0f;
    public Hashtable staticColliderTable = new Hashtable();
    public Hashtable dynamicColliderTable = new Hashtable();
    public Hashtable segmentationTable = new Hashtable();
    public int detectedNum = 0;

    StringBuilder meshPoints = new StringBuilder(1000000);
    StringBuilder sb = new StringBuilder(1000000);
    private string pathMesh;

    private Camera cam;
    private string camName;
    private string saveDir = "/Users/yuchiliu/projects/beachtown/bbox";
    private string saveFolder;

    // Start is called before the first frame update
    void Awake()
    {
        // rend = GetComponent<Renderer>();
        Clean();
        addTag();
        addMeshColliderByTag("SceneObject");
        addMeshColliderByTag("People");


        // DoUpdate(1);
        //Application.targetFrameRate = 30;
        // print(Application.dataPath);
        // RecordHook.instance.onFrame += DoUpdate;

        cam = GetComponent<Camera>();
        camName = cam.name;

        // pathMesh = Application.dataPath + "/bbox/" + camName + "_meshpoints.txt";
        // if (System.IO.File.Exists(pathMesh))
        // {
        //     try
        //     {
        //         System.IO.File.Delete(pathMesh);
        //     }
        //     catch (System.IO.IOException e)
        //     {
        //         Console.WriteLine(e.Message);
        //         return;
        //     }
        // }
    }

    public void Clean()
    {
        GameObject[] peopObjects = GameObject.FindGameObjectsWithTag("People");
        foreach (GameObject p in peopObjects)
        {
            if (p.GetComponent<BoxCollider>() != null)
            {   
                DestroyImmediate(p.GetComponent<BoxCollider>());
            }
            if (p.GetComponent<Collider>() != null)
            {   
                Collider[] clliders = p.transform.GetComponentsInChildren<Collider>();
                foreach (Collider c in clliders) 
                {
                    DestroyImmediate(c);
                }
            }
            if (p.GetComponent<Rigidbody>() != null)
            {
                Rigidbody rigibody =  p.GetComponent<Rigidbody>();
                rigibody.isKinematic = true;
            }
            Transform[] childrenTrans = p.transform.GetComponentsInChildren<Transform>();
            foreach (Transform childTrans in childrenTrans)
            {   
                if (childTrans == null){
                    continue;
                }
                GameObject child = childTrans.gameObject;
                if (child.GetComponent<BoxCollider>() != null)
                {   
                    DestroyImmediate(child.GetComponent<BoxCollider>());
                }
                if (child.GetComponent<Rigidbody>() != null)
                {
                    Rigidbody rigibody =  child.GetComponent<Rigidbody>();
                    rigibody.isKinematic = true;
                }
            }
        }
    }

    public void addTag ()
    {
        GameObject[] peopObjects = GameObject.FindGameObjectsWithTag("People");
        foreach (GameObject p in peopObjects)
        {
            Transform[] childrenTrans = p.transform.GetComponentsInChildren<Transform>();
            foreach (Transform childTrans in childrenTrans)
            {   
                if (childTrans == null){
                    continue;
                }
                GameObject child = childTrans.gameObject;
                if (child.tag == null)
                {
                    child.tag = "child";
                    continue;
                }
                if (child.tag == "People" || child.tag == "child")
                {
                    continue;
                }
                child.tag  = "child";
            }
        }
    }

    public void addMeshColliderByTag(string tag)
    {   
        
        if (tag == "People"){
            GameObject[] peopObjects = GameObject.FindGameObjectsWithTag("People");
            foreach (GameObject p in peopObjects)
            {      
                addMeshColliderDynamically(p);
            }
        }
        else if(tag == "SceneObject")
        {   
            GameObject[] SceneObjects = GameObject.FindGameObjectsWithTag("SceneObject");
            print ("Sceneobjets"+SceneObjects.Length.ToString());
            foreach (GameObject obj in SceneObjects)
            {      
                addMeshColliderStatically(obj);
            }

            // GameObject [] allGameObjects = FindObjectsOfType(typeof(GameObject)) as GameObject[];
            // GameObject[] otherObjects;
            // print ("allObjects"+allGameObjects.Length.ToString());
            // foreach(GameObject gameObject in allGameObjects)
            // {
            //     if (gameObject.tag ==  "Untagged")
            //     {
            //         addMeshColliderStatically(gameObject);
            //     }
            // }
        }
    }

    public void addMeshColliderStatically(GameObject instance)
    {   
        Transform[] childrenTrans = instance.transform.GetComponentsInChildren<Transform>();
        foreach (Transform childTrans in childrenTrans)
        {   
            bool childHasMesh = false;
            if (childTrans == null){
                continue;
            }
            Mesh ChildMesh = new Mesh();
            GameObject child = childTrans.gameObject;   
        
            if (child.GetComponent<MeshFilter>() != null)
            {   
                ChildMesh = child.GetComponent<MeshFilter>().mesh;
                childHasMesh =true;
            }
            else if(child.GetComponent<SkinnedMeshRenderer>() != null)
            {
                childHasMesh = true;
                ChildMesh = child.GetComponent<SkinnedMeshRenderer>().sharedMesh;
            }
            if (childHasMesh)
            {
                MeshCollider meshCollider;
                if (child.GetComponent<MeshCollider>() == null)
                {
                    meshCollider = child.AddComponent<MeshCollider>();
                }
                else
                {
                    meshCollider = child.GetComponent<MeshCollider>();
                }

                try{
                    meshCollider.convex = false;
                }
                catch{
                    meshCollider.convex = true;
                }
                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh =ChildMesh;

                if (!staticColliderTable.ContainsKey(meshCollider))
                {
                    staticColliderTable.Add(meshCollider, instance.name.ToString());
                }
            }   
        }    
    }

    public void addMeshColliderDynamically(GameObject instance)
    {      
        DestroyMeshColliders(instance);
        Transform[] childrenTrans = instance.transform.GetComponentsInChildren<Transform>();
        foreach (Transform childTrans in childrenTrans)
        {   
            bool childHasMesh = false;
            if (childTrans == null){
                continue;
            }
            Mesh ChildMesh = new Mesh();
            GameObject child = childTrans.gameObject;
            if (child.GetComponent<MeshFilter>() != null)
            {   
                ChildMesh = child.GetComponent<MeshFilter>().mesh;
                childHasMesh =true;
            }
            else if(child.GetComponent<SkinnedMeshRenderer>() != null)
            {
                childHasMesh = true;
                SkinnedMeshRenderer renderer = child.GetComponent<SkinnedMeshRenderer>();
                Mesh mesh = new Mesh();
                renderer.BakeMesh(mesh);
                Vector3[] vertices = mesh.vertices;
                GameObject GO = renderer.gameObject;
                float scale =  1.0f/GO.transform.lossyScale.y;

                for (int i=0;i<vertices.Length;i++)
                {
                    vertices[i] = vertices[i]*scale ;
                
                }
                mesh.vertices = vertices;
                ChildMesh  = mesh;
            }
            if (childHasMesh)
            {
                MeshCollider meshCollider;
                if (child.GetComponent<MeshCollider>() == null)
                {
                    meshCollider = child.AddComponent<MeshCollider>();
                }
                else
                {
                    meshCollider = child.GetComponent<MeshCollider>();
                }
                meshCollider.convex = false;
                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = ChildMesh;

                if (!dynamicColliderTable.ContainsKey(meshCollider))
                {
                    dynamicColliderTable.Add(meshCollider, instance.name.ToString());
                }
            }
        }    
    }

    public bool CheckOcclusion (GameObject instance)
    {
        float sampleRate = 0.10f;

        cam = GetComponent<Camera>();
        //bool isOcc = true;
        int total_points_num = 0;
        int hit_points_num = 0;



        if (instance.GetComponent<MeshFilter>() != null)
        {   
            Mesh instance_mesh;
            instance_mesh = instance.GetComponent<MeshFilter>().mesh;
            Vector3[] vertices = instance_mesh.vertices;
            foreach (Vector3 v in vertices)
            {
                if (UnityEngine.Random.Range(0.0f, 1.0f) > sampleRate)
                {
                    continue;
                }
                Vector3 worldPoints = instance.transform.TransformPoint(v);         
                Vector3 screenPos = cam.WorldToScreenPoint(worldPoints);
                Ray ray = cam.ScreenPointToRay(screenPos);
                RaycastHit hit;
                total_points_num++;
                if ( Physics.Raycast( ray, out hit ) )
                {
                    // a collision occured. Check if it's our plane object and create our cube at the
                    // collision point, facing toward the collision normal
                    if( hit.collider == instance.GetComponent<MeshCollider>())
                    {
                        // Instantiate( yourCubePrefab, hit.point, Quaternion.LookRotation( hit.normal ) ); 
                        //isOcc = false;
                        //return isOcc;
                        hit_points_num++;
                    }
          
                }
                
            }
        }
        else if(instance.GetComponent<SkinnedMeshRenderer>() != null)
        {
            SkinnedMeshRenderer renderer = instance.GetComponent<SkinnedMeshRenderer>();
            Mesh mesh = new Mesh();
            renderer.BakeMesh(mesh);
            Vector3[] vertices = mesh.vertices;
            GameObject GO = renderer.gameObject;
            float scale =  1.0f/GO.transform.lossyScale.y;

            for (int i=0;i<vertices.Length;i++)
            {
                vertices[i] = vertices[i]*scale ;
            
            }
            foreach (Vector3 v in vertices)
            {
                if (UnityEngine.Random.Range(0.0f, 1.0f) > sampleRate)
                {
                    continue;
                }
                Vector3 worldPoints = instance.transform.TransformPoint(v);         
                Vector3 screenPos = cam.WorldToScreenPoint(worldPoints);
                Ray ray = cam.ScreenPointToRay(screenPos);
                RaycastHit hit;
                total_points_num++;
                if ( Physics.Raycast( ray, out hit ) )
                {
                    // a collision occured. Check if it's our plane object and create our cube at the
                    // collision point, facing toward the collision normal
                    if( hit.collider == instance.GetComponent<MeshCollider>())
                    {
                        //isOcc = false;
                        //return isOcc;
                        hit_points_num++;
                    }
                        // Instantiate( yourCubePrefab, hit.point, Quaternion.LookRotation( hit.normal ) ); 
                }
            }
        }
    
        Transform[] childrenTrans = instance.transform.GetComponentsInChildren<Transform>();
        foreach (Transform childTrans in childrenTrans)
        {   
            bool childHasMesh = false;
            if (childTrans == null){
                continue;
            }
            Mesh ChildMesh;
            GameObject child = childTrans.gameObject;
        
            if (child.GetComponent<MeshFilter>() != null)
            {   
                ChildMesh = child.GetComponent<MeshFilter>().mesh;
                Vector3[] vertices = ChildMesh.vertices;
                foreach (Vector3 v in vertices)
                {
                    if (UnityEngine.Random.Range(0.0f, 1.0f) > sampleRate)
                    {
                        continue;
                    }
                    Vector3 worldPoints = child.transform.TransformPoint(v);         
                    Vector3 screenPos = cam.WorldToScreenPoint(worldPoints);
                    Ray ray = cam.ScreenPointToRay(screenPos);
                    RaycastHit hit;
                    total_points_num++;
                    if( Physics.Raycast( ray, out hit ) )
                    {
                        // a collision occured. Check if it's our plane object and create our cube at the
                        // collision point, facing toward the collision normal
                        //if (instance.name == "A160(clone)")
                        //{
                        //    Debug.DrawRay(cam.transform.position, hit.point, Color.red);
                        //}
                        if( hit.collider == child.GetComponent<MeshCollider>())
                        {
                            hit_points_num++;
                            // Instantiate( yourCubePrefab, hit.point, Quaternion.LookRotation( hit.normal ) ); 
                            //isOcc = false;
                            //return isOcc;
                        }            
                    }
                }

            }
            else if(child.GetComponent<SkinnedMeshRenderer>() != null)
            {
                childHasMesh = true;
                SkinnedMeshRenderer renderer = child.GetComponent<SkinnedMeshRenderer>();
                Mesh mesh = new Mesh();
                renderer.BakeMesh(mesh);
                Vector3[] vertices = mesh.vertices;
                GameObject GO = renderer.gameObject;
                float scale =  1.0f/GO.transform.lossyScale.y;

                for (int i=0;i<vertices.Length;i++)
                {
                    vertices[i] = vertices[i]*scale ;
                
                }
                foreach (Vector3 v in vertices)
                {
                    if (UnityEngine.Random.Range(0.0f, 1.0f) > sampleRate)
                    {   
                        continue;
                    }
                    Vector3 worldPoints = child.transform.TransformPoint(v);         
                    Vector3 screenPos = cam.WorldToScreenPoint(worldPoints);
                    Ray ray = cam.ScreenPointToRay(screenPos);
                    RaycastHit hit;
                    total_points_num++;
                    if( Physics.Raycast( ray, out hit ))
                    {
                        // a collision occured. Check if it's our plane object and create our cube at the
                        // collision point, facing toward the collision normal
                        //if (instance.name == "A160(clone)")
                        //{
                        //    Debug.DrawRay(cam.transform.position, hit.point, Color.red);
                        //}
                        if( hit.collider == child.GetComponent<MeshCollider>())
                        {
                            hit_points_num++;
                            // Instantiate( yourCubePrefab, hit.point, Quaternion.LookRotation( hit.normal ) ); 
                            //isOcc = false;
                            //return isOcc;
                        }   
                    }
                }
            }
        }
        //return isOcc;
        float OccThresh = 0.20f;
        float hitPortion = (float)hit_points_num / (float)total_points_num;
        if(hitPortion < OccThresh)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool CheckOcclusionV2(int Frame_Num, GameObject instance)
    {
        float sampleRate = 0.3f;
        HashSet<Tuple<int, int>> visited = new HashSet<Tuple<int, int>>();

        cam = GetComponent<Camera>();
        //bool isOcc = true;
        int total_points_num = 0;
        int hit_points_num = 0;

        Transform[] childrenTrans = instance.transform.GetComponentsInChildren<Transform>();
        foreach (Transform childTrans in childrenTrans)
        {
            
            bool childHasMesh = false;
            if (childTrans == null)
            {
                continue;
            }
            Mesh ChildMesh;
            GameObject child = childTrans.gameObject;

            bool saveClliderMesh = true;

            if (child.GetComponent<MeshCollider>() != null)
            {
                ChildMesh = child.GetComponent<MeshCollider>().sharedMesh;
                Vector3[] vertices = ChildMesh.vertices;
                foreach (Vector3 v in vertices)
                {
                    Vector3 worldPoints = child.transform.TransformPoint(v);
                    Vector3 screenPos = cam.WorldToScreenPoint(worldPoints);
                    var pos = new Tuple<int, int>((int)screenPos.x, (int)screenPos.y);
                    if (visited.Contains(pos))
                    {
                        continue;
                    }
                    visited.Add(pos);

                    if (UnityEngine.Random.Range(0.0f, 1.0f) > sampleRate)
                    {
                        continue;
                    }
                    total_points_num++;
                    // check whether the pixel has already been in the HashTable
                    if (segmentationTable.ContainsKey(pos))
                    {
                        if (segmentationTable[pos] == instance.name.ToString())
                        {
                            hit_points_num++;
                        }
                        continue;
                    }
                    // checkHashTable end
                    
                    Ray ray = cam.ScreenPointToRay(screenPos);
                    RaycastHit hit;
                    
                    
                    if (Physics.Raycast(ray, out hit))
                    {                 
                        if (staticColliderTable.ContainsKey(hit.collider))
                        {
                            if (!segmentationTable.ContainsKey(pos))
                            {
                                segmentationTable.Add(pos, staticColliderTable[hit.collider]);
                            }
                        }
                        else if (dynamicColliderTable.ContainsKey(hit.collider))
                        {   
                            if (!segmentationTable.ContainsKey(pos))
                            {
                                segmentationTable.Add(pos, dynamicColliderTable[hit.collider]);
                            }
                            if (dynamicColliderTable[hit.collider].ToString() == instance.name.ToString())
                            {
                                hit_points_num++;
                            }
                        }
                        else
                        {
                            if (!segmentationTable.ContainsKey(pos))
                            {
                                segmentationTable.Add(pos, null);
                            }
                        }
                    }
                }
            }
        }
        //return isOcc;
        if(total_points_num == 0){
            return false;
        }
            
        float OccThresh = 0.20f;
        float hitPortion = (float)hit_points_num / (float)total_points_num;
        if (hitPortion < OccThresh)
        {
            return true;
        }
        else
        {
            return false;
        }
    }


    Vector3[] GetBoxColliderVertexPositions(Transform transform)
    {
        var vertices = new Vector3[9];

        vertices[0] = transform.position + new Vector3(0.18f, 0f, 0.18f);
        vertices[1] = transform.position + new Vector3(-0.18f, 0f, 0.18f) ;
        vertices[2] = transform.position + new Vector3(-0.18f, 0f, -0.18f) ;
        vertices[3] = transform.position + new Vector3(0.18f, 0f, -0.18f) ;
        vertices[4] = transform.position + new Vector3(0.18f, 1.8f, 0.18f) ;
        vertices[5] = transform.position + new Vector3(-0.18f, 1.8f, 0.18f) ;
        vertices[6] = transform.position + new Vector3(-0.18f, 1.8f, -0.18f) ;
        vertices[7] = transform.position + new Vector3(0.18f, 1.8f, -0.18f) ;
        vertices[8] = transform.position;
        return vertices;
    }

    public void GetSubobjects(GameObject instance, ref List<float> x, ref List<float> y)
    {
        cam = GetComponent<Camera>();
        Queue<GameObject> q = new Queue<GameObject>();
        q.Enqueue(instance);
        while (q.Any()){
            GameObject temp = q.Dequeue();
            Transform[] childrenTrans = temp.transform.GetComponentsInChildren<Transform>();
            foreach (Transform childTrans in childrenTrans)
            {   
                if (childTrans == null){
                    continue;
                }
                GameObject child = childTrans.gameObject;
                if (child.GetComponent<MeshFilter>() != null){
                    Mesh mesh = child.GetComponent<MeshFilter>().mesh;
                    Vector3[] vertices = mesh.vertices;
                    foreach (Vector3 v in vertices){
                        Vector3 worldPoints = child.transform.TransformPoint(v);         
                        Vector3 screenPos = cam.WorldToScreenPoint(worldPoints);
                        x.Add(screenPos.x);
                        y.Add(screenPos.y);
                    }
                    
                }
                else if(child.GetComponent<SkinnedMeshRenderer>() != null){
                    Mesh mesh = child.GetComponent<SkinnedMeshRenderer>().sharedMesh;
                    Vector3[] vertices = mesh.vertices;
                    foreach (Vector3 v in vertices){
                        Vector3 worldPoints = child.transform.TransformPoint(v);         
                        Vector3 screenPos = cam.WorldToScreenPoint(worldPoints);
                        x.Add(screenPos.x);
                        y.Add(screenPos.y);
                        }
                }
                else{
                    q.Enqueue(child);
                }
            }
        }
    }

    public bool GetSubobjectsV2(int Frame_Num, GameObject instance, ref List<float> x, ref List<float> y)
    {
        cam = GetComponent<Camera>();
        Queue<GameObject> q = new Queue<GameObject>();
        q.Enqueue(instance);
        bool flag = false;

        while (q.Any()){
            GameObject temp = q.Dequeue();
            Transform[] childrenTrans = temp.transform.GetComponentsInChildren<Transform>();
            foreach (Transform childTrans in childrenTrans)
            {   
                if (childTrans == null){
                    continue;
                }

                GameObject child = childTrans.gameObject;
                if (child.GetComponent<MeshCollider>() != null)
                {   
                    flag = true;
                    Vector3[] vertices = child.GetComponent<MeshCollider>().sharedMesh.vertices;
                    foreach (Vector3 v in vertices)
                    {
                        Vector3 worldPoints = child.transform.TransformPoint(v);         
                        Vector3 screenPos = cam.WorldToScreenPoint(worldPoints);
                        if (screenPos.x < x[0]){
                            x[0] = screenPos.x;
                        }
                        if (screenPos.x > x[1]){
                            x[1] = screenPos.x;
                        }
                        if (screenPos.y < y[0]){
                            y[0] = screenPos.y;
                        }
                        if (screenPos.y > y[1]){
                            y[1] = screenPos.y;
                        }
                    }    
                    
                }
            }
        }
        return flag;
    }

    public bool CheckRayHitInstance(GameObject instance, Ray ray)
    {
        cam = GetComponent<Camera>();
        bool isHit = false;
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit))
            return isHit;

        if (instance.GetComponent<MeshCollider>() != null)
        {
            // a collision occured. Check if it's our plane object and create our cube at the
            // collision point, facing toward the collision normal
            if (hit.collider == instance.GetComponent<MeshCollider>())
            {
                isHit = true;
                return isHit;
            }
        }

        Transform[] childrenTrans = instance.transform.GetComponentsInChildren<Transform>();
        foreach (Transform childTrans in childrenTrans)
        {
            if (childTrans == null)
            {
                continue;
            }
            GameObject child = childTrans.gameObject;

            if (child.GetComponent<MeshCollider>() != null)
            {
                if (hit.collider == child.GetComponent<MeshCollider>())
                {
                    isHit = true;
                    return isHit;
                }
            }
        }
        return isHit;
    }

    public bool CheckRayHitInstanceV2(GameObject instance, Vector3 screenPos)
    {
        cam = GetComponent<Camera>();
        Ray ray = cam.ScreenPointToRay(screenPos);
        Tuple<int, int> pos = new Tuple<int, int>((int)screenPos.x, (int)screenPos.y);

        bool isHit = false;
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit))
            return isHit;

        if(staticColliderTable.ContainsKey(hit.collider))
        {   
            if (!segmentationTable.ContainsKey(pos)) 
            {
                segmentationTable.Add(pos, staticColliderTable[hit.collider]);
      
            }
            return isHit;       
        }
        else if(dynamicColliderTable.ContainsKey(hit.collider))
        {
            if (!segmentationTable.ContainsKey(pos))
            {
                segmentationTable.Add(pos, dynamicColliderTable[hit.collider]);

            }
            if (dynamicColliderTable[hit.collider] == instance)
            {
                isHit = true;
                return isHit;
            }
            else
            {
                isHit = false;
                return isHit;
            }
        }
        return isHit;
    }


    public void GetTightBBox(GameObject instance, ref List<float> x, ref List<float> y, float searchXMin, float searchXMax, float searchYMin, float searchYMax)
    {
        cam = GetComponent<Camera>();
        searchXMin = Mathf.Clamp(searchXMin, 0, cam.pixelWidth);
        searchXMax = Mathf.Clamp(searchXMax, 0, cam.pixelWidth);
        searchYMin = Mathf.Clamp(searchYMin, 0, cam.pixelHeight);
        searchYMax = Mathf.Clamp(searchYMax, 0, cam.pixelHeight);

        searchXMin = (float)Math.Floor(searchXMin);
        searchXMax = (float)Math.Ceiling(searchXMax);
        searchYMin = (float)Math.Floor(searchYMin);
        searchYMax = (float)Math.Ceiling(searchYMax);
        bool tmp_flag = false;
        for(int searchX = (int)searchXMin; searchX <= (int)searchXMax; searchX++)
        {
            if (tmp_flag)
                break;
            for(int searchY = (int)searchYMin; searchY <= (int)searchYMax; searchY++)
            {
                var pos = new Tuple<int, int>(searchX, searchY);

                Vector3 screenPos = new Vector3((float)searchX, (float)searchY, 1.0f);
                Ray ray = cam.ScreenPointToRay(screenPos);
                if(CheckRayHitInstance(instance, ray))
                {
                    //ht.Add(pos, instance);
                    if (screenPos.x < x[0])
                    {
                        x[0] = screenPos.x;
                        tmp_flag = true;
                        break;
                    }
                }
            }
        }
        tmp_flag = false;
        for (int searchX = (int)searchXMax; searchX >= (int)searchXMin; searchX--)
        {
            if (tmp_flag)
                break;
            for (int searchY = (int)searchYMin; searchY <= (int)searchYMax; searchY++)
            {
                Vector3 screenPos = new Vector3((float)searchX, (float)searchY, 1.0f);
                Ray ray = cam.ScreenPointToRay(screenPos);
                if (CheckRayHitInstance(instance, ray))
                {
                    if (screenPos.x > x[1])
                    {
                        x[1] = screenPos.x;
                        tmp_flag = true;
                        break;
                    }
                }
            }
        }
        tmp_flag = false;
        for (int searchY = (int)searchYMin; searchY <= (int)searchYMax; searchY++)
        {
            if (tmp_flag)
                break;
            for (int searchX = (int)searchXMin; searchX <= (int)searchXMax; searchX++)
            {
                Vector3 screenPos = new Vector3((float)searchX, (float)searchY, 1.0f);
                Ray ray = cam.ScreenPointToRay(screenPos);
                if (CheckRayHitInstance(instance, ray))
                {
                    if (screenPos.y < y[0])
                    {
                        y[0] = screenPos.y;
                        tmp_flag = true;
                        break;
                    }
                }
            }
        }
        tmp_flag = false;
        for (int searchY = (int)searchYMax; searchY >= (int)searchYMin; searchY--)
        {
            if (tmp_flag)
                break;
            for (int searchX = (int)searchXMin; searchX <= (int)searchXMax; searchX++)
            {
                Vector3 screenPos = new Vector3((float)searchX, (float)searchY, 1.0f);
                Ray ray = cam.ScreenPointToRay(screenPos);
                if (CheckRayHitInstance(instance, ray))
                {
                    if (screenPos.y > y[1])
                    {
                        y[1] = screenPos.y;
                        tmp_flag = true;
                        break;
                    }
                }
            }
        }
    }

    public void GetTightBBoxV2(GameObject instance, ref List<float> x, ref List<float> y, float searchXMin, float searchXMax, float searchYMin, float searchYMax)
    {
        cam = GetComponent<Camera>();
        searchXMin = Mathf.Clamp(searchXMin, 0, cam.pixelWidth);
        searchXMax = Mathf.Clamp(searchXMax, 0, cam.pixelWidth);
        searchYMin = Mathf.Clamp(searchYMin, 0, cam.pixelHeight);
        searchYMax = Mathf.Clamp(searchYMax, 0, cam.pixelHeight);

        searchXMin = (float)Math.Floor(searchXMin);
        searchXMax = (float)Math.Ceiling(searchXMax);
        searchYMin = (float)Math.Floor(searchYMin);
        searchYMax = (float)Math.Ceiling(searchYMax);
        bool tmp_flag = false;
        for (int searchX = (int)searchXMin; searchX <= (int)searchXMax; searchX++)
        {
            if (tmp_flag)
                break;
            for (int searchY = (int)searchYMin; searchY <= (int)searchYMax; searchY++)
            {
                print("## 1 ## searchX = " + searchX.ToString() + ", searchY = " + searchY.ToString());
                // ############################# start ####################################
                var pos = new Tuple<int, int>(searchX, searchY);
                if(segmentationTable.ContainsKey(pos))
                {
                    if(segmentationTable[pos] == instance)
                    {
                        x[0] = searchX;
                        tmp_flag = true;
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }

                Vector3 screenPos = new Vector3(searchX, searchY, 1.0f);
                
                if (CheckRayHitInstanceV2(instance, screenPos))
                {
                    //ht.Add(pos, instance);
                    if (screenPos.x < x[0])
                    {
                        x[0] = screenPos.x;
                        tmp_flag = true;
                        break;
                    }
                }
                // ############################# end ####################################
            }
        }
        tmp_flag = false;
        for (int searchX = (int)searchXMax; searchX >= (int)searchXMin; searchX--)
        {
            if (tmp_flag)
                break;
            for (int searchY = (int)searchYMin; searchY <= (int)searchYMax; searchY++)
            {
                print("## 2 ## searchX = " + searchX.ToString() + ", searchY = " + searchY.ToString());
                // ############################# start ####################################
                var pos = new Tuple<int, int>(searchX, searchY);
                if (segmentationTable.ContainsKey(pos))
                {
                    if (segmentationTable[pos] == instance)
                    {
                        x[1] = searchX;
                        tmp_flag = true;
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }

                Vector3Int screenPos = new Vector3Int(searchX, searchY, 1);

                if (CheckRayHitInstanceV2(instance, screenPos))
                {
                    if (screenPos.x > x[1])
                    {
                        x[1] = screenPos.x;
                        tmp_flag = true;
                        break;
                    }
                }
                // ############################# end ####################################
            }
        }
        tmp_flag = false;
        for (int searchY = (int)searchYMin; searchY <= (int)searchYMax; searchY++)
        {
            if (tmp_flag)
                break;
            for (int searchX = (int)searchXMin; searchX <= (int)searchXMax; searchX++)
            {
                print("## 3 ## searchX = " + searchX.ToString() + ", searchY = " + searchY.ToString());
                Vector3 screenPos = new Vector3((float)searchX, (float)searchY, 1.0f);
                Ray ray = cam.ScreenPointToRay(screenPos);
                if (CheckRayHitInstance(instance, ray))
                {
                    // ############################# start ####################################
                    var pos = new Tuple<int, int>(searchX, searchY);
                    if (segmentationTable.ContainsKey(pos))
                    {
                        if (segmentationTable[pos] == instance)
                        {
                            y[0] = searchY;
                            tmp_flag = true;
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (CheckRayHitInstanceV2(instance, screenPos))
                    {
                        if (screenPos.y < y[0])
                        {
                            y[0] = screenPos.y;
                            tmp_flag = true;
                            break;
                        }
                    }
                    // ############################# end ####################################
                }
            }
        }
        tmp_flag = false;
        for (int searchY = (int)searchYMax; searchY >= (int)searchYMin; searchY--)
        {
            if (tmp_flag)
                break;
            for (int searchX = (int)searchXMin; searchX <= (int)searchXMax; searchX++)
            {
                print("## 4 ## searchX = " + searchX.ToString() + ", searchY = " + searchY.ToString());
                // ############################# start ####################################
                var pos = new Tuple<int, int>(searchX, searchY);
                if (segmentationTable.ContainsKey(pos))
                {
                    if (segmentationTable[pos] == instance)
                    {
                        y[1] = searchY;
                        tmp_flag = true;
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }

                Vector3Int screenPos = new Vector3Int(searchX, searchY, 1);

                if (CheckRayHitInstanceV2(instance, screenPos))
                {
                    if (screenPos.y > y[1])
                    {
                        y[1] = screenPos.y;
                        tmp_flag = true;
                        break;
                    }
                }
                // ############################# end ####################################
            }
        }
    }

    public void CollectObservations(int Frame_Num, ref StringBuilder sb)
    {
        dynamicColliderTable.Clear();
        segmentationTable.Clear();
        detectedNum = 0;

        addMeshColliderByTag("People");

        GameObject[] Person_objects = GameObject.FindGameObjectsWithTag("People");

        bool needGetTightBBox = true;
        float acceptMinHeight = 20;
        int count = 0;
        foreach (GameObject Person in Person_objects)
        {   
            
            // count +=1;
            if (Person != null)
            {   
                if (Person.name == "Checking Box" || Person.name == "Boxcking Box"){
                    continue;
                }
                
                List<float> x = new List<float>(); // x_min, x_max
                List<float> y = new List<float>(); // y_min, y_max
                x.Add(float.MaxValue);
                x.Add(float.MinValue);
                y.Add(float.MaxValue);
                y.Add(float.MinValue);

                cam = GetComponent<Camera>();

                bool isDetected=false;

                isDetected = GetSubobjectsV2(Frame_Num, Person, ref x, ref y);

                isDetected = isDetected && (x[1] >= 0 && y[1] >= 0 && x[0] <= cam.pixelWidth && y[0] <= cam.pixelHeight && x[1]-x[0]<= cam.pixelWidth);

                isDetected = isDetected && (y[1] - y[0] > acceptMinHeight);           

                if (isDetected)
                {   
                    isDetected  = !CheckOcclusionV2(Frame_Num, Person);
                }

                //if (needGetTightBBox && isDetected)
                //{
                //    List<float> x_for_search = new List<float>(); // x_min, x_max
                //    List<float> y_for_search = new List<float>(); // y_min, y_max
                //    x_for_search.Add(float.MaxValue);
                //    x_for_search.Add(float.MinValue);
                //    y_for_search.Add(float.MaxValue);
                //    y_for_search.Add(float.MinValue);
                //    GetTightBBoxV2(Person, ref x_for_search, ref y_for_search, x[0], x[1], y[0], y[1]);
                //    x = null;
                //    y = null;
                //    x = x_for_search;
                //    y = y_for_search;
                //}

                //print(isDetected);
                
                if (isDetected)
                {
                    detectedNum += 1;
                    sb.Append(0).Append(" ");
                    sb.Append(Person.name);

                    StringBuilder log2dString = new StringBuilder();

                    if (x[0]<0){x[0]=0;}
                    if (x[1]>cam.pixelWidth){x[1]=cam.pixelWidth;}
                    if (y[0]<0){y[0]=0;}
                    if (y[1]>cam.pixelHeight){y[1]=cam.pixelHeight;}
                    
                    float x_center = (x[1]+x[0])/2;
                    float y_center = (y[1]+y[0])/2;
                    float width = x[1] - x[0];
                    float hight = y[1] - y[0];

                    log2dString.Append(" ").Append(x_center).Append(" ").Append(y_center).Append(" ").Append(width).Append(" ").Append(hight);

                    sb.Append(log2dString).Append("\n");

                }

            }
            else
            {
                print("person_is_null"+Person.name);
            }
        }
        // Debug.Log(cnt);
        
    }

    public void DestroyMeshColliders(GameObject instance)
    {
        MeshCollider[] childCollider = instance.transform.GetComponentsInChildren<MeshCollider>();
        foreach (MeshCollider collider in childCollider)
        {
            // DestroyImmediate(collider.sharedMesh);
            Destroy(collider.sharedMesh);
        }
    }

    // public void WriteMessage(string path, string msg)
    // {
    //     print("get_3D_bbox_1324"+" "+msg);
    //     using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
    //     {
    //         using (StreamWriter sw = new StreamWriter(fs))
    //         {
    //             sw.BaseStream.Seek(0, SeekOrigin.End);
    //             print("get_3D_bbox_1329"+" "+msg);
    //             sw.WriteLine("{0}\n", msg);
    //             sw.Flush();
    //         }
    //     }
    // }

    private void OnDisable()
    {
        // StreamWriter file = File.AppendText(path);
        // //print(path);
        // file.Write(sb.ToString());
        // file.Flush();
        // file.Close();

        // StreamWriter file3d = File.AppendText(path3d);
        // //print(path);
        // file3d.Write(sb3D.ToString());
        // file3d.Flush();
        // file3d.Close();

        // StreamWriter fileMesh = File.AppendText(pathMesh);
        // //print(path);
        // fileMesh.Write(meshPoints.ToString());
        // fileMesh.Flush();
        // fileMesh.Close();
    }

    public void DoUpdate(string sceneName, int frameNo)
    {  
        // StringBuilder sb = new StringBuilder(1000000);
        sb.Remove(0,sb.Length); // clean StringBuilder
        CollectObservations(frameNo, ref sb);
        string parentName = transform.parent.name;
        CamGenPath currentCamGen = GetComponent<CamGenPath>();
        int peopleNum = currentCamGen.peopleNum;

        saveFolder =  saveDir +"/"+ parentName + "/" + parentName+camName+"P"+peopleNum.ToString();
        // saveFolder =  saveDir +"/"+ sceneName + "/" + sceneName+parentName+camName+"P"+peopleNum.ToString();
        if (!Directory.Exists(saveFolder))
        {
    　　    Directory.CreateDirectory(saveFolder);
        }
        
        string path = saveFolder + "/" + frameNo.ToString() + ".txt";
        if (System.IO.File.Exists(path))
        {
            try
            {
                System.IO.File.Delete(path);
            }
            catch (System.IO.IOException e)
            {
                Console.WriteLine(e.Message);
                return;
            }
        }

        using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
        {
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.Write(sb.ToString());
                sw.Flush();
                sw.Close();
                fs.Close();
            }
        }

        // StreamWriter file = File.AppendText(path);
        // //print(path);
        // file.Write(sb.ToString());
        // file.Flush();
        // file.Close();

    }

    // Update is called once per frame
    // void Update()
    // {
    //     cam = GetComponent<Camera>();
    //     int Frame_Num = Time.frameCount;

    //     DoUpdate(Frame_Num);
    // }

    // public void Quit() 
    // {
    //     #if UNITY_EDITOR
    //         UnityEditor.EditorApplication.isPlaying = false;
    //     #else
    //         Application.Quit();
    //     #endif
    // }
}
