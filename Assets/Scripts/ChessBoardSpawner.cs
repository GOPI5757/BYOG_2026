using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
struct PawnGridMap
{
    public GameObject Pawn;
    public ChessGridName PawnGridName;
    public Vector3 velocity;
    public bool bHasHit;

    public PawnGridMap(GameObject obj, ChessGridName gridName, Vector3 vel, bool hitValue)
    {
        Pawn = obj;
        PawnGridName = gridName;
        velocity = vel;
        bHasHit = hitValue;
    }
}

public class ChessBoardSpawner : MonoBehaviour
{
    [SerializeField] private GameObject BlockPrefab;
    [SerializeField] private GameObject OuterFramePrefab;
    [SerializeField] private GameObject PawnPrefab;
    [SerializeField] private GameObject KingPrefab;

    [SerializeField] private Vector2 startPos;
    [SerializeField] private PawnGridMap[] PawnGridMap;

    [SerializeField] private PawnGridMap KingGridMap;

    [SerializeField] private float pawnMoveSpeed;
    
    private PawnGridMap[] initialPawnGridMap;
    [SerializeField] private float decreaseYOffset_A, decreaseYOffset_B;

    [SerializeField] private Material BlockMat_A, BlockMat_B;

    private int currentPawnMoveOrderIndex;

    private List<GameObject> pawns = new List<GameObject>();
    
    private GameObject outerFrameObj;

    private bool bIsBlockMatA = true;

    private string alphabets = "ABCDEFGH";

    void Start()
    {
        outerFrameObj = Instantiate(OuterFramePrefab, transform);
        outerFrameObj.GetComponent<OuterFrame>().chessBoardSpawner = this;
        SpawnBlocks();
        SpawnPawns();
    }

    private void SpawnBlocks()
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                Vector3 spawnPos = transform.position +
                    new Vector3(startPos.x + (j * 2), 1f, startPos.y + (i * 2));

