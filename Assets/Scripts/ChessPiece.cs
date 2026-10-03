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
        if(other.gameObject.tag == "enemy" && GameManager.instance.GetGameState() == GameState.Moving)
        {
            PawnScript ps = other.gameObject.GetComponent<PawnScript>();
            if(ps)
            {
                ps.SetCanKnockback();
            }
        }
    }
}
