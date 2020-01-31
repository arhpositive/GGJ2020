using UnityEngine;

public class LevelDesign
{
    public int CameraSize;
    public Vector3 CameraTargetPos;
    public int[,] LevelLayout;
    public int LevelWidth;
    public int LevelHeight;

    public LevelDesign(int cameraSize, Vector3 cameraTargetPos, int[,] levelLayout)
    {
        CameraSize = cameraSize;
        CameraTargetPos = cameraTargetPos;
        LevelLayout = levelLayout;
        LevelWidth = levelLayout.GetLength(0);
        LevelHeight = levelLayout.GetLength(1);
    }
}