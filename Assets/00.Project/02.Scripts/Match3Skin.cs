using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Mathematics.math;

[System.Serializable]
public struct Match
{
    public int2 coordinates;

    public int length;

    public bool isHorizontal;

    public Match(int x, int y, int length, bool isHorizontal)
    {
        coordinates.x = x;
        coordinates.y = y;
        this.length = length;
        this.isHorizontal = isHorizontal;
    }
}

public class Match3Skin : MonoBehaviour
{
    [SerializeField]
    Tile[] tilePrefabs;

    [SerializeField]
    Match3Game game;

    Grid2D<Tile> tiles;

    float2 tileOffset;

    [SerializeField, Range(0.1f, 1f)]
    float dragThreshold = 0.5f;

    [SerializeField]
    TileSwapper tileSwapper;


    [SerializeField, Range(0.1f, 20f)]
    float dropSpeed = 8f;

    [SerializeField, Range(0f, 10f)]
    float newDropOffset = 2f;
    float busyDuration;

    [SerializeField] Transform tileParent;
    public bool IsPlaying => true;

    public bool IsBusy => busyDuration > 0f;

    public void StartNewGame() {
        busyDuration = 0f;
        game.StartNewGame();
        tileOffset = -0.5f * (float2)(game.Size - 1);
        if (tiles.IsUndefined)
        {
            tiles = new(game.Size);
        }
        else
        {
            for (int y = 0; y < tiles.SizeY; y++)
            {
                for (int x = 0; x < tiles.SizeX; x++)
                {
                    tiles[x, y].Despawn();
                    tiles[x, y] = null;
                }
            }
        }
        for (int y = 0; y < tiles.SizeY; y++)
        {
            for (int x = 0; x < tiles.SizeX; x++)
            {
                tiles[x, y] = SpawnTile(game[x, y], x, y);
            }
        }
    }
    Tile SpawnTile(TileState t, float x, float y)
    {
        if (t == TileState.None)
            return null;

        Debug.Log(t);
        Tile tile = tilePrefabs[(int)t].Spawn(new Vector3(x + tileOffset.x, y + tileOffset.y));
        tile.transform.parent = tileParent;
        return tile;
    }


    public void DoWork()
    {
        if (busyDuration > 0f)
        {
            tileSwapper.Update();
            busyDuration -= Time.deltaTime;
            if (busyDuration > 0f)
            {
                return;
            }
        }

        if (game.HasMatches)
        {
            ProcessMatches();
        }
        else if (game.NeedsFilling)
        {
            DropTiles();
        }
    }
    void ProcessMatches()
    {
        game.ProcessMatches();

        for (int i = 0; i < game.ClearedTileCoordinates.Count; i++)
        {
            int2 c = game.ClearedTileCoordinates[i];
            busyDuration = Mathf.Max(tiles[c].Disappear(), busyDuration);
            tiles[c] = null;
        }
    }
    void DropTiles()
    {
        game.DropTiles();

        for (int i = 0; i < game.DroppedTiles.Count; i++)
        {
            TileDrop drop = game.DroppedTiles[i];
            Tile tile;
            if (drop.fromY < tiles.SizeY)
            {
                tile = tiles[drop.coordinates.x, drop.fromY];
            }
            else
            {
                tile = SpawnTile(
                    game[drop.coordinates], drop.coordinates.x, drop.fromY + newDropOffset
                );
            }
            tiles[drop.coordinates] = tile;
            busyDuration = Mathf.Max(
                tile.Fall(drop.coordinates.y + tileOffset.y, dropSpeed), busyDuration
            );
        }
    }
    public bool EvaluateDrag(Vector3 start, Vector3 end)
    {
        float2 a = ScreenToTileSpace(start), b = ScreenToTileSpace(end);
        float2 delta = b - a;

        MoveDirection direction = MoveDirection.None;

        if (length(delta) > dragThreshold)
        {
            float angle = atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            angle = (angle + 360f) % 360f; // 0~360도

            direction = angle switch
            {
                >= 337.5f or < 22.5f => MoveDirection.Right,
                >= 22.5f and < 67.5f => MoveDirection.UpRight,
                >= 67.5f and < 112.5f => MoveDirection.Up,
                >= 112.5f and < 157.5f => MoveDirection.UpLeft,
                >= 157.5f and < 202.5f => MoveDirection.Left,
                >= 202.5f and < 247.5f => MoveDirection.DownLeft,
                >= 247.5f and < 292.5f => MoveDirection.Down,
                >= 292.5f and < 337.5f => MoveDirection.DownRight,
                _ => MoveDirection.None
            };
        }

        var move = new Move((int2)floor(a), direction);

        if (
            move.IsValid &&
            tiles.AreValidCoordinates(move.From) &&
            tiles.AreValidCoordinates(move.To)
        )
        {
            DoMove(move);
            return false;
        }
        return true;
    }


    void DoMove(Move move)
    {
        bool success = game.TryMove(move);
        Tile a = tiles[move.From], b = tiles[move.To];
        busyDuration = tileSwapper.Swap(a, b, !success);
        if (success)
        {
            tiles[move.From] = b;
            tiles[move.To] = a;
        }
    }
    float2 ScreenToTileSpace(Vector3 screenPosition)
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
        return float2(Mathf.Floor(worldPos.x - tileOffset.x + 0.5f), Mathf.Floor(worldPos.y - tileOffset.y + 0.5f));
    }

}