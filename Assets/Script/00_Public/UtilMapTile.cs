using UnityEngine;

namespace  UtilMap
{
    public static class UtilMapTile
    {
        //grid shift
        private const byte ShiftGrid = 16;
        const byte ShiftGridXSign = 15;
        const byte ShiftGridX = 10;
        const byte ShiftGridYSign = 9;
        const byte ShiftGridY = 6;
        const byte ShiftGridZSign = 5;
        const byte ShiftGridZ = 0;
        
        // tile shift
        private const byte ShiftHalfScale = 15;
        private const byte ShiftTileX = 9;
        private const byte ShiftTileY = 6;
        private const byte ShiftTileZ = 0;
        
        
        public static Vector3 GetTilePivot(int indexFlag)
        {
            // grid pivot
            // _gridPivot = new Vector3(gridX * 32, gridY * 4, gridZ * 32).Truncate();
            var gridBits = indexFlag >> ShiftGrid;

            var gridXSign = (gridBits >> ShiftGridXSign) != 0 ? -1 : 1;
            var gridX = (gridBits >> ShiftGridX) & (0b_0001_1111);
            
            var gridYSign = (gridBits >> ShiftGridYSign) != 0 ? -1 : 1;
            var gridY = (gridBits >> ShiftGridY) & (0b_0111);
            
            var gridZSign = (gridBits >> ShiftGridZSign) != 0 ? -1 : 1;
            var gridZ = (gridBits >> ShiftGridZ) & (0b_0001_1111);
            
            var gridPivot = new Vector3(gridXSign * gridX * 32, gridYSign * gridY * 4, gridZSign * gridZ * 32);
            
            
            // tile relative coord
            var signX = ((indexFlag >> ShiftTileX) >> 5) == 1 ? -1 : 1;
            var x = (indexFlag >> ShiftTileX) & (0b_0001_1111);
                
            var signY = ((indexFlag >> ShiftTileY) >> 3) == 1 ? -1 : 1;
            var y = (indexFlag >> ShiftTileY) & (0b_0000_0111);

            var signZ = ((indexFlag >> ShiftTileZ) >> 5) == 1 ? -1 : 1;
            var z = (indexFlag >> ShiftTileZ) & (0b_0011_1111);
                
            // var scale = IsHalfScale() ? 0.5f : 1f;
            var scale = 1;

            //pivot = grid pivot + tile relative coord
            var tilePivot = new Vector3(signX * x, signY * y, signZ * z) * scale;
            return gridPivot + tilePivot;
        }
        
        
        
        private static short GetGridIndexFlag(int pointX, int pointY, int pointZ)
        {
            var gridFlag = 0;

            if (pointX < 0)
            {
                gridFlag |= 1 << ShiftGridXSign;
                gridFlag |= (-pointX) << ShiftGridX;
            }
            else
            {
                gridFlag |= pointX << ShiftGridX;
            }

            if (pointY < 0)
            {
                gridFlag |= 1 << ShiftGridYSign;
                gridFlag |= (-pointY) << ShiftGridY;
            }
            else
            {
                gridFlag |= pointY << ShiftGridY;
            }

            if (pointZ < 0)
            {
                gridFlag |= 1 << ShiftGridZSign;
                gridFlag |= (-pointZ) << ShiftGridZ;
            }
            else
            {
                gridFlag |= pointZ << ShiftGridZ;
            }

            return (short)gridFlag;
        }
    }    
}

