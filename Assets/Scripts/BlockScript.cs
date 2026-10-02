using System.Collections;
using UnityEngine;

public enum BlockState
{
    Normal,
    Highlighted,
    Threat,
    Selected
}

public class BlockScript : MonoBehaviour
{
    [SerializeField] private Material highlightedMaterial;
    [SerializeField] private Material ThreatMaterial;
    [SerializeField] private Material SelectedMaterial;
    [SerializeField] private float materialLerpTime;

    [Header("Decrease Height")]
    [SerializeField] private float decreaseYOffset;
    [SerializeField] private float decreaseLerpTime;
    
    private float decreaseElapsedTime;
    private Vector3 initialPosition;
    private Vector3 currentPosition;
    private Vector3 decreaseHeightPosition;

    private bool bIsDecreasingY;

    private BlockState blockState;

    [HideInInspector] public Material initialMaterial;

    private void Start()
    {
        
    }

    private void Update()
    {
        DecreaseHeight();
    }

    void DecreaseHeight()
    {
        float t = decreaseElapsedTime / decreaseLerpTime;
        Vector3 newY_offset = Vector3.Lerp(currentPosition,
            bIsDecreasingY ? decreaseHeightPosition : initialPosition, t);

        transform.localPosition = newY_offset;
        decreaseElapsedTime += Time.deltaTime;
    }

    public void SetIsDecreasingY(bool value)
    {
        bIsDecreasingY = value;
        decreaseElapsedTime = 0f;
        currentPosition = transform.localPosition;
    }

    public void SetDecreaseOffset(float value)
    {
        decreaseYOffset = value;
        initialPosition = transform.localPosition;
        currentPosition = initialPosition;
        decreaseHeightPosition = transform.localPosition - new Vector3(0f, decreaseYOffset, 0f);
    }

    public void SetBlockState(BlockState state)
    {
        blockState = state;
        ChangeBlockState();
    }

    private void ChangeBlockState()
    {

        switch (blockState)
        {
            case BlockState.Normal:
                if(transform.GetChild(0).gameObject.activeInHierarchy)
                {
                    StartCoroutine(HideFrameBlock());
                    transform.GetChild(0).GetComponent<Animator>().SetBool("canHide", true);
                }

                GetComponent<Renderer>().material = initialMaterial;
                break;
            case BlockState.Highlighted:

                transform.GetChild(0).gameObject.SetActive(true);
                transform.GetChild(0).GetComponent<Animator>().SetBool("canHide", false);

                GetComponent<Renderer>().material = highlightedMaterial;
                break;
            case BlockState.Threat:

                transform.GetChild(0).gameObject.SetActive(true);
                transform.GetChild(0).GetComponent<Animator>().SetBool("canHide", false);

                GetComponent<Renderer>().material = ThreatMaterial;
                break;
            case BlockState.Selected:
                GetComponent<Renderer>().material = SelectedMaterial;
                break;
            default:
                break;
        }
    }

    public BlockState GetBlockState()
    {
        return blockState;
    }

    IEnumerator HideFrameBlock()
    {
        yield return new WaitForSeconds(1.5f);
        transform.GetChild(0).gameObject.SetActive(false);
    }
}
