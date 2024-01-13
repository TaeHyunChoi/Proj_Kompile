using UnityEngine;
using System.Collections.Generic;


public class Unit : MonoBehaviour
{
    //아래 내용을 Player.cs로 넘기는 것을 고려하자...
    private int index;

    public void Move(Dictionary<int, int> voxel, Vector3 dir)
    {
        float delta = Public.MOVE_SPEED * Time.deltaTime;

        //check original input
        if (Parser.IsMovePossible(voxel, transform.position + dir * delta))
        {
            index = 0; //매번 호출하는게 아쉬움;
            transform.position += dir * delta;
            return;
        }

        //check neighbor voxels
        Vector3 forcedDir;
        while (true)
        {
            switch (index)
            {
                case  1: forcedDir = new Vector3(-1f,  0f,  0f);            break; //left
                case  2: forcedDir = new Vector3(-1f,  1f,  0f).normalized; break; //left up
                case  3: forcedDir = new Vector3(-1f, -1f,  0f).normalized; break; //left down

                case  4: forcedDir = new Vector3( 1f,  0f,  0f);            break; //right
                case  5: forcedDir = new Vector3( 1f,  1f,  0f).normalized; break; //right up
                case  6: forcedDir = new Vector3( 1f, -1f,  0f).normalized; break; //right down

                case  7: forcedDir = new Vector3( 0f,  0f, -1f);            break; //forward
                case  8: forcedDir = new Vector3( 0f,  1f, -1f).normalized; break; //forward up
                case  9: forcedDir = new Vector3( 0f, -1f, -1f).normalized; break; //forward down

                case 10: forcedDir = new Vector3( 0f,  0f,  1f);            break; //back
                case 11: forcedDir = new Vector3( 0f,  1f,  1f).normalized; break; //back up
                case 12: forcedDir = new Vector3( 0f, -1f,  1f).normalized; break; //back down

                default: index = (index < 13) ? index + 1 : 1;  continue; //무한루프가 돌면 문제가 있는거임
            }

            if(Parser.IsMovePossible(voxel, transform.position + forcedDir * delta))
            {
                break;
            }
            
            ++index;
        }
        
        transform.position += forcedDir * delta;
    }
}
