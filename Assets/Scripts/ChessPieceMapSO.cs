using System;
using UnityEngine;

[System.Serializable]
public struct PieceMap
{
    public ChessPieceType pieceType;
    public GameObject chessPiece;
    public Vector3 localScale;

    public PieceMap(ChessPieceType type, GameObject piece, Vector3 scale)
    {
        pieceType = type;
        chessPiece = piece;
        localScale = scale;
    }
}

[CreateAssetMenu(fileName = "ChessPieceMapSO", menuName = "SO/ChessPieceMapSO")]
public class ChessPieceMapSO : ScriptableObject
{
    [SerializeField] private PieceMap[] pieceMaps;
    
    public GameObject retrieveGameObjectOfType(int ct_index)
    {
        GameObject obj = null;

        for(int i = 0; i < pieceMaps.Length; i++)
        {
            if(pieceMaps[i].pieceType == (ChessPieceType)ct_index)
            {
                obj = pieceMaps[i].chessPiece;
                break;
            }
        }

        return obj;
    }
}
