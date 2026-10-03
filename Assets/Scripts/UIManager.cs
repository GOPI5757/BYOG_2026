using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Transform[] slots;
    [SerializeField] private Transform[] slotDragButtons;
    [SerializeField] private List<Vector3> slotDragInitialPositions;
    [SerializeField] private Sprite pb_activeSprite, pb_inactiveSprite;

    [SerializeField] private GameObject bottomPanel;

    [SerializeField] private Transform PlayButton;

    private Image pb_image;
    private Button pb_button;

    [SerializeField] private Sprite emptySlotSprite, hoverSlotSprite;

    [SerializeField] private float slotFragMoveSpeed;

    [SerializeField] private List<int> smashOrder;

    private Transform currentActiveDrag;
    private int currentHoverSlotIndex;

    private Vector3 positionDifference;

    void Start()
    {
        for(int i = 0;  i < slotDragButtons.Length; i++)
        {
            slotDragInitialPositions.Add(slotDragButtons[i].position);
        }

        for(int i = 0; i < 4; i++)
        {
            smashOrder.Add(-1);
        }

        pb_image = PlayButton.GetComponent<Image>();
        pb_button = PlayButton.GetComponent<Button>();
    }

    void Update()
    {
        UpdateCurrentActiveDrag();
        UpdateDragToInitial();
        CheckOverlaps();
    }

    private void CheckOverlaps()
    {
        if(currentActiveDrag)
        {
            bool flag = false;
            for(int i = 0; i < slots.Length; i++)
            {
                slots[i].GetComponent<Image>().sprite = emptySlotSprite;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                RectTransform cad_rt = currentActiveDrag.GetComponent<RectTransform>();
                RectTransform slot_rt = slots[i].GetComponent<RectTransform>();
                if(AreUIElementsOverlapping(cad_rt, slot_rt))
                {
                    currentHoverSlotIndex = i;
                    flag = true;
                    break;
                }
            }

            if(!flag)
            {
                currentHoverSlotIndex = -1;
            }
        }
    }

    bool AreUIElementsOverlapping(RectTransform a, RectTransform b)
    {
        Rect rectA = GetWorldRect(a);
        Rect rectB = GetWorldRect(b);

        return rectA.Overlaps(rectB);
    }

    Rect GetWorldRect(RectTransform rectTransform)
    {
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        Vector2 min = corners[0];
        Vector2 max = corners[2];

        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private void UpdateCurrentActiveDrag()
    {
        if (currentActiveDrag != null)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            currentActiveDrag.transform.position = mousePosition + new Vector2(positionDifference.x, positionDifference.y);
        }
    }

    private void UpdateDragToInitial()
    {
        for(int i = 0; i < slotDragButtons.Length; i++)
        {
            if (slotDragButtons[i])
            {
                if (slotDragButtons[i] != currentActiveDrag)
                {
                    int slotIndex = GetIndexFromSmashOrder(FindSlotDragIndex(slotDragButtons[i]));
                    Vector3 final = slotIndex == -1 ? slotDragInitialPositions[i] : slots[slotIndex].transform.position;

                    Vector3 newPos = Vector3.MoveTowards(slotDragButtons[i].transform.position,
                        final, 
                        Time.deltaTime * slotFragMoveSpeed);

                    slotDragButtons[i].transform.position = newPos;
                }
            }
        }
    }

    private int GetIndexFromSmashOrder(int slotIndex)
    {
        int index = -1;
        for(int i = 0; i < smashOrder.Count; i++)
        {
            if(smashOrder[i] == slotIndex)
            {
                index = i;
                break;
            }
        }
        return index;
    }

    public void PointerDown(Transform obj_trans)
    {
        currentActiveDrag = obj_trans;
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        positionDifference =  new Vector2(currentActiveDrag.position.x,
            currentActiveDrag.position.y) - mousePosition;

        currentActiveDrag.SetAsLastSibling();
    }

    public void PointerUp(Transform obj_trans)
    {
        if(currentHoverSlotIndex != -1)
        {
            int cad_slotIndex = GetIndexFromSmashOrder(FindSlotDragIndex(currentActiveDrag));
            if (smashOrder[currentHoverSlotIndex] != -1 && cad_slotIndex != -1)
            {
                int temp = smashOrder[currentHoverSlotIndex];
                smashOrder[currentHoverSlotIndex] = FindSlotDragIndex(currentActiveDrag);
                smashOrder[cad_slotIndex] = temp;
            } else
            {
                smashOrder[currentHoverSlotIndex] = FindSlotDragIndex(currentActiveDrag);
            }

            bool flag = false;
            for(int i = 0; i < smashOrder.Count; i++)
            {
                if (smashOrder[i] == -1)
                {
                    flag = true;
                    break;
                }
            }

            if(!flag)
            {
                pb_button.enabled = true;
                pb_image.sprite = pb_activeSprite;
            }
        }
        currentActiveDrag = null;
    }

    private int FindSlotDragIndex(Transform dragTrans)
    {
        int index = -1;
        for(int i = 0; i < slotDragButtons.Length; i++)
        {
            if (slotDragButtons[i] == dragTrans)
            {
                index = i; 
                break;
            }
        }

        return index;
    }

    public void ResetButtonClicked()
    {
        for(int i = 0; i < smashOrder.Count; i++)
        {
            smashOrder[i] = -1;
        }

        for(int i = 0; i < slots.Length; i++)
        {
            slots[i].GetComponent<Image>().sprite = emptySlotSprite;
        }

        for(int i = 0; i < slotDragButtons.Length; i++)
        {
            slotDragButtons[i].transform.position = slotDragInitialPositions[i];
        }

        pb_button.enabled = false;
        pb_image.sprite = pb_inactiveSprite;
    }

    public void PlayButtonClicked()
    {
        bottomPanel.GetComponent<Animator>().SetBool("canSlideDown", true);
        GameManager.instance.activeSmashOrder = smashOrder;
        StartCoroutine(DisableBottomPanel());   
    }

    IEnumerator DisableBottomPanel()
    {
        yield return new WaitForSeconds(1f);
        bottomPanel.SetActive(false);
        GameManager.instance.SetGameState(GameState.PlayerScalingUp);
    }
}
