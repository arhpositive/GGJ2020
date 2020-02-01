using UnityEngine;

public class StartTile : Tile
{

    private void Start()
    {
        _usedUp = false;
        _canBeMoved = false;
        _inJubilation = true;
        _jubilationStartTime = Time.time;
        ResetSwipe();
    }

    public override bool CanBeMoved()
    {
        return _canBeMoved;
    }

}
