using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using DataStruct;

public partial class SceneMgr // Coroutine
{
    public class LoadFieldScene : IRoutineUpdater
    {
        private AsyncOperation  mLoadAsyncOper;
        private CanvasGroup     mCurtainCanvas;
        private Task<OnField>   mTaskField;
        private Task            mTaskLoadUI;
        private MapData         mMapData;

        public int MoveNext(int index)
        {
            switch (index)
            {
                case 0:
                    mCurtainCanvas.alpha = 0;
                    mCurtainCanvas.gameObject.SetActive(true);
                    break;
                case 1:
                    if (mCurtainCanvas.alpha < 1)
                    {
                        mCurtainCanvas.alpha += Time.fixedDeltaTime;
                        return index;
                    }
                    mCurtainCanvas.alpha = 1;
                    break;
                case 2:
                    //TODO: dev Mapdata (using grid?)
                    string sceneName = string.Empty;
                    int chapter = mMapData.Code / 100;
                    switch (chapter)
                    {
                        case 0: sceneName = "010_OpeningScene"; break;
                        case 1: sceneName = "020_FieldTestScene"; break;
                    }
                    mLoadAsyncOper = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                    break;
                case 3:
                    if (false == mLoadAsyncOper.isDone)
                    {
                        return index;
                    }
                    break;
                case 4:
                    Main.Instance.Release();
                    Transform level = GameObject.FindWithTag("Field").transform;
                    mTaskField = OnField.InitAsync(level, mMapData);
                    mTaskLoadUI    = Main.UIMgr.InitAsync(EGameStateFlag.Field);
                    break;
                case 5:
                    if (false == mTaskField.IsCompletedSuccessfully
                        || false == mTaskLoadUI.IsCompletedSuccessfully)
                    {
                        return index;
                    }
                    break;
                case 6:
                    if (mCurtainCanvas.alpha > 0)
                    {
                        mCurtainCanvas.alpha -= Time.fixedDeltaTime * 3;
                        return index;
                    }
                    mCurtainCanvas.gameObject.SetActive(false);
                    break;
                default:
                    mTaskField.Dispose();
                    mTaskLoadUI.Dispose();
                    return -1;
            }

            return index + 1;
        }

        public LoadFieldScene(CanvasGroup curtain, MapData map)
        {
            mCurtainCanvas = curtain;
            mMapData = map;
        }
    }
}
