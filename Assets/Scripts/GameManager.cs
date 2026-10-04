using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public struct ChessGridName
{
    public alphabetType alph_type;
    [Range(1, 8)] public int number;

    public ChessGridName(alphabetType type, int num)
    {
        alph_type = type;
        number = num;   
    }
}

[System.Serializable]
public struct chessLevelData
{
    public GameObject chessBoard;
    public ChessPieceType[] typeOrder;
    public int totalMoves;
    public ChessGridName ChessGridName;
}

public enum GameState
{
    PreparingArena,
    ChoosingStrategy,
    PlayerScalingUp,
    PlayerScalingDown,
    Playing,
    MoveSelecting,
    Moving,
    PlacingWall,
    Hiding,
    BlocksBackToPosition,
    PawnMoveWait,
    PawnMoving,
    PrepareNextRound,
    LevelClose,
    TransitioningToNextLevel
}

public class GameManager : MonoBehaviour
{
    // Chess Board Spawn

    [Header("Base Datas")]

    [SerializeField] private GameObject Player;
    [SerializeField] private Transform MainCamera;

    [SerializeField] private GameObject bottomPanel;

    [SerializeField] private chessLevelData[] levelDatas;
    [SerializeField] private ChessPieceMapSO chessPieceMapSO;
    [SerializeField] private float chessPieceOffsetY;
    [SerializeField] private float chessBlockBusyOffsetY;

    [SerializeField] private TMP_Text movesLeftText;
    [SerializeField] private LayerMask blockLayer;

    [SerializeField] private float pieceScaleLerpTime;
    [SerializeField] private float moveWaitTime;
    [SerializeField] private float destinationMoveSpeed;
    [SerializeField] private float gameStartDelay;
    [SerializeField] private float pawnMoveDelay;
    [SerializeField] private float prepareNextRoundDelay;
    [SerializeField] private float levelCloseDelay;
    [SerializeField] private float outerFrameDownDelay;

    [SerializeField] private GameObject PlaceWallPrefab;
    [SerializeField] private float placeWall_LerpTime;
    [SerializeField] private float placeWallConformSpeed;
    [SerializeField] private float placeWallConformY_Offset;
    [SerializeField] private Material placeWallMaterial_place, placeWallMaterial_conform;

    public float smashOverDelay;

    public List<int> activeSmashOrder = new List<int>();
    private int currentSmashOrderIndex;

    private Vector3 moveVelocity;
    private Vector3 placeWallConformTargetPos;

    private float placeWallElapsedTime;

    private float currentwallPlaceAngle;
    private float finalwallPlaceAngle;

    private GameObject wallPlaceObj;

    private float pieceScaleElapsedTime;

    private List<string> possibleMovesList = new List<string>();
    private List<string> takedownMovesList = new List<string>();

    [Header("Level Transition")]

    [SerializeField] private float transitionLerpTime;
    [SerializeField] private float transitionLevelDelay;
    [SerializeField] private float jumpHeight;

    private float transitionElapsedTime;
    private GameObject NextLevelStartingBlock;

    private Vector3 transitionInitialPos;
    private Vector3 transitionTargetPos;


    [Header("Camera Angles")]

    [SerializeField] private Transform[] BoardcamAngles;
    [SerializeField] private Transform[] TotalcamAngles;
    [SerializeField] private List<Vector3> BoardCamAnglesDistances = new List<Vector3>();
    [SerializeField] private float camAngleSpeed;
    [SerializeField] private float camLerpTime;
    [SerializeField] private TMP_Text camAngleText;

    [SerializeField] private Transform StrategyCamera;

    private float yRotation;
    private bool bIsRMB_Clicked;

    private Vector2 lastMousePosition;
    private int prevCamAngleIndex;
    private int currentCamAngleIndex;

    private float camElapsedTime;

    private Vector3 mc_initialPos, mc_targetPos;
    private Vector3 mc_initialRot, mc_targetRot;

    private List<Transform> camAngleParents = new List<Transform>();

    [Header("Non SerializeField Datas")]

    private PlayerInputs inputActions;

    private chessLevelData activeLevelData;
    private int activeLevelDataIndex;
    private List<PieceMap> currentPieceMap = new List<PieceMap>();

    private float pieceChangeElapsedTime;
    private int movesTaken;

    private int prevPieceIndex;
    private int currentPieceIndex;
    public ChessGridName currentGridName;

    [SerializeField] private GameState gameState;

    private BlockScript prevHoverBS;
    private BlockScript currentHoverBS;

    private float[] angles_rook;
    private float[] angles_bishop;

    private Vector3[] subtractValues_rook;
    private Vector3[] subtractValues_bishop;
    private Vector3[] knight_values;

    private float[][] wallAngleBlock_rook;
    private float[][] wallAngleBlock_bishop;

    private float[] wallBlock_rook;
    private float[] wallBlock_Bishop;

    private string destinationGridName;

    public static GameManager instance;

    private void Awake()
    {
        if(instance == null) instance = this;
    }

