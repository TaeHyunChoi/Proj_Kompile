using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class SceneMgr // Coroutine
{
    public class LoadOpeningScene : IRoutineUpdater
    {
        private AsyncOperation  loadAsync;
        private CanvasGroup     curtain;
        private Task<OnOpening> taskOpening;
        private Task            taskUI;

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
                    Main.Instance.Release();
                    Transform transformCameraCanvas = Main.UIMgr.CameraCanvas.transform;
                    taskOpening = OnOpening.InitAsync(transformCameraCanvas);
                    taskUI      = Main.UIMgr.InitAsync(GameState.Opening);
                    break;
                case 4:
                    if (false == taskOpening.IsCompletedSuccessfully
                        || false == taskUI.IsCompletedSuccessfully)
                    {
                        return index;
                    }

                    taskOpening.Result.Set();
                    curtain.gameObject.SetActive(false);
                    break;
                default:
                    taskOpening.Dispose();
                    taskUI.Dispose();
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
    public class LoadFieldScene : IRoutineUpdater
    {
        private AsyncOperation loadAsync;
        private CanvasGroup curtain;
        private Task<OnField> taskField;
        private Task taskUI;
        private MapData mapData;

        public int MoveNext(int index)
        {
            switch (index)
            {
                case 0:
                    Main.SceneMgr.state = SceneState.Leave;
                    curtain.alpha = 0;
                    curtain.gameObject.SetActive(true);
                    break;
                case 1:
                    if (curtain.alpha < 1)
                    {
                        curtain.alpha += Time.fixedDeltaTime;
                        return index;
                    }
                    curtain.alpha = 1;
                    break;
                case 2:
                    //TODO: dev Mapdata (using grid?)
                    Main.SceneMgr.state = SceneState.Load;
                    string sceneName = string.Empty;
                    int chapter = mapData.Code / 100;
                    switch (chapter)
                    {
                        case 0: sceneName = "010_OpeningScene"; break;
                        case 1: sceneName = "020_FieldTestScene"; break;
                    }
                    loadAsync = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                    break;
                case 3:
                    if (false == loadAsync.isDone)
                    {
                        return index;
                    }
                    break;
                case 4:
                    Main.Instance.Release();
                    Transform level = GameObject.FindWithTag("Field").transform;
                    taskField = OnField.InitAsync(level, mapData);
                    taskUI    = Main.UIMgr.InitAsync(GameState.Field);
                    break;
                case 5:
                    if (false == taskField.IsCompletedSuccessfully
                        || false == taskUI.IsCompletedSuccessfully)
                    {
                        return index;
                    }
                    break;
                case 6:
                    if (curtain.alpha > 0)
                    {
                        curtain.alpha -= Time.fixedDeltaTime * 3;
                        return index;
                    }
                    curtain.gameObject.SetActive(false);
                    break;
                default:
                    taskField.Dispose();
                    taskUI.Dispose();
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
