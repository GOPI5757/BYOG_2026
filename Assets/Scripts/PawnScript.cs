using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.Android;

public class PawnScript : MonoBehaviour
{
    [SerializeField] private bool bCanKnockback;
    [SerializeField] private float knockbackForce = 3f;
    [SerializeField] private float upwardForce = 1f;
    [SerializeField] private float destroyTime;

    public ChessBoardSpawner cb_spawner;
    private Vector3 hitDirection;

    private Rigidbody pawnRb;
    
    void Start()
    {
        pawnRb = GetComponent<Rigidbody>();
        pawnRb.useGravity = false;
        //pawnRb.freezeRotation = true;
        pawnRb.constraints = RigidbodyConstraints.FreezeAll;
    }

    void Update()
    {
        if(bCanKnockback)
        {
            if(cb_spawner != null)
            {
                cb_spawner.UpdateHasHitToPawn(gameObject);
            }
            pawnRb.useGravity = true;
            pawnRb.constraints = RigidbodyConstraints.None;
            Knockback();
            bCanKnockback = false;
        }
    }

    public void Knockback()
    {
        hitDirection.y = -0.5f;
        hitDirection.Normalize();

        Vector3 force =
            hitDirection * knockbackForce +
            Vector3.up * upwardForce;

        pawnRb.AddForce(force, ForceMode.Impulse);
    }

    public void SetCanKnockback(Vector3 directionHit)
    {
        bCanKnockback = true;
        hitDirection = directionHit;
        Destroy(gameObject, destroyTime);
    }
}
