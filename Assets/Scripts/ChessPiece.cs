using UnityEngine;

public class ChessPiece : MonoBehaviour
{
    void Start()
    {
        
    }

    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (GameManager.instance.GetGameState() != GameState.Moving) return;
        if(other.gameObject.tag == "enemy")
        {
            PawnScript ps = other.gameObject.GetComponent<PawnScript>();
            if(ps)
            {
                print("ps.currentBS.name.ToLower() : " + ps.currentBS.name.ToLower());
                print("GameManager.instance.destinationGridName : " + GameManager.instance.destinationGridName);
                if (ps.currentBS.name.ToLower() == GameManager.instance.destinationGridName.ToLower())
                {
                    Vector3 direction = ps.transform.position - transform.position;
                    ps.SetCanKnockback(direction);
                }
            }
            SoundManager.instance.PlaySound(SoundManager.instance.PawnTakeDownSound);
        }

        if(other.gameObject.tag == "king")
        {
            SoundManager.instance.PlaySound(SoundManager.instance.KingCaptureSound);
            PawnScript ps = other.gameObject.GetComponent<PawnScript>();
            if (ps)
            {
                if (ps.currentBS.name.ToLower() == GameManager.instance.destinationGridName.ToLower())
                {
                    Vector3 direction = ps.transform.position - transform.position;
                    ps.SetCanKnockback(direction);
                }
            }
        }
    }
}