                GameObject blockObj = Instantiate(BlockPrefab, transform);
                blockObj.transform.position = spawnPos;
                blockObj.GetComponent<Renderer>().material = bIsBlockMatA ? BlockMat_A : BlockMat_B;
                blockObj.transform.GetComponent<BlockScript>().initialMaterial = bIsBlockMatA ? BlockMat_A : BlockMat_B;
                blockObj.transform.GetComponent<BlockScript>().SetDecreaseOffset(bIsBlockMatA ? decreaseYOffset_A : decreaseYOffset_B);
                blockObj.transform.name = alphabets[j] + (i + 1).ToString();
                bIsBlockMatA = !bIsBlockMatA;
            }

            bIsBlockMatA = !bIsBlockMatA;
        }
    }

    private void SpawnPawns()
    {
        string kingGridname = KingGridMap.PawnGridName.alph_type.ToString() +
                KingGridMap.PawnGridName.number.ToString();

        PawnSpawnRefactor(kingGridname, KingPrefab, ref KingGridMap);

        for (int i = 0; i < PawnGridMap.Length; i++)
        {
            string name = PawnGridMap[i].PawnGridName.alph_type.ToString() + 
                PawnGridMap[i].PawnGridName.number.ToString();

            PawnSpawnRefactor(name, PawnPrefab, ref PawnGridMap[i]);
            //GameObject chessBlock = RetrieveGameObjectOfName(name);
            //if(chessBlock != null)
            //{
            //    GameObject PawnSpawnObj = Instantiate(PawnPrefab, transform);
            //    PawnSpawnObj.transform.position = chessBlock.transform.position;
            //    PawnSpawnObj.GetComponent<PawnScript>().cb_spawner = this;

            //    PawnGridMap map = new PawnGridMap(PawnSpawnObj, PawnGridMap[i].PawnGridName, 
            //        PawnGridMap[i].velocity, false);
            //    PawnGridMap[i] = map;

            //    SetBlockOccupied(name, PawnSpawnObj);

            //    PawnSpawnObj.transform.parent = chessBlock.transform;
            //}
        }
    }

    private void PawnSpawnRefactor(string name, GameObject prefab, ref PawnGridMap gridMap)
    {
        GameObject chessBlock = RetrieveGameObjectOfName(name);
        if (chessBlock != null)
        {
            GameObject PawnSpawnObj = Instantiate(prefab, transform);
            PawnSpawnObj.transform.position = chessBlock.transform.position;
            PawnSpawnObj.GetComponent<PawnScript>().cb_spawner = this;

            PawnGridMap map = new PawnGridMap(PawnSpawnObj, gridMap.PawnGridName,
                gridMap.velocity, false);
            gridMap = map;

            SetBlockOccupied(name, PawnSpawnObj);

            PawnSpawnObj.transform.parent = chessBlock.transform;
        }
    }

    public void UpdateHasHitToPawn(GameObject pawn)
    {
        for(int i = 0; i < PawnGridMap.Length; i++)
        {
            if (PawnGridMap[i].Pawn == pawn)
            {
                PawnGridMap[i].bHasHit = true;
                break;
            }
        }
    }

    public GameObject RetrieveGameObjectOfName(string name)
    {
        GameObject obj = null;
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).name.ToLower() == name.ToLower())
            {
                obj = transform.GetChild(i).gameObject;
                break;
            }
        }

        return obj;
    }

    void Update()
    {
        UpdatePawnLocations();
    }

    public void MovePawnsForward()
    {
        if(!IsAllPawnsDefeated())
        {
            FindNextPawn();

            string nextGridName = PawnGridMap[currentPawnMoveOrderIndex].PawnGridName.alph_type.ToString() +
                (PawnGridMap[currentPawnMoveOrderIndex].PawnGridName.number - 1).ToString();

            GameObject nextBlock = RetrieveGameObjectOfName(nextGridName);
            if (nextBlock)
            {
                BlockScript b_script = nextBlock.GetComponent<BlockScript>();
                if (b_script != null)
                {
                    if (!b_script.IsOccupiedObj())
                    {
                        string name = GetStringFromChessGridName(
                            PawnGridMap[currentPawnMoveOrderIndex].PawnGridName);
                        SetBlockOccupied(name, null);

                        PawnGridMap[currentPawnMoveOrderIndex].PawnGridName = new ChessGridName(
                            PawnGridMap[currentPawnMoveOrderIndex].PawnGridName.alph_type,
                            PawnGridMap[currentPawnMoveOrderIndex].PawnGridName.number - 1);

                        string name_1 = GetStringFromChessGridName(
                            PawnGridMap[currentPawnMoveOrderIndex].PawnGridName);
                        SetBlockOccupied(name_1, PawnGridMap[currentPawnMoveOrderIndex].Pawn);

                        PawnGridMap[currentPawnMoveOrderIndex].Pawn.transform.parent = nextBlock.transform;
                    }
                }
            }
        }

        //for(int i = 0; i < PawnGridMap.Length; i++)
        //{
        //    if (PawnGridMap[i].bHasHit) continue;
        //    string nextGridName = PawnGridMap[i].PawnGridName.alph_type.ToString() + 
        //        (PawnGridMap[i].PawnGridName.number - 1).ToString();

        //    GameObject nextBlock = RetrieveGameObjectOfName(nextGridName);
        //    if (nextBlock)
        //    {
        //        BlockScript b_script = nextBlock.GetComponent<BlockScript>();
        //        if(b_script != null)
        //        {
        //            if(!b_script.IsOccupiedObj())
        //            {
        //                string name = GetStringFromChessGridName(PawnGridMap[i].PawnGridName);
        //                SetBlockOccupied(name, null);

        //                PawnGridMap[i].PawnGridName = new ChessGridName(PawnGridMap[i].PawnGridName.alph_type,
        //                    PawnGridMap[i].PawnGridName.number - 1);

        //                string name_1 = GetStringFromChessGridName(PawnGridMap[i].PawnGridName);
        //                SetBlockOccupied(name_1, PawnGridMap[i].Pawn);

        //                PawnGridMap[i].Pawn.transform.parent = nextBlock.transform;
        //            }
        //        }
        //    }
        //}
    }

    public bool IsKingDefeated()
    {
        return KingGridMap.bHasHit;
    }

    private bool IsAllPawnsDefeated()
    {
        for(int i = 0; i < PawnGridMap.Length; i++)
        {
            if (!PawnGridMap[i].bHasHit) return false;
        }

        return true;
    }

    private void FindNextPawn()
    {
        for(int i = 0; i < PawnGridMap.Length - 1; i++)
        {
            if(++currentPawnMoveOrderIndex >= PawnGridMap.Length)
            {
                currentPawnMoveOrderIndex = 0;
            }

            if (!PawnGridMap[currentPawnMoveOrderIndex].bHasHit)
            {
                break;
            }
        }
    }

    private void UpdatePawnLocations()
    {
        if (PawnGridMap[currentPawnMoveOrderIndex].Pawn == null) return;
        string name = GetStringFromChessGridName(PawnGridMap[currentPawnMoveOrderIndex].PawnGridName);
        GameObject nextBlock = RetrieveGameObjectOfName(name);

        if (nextBlock != null)
        {
            Vector3 newLocation = Vector3.SmoothDamp(
                PawnGridMap[currentPawnMoveOrderIndex].Pawn.transform.position,
                nextBlock.transform.position, ref PawnGridMap[currentPawnMoveOrderIndex].velocity, 
                pawnMoveSpeed);

            PawnGridMap[currentPawnMoveOrderIndex].Pawn.transform.position = newLocation;
        }

        //for (int i = 0; i < PawnGridMap.Length; i++)
        //{
        //    if (PawnGridMap[i].bHasHit) continue;
        //    string name = GetStringFromChessGridName(PawnGridMap[i].PawnGridName);
        //    GameObject nextBlock = RetrieveGameObjectOfName(name);

        //    if(nextBlock != null)
        //    {
        //        Vector3 newLocation = Vector3.SmoothDamp(PawnGridMap[i].Pawn.transform.position,
        //            nextBlock.transform.position, ref PawnGridMap[i].velocity, pawnMoveSpeed);

        //        PawnGridMap[i].Pawn.transform.position = newLocation;
        //        float distance = Vector3.Distance(PawnGridMap[i].Pawn.transform.position, nextBlock.transform.position);
        //    }
        //}
    }

    private void SetBlockOccupied(string name, GameObject obj)
    {
        GameObject chessBlock = RetrieveGameObjectOfName(name);
        if (chessBlock)
        {
            BlockScript blockScript = chessBlock.GetComponent<BlockScript>();
            if (blockScript != null)
            {
                blockScript.SetOccupiedObj(obj);
            }
        }
    }

    public GameObject GetOuterFrameObject() { return outerFrameObj; }

    private string GetStringFromChessGridName(ChessGridName gridName)
    {
        return (gridName.alph_type.ToString() + gridName.number.ToString());
    }
}