    void Start()
    {
        Application.targetFrameRate = 240;

        InitialSetup();
        CursorSetup(true);
        SpawnMainChessPiece();
        PositionChessPiece();
        SetCameraAngle();
        SetWallBlockValues();
        movesLeftText.SetText("Moves Left : " + activeLevelData.totalMoves);
        StartCoroutine(StartGame());
    }

    IEnumerator StartGame()
    {
        yield return new WaitForSeconds(gameStartDelay);
        ChessBoardSpawner chessBoardSpawner = activeLevelData.chessBoard.GetComponent<ChessBoardSpawner>();
        chessBoardSpawner.GetOuterFrameObject().transform.GetComponent<OuterFrame>().SetCanActivateFrameMove(true);
        chessBoardSpawner.GetOuterFrameObject().transform.GetComponent<OuterFrame>().EnableMainLight(true);
    }

    private void SetWallBlockValues()
    {
        wallAngleBlock_rook = new float[][]
        {
            new float[]{ 90f, 180f },
            new float[]{ 180f, 270f },
            new float[]{ 0f, 270f },
            new float[]{ 0f, 90f },
        };

        wallAngleBlock_bishop = new float[][]
        {
            new float[]{ 135f },
            new float[]{ 225f },
            new float[]{ 315f },
            new float[]{ 45f },
        };

        wallBlock_rook = new float[] { };
        wallBlock_Bishop = new float[] { };
    }

    private void InitialSetup()
    {
        inputActions = new PlayerInputs();
        inputActions.Enable();

        prevCamAngleIndex = -1;
        currentCamAngleIndex = 0;
        camAngleText.SetText((currentCamAngleIndex + 1).ToString());

        activeLevelData = levelDatas[0];
        activeLevelDataIndex = 0;
        currentGridName = activeLevelData.ChessGridName;

        SetBlockOccupied(GetStringFromChessGridName(currentGridName), true);

        Transform camAngleParentTrans = RetrieveCamAnglesTransformFromChessBoard();
        foreach (Transform ca_trans in BoardcamAngles)
        {
            ca_trans.transform.parent = camAngleParentTrans;
            Vector3 difference = (ca_trans.position - camAngleParentTrans.position);
            BoardCamAnglesDistances.Add(difference);
        }

        foreach (Transform ca_trans in TotalcamAngles)
        {
            camAngleParents.Add(ca_trans.parent);
        }

        wallPlaceObj = Instantiate(PlaceWallPrefab, new Vector3(-10000, 10000, -10000), Quaternion.identity);

        SetInitialCPValues();
    }

    private void SetBlockOccupied(string name, bool isPlayer)
    {
        GameObject chessBlock = RetrieveGameObjectOfName(name);
        if (chessBlock)
        {
            BlockScript blockScript = chessBlock.GetComponent<BlockScript>();
            if (blockScript != null)
            {
                blockScript.SetOccupiedObj(isPlayer ? Player : null);
            }
        }
    }

    private void SetInitialCPValues()
    {
        angles_rook = new float[] { 0, 90, 180, 270 };
        subtractValues_rook = new Vector3[]
        {
            new Vector3(0f, 0f, 2f),
            new Vector3(2f, 0f, 0f),
            new Vector3(0f, 0f, -2f),
            new Vector3(-2f, 0f, 0f),
        };

        angles_bishop = new float[] { 45, 135, 225, 315 };
        subtractValues_bishop = new Vector3[]
        {
            new Vector3(2f, 0f, 2f),
            new Vector3(2f, 0f, -2f),
            new Vector3(-2f, 0f, -2f),
            new Vector3(-2f, 0f, 2f),
        };

        knight_values = new Vector3[]
        {
            new Vector3(2, 0, 4),
            new Vector3(2, 0, -4),
            new Vector3(-2, 0, 4),
            new Vector3(-2, 0, -4),
            new Vector3(4, 0, 2),
            new Vector3(-4, 0, 2),
            new Vector3(4, 0, -2),
            new Vector3(-4, 0, -2)
        };
    }

    private void CursorSetup(bool bIsActive)
    {
        Cursor.visible = bIsActive;
        Cursor.lockState = CursorLockMode.Confined;
    }

    private Transform RetrieveCamAnglesTransformFromChessBoard()
    {
        Transform obj = null;
        for (int i = 0; i < activeLevelData.chessBoard.transform.childCount; i++)
        {
            if (activeLevelData.chessBoard.transform.GetChild(i).gameObject.tag == "camAnglesParent")
            {
                obj = activeLevelData.chessBoard.transform.GetChild (i);
            }
        }
        return obj;
    }

    private void SpawnMainChessPiece()
    {
        for(int i = 0; i < Enum.GetValues(typeof(ChessPieceType)).Length; i++)
        {
            GameObject spawnObj = chessPieceMapSO.retrieveGameObjectOfType(i);
            Vector3 spawnPos = new Vector3(-10000, 10000, -10000);
            GameObject obj = Instantiate(spawnObj, spawnPos, Quaternion.identity);

            PieceMap map = new PieceMap((ChessPieceType)i, obj, obj.transform.localScale);
            currentPieceMap.Add(map);
        }
    }

