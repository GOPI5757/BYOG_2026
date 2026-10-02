using UnityEngine;

public class ChessBoardSpawner : MonoBehaviour
{
    [SerializeField] private GameObject BlockPrefab;
    [SerializeField] private Vector2 startPos;

    [SerializeField] private float decreaseYOffset_A, decreaseYOffset_B;

    [SerializeField] private Material BlockMat_A, BlockMat_B;

    private bool bIsBlockMatA = true;

    private string alphabets = "ABCDEFGH";

    void Start()
    {
        for(int i = 0; i < 8; i++)
        {
            for(int j = 0; j < 8; j++)
            {
                Vector3 spawnPos = transform.position + 
                    new Vector3(startPos.x + (j * 2), 1f, startPos.y + (i * 2));

                GameObject blockObj = Instantiate(BlockPrefab, transform);
                blockObj.transform.position = spawnPos;
                blockObj.GetComponent<Renderer>().material = bIsBlockMatA ? BlockMat_A : BlockMat_B;
                blockObj.transform.GetComponent<BlockScript>().initialMaterial = bIsBlockMatA ? BlockMat_A : BlockMat_B;
                blockObj.transform.GetComponent<BlockScript>().SetDecreaseOffset(bIsBlockMatA ? decreaseYOffset_A : decreaseYOffset_B);
                blockObj.transform.name = alphabets[j] + (i+1).ToString();
                bIsBlockMatA = !bIsBlockMatA;
            }

            bIsBlockMatA = !bIsBlockMatA;
        }
    }

    void Update()
    {
        
    }
}
