using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileController : MonoBehaviour
{
    public int id;
    private BoardManager board;
    private SpriteRenderer spriteR;

    private static readonly Color selectedColor = 
new Color(0.5f,0.5f,0.5f);

    private static readonly Color normalColor = Color.white;

    private static TileController prevSelected = null;

    private bool isSelected = false;

    private static readonly float moveDuration = 0.5f;

    private static readonly float destroyBigDuration = 0.1f;

    private static readonly float destroySmallDuration = 0.4f;

    private static readonly Vector2 sizeBig = Vector2.one * 1.2f;

    private static readonly Vector2 sizeSmall = Vector2.zero;

    private static readonly Vector2 sizeNormal = Vector2.one;

    private static readonly Vector2[] adjacentDirection = new Vector2[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right};
    private GameFlowManager game;

    public bool isDestroyed{get; private set;}
    private void Awake()
    {
        game = GameFlowManager.Instance;
        board = BoardManager.Instance;
        spriteR = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        
        isDestroyed = false;
    }

    private void OnMouseDown()
    {
        // Non Selectable condition
        if(spriteR.sprite == null || board.IsAnimasi || game.IsGO) return;

        SoundManager.Instance.PlayTap();
        // already selected tile
        if(isSelected) Deselect();

        else
        {
            // nothing selected
            if(prevSelected == null) Select();
            else
            {
                // adjacent tile?
                if(GetAllAdjacentTiles().Contains(prevSelected))
                {
                    TileController otherTile = prevSelected;
                    prevSelected.Deselect();
                
                
                // swap tile
                TukarTile(otherTile, () => {
                    if(board.GetAllMatches().Count> 0)
                    {
                        //Debug.Log("MATCH FOUND!");
                        board.Process();
                    } 
                    else 
                    {
                    SoundManager.Instance.PlayWrong();
                      TukarTile(otherTile);  
                    }
                });
                }
                else
                {
                    prevSelected.Deselect();
                    Select();
                }
                
            }
        }

    }

    public void ChangeId(int id, int a, int b)
    {
        spriteR.sprite = board.tileTypes[id];
        this.id = id;
        name = "TILE_" + id + " (" + a + "," + b + ")";
    }
   
    
    #region  Select & Deselect

    private void Select()
    {
        isSelected = true;
        spriteR.color = selectedColor;
        prevSelected = this;
    }

    private void Deselect()
    {
        isSelected = false;
        spriteR.color = normalColor;
        prevSelected = null;
    }

    #endregion

    #region Swapping & Moving

     public void TukarTile(TileController otherTile, System.Action onCompleted = null)
    {
         StartCoroutine(board.SwapTilePos(this, otherTile, onCompleted));
    }

     public IEnumerator MoveTilePos(Vector2 targetPos, System.Action onCompleted)
    {
        Vector2 startPos = transform.position;
        float time = 0.0f;

        // run animation untuk next frame biar aman
        yield return new WaitForEndOfFrame();

        while(time < moveDuration)
        {
            transform.position = Vector2.Lerp(startPos, targetPos, time/moveDuration);
            time += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }

        transform.position = targetPos;
        onCompleted?.Invoke();
    }

    #endregion


    #region Adjacent
    private TileController GetAdjacent(Vector2 castDir)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position,castDir,spriteR.size.x);

        if(hit)
        {
            return hit.collider.GetComponent<TileController>();
        }

        return null;
    }

    public List<TileController> GetAllAdjacentTiles()
    {
        List<TileController> adjacentTiles = new List<TileController>();
        
        for(int i =0;i<adjacentDirection.Length;i++)
        {
            adjacentTiles.Add(GetAdjacent(adjacentDirection[i]));
        }
        return adjacentTiles;

    }

    #endregion

    #region Check Match
    private List<TileController> GetMatch(Vector2 castDir)
    {
        List<TileController> matchingTiles = new List<TileController>();
        RaycastHit2D hit = Physics2D.Raycast(transform.position,castDir,spriteR.size.x);

        while(hit)
        {
            TileController otherTile = hit.collider.GetComponent<TileController>();

            if(otherTile.id != id || otherTile.isDestroyed) break;

            matchingTiles.Add(otherTile);
            hit = Physics2D.Raycast(otherTile.transform.position,castDir,spriteR.size.x);
        }

        return matchingTiles;
    }

    private List<TileController> GetOneLineMatch(Vector2[] paths)
    {
        List<TileController> matchingTiles = new List<TileController>();

        for(int i=0;i<paths.Length;i++)
        {
            matchingTiles.AddRange(GetMatch(paths[i]));
        }

        // kalo match apabila lebih dari 2 ( 3) dalam 1 line
        if(matchingTiles.Count >=2)
        {
            return matchingTiles;
        }
        return null;
    }

    public List<TileController> GetAllMatches()
    {
        if(isDestroyed) return null;
        List<TileController> matchingTiles = new List<TileController>();

        // get matches untuk hori dan verti
        List<TileController> horizontalMatch = GetOneLineMatch(new Vector2[2] {Vector2.up,Vector2.down});
        
        List<TileController> verticalMatch = GetOneLineMatch(new Vector2[2] {Vector2.left,Vector2.right});

        if(horizontalMatch != null) matchingTiles.AddRange(horizontalMatch);

        if(verticalMatch != null) matchingTiles.AddRange(verticalMatch);

        // tambahkan ke daftar matched tiles apa bila ketemu
        if(matchingTiles != null && matchingTiles.Count >= 2) matchingTiles.Add(this);

        return matchingTiles;
    }

    #endregion



    #region Destory & Generate
    public IEnumerator SetDestroyed(System.Action onCompleted)
    {
        isDestroyed = true;
        id = -1;
        name = "TILE_NULL";

        Vector2 startSize = transform.localScale;
        float time = 0.0f;

        while(time < destroyBigDuration)
        {
            transform.localScale = Vector2.Lerp(startSize,sizeBig,time/destroyBigDuration);

            time += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }

        transform.localScale = sizeBig;

        startSize = transform.localScale;
        time = 0.0f;

        while(time < destroySmallDuration)
        {
            transform.localScale = Vector2.Lerp(startSize,sizeSmall,time/destroySmallDuration);
            time += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }

        transform.localScale = sizeSmall;
        spriteR.sprite = null;

        onCompleted?.Invoke();

    }

     public void GenerateRandomTile(int x, int y)
    {
        transform.localScale = sizeNormal;
        isDestroyed = false;

        ChangeId(Random.Range(0,board.tileTypes.Count),x,y);
    }

    #endregion

   

}
