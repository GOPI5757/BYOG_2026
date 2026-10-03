using System.Collections;
using UnityEngine;

public enum smashState{
    back,
    Smash,
    wait,
    Return
}

public class HandSmasher : MonoBehaviour
{
    [SerializeField] private Transform handRod;
    [SerializeField] private Transform handRod_handLoc;

    [SerializeField] private Transform HandTransform;

    [SerializeField] private float handSmashLerpTime;
    [SerializeField] private float handBackLerpTime;
    [SerializeField] private float handWaitLerpTime;
    [SerializeField] private float handReturnLerpTime;
    [SerializeField] private OuterFrame outerFrame;

    [SerializeField] private float handRodMaxScale;
    [SerializeField] private float handRodBackScale;

    [SerializeField] private ChessGridName startGridName;
    [SerializeField] private ChessGridName endGridName;

    private float hr_maxScaleDynamic;

    private float[] handSmashScaleValues;

    [SerializeField] private bool bCanSmashHand;

    [SerializeField] private bool bCanIndicateNextAttack;
    [SerializeField] private float indicateAttackDelay;

    [SerializeField] private Material hand_normalMaterial, hand_indicateMaterial;

    private smashState state;
    private float handRodInitialScale;
    private float handRodCurrentScale;

    private float handSmashElapsedTime;

    private float finalLerpTime;
    private float finalLerpB;

    void Start()
    {
        handRodInitialScale = handRod.transform.localScale.x;
        handRodCurrentScale = handRodInitialScale;

        handSmashScaleValues = new float[]
        {
            5.6f,
            8.4f,
            11f,
            13.5f,
            16.3f,
            19f,
            21.6f,
            24.2f
        };
    }

    void Update()
    {
        HandTransform.transform.position = handRod_handLoc.position;
        HandleSmashing();
    }

    private void HandleSmashing()
    {
        if (!bCanSmashHand) return;
        switch(state)
        {
            case smashState.back:
                finalLerpTime = handBackLerpTime;
                finalLerpB = handRodBackScale;
                break;
            case smashState.Smash:
                finalLerpTime = handSmashLerpTime;
                finalLerpB = hr_maxScaleDynamic;
                break;
            case smashState.wait:
                finalLerpTime = handWaitLerpTime;
                break;
            case smashState.Return:
                finalLerpTime = handReturnLerpTime;
                finalLerpB = handRodInitialScale;
                break;
            default:
                break;
        }

        float t = handSmashElapsedTime / finalLerpTime;
        t = Mathf.SmoothStep(0f, 1f, t);

        if(state != smashState.wait)
        {
            float newLocalScale = Mathf.Lerp(handRodCurrentScale, finalLerpB, t);

            handRod.transform.localScale = new Vector3(newLocalScale, 1f, 1f);
        }
        handSmashElapsedTime += Time.deltaTime;

        if(t >= 1)
        {
            handSmashElapsedTime = 0f;
            handRodCurrentScale = handRod.transform.localScale.x;
            if (state == smashState.back)
            {
                state = smashState.Smash;
            } else if(state == smashState.Smash)
            {
                state = smashState.wait;
            }  else if(state == smashState.wait)
            {
                state = smashState.Return;
            }
            else if (state == smashState.Return)
            {
                bCanSmashHand = false;
                StartCoroutine(SetSmashOver());
            }
        }
    }

    IEnumerator SetSmashOver()
    {
        yield return new WaitForSeconds(GameManager.instance.smashOverDelay);
        if(GameManager.instance.GetGameState() != GameState.BlocksBackToPosition)
        {
            GameManager.instance.SetGameState(GameState.BlocksBackToPosition);
        }
    }

    public void SetCanSmashHand(bool value)
    {
        bCanSmashHand = value;
        state = smashState.back;
        FindMaxScale();
    }

    private void FindMaxScale()
    {
        string alph = "ABCDEFGH";
        int scaleIndex = -1;

        int i_startValue =
            startGridName.alph_type == endGridName.alph_type
            ? startGridName.number
            : alph.IndexOf(startGridName.alph_type.ToString()) + 1;

        int step = i_startValue == 1 ? 1 : -1;

        // Always 1 -> 8 or 8 -> 1
        int start = step > 0 ? 1 : 8;

        for (int i = start;
             step > 0 ? i <= 8 : i >= 1;
             i += step)
        {

            string alph_value =
                startGridName.alph_type == endGridName.alph_type
                ? startGridName.alph_type.ToString()
                : alph[i - 1].ToString();

            string number_value =
                startGridName.number == endGridName.number
                ? startGridName.number.ToString()
                : i.ToString();

            string finalValue = alph_value + number_value;

            if (outerFrame && outerFrame.chessBoardSpawner)
            {
                GameObject chessBlock =
                    outerFrame.chessBoardSpawner.RetrieveGameObjectOfName(finalValue);

                if (chessBlock)
                {
                    BlockScript b_script = chessBlock.GetComponent<BlockScript>();

                    if (b_script)
                    {
                        GameObject player = GameObject.FindGameObjectWithTag("Player");

                        if (player && player == b_script.GetOccupiedObj())
                        {
                            scaleIndex = step > 0
                                ? i - 1
                                : 8 - i;

                            break;
                        }
                    }
                }
            }
        }

        if (scaleIndex == -1)
        {
            hr_maxScaleDynamic = handRodMaxScale;
        }
        else
        {
            hr_maxScaleDynamic = handSmashScaleValues[scaleIndex];
        }
    }

    public void SetCanIndicateNextAttack(bool value)
    {
        bCanIndicateNextAttack = value;
        StartCoroutine(SetHandMat());
    }

    IEnumerator SetHandMat()
    {
        yield return new WaitForSeconds(bCanIndicateNextAttack ? indicateAttackDelay : 
            0.5f - indicateAttackDelay);
        Renderer handRenderer = HandTransform.GetComponent<Renderer>();
        Material[] mats = handRenderer.materials;
        mats[1] = bCanIndicateNextAttack ? hand_indicateMaterial : hand_normalMaterial;

        handRenderer.materials = mats;
    }
}
