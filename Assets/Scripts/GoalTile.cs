using UnityEngine;

public enum GoalType
{
    gt_score,
    gt_bomb,
    gt_timeBonus
}

public class GoalTile : Tile
{
    public GoalType typeOfGoal;
    public int timeBonus;

    internal override void Start()
    {
        base.Start();
    }

    internal override void Update()
    {
        base.Update();      
    }

    internal override void Jubilation()
    {
        base.Jubilation();
    }
}
