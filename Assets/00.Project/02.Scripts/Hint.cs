using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Hint : MonoBehaviour
{
    private GridManager _gm => GameManager.Instance.gridManager;
    private TileMatcher _matcher => GameManager.Instance.tileMatcher;

    public Tile hintTileA = null;
    public Tile hintTileB = null;

    public void ShowHint()
    {
        var grid = _gm.Grid;

        if (_matcher.TryFindFirstValidSwap(grid, out var tileA, out var tileB))
        {
            hintTileA = tileA;
             hintTileB = tileB;
            StartCoroutine(tileA.PlayHintAnimation());
            StartCoroutine(tileB.PlayHintAnimation());
        }
        else
        {
            Debug.Log("No matchable hint found.");
        }
    }




}
