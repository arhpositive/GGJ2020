using System;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public float SwipeSpeed = 5.0f;
    public float JubilationCoef = 0.1f;

    public Vector2Int[] ConnectionDirs;

    protected Vector2Int _tilePosition;
    protected bool _canBeMoved;
    protected bool _inJubilation;
    protected float _jubilationStartTime;
    protected bool _usedUp;

    private Vector3 _swipeBegin;
    private Vector3 _swipeDestination;
    private float _swipeStartTime;
    private bool _inMotion;

    // Start is called before the first frame update
    void Start()
    {
        _usedUp = false;
        _canBeMoved = true;
        FinalizeJubilation();
        ResetSwipe();
    }

    protected void ResetSwipe()
    {
        _inMotion = false;
        _swipeDestination = Vector3.zero;
        _swipeStartTime = Time.time;
    }

    protected void FinalizeJubilation()
    {
        _inJubilation = false;
        _jubilationStartTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if (_inMotion)
        {
            float currentMovementDistance = (Time.time - _swipeStartTime) * SwipeSpeed;
            transform.position = Vector3.Lerp(_swipeBegin, _swipeDestination, currentMovementDistance);

            if (currentMovementDistance > 1.0f) //float comparison might be dangerous
            {
                ResetSwipe();
            }
        }

        if (_inJubilation)
        {
            float timeDiff = Time.time - _jubilationStartTime; //starts at 0, goes to infinity
            float zoomCoef = 1.0f;
            if (timeDiff > JubilationCoef)
            {
                zoomCoef += (JubilationCoef * 2) - timeDiff;
            }
            else
            {
                zoomCoef += timeDiff;
            }

            if (zoomCoef < 1.0f)
            {
                transform.localScale = Vector3.one;
                FinalizeJubilation();
            }
            else
            {
                transform.localScale = zoomCoef * Vector3.one;
            }
        }
    }

    public void SwipeTile(Vector2Int newTilePosition)
    {
        if (!_inMotion)
        {
            _inMotion = true;

            _swipeBegin = new Vector3(_tilePosition.x, _tilePosition.y, 0);
            _swipeDestination = new Vector3(newTilePosition.x, newTilePosition.y, 0);
            _swipeStartTime = Time.time;
            SetTilePosition(newTilePosition);
        }       
    }

    public void SetTilePosition(Vector2Int tilePosition)
    {
        _tilePosition = tilePosition;
    }

    public Vector2Int GetTilePosition()
    {
        return _tilePosition;
    }

    public bool IsInMotion()
    {
        return _inMotion;
    }

    public virtual bool CanBeMoved()
    {
        return _canBeMoved;
    }

    internal void Jubilation()
    {
        GetComponent<SpriteRenderer>().color = Color.green;
        _inJubilation = true;
        _jubilationStartTime = Time.time;
        _canBeMoved = false;
        _usedUp = true;
    }

    internal bool IsUsedUp()
    {
        return _usedUp;
    }
}
