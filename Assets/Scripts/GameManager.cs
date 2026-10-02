using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine.InputSystem;

[System.Serializable]
public struct ChessGridName
{
    public alphabetType alph_type;
    [Range(1, 8)] public int number;
}

[System.Serializable]
public struct chessLevelData
{
    public GameObject chessBoard;
    public ChessPieceType[] typeOrder;
    public float pieceChangeTimeInterval;
    public ChessGridName ChessGridName;
}

public enum GameState
{
    PlayerScalingUp,
    PlayerScalingDown,
    Playing,
}

public class GameManager : MonoBehaviour
{
    // Chess Board Spawn

    [Header("Base Datas")]

    [SerializeField] private GameObject Player;
    [SerializeField] private Transform MainCamera;

    [SerializeField] private chessLevelData[] levelDatas;
    [SerializeField] private ChessPieceMapSO chessPieceMapSO;
    [SerializeField] private float chessPieceOffsetY;
    [SerializeField] private float chessBlockBusyOffsetY;

    [SerializeField] private TMP_Text pieceActiveTimeText;
    [SerializeField] private LayerMask blockLayer;

    [SerializeField] private float pieceScaleLerpTime;
    private float pieceScaleElapsedTime;

    private List<string> possibleMovesList = new List<string>();

    [Header("Camera Angles")]

    [SerializeField] private Transform[] BoardcamAngles;
    [SerializeField] private Transform[] TotalcamAngles;
    [SerializeField] private float camAngleSpeed;
    [SerializeField] private float camLerpTime;
    [SerializeField] private TMP_Text camAngleText;

    private float yRotation;

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
    private List<PieceMap> currentPieceMap = new List<PieceMap>();

    private float pieceChangeElapsedTime;

    private int prevPieceIndex;
    private int currentPieceIndex;
    private ChessGridName currentGridName;

    private GameState gameState;

    void Start()
    {
        InitialSetup();
        CursorSetup(true);
        SpawnMainChessPiece();
        PositionChessPiece();
        //StartCoroutine(findMoves());
    }

    IEnumerator findMoves()
    {
        yield return null;
        FindPossibleMoves();
    }

