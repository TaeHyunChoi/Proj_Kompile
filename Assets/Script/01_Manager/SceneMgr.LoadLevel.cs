using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class SceneMgr // .LoadScene
{
    public class LoadOpeningScene : IUpdateRoutine
    {
        private AsyncOperation  loadAsync;
        private CanvasGroup     curtain;
        private Task<OnOpening> taskOpening;
        private Task   taskUI;

        public int MoveNext(int index)
        {
            switch (index)
            {
                case 0:
                    Main.SceneMgr.SetState(SceneState.Load);
                    curtain.gameObject.SetActive(true);
                    break;
                case 1:
                    loadAsync = SceneManager.LoadSceneAsync("010_OpeningScene", LoadSceneMode.Single);
                    break;
                case 2:
                    if (false == loadAsync.isDone)
                    {
                        return index;
                    }
                    break;
                case 3:
                    Transform transformCameraCanvas = Main.UIMgr.CameraCanvas.transform;
                    taskOpening = OnOpening.InitAsync(transformCameraCanvas);
                    taskUI = Main.UIMgr.InitUIAsync(GameState.Opening);
                    break;
                case 4:
                    if (false == taskOpening.IsCompletedSuccessfully)
                    {
                        return index;
                    }
                    if (false == taskUI.IsCompletedSuccessfully)
                    {
                        return index;
                    }

                    taskOpening.Result.Set();
                    curtain.gameObject.SetActive(false);
                    break;
                default:
                    taskOpening.Dispose();
                    Main.SceneMgr.SetState(SceneState.Play);
                    return -1;
            }

            return index + 1;
        }

        public LoadOpeningScene(CanvasGroup curtain)
        {
            this.curtain = curtain;
        }
    }
    public class LoadFieldScene : IUpdateRoutine
    {
        private AsyncOperation loadAsync;
        private CanvasGroup curtain;
        private MapData mapData;

        public int MoveNext(int index)
        {
            switch (index)
            {
                case 0:
                    Main.SceneMgr.state = SceneState.Load;
                    //Main.GameMgr.SetMap(mapData);
                    curtain.alpha = 0;
                    curtain.gameObject.SetActive(true);
                    break;
                case 1:
                    if (curtain.alpha < 1)
                    {
                        curtain.alpha += Time.fixedDeltaTime * 0.75f;
                        return index;
                    }
                    curtain.alpha = 1;
                    break;
                case 2:
                    string sceneName = string.Empty;
                    int chapter = mapData.Code / 100;
                    switch (chapter)
                    {
                        case 0: sceneName = "010_OpeningScene"; break;
                        case 1: sceneName = "020_FieldTestScene"; break;
                    }
                    loadAsync = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                    break;
                case 3:
                    if (false == loadAsync.isDone)
                    {
                        return index;
                    }
                    break;
                case 4:
                    Main.Instance.Enter(GameState.Field);
                    break;
                case 5:
                    if (SceneState.Play != Main.SceneMgr.state)
                    {
                        return index;
                    }
                    break;
                case 6:
                    if (curtain.alpha > 0)
                    {
                        curtain.alpha -= Time.fixedDeltaTime;
                        return index;
                    }
                    break;
                case 7:
                    //Main.Get.StartContent();
                    break;
                default:
                    return -1;
            }

            return index + 1;
        }

        public LoadFieldScene(CanvasGroup curtain, MapData map)
        {
            this.curtain = curtain;
            mapData = map;
        }
    }
}
