using System.Collections;
using UnityEngine;

public class HandSmasher : MonoBehaviour
{
    [SerializeField] private Transform handRod;
    [SerializeField] private Transform handRod_handLoc;

    [SerializeField] private Transform HandTransform;

    [SerializeField] private float handSmashLerpTime;
    [SerializeField] private float handRodMaxScale;

    [SerializeField] private bool bCanSmashHand;
    [SerializeField] private bool bCanIndicateNextAttack;
    [SerializeField] private float indicateAttackDelay;

    [SerializeField] private Material hand_normalMaterial, hand_indicateMaterial;

    private float handRodCurrentScale;
    private float handRodInitialScale;
    private float handSmashElapsedTime;


    void Start()
    {
        handRodInitialScale = handRod.transform.localScale.x;
        handRodCurrentScale = handRodInitialScale;
    }

    void Update()
    {
        HandTransform.transform.position = handRod_handLoc.position;
        HandleSmashing();
    }

    private void HandleSmashing()
    {
        float t = handSmashElapsedTime / handSmashLerpTime;

        float lerp_b = bCanSmashHand ? handRodMaxScale : handRodInitialScale;
        float newLocalScale = Mathf.Lerp(handRodCurrentScale, lerp_b, t);

        handRod.transform.localScale = new Vector3(newLocalScale, 1f, 1f);
        handSmashElapsedTime += Time.deltaTime;
    }

    public void SetCanIndicateNextAttack(bool value)
    {
        bCanIndicateNextAttack = value;
        StartCoroutine(SetHandMat());
    }

    IEnumerator SetHandMat()
    {
        yield return new WaitForSeconds(bCanIndicateNextAttack ? indicateAttackDelay : 
            1 - indicateAttackDelay);
        HandTransform.GetComponent<Renderer>().material = bCanIndicateNextAttack ? 
            hand_indicateMaterial : hand_normalMaterial;
    }
}
