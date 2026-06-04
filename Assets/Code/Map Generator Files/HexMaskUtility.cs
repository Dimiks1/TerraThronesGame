using System.Collections.Generic;
using UnityEngine;

public static class HexMaskUtility
{
    public static int BuildMaskFromDirections(List<int> directions)
    {
        int mask = 0;

        if (directions == null)
            return mask;

        for (int i = 0; i < directions.Count; i++)
        {
            int dir = directions[i];

            if (dir < 0 || dir >= 6)
                continue;

            mask |= 1 << dir;
        }

        return mask;
    }

    public static int RotateMask(int mask, int rotation)
    {
        rotation = NormalizeDirectionIndex(rotation);

        int result = 0;

        for (int dir = 0; dir < 6; dir++)
        {
            if ((mask & (1 << dir)) == 0)
                continue;

            int rotatedDir = (dir + rotation) % 6;
            result |= 1 << rotatedDir;
        }

        return result;
    }

    public static int CountBits(int mask)
    {
        int count = 0;

        while (mask != 0)
        {
            count += mask & 1;
            mask >>= 1;
        }

        return count;
    }

    public static int FirstSetBitIndex(int mask)
    {
        for (int i = 0; i < 6; i++)
        {
            if ((mask & (1 << i)) != 0)
                return i;
        }

        return -1;
    }
    //commit
    //Why this is not working

    public static int NormalizeDirectionIndex(int index)
    {
        index %= 6;

        if (index < 0)
            index += 6;

        return index;
    }

    public static int AngularDistance(int a, int b)
    {
        int difference = Mathf.Abs(a - b);
        return Mathf.Min(difference, 6 - difference);
    }

    public static int DirectionIndex(Vector2Int from, Vector2Int to)
    {
        Vector2Int delta = to - from;

        for (int i = 0; i < HexGridGenerator.hexDirs.Length; i++)
        {
            if (HexGridGenerator.hexDirs[i] == delta)
                return i;
        }

        return -1;
    }

    public static bool HasAdjacentBits(int mask)
    {
        for (int dir = 0; dir < 6; dir++)
        {
            int nextDir = (dir + 1) % 6;

            bool hasCurrent = (mask & (1 << dir)) != 0;
            bool hasNext = (mask & (1 << nextDir)) != 0;

            if (hasCurrent && hasNext)
                return true;
        }

        return false;
    }
}
