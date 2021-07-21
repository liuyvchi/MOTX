using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System;

public class MyWalkPath : MonoBehaviour
{
    public enum EnumDir
    {
        Forward,
        Backward,
        HugLeft,
        HugRight,
        WeaveLeft,
        WeaveRight
    };

    protected float _distances;

    [HideInInspector] public List<Vector3> pathPoint = new List<Vector3>();
    [HideInInspector] public int pointLength;

    public Vector3 getNextPoint(int index)
    {
        return pathPoint[index];
    }

    public Vector3 getStartPoint()
    {
        return pathPoint[0];
    }

    public int getPointsTotal()
    {   
        var _pathPoint = pathPoint;
        pointLength = _pathPoint.Count;
        return pointLength;
    }
}