    private void PositionChessPiece()
    {
        ChessGridName sp = activeLevelData.ChessGridName;
        string positionString = GetStringFromChessGridName(sp).ToLower();
        
        Vector3 finalPos = Vector3.zero;

        GameObject chessBoard = activeLevelData.chessBoard;

        for(int i = 0; i < chessBoard.transform.childCount; i++)
        {
            if(chessBoard.transform.GetChild(i).name.ToLower() == positionString)
            {
                GameObject chessBlock = chessBoard.transform.GetChild(i).gameObject;
                chessBlock.transform.position = chessBlock.transform.position - new Vector3(0f, chessBlockBusyOffsetY, 0f);

                Vector3 pos = chessBoard.transform.GetChild(i).transform.position;
                finalPos = new Vector3(pos.x, pos.y + chessPieceOffsetY, pos.z);

                chessBlock = chessBoard.transform.GetChild(i).gameObject;

                break;
            }
        }

        Player.transform.position = finalPos;
        GameObject activeChessPiece = RetrieveGameObjectOfType(currentPieceIndex);
        if(activeChessPiece != null)
        {
            activeChessPiece.transform.parent = Player.transform;
            activeChessPiece.transform.localScale = Vector3.zero;
            activeChessPiece.transform.localPosition = Vector3.zero;
        }
    }

    public void CamAngleChangeButton()
    {
        if (gameState != GameState.Playing) return;
        if(++currentCamAngleIndex > TotalcamAngles.Length - 1)
        {
            currentCamAngleIndex = 0;
        }
        camAngleText.SetText((currentCamAngleIndex + 1).ToString());
        SetCameraAngle();
    }

    void Update()
    {
        HandleInputs();
        MoveToDestination();
        PlaceWallBlock();
        PlaceBlockConformPlace();
        ShootRayFromCamera();
        //SetCameraAngle();
        MoveCameraToAngleLocation();
        //HandleChessPieceChanging();
        ScaleChessPiece();
        RemoveWallAfterMove();
        TransitionLevel();
    }

    void HandleInputs()
    {
        if (inputActions.Player.RightMouseButton.WasPressedThisFrame())
        {
            lastMousePosition = Mouse.current.position.ReadValue();
            CursorSetup(false);
            bIsRMB_Clicked = true;
            
            if(gameState == GameState.Playing)
            {
                for(int i = 0; i < activeLevelData.chessBoard.transform.childCount; i++)
                {
                    if(IsInPossibleMovesList(activeLevelData.chessBoard.transform.GetChild(i).name))
                    {
                        BlockScript b_script = activeLevelData.chessBoard.transform.GetChild(i).GetComponent<BlockScript>();
                        if(b_script != null)
                        {
                            b_script.SetBlockState(BlockState.Highlighted);
                        }
                    }
                }
            }

        }

        if(inputActions.Player.RightMouseButton.WasReleasedThisFrame())
        {
            Mouse.current.WarpCursorPosition(lastMousePosition);
            CursorSetup(true);
            bIsRMB_Clicked = false;
        }

        if(inputActions.Player.RightMouseButton.IsPressed())
        {
            Vector2 mouseValue = inputActions.Player.DeltaMouse.ReadValue<Vector2>();

            yRotation += mouseValue.x * camAngleSpeed;

            foreach(Transform ca_parent in camAngleParents)
            {
                ca_parent.transform.localEulerAngles = new Vector3(0f, yRotation, 0f);
            }
        }

        if(inputActions.Player.LeftMouseButton.WasPressedThisFrame() && !bIsRMB_Clicked && 
            gameState == GameState.Playing)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit hit;
            Physics.Raycast(ray, out hit, 1000f, blockLayer);
            if (hit.collider != null)
            {
                if (IsInPossibleMovesList(hit.collider.name) || IsInTakeDownMovesList(hit.collider.name))
                {
                    SetGameState(GameState.MoveSelecting);
                    destinationGridName = hit.collider.name;

                    StartCoroutine(SetToMoving());

                    movesTaken++;
                    movesLeftText.SetText("Moves Left : " + (activeLevelData.totalMoves - movesTaken).ToString());

                    ResetChessBlocks();
                }
            }
        }
    
