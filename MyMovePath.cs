using UnityEngine;

public class MyMovePath : MonoBehaviour
{
    [HideInInspector] public float _walkPointThreshold = 0.5f;
    [HideInInspector] public MyWalkPath walkPath;
     public Vector3 finishPos;
     public Vector3 nextFinishPos = Vector3.zero;
    // [HideInInspector] public int targetPoint;
    public int targetPoint;
    [HideInInspector] public int targetPointsTotal;

    public void InitStartPosition(int _i)
    {
        var _WalkPath = walkPath;
        targetPointsTotal = _WalkPath.getPointsTotal() - 2;

        if (_i < targetPointsTotal && _i > 0)
        {
            targetPoint = _i + 1;
            finishPos = _WalkPath.getNextPoint(_i + 1);
        }
        else
        {
            targetPoint = 1;
            finishPos = _WalkPath.getNextPoint(1);
        }

    }

    public void SetLookPosition()
    {
        Vector3 targetPos = new Vector3(finishPos.x, transform.position.y, finishPos.z);
        transform.LookAt(targetPos);
    }
}