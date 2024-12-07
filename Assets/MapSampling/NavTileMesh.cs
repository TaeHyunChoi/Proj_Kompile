using System;
using UnityEngine;
using System.Threading.Tasks;
using Script.Util;

[Serializable]
public class NavTileMesh : MonoBehaviour
{
    [SerializeField]
    private ulong heightFlag;

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

            heightFlag |= h << i;
            i += 4;
        }
        
        Debug.Log("Set: " + Convert.ToString((long)heightFlag,2));
    }

    public async Task  BakeMesh()
    {
        await Task.Yield();
        
        var rotY = transform.rotation.eulerAngles.y;
        TestForRotate(rotY.ToInt());
    }



    // 회전 대응
    public void TestForRotate(int angle)
    {
        var matrix = GetHeightMatrix(angle);
        //이걸 받아서 다시 heightFlag에 저장한다.
        //
    }

    // 확인 요망
    private int[,] GetHeightMatrix(int angle)
    {
        var matrix = new int[5, 5];
        var flag = heightFlag;
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

        return RotateMatrix(matrix, angle);
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
