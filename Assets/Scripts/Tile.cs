using UnityEngine;

public class Tile : MonoBehaviour
{
    public float SwipeSpeed = 5.0f;
    public float JubilationCoef = 0.1f;

    public bool CanBeMoved;
    public Vector2Int[] ConnectionDirs;
    public bool RemoveOnUse;

    protected Vector2Int _tilePosition;
    
    protected bool _inJubilation;
    protected float _jubilationStartTime;
    protected bool _usedUp;

    private Vector3 _swipeBegin;
    private Vector3 _swipeDestination;
    private float _swipeStartTime;
    private bool _inMotion;

    // Start is called before the first frame update
    internal virtual void Start()
    {
        _usedUp = false;
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
    internal virtual void Update()
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
                if (RemoveOnUse)
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                transform.localScale = zoomCoef * Vector3.one;
            }
        }
    }

    public bool SwipeTile(Vector2Int newTilePosition)
    {
        if (_inMotion)
        {
            return false;
        }

        _inMotion = true;
        _swipeBegin = new Vector3(_tilePosition.x, _tilePosition.y, 0);
        _swipeDestination = new Vector3(newTilePosition.x, newTilePosition.y, 0);
        _swipeStartTime = Time.time;
        SetTilePosition(newTilePosition);
        return true;
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

    internal virtual void Jubilation()
    {        
        GetComponent<SpriteRenderer>().color = Color.green;
        _inJubilation = true;
        _jubilationStartTime = Time.time;
        CanBeMoved = false;
        _usedUp = true;
    }

    internal bool IsUsedUp()
    {
        return _usedUp;
    }
}
