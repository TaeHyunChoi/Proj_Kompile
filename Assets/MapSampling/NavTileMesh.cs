using System;
using UnityEngine;
using System.Threading.Tasks;
using Script.Util;
using UnityEngine.Serialization;

[Serializable]
public class NavTileMesh : MonoBehaviour
{
    [SerializeField] private ulong naviMask;
    [SerializeField] private uint  infoMask;
    
    [SerializeField] private Vector3 tilePivot;
    [SerializeField] private Vector3 gridPivot;
    
    public void SetData(int[] heights)
    {
        int i = 0;
        foreach (int height in heights)
        {
            ulong h;
            if (-1 == height)
            {
                h = 0b1111;
            }
            else
            {
                h = (ulong)height;
            }

            naviMask |= h << i;
            i += 4;
        }
    }

    public async Task  BakeMesh()
    {
        await Task.Yield();
        
        var rot = (transform.rotation.eulerAngles.y).ToInt();
        rot %= 360;
        if (0 != rot % 90)
        {
            Debug.LogError($"Wrong Rotate: {rot}");
            return;
        }

        naviMask = GetHeightMaskRotated(rot);
        // get rotate pivot
    }

    
    // Pivot
    private Vector3 GetPivotRotated(int rot)
    {
        Vector3 point = transform.position;

        switch (rot)
        {
            case 90:
                break;
            case 180:
                break;
            case 270:
                break;
            default:
                break;
        }

        return point;
    }

    
    // Height
    private ulong GetHeightMaskRotated (int rot)
    {
        ulong newMask = 0;
        
        var matrix = GetHeightMatrixRotated(rot);
        matrix = RotateMatrix(matrix, rot);

        ulong mask;
        for (int i = 0; i < 13; ++i)
        {
            switch (i)
            {
                case  0: mask = (ulong)matrix[0,4]; break;
                case  1: mask = (ulong)matrix[2,4]; break;
                case  2: mask = (ulong)matrix[4,4]; break;
                case  3: mask = (ulong)matrix[1,3]; break;
                case  4: mask = (ulong)matrix[3,3]; break;
                case  5: mask = (ulong)matrix[0,2]; break;
                case  6: mask = (ulong)matrix[2,2]; break;
                case  7: mask = (ulong)matrix[4,2]; break;
                case  8: mask = (ulong)matrix[1,1]; break;
                case  9: mask = (ulong)matrix[3,1]; break;
                case 10: mask = (ulong)matrix[0,0]; break;
                case 11: mask = (ulong)matrix[2,0]; break;
                case 12: mask = (ulong)matrix[4,0]; break;
                default: mask = (ulong)0;           break;
            }

            newMask |= mask << i * 4;
        }
        
        return newMask;
    }
    private int[,] GetHeightMatrixRotated(int rot)
    {
        var matrix = new int[5, 5];
        var flag = naviMask;
        for (var i = 0; i < 13; i++)
        {
            var h = (int)(flag & 0b1111);

            switch (i)
            {
                case  0: matrix[0,4] = h; break;
                case  1: matrix[2,4] = h; break;
                case  2: matrix[4,4] = h; break;
                case  3: matrix[1,3] = h; break;
                case  4: matrix[3,3] = h; break;
                case  5: matrix[0,2] = h; break;
                case  6: matrix[2,2] = h; break;
                case  7: matrix[4,2] = h; break;
                case  8: matrix[1,1] = h; break;
                case  9: matrix[3,1] = h; break;
                case 10: matrix[0,0] = h; break;
                case 11: matrix[2,0] = h; break;
                case 12: matrix[4,0] = h; break;
            }
            
            flag >>= 4;
        }

        return matrix;
    }
    private int[,] RotateMatrix(int[,] matrix, int rot)
    {
        if (0 == rot)
        {
            return matrix;
        }
        
        var n = matrix.GetLength(0); // 행렬 크기
        var rotated = new int[n, n];
        
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++)
            {
                switch (rot)
                {
                    case 270:
                        rotated[j, n - 1 - i] = matrix[i, j];
                        break;
                    case 180:
                        rotated[n - 1 - i, n - 1 - j] = matrix[i, j];
                        break;
                    case 90:
                        rotated[n - 1 - j, i] = matrix[i, j];
                        break;
                    default:
                        break;
                }
            }
        }

        return rotated;
    }
}
