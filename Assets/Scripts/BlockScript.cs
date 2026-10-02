using System.Collections;
using UnityEngine;

public enum BlockState
{
    Normal,
    Highlighted,
    Threat
}

public class BlockScript : MonoBehaviour
{
    [SerializeField] private Material highlightedMaterial;
    [SerializeField] private Material ThreatMaterial;
    [SerializeField] private float materialLerpTime;

    private float materialElapsedTime;

    private Material initialMaterialLerp;
    private Material targetMaterialLerp;

    private Material materialComp;

    private BlockState blockState;

    [HideInInspector] public Material initialMaterial;

    private void Start()
    {
        materialComp = GetComponent<Renderer>().material;

        initialMaterialLerp = initialMaterial;
        targetMaterialLerp = initialMaterial;
    }

    private void Update()
    {
        float t = materialElapsedTime / materialLerpTime;
        materialComp.Lerp(initialMaterialLerp, targetMaterialLerp, t);

        materialElapsedTime += Time.deltaTime;
    }

    public void SetBlockState(BlockState state)
    {
        blockState = state;
        ChangeBlockState();
    }

    private void ChangeBlockState()
    {
        materialElapsedTime = 0f;

        initialMaterialLerp = GetComponent<Renderer>().material;
        switch (blockState)
        {
            case BlockState.Normal:
                targetMaterialLerp = initialMaterial;
                if(transform.GetChild(0).gameObject.activeInHierarchy)
                {
                    StartCoroutine(HideFrameBlock());
                    transform.GetChild(0).GetComponent<Animator>().SetBool("canHide", true);
                }
                break;
            case BlockState.Highlighted:
                targetMaterialLerp = highlightedMaterial;
                transform.GetChild(0).gameObject.SetActive(true);
                transform.GetChild(0).GetComponent<Animator>().SetBool("canHide", false);
                break;
            case BlockState.Threat:
                targetMaterialLerp = ThreatMaterial;
                transform.GetChild(0).gameObject.SetActive(true);
                transform.GetChild(0).GetComponent<Animator>().SetBool("canHide", false);
                break;
            default:
                break;
        }
    }

    IEnumerator HideFrameBlock()
    {
        yield return new WaitForSeconds(1.5f);
        transform.GetChild(0).gameObject.SetActive(false);
    }
}
