using System;
using UnityEngine;

[Serializable]
public class NavTileMesh : MonoBehaviour
{
    [SerializeField]
    private long heightFlag;

    public void Initialize(int[] heights)
    {
        var i = 0;
        foreach (long height in heights)
        {
            var h = height;
            if (-1 == height)
            {
                h = 0b1111;
            }

            heightFlag |= h << i;
            i += 4;
        }
        
        Debug.Log("Initialized: " + Convert.ToString((long)heightFlag,2));
    }
    
    

    // 회전 대응
    public void TestForRotate(int angle)
    {
        // pivot
        //pivot 계산도 함께 처리할까?
        
        // heights
        var matrix = GetHeightMatrix();
        int[,] rotated = RotateMatrix(matrix, angle);
        //이걸 받아서 다시 heightFlag에 저장한다.
    }

    // 확인 요망
    private int[,] GetHeightMatrix()
    {
        int[,] result = new int[5, 5];
        
        //여기서부터...

        return result;
    }

    private int GetHeight(int index)
    {
        var flag = heightFlag >> (index * 4);
        return (int)(flag & 0b1111);
    }
    
    private int[,] RotateMatrix(int[,] matrix, int angle)
    {
        var n = matrix.GetLength(0); // 행렬 크기
        var rotated = new int[n, n];

        angle %= 360;
        if (0 == angle)
        {
            return matrix;
        }

        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++)
            {
                switch (angle)
                {
                    case 90:
                        rotated[j, n - 1 - i] = matrix[i, j];
                        break;
                    case 180:
                        rotated[n - 1 - i, n - 1 - j] = matrix[i, j];
                        break;
                    case 270:
                        rotated[n - 1 - j, i] = matrix[i, j];
                        break;
                    // default:
                    //     rotated[i, j] = matrix[i, j];
                    //     break;
                        
                }
            }
        }

        return rotated;
    }
}