        if(inputActions.Player.LeftMouseButton.WasPressedThisFrame() && gameState == GameState.PlacingWall)
        {
            SetGameState(GameState.Hiding);
            placeWallElapsedTime = 0f;
            activeLevelData.chessBoard.GetComponent<ChessBoardSpawner>().
                GetOuterFrameObject().GetComponent<OuterFrame>().SmashHands(activeSmashOrder[currentSmashOrderIndex]);
            
            placeWallConformTargetPos = RetrieveGameObjectOfName(destinationGridName).transform.position;

            wallPlaceObj.transform.position = placeWallConformTargetPos + new Vector3(0f, placeWallConformY_Offset, 0f);
            wallPlaceObj.GetComponent<Renderer>().material = placeWallMaterial_place;

            List<float> refAngles = new List<float>() { 0f, 90f, 180f, 270f };

            float wallPlaceAngle = (wallPlaceObj.transform.eulerAngles.y % 360f + 360f) % 360f;
            int index = findIndexFromArray(refAngles, wallPlaceAngle);
            wallBlock_rook = wallAngleBlock_rook[index];
            wallBlock_Bishop = wallAngleBlock_bishop[index];
        }
    }

    private int findIndexFromArray(List<float> refAngles, float value)
    {
        for(int i = 0; i < refAngles.Count; i++)
        {
            if (Mathf.RoundToInt(refAngles[i]) == Mathf.RoundToInt(value))
            {
                return i;
            }
        }

        return 0;
    }

    private void PlaceBlockConformPlace()
    {
        if (gameState != GameState.Hiding) return;
        Vector3 newPos = Vector3.MoveTowards(wallPlaceObj.transform.position, placeWallConformTargetPos,
            Time.deltaTime * placeWallConformSpeed);

        wallPlaceObj.transform.position = newPos;
    }

    IEnumerator SetToMoving()
    {
        yield return new WaitForSeconds(moveWaitTime);
        SetGameState(GameState.Moving);
    }

    private void MoveToDestination()
    {
        if (gameState != GameState.Moving) return;
        GameObject targetBlock = RetrieveGameObjectOfName(destinationGridName);
        Vector3 targetLocation = new Vector3(targetBlock.transform.position.x, Player.transform.position.y,
            targetBlock.transform.position.z);
        
        Vector3 newLocation = Vector3.SmoothDamp(Player.transform.position,
            targetLocation, ref moveVelocity, destinationMoveSpeed);

        Player.transform.position = newLocation;

        float distance = Vector3.Distance(Player.transform.position, targetBlock.transform.position);
       
        if(distance <= 0.11f)
        {
            if(activeLevelData.chessBoard.GetComponent<ChessBoardSpawner>().IsKingDefeated())
            {
                SetGameState(GameState.LevelClose);
            } 
            else
            {
                SetGameState(GameState.PlacingWall);
                alphabetType type;
                System.Enum.TryParse(destinationGridName[0].ToString(), out type);

                SetBlockOccupied(GetStringFromChessGridName(currentGridName), false);
                SetBlockOccupied(destinationGridName, true);
                currentGridName = new ChessGridName(type, int.Parse(destinationGridName[1].ToString()));

                wallPlaceObj.GetComponent<Renderer>().material = placeWallMaterial_conform;
            
                for (int i = 0; i < activeLevelData.chessBoard.transform.childCount; i++)
                {
                    GameObject chessBlock = activeLevelData.chessBoard.transform.GetChild(i).gameObject;
                    if(chessBlock.gameObject.tag == "block")
                    {
                        if (chessBlock.name.ToLower() != destinationGridName.ToLower())
                        {
                            chessBlock.GetComponent<BlockScript>().SetIsDecreasingY(true);
                        }
                    }
                }
            }

        }
    }

    private void PlaceWallBlock()
    {
        if (gameState != GameState.PlacingWall) return;
        GameObject MainChessBlock = RetrieveGameObjectOfName(destinationGridName);

        wallPlaceObj.transform.position = MainChessBlock.transform.position;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 mouseWorldPos = ray.GetPoint(distance);

            Vector3 direction = mouseWorldPos - MainChessBlock.transform.position;

            float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

            if(angle >= -45f && angle < 45f && finalwallPlaceAngle != 180f)
            {
                DetermineFinalWallPlaceAngle(180f);
            }
            else if(angle >= 45f && angle < 135f && finalwallPlaceAngle != 270f)
            {
                DetermineFinalWallPlaceAngle(270f);
            } else if(angle >= -135f && angle < -45f && finalwallPlaceAngle != 90f)
            {
                DetermineFinalWallPlaceAngle(90f);
            } else if (finalwallPlaceAngle != 0 && (angle >= 135f || angle < -135f))
            {
                DetermineFinalWallPlaceAngle(0f);
            }

            float t = placeWallElapsedTime / placeWall_LerpTime;
            t = Mathf.SmoothStep(0f, 1f, t);

            float newAngle = Mathf.LerpAngle(currentwallPlaceAngle, finalwallPlaceAngle, t);
            wallPlaceObj.transform.eulerAngles = new Vector3(0f, newAngle, 0f);

            placeWallElapsedTime += Time.deltaTime;
        }
    }

    private void RemoveWallAfterMove()
    {
        if (gameState != GameState.Moving) return;
        Vector3 newPos = Vector3.MoveTowards(wallPlaceObj.transform.position, 
            new Vector3(wallPlaceObj.transform.position.x, 100f, wallPlaceObj.transform.position.z),
            Time.deltaTime * placeWallConformSpeed);

        wallPlaceObj.transform.position = newPos;
    }

    private void DetermineFinalWallPlaceAngle(float angle)
    {
        currentwallPlaceAngle = wallPlaceObj.transform.eulerAngles.y;
        finalwallPlaceAngle = angle;
        placeWallElapsedTime = 0f;
    }

    private void ShootRayFromCamera()
    {
        if (gameState != GameState.Playing || bIsRMB_Clicked) return;
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;
        Physics.Raycast(ray, out hit, 1000f, blockLayer);
        if(hit.collider != null)
        {
            if(prevHoverBS && prevHoverBS != currentHoverBS)
            {
                prevHoverBS.SetBlockState(IsInTakeDownMovesList(prevHoverBS.name) ? BlockState.Threat : BlockState.Highlighted);
                prevHoverBS = null;
            }

            BlockScript b_script = hit.collider.GetComponent<BlockScript>();
            if(b_script != null && currentHoverBS != b_script)
            {
                if(IsInPossibleMovesList(hit.collider.name) || IsInTakeDownMovesList(hit.collider.name))
                {
                    b_script.SetBlockState(BlockState.Selected);
                    prevHoverBS = currentHoverBS;
                    currentHoverBS = b_script;
                } else
                {
                    if(currentHoverBS && currentHoverBS.GetBlockState() != BlockState.Highlighted)
                    {
                        currentHoverBS.SetBlockState(IsInTakeDownMovesList(currentHoverBS.name) ? BlockState.Threat : BlockState.Highlighted);
                        currentHoverBS = null;
                    }
                }
            }
        }
    }

    private bool IsInPossibleMovesList(string gridName)
    {
        for(int i = 0; i < possibleMovesList.Count; i++)
        {
            if (possibleMovesList[i].ToLower() == gridName.ToLower())
            {
                return true;
            }
        }

        return false;
    }

    private bool IsInTakeDownMovesList(string gridName)
    {
        for (int i = 0; i < takedownMovesList.Count; i++)
        {
            if (takedownMovesList[i].ToLower() == gridName.ToLower())
            {
                return true;
            }
        }

        return false;
    }

    private void SetCameraAngle()
    {
        //if (prevCamAngleIndex == currentCamAngleIndex) return;

        if(gameState == GameState.PreparingArena)
        {
            MainCamera.transform.parent = TotalcamAngles[0];
        } else if (gameState == GameState.ChoosingStrategy)
        {
            MainCamera.transform.parent = StrategyCamera;
        } else if(gameState == GameState.LevelClose)
        {
            MainCamera.transform.parent = TotalcamAngles[1];
        }
        else
        {
            MainCamera.transform.parent = TotalcamAngles[currentCamAngleIndex];
            prevCamAngleIndex = currentCamAngleIndex;
        }

        SetMCInitials();
    }

    private void SetMCInitials()
    {
        camElapsedTime = 0f;

        mc_initialPos = MainCamera.transform.localPosition;
        mc_targetPos = Vector3.zero;

        mc_initialRot = MainCamera.transform.localEulerAngles;
        mc_targetRot = Vector3.zero;
    }

    private void MoveCameraToAngleLocation()
    {
        float t = camElapsedTime / camLerpTime;
        t = Mathf.SmoothStep(0f, 1f, t);
        if (t >= 1) return;
        Vector3 newPosition = Vector3.Lerp(mc_initialPos, mc_targetPos, t);
        float new_angleX = Mathf.LerpAngle(mc_initialRot.x, mc_targetRot.x, t);
        float new_angleY = Mathf.LerpAngle(mc_initialRot.y, mc_targetRot.y, t);
        float new_angleZ = Mathf.LerpAngle(mc_initialRot.z, mc_targetRot.z, t);

        Vector3 newRotation = new Vector3(new_angleX, new_angleY, new_angleZ);

        MainCamera.transform.localPosition = newPosition;
        MainCamera.transform.localEulerAngles = newRotation;

        camElapsedTime += Time.deltaTime;
    }

    void ScaleChessPiece()
    {
        if (gameState != GameState.PlayerScalingUp  && gameState != GameState.PlayerScalingDown) return;
        float t = pieceScaleElapsedTime / pieceScaleLerpTime;
        PieceMap pieceMap = RetrieveCurrentPieceMapOfType(currentPieceIndex);

        Vector3 lerp_a = gameState == GameState.PlayerScalingDown ? pieceMap.localScale : Vector3.zero;
        Vector3 lerp_b = gameState == GameState.PlayerScalingDown ? Vector3.zero : pieceMap.localScale;

        Vector3 newScale = Vector3.Lerp(lerp_a, lerp_b, t);
        if (pieceMap.chessPiece != null)
        {
            pieceMap.chessPiece.transform.localScale = newScale;
        }

        pieceScaleElapsedTime += Time.deltaTime;
        if (t >= 1)
        {
            pieceScaleElapsedTime = 0f;
            if (gameState == GameState.PlayerScalingDown)
            {
                ChangeChessPiece();
                SetGameState(GameState.PlayerScalingUp);
            } else
            {
                movesTaken = 0;
                movesLeftText.SetText("Moves Left : " + activeLevelData.totalMoves);
                SetGameState(GameState.Playing);
                FindPossibleMoves();
            }
        }
    }

    private void ChangeChessPiece()
    {
        if (++currentPieceIndex >= Enum.GetValues(typeof(ChessPieceType)).Length)
        {
            currentPieceIndex = 0;
        }

        GameObject prevPiece = RetrieveGameObjectOfType(prevPieceIndex);
        GameObject currentPiece = RetrieveGameObjectOfType(currentPieceIndex);

        prevPiece.transform.parent = null;
        currentPiece.transform.parent = Player.transform;

        currentPiece.transform.localScale = Vector3.zero;
        currentPiece.transform.localPosition = Vector3.zero;
        prevPiece.transform.position = new Vector3(-10000, 10000, -10000);
        
        prevPieceIndex = currentPieceIndex;
    }

    private GameObject RetrieveGameObjectOfType(int pieceIndex)
    {
        ChessPieceType type = activeLevelData.typeOrder[pieceIndex];
        GameObject obj = null;

        for (int i = 0; i < currentPieceMap.Count; i++)
        {
            if (currentPieceMap[i].pieceType == type)
            {
                obj = currentPieceMap[i].chessPiece;
                break;
            }
        }

        return obj;
    }

    private GameObject RetrieveGameObjectOfName(string name)
    {
        GameObject obj = null;
        for(int i = 0; i < activeLevelData.chessBoard.transform.childCount; i++)
        {
            if(activeLevelData.chessBoard.transform.GetChild(i).name.ToLower() == name.ToLower())
            {
                obj = activeLevelData.chessBoard.transform.GetChild(i).gameObject;
                break;
            }
        }

        return obj;
    }

    private PieceMap RetrieveCurrentPieceMapOfType(int pieceIndex)
    {
        ChessPieceType type = activeLevelData.typeOrder[pieceIndex];
        PieceMap map = new PieceMap();

        for (int i = 0; i < currentPieceMap.Count; i++)
        {
            if (currentPieceMap[i].pieceType == type)
            {
                map = currentPieceMap[i];
                break;
            }
        }

        return map;
    }

    private void FindPossibleMoves()
    {
        possibleMovesList.Clear();
        takedownMovesList.Clear();
        ResetChessBlocks();
        switch (activeLevelData.typeOrder[currentPieceIndex])
        {
            case ChessPieceType.Rook:
                RookAndBishopMoveReactor(angles_rook, subtractValues_rook, true);
                break;
            case ChessPieceType.Bishop:
                RookAndBishopMoveReactor(angles_bishop, subtractValues_bishop, false);
                break;
            case ChessPieceType.Knight:
                KnightMoveRefactor(knight_values);
                break;
        }
    }

    private void ResetChessBlocks()
    {
        for(int i = 0; i < activeLevelData.chessBoard.transform.childCount; i++)
        {
            if(activeLevelData.chessBoard.transform.GetChild(i).gameObject.tag == "block")
            {
                BlockScript b_script = activeLevelData.chessBoard.transform.GetChild(i).GetComponent<BlockScript>();
                if(b_script)
                {
                    b_script.SetBlockState(BlockState.Normal);
                }
            }
        }
    }

    private void FindPathToDestination()
    {
        GameObject currentChessPiece = RetrieveGameObjectOfType(currentPieceIndex);
        for (int i = 0; i < 4; i++)
        {
            List<string> pathOrder = new List<string>();
            Vector3 startPos = currentChessPiece.transform.position + new Vector3(0f, 3f, 0f);

            bool bFlag = false;
            for (int j = 0; j < 8; j++)
            {
                Vector3 v_startPos = startPos + subtractValues_bishop[i] * j;

                RaycastHit hit;
                bool bSuccess = Physics.Raycast(v_startPos, Vector3.down, out hit, 5f, blockLayer);
                if (bSuccess)
                {
                    if(hit.collider != null)
                    {
                        if(hit.collider.name.ToLower() != currentChessPiece.name.ToLower())
                        {
                            pathOrder.Add(hit.collider.name);
                            if(hit.collider.name.ToLower() == destinationGridName.ToLower())
                            {
                                bFlag = true;
                                break;
                            }
                        }
                    }                   
                }
            }

            if(bFlag)
            {
                for(int j = 0; j < pathOrder.Count;  j++)
                {
                    print(pathOrder[j]);
                }
                break;
            }
        }
    }

    private void KnightMoveRefactor(Vector3[] knight_values)
    {
        GameObject currentChessPiece = RetrieveGameObjectOfType(currentPieceIndex);
        for (int i = 0; i < 8; i++)
        {
            Vector3 pos = currentChessPiece.transform.position + new Vector3(0f, 3f, 0f) + knight_values[i];
            Vector3 endPos = pos + (Vector3.down * 5f);
            RaycastHit hit;
            bool bSuccess = Physics.Raycast(pos, Vector3.down, out hit, 5f);

            if (bSuccess)
            {
                BlockAndEnemyDetection(pos, hit);
            }
        }
    }

    bool checkExistenceInArray(float[] array, float element)
    {
        for(int i = 0; i < array.Length; i++)
        {
            if (Mathf.RoundToInt(array[i]) == Mathf.RoundToInt(element))
            {
                return true;
            }
        }
        return false;
    }

    private void RookAndBishopMoveReactor(float[] angles, Vector3[] subtractValues, bool bIsRook)
    {
        GameObject currentChessPiece = RetrieveGameObjectOfType(currentPieceIndex);
        float[] finalWallBlockArray = bIsRook ? wallBlock_rook : wallBlock_Bishop;
        for (int i = 0; i < 4; i++)
        {
            if (checkExistenceInArray(finalWallBlockArray, angles[i])) continue;
            Vector3 startPos = currentChessPiece.transform.position + new Vector3(0f, 6f, 0f);

            Vector3 myAngles = new Vector3(0f, angles[i], 0f);
            Vector3 forwardVector = Quaternion.Euler(myAngles) * Vector3.forward;

            Vector3 endPos = startPos + (forwardVector * 16);
            if (i == 1)
            {
                Debug.DrawLine(startPos, endPos, Color.red, 100f);   
            }

            for (int j = 0; j < 8; j++)
            {
                Vector3 v_startPos = startPos + subtractValues[i] * j;

                RaycastHit hit;
                bool bSuccess = Physics.Raycast(v_startPos, Vector3.down, out hit, 10f);
                if (bSuccess)
                {
                    if (BlockAndEnemyDetection(v_startPos, hit))
                    {
                        break;
                    }
                }
            }
        }
    }

    private bool BlockAndEnemyDetection(Vector3 v_startPos, RaycastHit hit)
    {
        string currentgridNameString = GetStringFromChessGridName(currentGridName);
        if (hit.collider.gameObject.tag == "enemy")
        {
            RaycastHit blockHit;
            bool bBlockSuccess = Physics.Raycast(v_startPos, Vector3.down, out blockHit, 10f, blockLayer);
            if (bBlockSuccess)
            {
                if (blockHit.collider != null)
                {
                    blockHit.collider.GetComponent<BlockScript>().SetBlockState(BlockState.Threat);
                    takedownMovesList.Add(blockHit.collider.name);
                }
            }
            return true;
        }
        else if (hit.collider.gameObject.tag == "block")
        {
            
            if(hit.collider.gameObject.name.ToLower() != currentgridNameString.ToLower())
            {
                hit.collider.GetComponent<BlockScript>().SetBlockState(BlockState.Highlighted);
                possibleMovesList.Add(hit.collider.gameObject.name);
            }
        }

        return false;
    }

    private string GetStringFromChessGridName(ChessGridName gridName)
    {
        return (gridName.alph_type.ToString() + gridName.number.ToString());
    }

    IEnumerator CloseLevel()
    {
        yield return new WaitForSeconds(levelCloseDelay);
        GameObject chessObject = activeLevelData.chessBoard;
        if (chessObject)
        {
            ChessBoardSpawner cb_spawner = chessObject.GetComponent<ChessBoardSpawner>();
            if (cb_spawner)
            {
                OuterFrame outerFrame = cb_spawner.GetOuterFrameObject().GetComponent<OuterFrame>();
                if (outerFrame)
                {
                    SetCameraAngle();
                    outerFrame.PrepareSmashSet(activeSmashOrder[currentSmashOrderIndex], false);
                    StartCoroutine(MoveOuterFrameDown(outerFrame));
                }
            }
        }
    }

    private void TransitionLevel()
    {
        if(gameState == GameState.TransitioningToNextLevel)
        {
            float t = Mathf.Clamp01(transitionElapsedTime / transitionLerpTime);
            Vector3 position = Vector3.Lerp(transitionInitialPos, transitionTargetPos, t);
            float height = 4f * jumpHeight * t * (1f - t);

            position.y = 1f + height;

            Player.transform.position = position;

            transitionElapsedTime += Time.deltaTime;

            if(t >= 1)
            {
                activeLevelData.chessBoard.GetComponent<ChessBoardSpawner>().GetOuterFrameObject().
                    GetComponent<OuterFrame>().EnableMainLight(false);

                activeLevelDataIndex++;
                activeLevelData = levelDatas[activeLevelDataIndex];

                Transform camAngleParentTrans = RetrieveCamAnglesTransformFromChessBoard();

                for(int i = 0; i < BoardcamAngles.Length; i++)
                {
                    BoardcamAngles[i].transform.parent = camAngleParentTrans;
                    BoardcamAngles[i].transform.position = camAngleParentTrans.position + BoardCamAnglesDistances[i];
                }
                SetGameState(GameState.PreparingArena);
                StartCoroutine(StartGame());
            }
        }
    }

    IEnumerator MoveOuterFrameDown(OuterFrame frame)
    {
        yield return new WaitForSeconds(outerFrameDownDelay);
        frame.SetLevelBool();

        if(IsNextLevelExist())
        {
            string name = GetStringFromChessGridName(levelDatas[activeLevelDataIndex + 1].ChessGridName);

            NextLevelStartingBlock = levelDatas[activeLevelDataIndex + 1].chessBoard.
                GetComponent<ChessBoardSpawner>().RetrieveGameObjectOfName(name);

            //print(levelDatas[activeLevelDataIndex + 1].chessBoard.GetComponent<ChessBoardSpawner>());
            //print(levelDatas[activeLevelDataIndex + 1].chessBoard.GetComponent<ChessBoardSpawner>().GetOuterFrameObject());
            //print(levelDatas[activeLevelDataIndex + 1].chessBoard.
            //    GetComponent<ChessBoardSpawner>().GetOuterFrameObject().GetComponent<OuterFrame>());

            levelDatas[activeLevelDataIndex + 1].chessBoard.
                GetComponent<ChessBoardSpawner>().GetOuterFrameObject().GetComponent<OuterFrame>().
                    EnableMainLight(true);

            transitionInitialPos = Player.transform.position;
            transitionTargetPos = NextLevelStartingBlock.transform.position;

            StartCoroutine(SetToTransitionLevel());
        }
    }

    IEnumerator SetToTransitionLevel()
    {
        yield return new WaitForSeconds(transitionLevelDelay);
        SetGameState(GameState.TransitioningToNextLevel);
    }

    public void SetGameState(GameState state) { 
        gameState = state; 

        if(gameState == GameState.LevelClose)
        {
            StartCoroutine(CloseLevel());
        }

        if(gameState == GameState.BlocksBackToPosition)
        {
            for (int i = 0; i < activeLevelData.chessBoard.transform.childCount; i++)
            {
                GameObject chessBlock = activeLevelData.chessBoard.transform.GetChild(i).gameObject;
                if (chessBlock.gameObject.tag == "block")
                {
                    if (chessBlock.name.ToLower() != destinationGridName.ToLower())
                    {
                        chessBlock.GetComponent<BlockScript>().SetIsDecreasingY(false);
                    }
                }
            }
        }

        if(gameState == GameState.PrepareNextRound)
        {
            GameObject chessObject = activeLevelData.chessBoard;
            if (chessObject)
            {
                ChessBoardSpawner cb_spawner = chessObject.GetComponent<ChessBoardSpawner>();
                if (cb_spawner)
                {
                    OuterFrame outerFrame = cb_spawner.GetOuterFrameObject().GetComponent<OuterFrame>();
                    if (outerFrame)
                    {
                        outerFrame.PrepareSmashSet(activeSmashOrder[currentSmashOrderIndex], false);
                    }
                }

                if (++currentSmashOrderIndex >= 4)
                {
                    currentSmashOrderIndex = 0;
                }

                if(movesTaken == activeLevelData.totalMoves)
                {
                    SetGameState(GameState.PlayerScalingDown);
                } 
                else
                {
                    SetGameState(GameState.Playing);
                    FindPossibleMoves();
                }
            }

        }

        if(gameState == GameState.PawnMoveWait)
        {
            StartCoroutine(MovePawnsWithDelay());
        }

        if(gameState == GameState.Playing)
        {
            SetCameraAngle();
            GameObject chessObject = activeLevelData.chessBoard;
            if(chessObject)
            {
                ChessBoardSpawner cb_spawner = chessObject.GetComponent<ChessBoardSpawner>();
                if(cb_spawner)
                {
                    OuterFrame outerFrame = cb_spawner.GetOuterFrameObject().GetComponent<OuterFrame>();
                    if(outerFrame)
                    {
                        outerFrame.PrepareSmashSet(activeSmashOrder[currentSmashOrderIndex], true);
                    }
                }
            }
        }

        if (gameState == GameState.PawnMoving)
        {
            StartCoroutine(PrepareNextRoundWithDelay());
        }

        if (gameState < GameState.PlayerScalingUp)
        {
            SetCameraAngle();
        }
        if(gameState == GameState.ChoosingStrategy)
        {
            bottomPanel.SetActive(true);
        }
    }

    IEnumerator PrepareNextRoundWithDelay()
    {
        yield return new WaitForSeconds(prepareNextRoundDelay);
        SetGameState(GameState.PrepareNextRound);
    }

    IEnumerator MovePawnsWithDelay()
    {
        yield return new WaitForSeconds(pawnMoveDelay);
        activeLevelData.chessBoard.GetComponent<ChessBoardSpawner>().MovePawnsForward();
        SetGameState(GameState.PawnMoving);
    }

    public GameState GetGameState() { return gameState; }

    public bool IsNextLevelExist()
    {
        return (activeLevelDataIndex + 1) < levelDatas.Length;
    }
}
