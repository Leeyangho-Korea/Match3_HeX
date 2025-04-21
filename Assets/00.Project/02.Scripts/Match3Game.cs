using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

using Random = UnityEngine.Random;

using static Unity.Mathematics.math;

[System.Serializable]
public struct TileDrop
{
    public int2 coordinates;

    public int fromY;

    public TileDrop(int x, int y, int distance)
    {
        coordinates.x = x;
        coordinates.y = y;
        fromY = y + distance;
    }
}

public class Match3Game : MonoBehaviour
{
    [SerializeField]
    int2 size = 8;

    [SerializeField, HideInInspector]
    Grid2D<TileState> grid;

    public TileState this[int x, int y] => grid[x, y];

    public TileState this[int2 c] => grid[c];

    public int2 Size => size;
    List<Match> matches;
    public List<TileDrop> DroppedTiles
    { get; private set; }
    public List<int2> ClearedTileCoordinates
    { get; private set; }

    public bool NeedsFilling
    { get; private set; }
    public bool HasMatches => matches.Count > 0;
    public void StartNewGame()
    {
        if (grid.IsUndefined)
        {
            grid = new(size);
            matches = new();
            ClearedTileCoordinates = new();
            DroppedTiles = new();
        }
        FillGrid();
    }
    public void DropTiles()
    {
        DroppedTiles.Clear();

        for (int x = 0; x < size.x; x++)
        {
            int holeCount = 0;
            for (int y = 0; y < size.y; y++)
            {
                if (grid[x, y] == TileState.None)
                {
                    holeCount += 1;
                }
                else if (holeCount > 0)
                {
                    grid[x, y - holeCount] = grid[x, y];
                    DroppedTiles.Add(new TileDrop(x, y - holeCount, holeCount));
                }
            }
            for (int h = 1; h <= holeCount; h++)
            {
                grid[x, size.y - h] = (TileState)Random.Range(0, 6);
                DroppedTiles.Add(new TileDrop(x, size.y - h, holeCount));
            }
        }

        NeedsFilling = false;
        FindMatches();
    }
    bool FindMatches()
    {
        matches.Clear();
        HashSet<int2> matchedCoords = new();

        // 기존 가로 매칭
        for (int y = 0; y < size.y; y++)
        {
            TileState start = grid[0, y];
            int length = 1;
            for (int x = 1; x < size.x; x++)
            {
                TileState t = grid[x, y];
                if (t == start)
                {
                    length += 1;
                }
                else
                {
                    if (length >= 3 && start != TileState.None)
                    {
                        for (int i = 0; i < length; i++)
                            matchedCoords.Add(new int2(x - i - 1, y));
                    }
                    start = t;
                    length = 1;
                }
            }
            if (length >= 3 && start != TileState.None)
            {
                for (int i = 0; i < length; i++)
                    matchedCoords.Add(new int2(size.x - i - 1, y));
            }
        }

        // 기존 세로 매칭
        for (int x = 0; x < size.x; x++)
        {
            TileState start = grid[x, 0];
            int length = 1;
            for (int y = 1; y < size.y; y++)
            {
                TileState t = grid[x, y];
                if (t == start)
                {
                    length += 1;
                }
                else
                {
                    if (length >= 3 && start != TileState.None)
                    {
                        for (int i = 0; i < length; i++)
                            matchedCoords.Add(new int2(x, y - i - 1));
                    }
                    start = t;
                    length = 1;
                }
            }
            if (length >= 3 && start != TileState.None)
            {
                for (int i = 0; i < length; i++)
                    matchedCoords.Add(new int2(x, size.y - i - 1));
            }
        }

        // 추가: 2x2 네모 매칭
        for (int y = 0; y < size.y - 1; y++)
        {
            for (int x = 0; x < size.x - 1; x++)
            {
                TileState t = grid[x, y];
                if (t == TileState.None)
                    continue;

                if (grid[x + 1, y] == t &&
                    grid[x, y + 1] == t &&
                    grid[x + 1, y + 1] == t)
                {
                    // 2x2 네모의 4칸 추가
                    matchedCoords.Add(new int2(x, y));
                    matchedCoords.Add(new int2(x + 1, y));
                    matchedCoords.Add(new int2(x, y + 1));
                    matchedCoords.Add(new int2(x + 1, y + 1));

                    // + 주변 동일한 색도 추가
                    int2[] neighbors = {
                    new int2(x - 1, y), new int2(x + 2, y),
                    new int2(x, y - 1), new int2(x, y + 2),
                    new int2(x + 1, y - 1), new int2(x + 1, y + 2),
                    new int2(x - 1, y + 1), new int2(x + 2, y + 1)
                };

                    foreach (var n in neighbors)
                    {
                        if (n.x >= 0 && n.x < size.x && n.y >= 0 && n.y < size.y)
                        {
                            if (grid[n] == t)
                                matchedCoords.Add(n);
                        }
                    }
                }
            }
        }

        // 결과를 Match 객체로 변환
        foreach (var c in matchedCoords)
        {
            matches.Add(new Match(c.x, c.y, 1, true)); // 길이는 1이지만 좌표 기록용
        }

        return matches.Count > 0;
    }


    void FillGrid()
    {
        for (int y = 0; y < size.y; y++)
        {
            for (int x = 0; x < size.x; x++)
            {
                TileState a = TileState.None, b = TileState.None;
                int potentialMatchCount = 0;
                if (x > 1)
                {
                    a = grid[x - 1, y];
                    if (a == grid[x - 2, y])
                    {
                        potentialMatchCount = 1;
                    }
                }
                if (y > 1)
                {
                    b = grid[x, y - 1];
                    if (b == grid[x, y - 2])
                    {
                        potentialMatchCount += 1;
                        if (potentialMatchCount == 1)
                        {
                            a = b;
                        }
                        else if (b < a)
                        {
                            (a, b) = (b, a);
                        }
                    }
                }

                TileState t = (TileState)Random.Range(0, 6 - potentialMatchCount);
                if (potentialMatchCount > 0 && t >= a)
                {
                    t += 1;
                }
                if (potentialMatchCount == 2 && t >= b)
                {
                    t += 1;
                }
                grid[x, y] = t;
            }
        }
    }
    public bool TryMove(Move move)
    {
        grid.Swap(move.From, move.To);
        if (FindMatches())
        {
            return true;
        }
        grid.Swap(move.From, move.To);
        return false;
    }

    public void ProcessMatches()
    {
        ClearedTileCoordinates.Clear();

        for (int m = 0; m < matches.Count; m++)
        {
            Match match = matches[m];
            int2 step = match.isHorizontal ? int2(1, 0) : int2(0, 1);
            int2 c = match.coordinates;
            for (int i = 0; i < match.length; c += step, i++)
            {
                if (grid[c] != TileState.None)
                {
                    grid[c] = TileState.None;
                    ClearedTileCoordinates.Add(c);
                }
            }
        }

        matches.Clear();
        NeedsFilling = true;
    }
}
