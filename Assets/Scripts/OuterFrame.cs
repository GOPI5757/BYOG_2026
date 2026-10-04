using UnityEngine;
using UnityEngine.UI;

public class OuterFrame : MonoBehaviour
{
    [SerializeField] private GameObject FrameMoveArea;
    [SerializeField] private float frameStartY;
    [SerializeField] private float frameMoveLerpTime;
    [SerializeField] private GameObject[] smashSets;

    [SerializeField] private GameObject MainLight;

    public ChessBoardSpawner chessBoardSpawner;

    private bool canActivateFrameMove;

    private float frameMoveElapsedTime;
    private Vector3 initialPos;
    private Vector3 targetPos;

    private bool bLevelClose;

    void Start()
    {
        FrameMoveArea.transform.localPosition = FrameMoveArea.transform.localPosition - new Vector3(0f, frameStartY, 0f);
        
        initialPos = FrameMoveArea.transform.localPosition;
        targetPos = Vector3.zero;
    }

    void Update()
    {
        if (GameManager.instance.GetGameState() != GameState.PreparingArena && !bLevelClose) return;
        if(canActivateFrameMove)
        {
            float t = frameMoveElapsedTime / frameMoveLerpTime;
            t = Mathf.SmoothStep(0f, 1f, t);
            Vector3 newPos = Vector3.Lerp(!bLevelClose ? initialPos : targetPos, 
                !bLevelClose ? targetPos : initialPos, t);
            FrameMoveArea.transform.localPosition = newPos;

            frameMoveElapsedTime += Time.deltaTime;

            if(t >= 1)
            {
                if(GameManager.instance.GetGameState() == GameState.PreparingArena && !bLevelClose)
                {
                    GameManager.instance.SetGameState(GameState.ChoosingStrategy);
                } else
                {
                    FrameMoveArea.SetActive(false);
                }
            }
        }
    }

    public void SetLevelBool()
    {
        frameMoveElapsedTime = 0f;
        bLevelClose = true;
    }

    public void SetCanActivateFrameMove(bool value)
    {
        FrameMoveArea.SetActive(true);
        canActivateFrameMove = true;
    }

    public void PrepareSmashSet(int index, bool value)
    {
        for (int i = 0; i < smashSets[index].transform.childCount; i++)
        {
            HandSmasher handSmasher = smashSets[index].transform.GetChild(i).GetComponent<HandSmasher>();
            if(handSmasher)
            {
                handSmasher.SetCanIndicateNextAttack(value);
            }
        }
    }

    public void SmashHands(int index)
    {
        for (int i = 0; i < smashSets[index].transform.childCount; i++)
        {
            HandSmasher handSmasher = smashSets[index].transform.GetChild(i).GetComponent<HandSmasher>();
            if (handSmasher)
            {
                handSmasher.SetCanSmashHand(true);
            }
        }
    }

    public void EnableMainLight(bool enabled)
    {
        MainLight.SetActive(enabled);
    }
}
