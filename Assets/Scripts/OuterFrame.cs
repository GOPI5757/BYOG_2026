using UnityEngine;
using UnityEngine.UI;

public class OuterFrame : MonoBehaviour
{
    [SerializeField] private GameObject FrameMoveArea;
    [SerializeField] private float frameStartY;
    [SerializeField] private float frameMoveLerpTime;
    [SerializeField] private GameObject[] smashSets;

    public ChessBoardSpawner chessBoardSpawner;

    private bool canActivateFrameMove;

    private float frameMoveElapsedTime;
    private Vector3 initialPos;
    private Vector3 targetPos;
    

    void Start()
    {
        FrameMoveArea.transform.localPosition = FrameMoveArea.transform.localPosition - new Vector3(0f, frameStartY, 0f);
        initialPos = FrameMoveArea.transform.localPosition;
        targetPos = Vector3.zero;
    }

    void Update()
    {
        if (GameManager.instance.GetGameState() != GameState.PreparingArena) return;
        if(canActivateFrameMove)
        {
            float t = frameMoveElapsedTime / frameMoveLerpTime;
            t = Mathf.SmoothStep(0f, 1f, t);
            Vector3 newPos = Vector3.Lerp(initialPos, targetPos, t);
            FrameMoveArea.transform.localPosition = newPos;

            frameMoveElapsedTime += Time.deltaTime;

            if(t >= 1)
            {
                GameManager.instance.SetGameState(GameState.ChoosingStrategy);
            }
        }
    }

    public void SetCanActivateFrameMove(bool value)
    {
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
}