    private void InitialSetup()
    {
        inputActions = new PlayerInputs();
        inputActions.Enable();

        prevCamAngleIndex = -1;
        currentCamAngleIndex = 0;
        camAngleText.SetText((currentCamAngleIndex + 1).ToString());

        activeLevelData = levelDatas[0];
        currentGridName = activeLevelData.ChessGridName;

        Transform camAngleParentTrans = RetrieveCamAnglesTransformFromChessBoard();
        foreach(Transform ca_trans in BoardcamAngles)
        {
            ca_trans.transform.parent = camAngleParentTrans;
        }

        foreach(Transform ca_trans in TotalcamAngles)
        {
            camAngleParents.Add(ca_trans.parent);
        }
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
            activeChessPiece.transform.localPosition = Vector3.zero;
        }
    }

    public void CamAngleChangeButton()
    {
        if(++currentCamAngleIndex > 2)
        {
            currentCamAngleIndex = 0;
        }

        camAngleText.SetText((currentCamAngleIndex + 1).ToString());
    }

    void Update()
    {
        HandleInputs();
        SetCameraAngle();
        MoveCameraToAngleLocation();
        HandleChessPieceChanging();
        ScaleChessPiece();
    }

    void HandleInputs()
    {
        if (inputActions.Player.RightMouseButton.WasPressedThisFrame())
        {
            lastMousePosition = Mouse.current.position.ReadValue();
            CursorSetup(false);
        }

        if(inputActions.Player.RightMouseButton.WasReleasedThisFrame())
        {
            Mouse.current.WarpCursorPosition(lastMousePosition);
            CursorSetup(true);
        }

        if(inputActions.Player.RightMouseButton.IsPressed())
        {
            Vector2 mouseValue = inputActions.Player.DeltaMouse.ReadValue<Vector2>();

            yRotation += mouseValue.x * camAngleSpeed;
            camAngleParents[currentCamAngleIndex].transform.localEulerAngles = new Vector3(0f, yRotation, 0f);
        }

    }

    private void SetCameraAngle()
    {
        if (prevCamAngleIndex == currentCamAngleIndex) return;
        MainCamera.transform.parent = null;

        foreach(Transform ca_parent in camAngleParents)
        {
            ca_parent.transform.localEulerAngles = Vector3.zero;
        }
        yRotation = 0f;

        MainCamera.transform.parent = TotalcamAngles[currentCamAngleIndex];
        prevCamAngleIndex = currentCamAngleIndex;

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

    private void HandleChessPieceChanging()
    {
        if (gameState != GameState.Playing) return;
        pieceChangeElapsedTime += Time.deltaTime;

        float textValue = Mathf.Clamp(activeLevelData.pieceChangeTimeInterval - pieceChangeElapsedTime, 
            0f, activeLevelData.pieceChangeTimeInterval);

        pieceActiveTimeText.SetText(textValue.ToString("F1"));

        if (pieceChangeElapsedTime >= activeLevelData.pieceChangeTimeInterval)
        {
            gameState = GameState.PlayerScalingDown;

            ResetChessBlocks();

            pieceChangeElapsedTime = 0f;
        }
    }

    void ScaleChessPiece()
    {
        if (gameState == GameState.Playing) return;
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
                gameState = GameState.PlayerScalingUp;
            } else
            {
                gameState = GameState.Playing;
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
        List<string> moveGridNames = new List<string>();

        float[] angles_rook = { 0, 90, 180, 270 };
        Vector3[] subtractValues_rook =
        {
            new Vector3(0f, 0f, 2f),
            new Vector3(2f, 0f, 0f),
            new Vector3(0f, 0f, -2f),
            new Vector3(-2f, 0f, 0f),
        };

        float[] angles_bishop = { 45, 135, 225, 315 };
        Vector3[] subtractValues_bishop =
        {
            new Vector3(2f, 0f, 2f),
            new Vector3(2f, 0f, -2f),
            new Vector3(-2f, 0f, -2f),
            new Vector3(-2f, 0f, 2f),
        };

        Vector3[] knight_values =
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

        ResetChessBlocks();
        switch (activeLevelData.typeOrder[currentPieceIndex])
        {
            case ChessPieceType.Rook:
                RookAndBishopMoveReactor(angles_rook, subtractValues_rook);
                break;
            case ChessPieceType.Bishop:
                RookAndBishopMoveReactor(angles_bishop, subtractValues_bishop);
                break;
            case ChessPieceType.Knight:
                KnightMoveRefactor(knight_values);
                break;
        }

        //KnightMoveRefactor(knight_values);
        //RookAndBishopMoveReactor(angles_bishop, subtractValues_bishop);
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

    private void RookAndBishopMoveReactor(float[] angles, Vector3[] subtractValues)
    {
        GameObject currentChessPiece = RetrieveGameObjectOfType(currentPieceIndex);
        for (int i = 0; i < 4; i++)
        {
            Vector3 startPos = currentChessPiece.transform.position + new Vector3(0f, 3f, 0f);

            Vector3 myAngles = new Vector3(0f, angles[i], 0f);
            Vector3 forwardVector = Quaternion.Euler(myAngles) * Vector3.forward;

            Vector3 endPos = startPos + (forwardVector * 16);

            //Debug.DrawLine(startPos, endPos, Color.red, 100f);

            for (int j = 0; j < 8; j++)
            {
                Vector3 v_startPos = startPos + subtractValues[i] * j;
                Vector3 v_endPos = v_startPos + new Vector3(0f, -4f, 0f);
                Vector3 direction = (v_endPos - v_startPos).normalized;

                //Debug.DrawLine(v_startPos, v_endPos, Color.red, 100f);

                RaycastHit hit;
                bool bSuccess = Physics.Raycast(v_startPos, direction, out hit, 5f);
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
        if (hit.collider.gameObject.tag == "enemy")
        {
            RaycastHit blockHit;
            bool bBlockSuccess = Physics.Raycast(v_startPos, Vector3.down, out blockHit, 5f, blockLayer);
            if (bBlockSuccess)
            {
                if (blockHit.collider != null)
                {
                    //blockHit.collider.gameObject.GetComponent<Renderer>().material = ThreatMaterial;
                    //blockHit.collider.transform.GetChild(0).gameObject.SetActive(true);
                    blockHit.collider.GetComponent<BlockScript>().SetBlockState(BlockState.Threat);
                }
            }
            return true;
        }
        else if (hit.collider.gameObject.tag == "block")
        {
            string currentgridName = GetStringFromChessGridName(currentGridName);
            if(hit.collider.gameObject.name.ToLower() != currentgridName.ToLower())
            {
                //hit.collider.gameObject.GetComponent<Renderer>().material = highlightedMaterial;
                //hit.collider.transform.GetChild(0).gameObject.SetActive(true);
                hit.collider.GetComponent<BlockScript>().SetBlockState(BlockState.Highlighted);
            }
        }

        return false;
    }

    private string GetStringFromChessGridName(ChessGridName gridName)
    {
        return (gridName.alph_type.ToString() + gridName.number.ToString());
    }
}
