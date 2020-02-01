using UnityEngine;

public class LevelDesign
{
    public int CameraSize;
    public Vector3 CameraPos;
    public int[,] LevelLayout;
    public int LevelWidth;
    public int LevelHeight;

    public LevelDesign(int cameraSize, Vector3 cameraPos, int[,] levelLayout)
    {
        CameraSize = cameraSize;
        CameraPos = cameraPos;
        LevelLayout = levelLayout;
        LevelWidth = levelLayout.GetLength(0);
        LevelHeight = levelLayout.GetLength(1);
    }
}