using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum closePanelState
{
    None,
    closing,
    waiting,
    opening,
}

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject creditsPage;
    [SerializeField] private Transform hoverBG;

    [SerializeField] private float hoverBGMoveSpeed;
    [SerializeField] private Transform playButtonTrans;

    [SerializeField] private GameObject[] creditsPageDisableUI;

    [SerializeField] private Image closingPanelImage;
    [SerializeField] private float closingPanelLerpTime;
    [SerializeField] private float closingPanelWaitTime;

    [SerializeField] private GameObject canvas;
    [SerializeField] private GameObject MainPanel;

    private closePanelState panelState;
    private float closingPanelElapsedTime;

    private Vector3 targetPos;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        DontDestroyOnLoad(canvas);
    }

    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;

        targetPos = new Vector3(hoverBG.transform.position.x, playButtonTrans.position.y,
            hoverBG.transform.position.z);
    }

    void Update()
    {
        UpdateHoverBG();
        ClosePanel();
    }

    private void ClosePanel()
    {
        if (panelState == closePanelState.None) return;
        float t = closingPanelElapsedTime / closingPanelLerpTime;
        t = Mathf.SmoothStep(0f, 1f, t);

        Color cp_color = closingPanelImage.color;
        if(panelState != closePanelState.waiting)
        {
            float alpha = Mathf.Lerp(panelState == closePanelState.closing ? 0f : 1f,
                panelState == closePanelState.closing ? 1f : 0f, t);

            cp_color.a = alpha;

            closingPanelImage.color = cp_color;
        }

        closingPanelElapsedTime += Time.deltaTime;

        if(t >= 1)
        {
            closingPanelElapsedTime = 0f;
            if(panelState == closePanelState.closing)
            {
                SceneManager.LoadScene(1);
                panelState = closePanelState.waiting;
            } else if (panelState == closePanelState.waiting)
            {
                panelState = closePanelState.opening;
                MainPanel.SetActive(false);
            } else
            {
                closingPanelImage.gameObject.SetActive(false);
            }
        }

    }

    private void UpdateHoverBG()
    {
        Vector3 newPos = Vector3.MoveTowards(hoverBG.transform.position, targetPos,
            hoverBGMoveSpeed * Time.deltaTime);

        hoverBG.transform.position = newPos;
    }

    public void QuitButtonClicked()
    {
        Application.Quit();
        SoundManager.instance.PlaySound(SoundManager.instance.clickSound);
    }

    public void CreditsButtonClicked()
    {
        creditsPage.SetActive(true);
        for(int i = 0; i < creditsPageDisableUI.Length; i++)
        {
            creditsPageDisableUI[i].SetActive(false);
        }
        SoundManager.instance.PlaySound(SoundManager.instance.clickSound);
    }

    public void BackButtonClicked()
    {
        creditsPage.SetActive(false);
        for (int i = 0; i < creditsPageDisableUI.Length; i++)
        {
            creditsPageDisableUI[i].SetActive(true);
        }
        SoundManager.instance.PlaySound(SoundManager.instance.clickSound);
    }

    public void PlayButtonClicked()
    {
        closingPanelImage.gameObject.SetActive(true);
        panelState = closePanelState.closing;
        closingPanelElapsedTime = 0f;
        SoundManager.instance.PlaySound(SoundManager.instance.clickSound);
    }

    public void HoverButton(Transform button_trans)
    {
        targetPos = new Vector3(hoverBG.transform.position.x, button_trans.position.y,
            hoverBG.transform.position.z);

        SoundManager.instance.PlaySound(SoundManager.instance.hoverSound);
    }

}
