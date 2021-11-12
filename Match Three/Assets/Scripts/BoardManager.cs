using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{

    
    [Header("Board")]
    public Vector2Int size;
    public Vector2 offsetTile;
    public Vector2 offsetBoard;

    [Header("Tile")]
    public List <Sprite> tileTypes= new List<Sprite>();
    public GameObject tilePrefab;

    private Vector2 startPos;
    private Vector2 endPos;
    private TileController[,] tiles;

    private int combo;

    public bool IsAnimasi
    {
        get{
            return Istukeran || IsProcessing;
        }
    }

    public bool Istukeran{get; set;}
    public bool IsProcessing{get;set;}


    #region Singleton
    private static BoardManager _instance = null;
    
    public static BoardManager Instance
    {
        get{
            if(_instance == null)
            {
                _instance = FindObjectOfType<BoardManager>();
                if(_instance == null)
                {
                    Debug.LogError("Fatal Error : BoardManager not Found!");
                }
            } 
            return _instance;
        }
    }

    #endregion


    private void Start()
    {
        Vector2 tileSize = tilePrefab.GetComponent<SpriteRenderer>().size;
        BuatBoard(tileSize);

        IsProcessing = false;
        Istukeran = false;
    }


    #region Generate
    private void BuatBoard(Vector2 tileSize)
    {
        tiles = new TileController[size.x,size.y];
        Vector2 totalSize = (tileSize + offsetTile) * (size - Vector2.one);
        startPos = (Vector2)transform.position - (totalSize / 2) + offsetBoard;
        endPos = startPos + totalSize;

        for(int x=0;x<size.x;x++)
        {
            for(int y=0;y<size.y;y++)
            {
            TileController newTile = Instantiate(tilePrefab, new Vector2(startPos.x + ((tileSize.x + offsetTile.x) * x), startPos.y + ((tileSize.y + offsetTile.y) * y)), tilePrefab.transform.rotation, transform).GetComponent<TileController>();
            tiles[x, y] = newTile;

            // get no tile id
            List<int> possibleId = GetStartingPossibleIdList(x,y);
            int newId = possibleId[Random.Range(0,possibleId.Count)];

            newTile.ChangeId(newId,x,y);
            }
        }
    }

    private List<int> GetStartingPossibleIdList(int a, int b)
    {
        List<int> possibleId = new List<int>();
        for(int i=0;i<tileTypes.Count;i++)
        {
            possibleId.Add(i);
        }

        if(a > 1 && tiles[a-1,b].id == tiles[a-2,b].id)
        {
            possibleId.Remove(tiles[a-1,b].id);
        }
        if(b > 1 && tiles[a,b-1].id == tiles[a,b-2].id)
        {
            possibleId.Remove(tiles[a,b-1].id);
        }
        return possibleId;
    }

    #endregion

    
    #region Swapping

    public IEnumerator SwapTilePos(TileController a, TileController b, System.Action onCompleted)
    {
        Istukeran = true;

        Vector2Int indexA = GetTileIndex(a);
        Vector2Int indexB = GetTileIndex(b);

        tiles[indexA.x, indexA.y] = b;
        tiles[indexB.x, indexB.y] = a;

       a.ChangeId(a.id, indexB.x, indexB.y);
       b.ChangeId(b.id, indexA.x, indexA.y);

       bool isRoutineASelesai = false;
       bool isRoutineBSelesai = false;

       StartCoroutine(a.MoveTilePos(GetIndexPos(indexB), () => { isRoutineASelesai = true; }));
       StartCoroutine(b.MoveTilePos(GetIndexPos(indexA), () => { isRoutineBSelesai = true; }));

     yield return new WaitUntil(() => { return isRoutineASelesai && isRoutineBSelesai; });

    onCompleted?.Invoke();

    Istukeran = false;
    }

    #endregion

   
    #region Process
    public void Process()
    {
        IsProcessing = true;

        combo = 0;
        ProcessMatches();
    }

    #region Match

    private void ProcessMatches()
    {
        
        List<TileController> matchingTiles = GetAllMatches();

        // stop lock kalo nda temu matchnya

        if(matchingTiles == null || matchingTiles.Count == 0)
        {
            IsProcessing = false;
            return;
        }
        combo++;

        ScoreManager.Instance.IncrementCurrScore(matchingTiles.Count,combo);
        StartCoroutine(ClearMatches(matchingTiles,ProcessDrop));
    }

    public List<TileController> GetAllMatches()
    {
        List<TileController> matchingTiles = new List<TileController>();

        for(int x=0;x < size.x; x++)
        {
            for(int y=0;y<size.y; y++)
            {
                List<TileController> tileMatched = tiles[x,y].GetAllMatches();

                // just ke next tile apabila ga match
                if(tileMatched == null|| tileMatched.Count == 0) continue;

                foreach(TileController item in tileMatched)
                {
                    // add yang blm diadd
                    if(!matchingTiles.Contains(item)) matchingTiles.Add(item);
                }
            }
        }
        return matchingTiles;
    }

    private IEnumerator ClearMatches(List<TileController> matchingTiles,System.Action onCompleted)
    {
        List<bool> isCompleted = new List<bool>();

        for(int i=0;i<matchingTiles.Count;i++)
        {
            isCompleted.Add(false);
        }
        
        for(int i=0;i<matchingTiles.Count;i++)
        {
            int idx = i;
            StartCoroutine(matchingTiles[i].SetDestroyed(()=>{ isCompleted[idx] =true;}));
        }
        yield return new WaitUntil(()=> {return isAllTrue(isCompleted);});

        onCompleted?.Invoke();
    }
    #endregion
  

  #region Drop

    private void ProcessDrop()
    {
        Dictionary<TileController,int> droppingTiles = GetAllDrop();
        StartCoroutine(DropTiles(droppingTiles,ProcessDestroyandFill));
    }

    private Dictionary<TileController, int> GetAllDrop()
    {
        Dictionary<TileController,int> droppingTiles = new Dictionary<TileController, int>();

        for(int x =0; x < size.x ; x++)
        {
            for(int y = 0; y<size.y ; y++)
            {
                if(tiles[x,y].isDestroyed)
                {
                    // process for all tile on top destroyed tile
                    for(int i = y+1;i<size.y;i++)
                    {
                        if(tiles[x,i].isDestroyed) continue;

                        // if file alr drop list, increas drop range

                        if(droppingTiles.ContainsKey(tiles[x,i])) droppingTiles[tiles[x,i]]++;

                        // if not on drop list, add with drop range onCompleted
                        else droppingTiles.Add(tiles[x,i],1);
                    }

                    
                    
                }
            }
        }
        return droppingTiles;
    }

    private IEnumerator DropTiles(Dictionary<TileController,int> droppingTiles,System.Action onCompleted)
    {
        foreach(KeyValuePair<TileController,int> pair in droppingTiles)
        {
            Vector2Int tileIdx = GetTileIndex(pair.Key);

            TileController temp = pair.Key;
            tiles[tileIdx.x, tileIdx.y] = tiles[tileIdx.x,tileIdx.y - pair.Value];
            tiles[tileIdx.x, tileIdx.y - pair.Value] =temp;

            temp.ChangeId(temp.id,tileIdx.x, tileIdx.y - pair.Value);
        }

        yield return null;

        onCompleted?.Invoke();
    }

    #endregion

#region Destory & Fill

    private void ProcessDestroyandFill()
    {
        List<TileController> destroyedTiles = GetAllDestroyed();

        StartCoroutine(DestroyAndFillTiles(destroyedTiles,ProcessReposition));
    }

    private List<TileController> GetAllDestroyed()
    {
        List<TileController> destroyedTiles = new List<TileController>();

        for(int x =0 ; x<size.x;x++)
        {
            for(int y =0 ; y <size.y;y++)
            {
                if(tiles[x,y].isDestroyed)
                {
                    destroyedTiles.Add(tiles[x,y]);
                }
            }
        }

        return destroyedTiles;
    }

    private IEnumerator DestroyAndFillTiles(List<TileController> destroyedTiles, System.Action onCompleted)
    {
        List<int> highestidx = new List<int>();

        for(int i=0; i<size.x;i++)
        {
            highestidx.Add(size.y-1);
        }

        float spawnHeight = endPos.y + tilePrefab.GetComponent<SpriteRenderer>().size.y + offsetTile.y;

        foreach(TileController tile in destroyedTiles)
        {
            Vector2Int tileIdx = GetTileIndex(tile);
            Vector2Int targetIdx = new Vector2Int(tileIdx.x,highestidx[tileIdx.x]);
            highestidx[tileIdx.x]--;

            tile.transform.position = new Vector2(tile.transform.position.x,spawnHeight);
            tile.GenerateRandomTile(targetIdx.x,targetIdx.y);
        }

        yield return null;

        onCompleted?.Invoke();
    }

    #endregion

    #region Reposition

    private void ProcessReposition()
    {
        StartCoroutine(RepositionTiles(ProcessMatches));
    }

    private IEnumerator RepositionTiles(System.Action onCompleted)
    {
        List<bool> isCompleted = new List<bool>();

        int i =0;
        for(int x =0 ; x< size.x;x++)
        {
            for(int y=0;y<size.y;y++)
            {
                Vector2 targetPos = GetIndexPos(new Vector2Int(x,y));

                //skip kalo ada di posisi

                if((Vector2)tiles[x,y].transform.position == targetPos) continue;

                isCompleted.Add(false);

                int idx =i;
                StartCoroutine(tiles[x,y].MoveTilePos(targetPos,()=> {isCompleted[idx] = true;}));

                i++;
            }
        }

        yield return new WaitUntil(() => {return isAllTrue(isCompleted);});

        onCompleted?.Invoke();
    }

    #endregion

    #endregion

    #region Helper

    public Vector2Int GetTileIndex(TileController tile)
    {
        for(int x=0;x<size.x;x++)
        {
            for(int y=0;y<size.y;y++)
            {
                if(tile == tiles[x,y]) return new Vector2Int(x,y);
            }
        }
        return new Vector2Int(-1,-1);
    }

    public Vector2 GetIndexPos(Vector2Int index)
    {
           Vector2 tileSize = tilePrefab.GetComponent<SpriteRenderer>().size;
    return new Vector2(startPos.x + ((tileSize.x + offsetTile.x) * index.x), startPos.y + ((tileSize.y + offsetTile.y) * index.y));
    }

    public bool isAllTrue(List<bool> list)
    {
        foreach(bool status in list)
        {
            if(!status) return false;
        }

        return true;
    }


    #endregion
    
}
