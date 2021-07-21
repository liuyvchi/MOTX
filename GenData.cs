using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenData : MonoBehaviour
{
    public GameObject[] walkingPrefabs;

    private GameObject currentCamGO;
    private GameObject[] camerasGO;
    private int idxCam = 0;
    private int GenTimes = 0;
    private int CamNum;
    private CamGenPath currentCamGen;
    

    // Start is called before the first frame update
    void Start()
    {
        camerasGO = GameObject.FindGameObjectsWithTag("Camera");
        CamNum = camerasGO.Length;
        setCameraFalse();
    }

    private void setCameraFalse()
    {
        foreach (GameObject camGO in camerasGO)
        {
            camGO.SetActive(false);
            print("line27");
        }   
        currentCamGO = camerasGO[0];
        currentCamGO.SetActive(true);
        currentCamGen = currentCamGO.AddComponent<CamGenPath>();
        initializeCam(ref currentCamGen);
        
    }

    private void initializeCam(ref CamGenPath currentCamGen)
    {
        currentCamGen.walkingPrefabs = walkingPrefabs;

        // currentCamGen.walkSpeed = Random.Range(1.2f, 1.3f);
        // currentCamGen.walkSpeed = Random.Range(1.0f, 2.1f);
        currentCamGen.walkSpeed = Random.Range(4.0f, 4.1f);
        // currentCamGen.walkSpeed = Random.Range(1.0f, 1.7f);
        // currentCamGen.camSpeed = 1.2f;
        // currentCamGen.walkSpeed = Random.Range(2.0f, 3.0f);
        // currentCamGen.staticRatio = Random.Range(0.2f, 0.5f);

        currentCamGen.peopleNum = Random.Range(58, 60);

        currentCamGen.deltaCamHeight = 0; //Random.Range(-0.5f, 0.5f)

        currentCamGen.deltaXEulerAngles = 0;//Random.Range(-1.0f, 1.0f)

    }
        
    

    // Update is called once per frame
    void Update()
    {
        bool checkStop = currentCamGen.checkStop();
        if (checkStop == true)
        {
            currentCamGO.SetActive(false);
            GameObject walkingObjects = GameObject.Find("walkingObjects");
            int count = walkingObjects.transform.childCount;
            for (int i = 0; i < count; i++)
            {  
                Destroy(walkingObjects.transform.GetChild(i).gameObject);
            }
            Destroy(walkingObjects);
            idxCam = GenTimes;
            if (GenTimes > CamNum)
            {
                UnityEditor.EditorApplication.isPlaying = false;
            }
            currentCamGO = camerasGO[idxCam];
            currentCamGO.SetActive(true);
            currentCamGen = currentCamGO.AddComponent<CamGenPath>();
            initializeCam(ref currentCamGen);
            GenTimes+=1;
        }
        else
        {
            currentCamGen = currentCamGO.GetComponent<CamGenPath>();
            currentCamGen.Doupdate();
        }

    }

}
