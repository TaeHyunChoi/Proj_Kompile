using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager
{
    public class Level : IUpdateRoutine
    {
        private AsyncOperation loadAsync;
        private CanvasGroup curtain;
        private ContentType contentType;
        private MapData mapData;

        public Level(CanvasGroup curtain, ContentType type, MapData map)
        {
            this.curtain = curtain;
            contentType = type;
            mapData = map;
        }

        public int Update(int index)
        {
            switch (index)
            {
                case 0:
                    Main.GameMgr.SetMap(mapData);
                    Main.InputMgr.Set(null);
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
                    Main.Get.Dispose();
                    GC.Collect();
                    break;
                case 2:
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
                    Debug.Log("Need to dev: Main.Get.SetContent(type)");
                    //TODO: LevelManager.State 로 받아오는게 차라리 깔끔할 거 같은데?
                    break;
                case 5:
                    if (curtain.alpha > 0)
                    {
                        curtain.alpha -= Time.fixedDeltaTime;
                        return index;
                    }
                    break;
                case 6:
                    Main.Get.StartContent();
                    break;
                default:
                    return -1;
            }

            return index + 1;
        }
    }

    private CanvasGroup loadingCurtain;
    //enum SceneState 만들어야 하나?
    //LevelMgr이 맞는건가... SceneMgr로 넣어야 하나

    public void LoadSceneAsync(ContentType type, int code)
    {
        MapData map = DataTable.MapTable.Find(x => x.Code == code);
        Level level = new Level(loadingCurtain, type, map);
        CoroutineUpdater.SetHandler(new CoroutineLoad<Level>(level));

        //Main.GameMgr.SetMap(map);
        //Coroutiner.PlayCoroutine(IELoadSceneAsync(type, map.Code));
    }
    public LevelManager()
    {
        loadingCurtain = Main.UIMgr.GetOverlayCanvas().transform.GetChild(0).GetComponent<CanvasGroup>(); ;
        loadingCurtain.alpha = 0;
        loadingCurtain.gameObject.SetActive(false);
    }

    /*
        private IEnumerator IELoadSceneAsync(ContentType type, int mapCode)
    {
        Main.InputMgr.Set(null);

        loadingCurtain.alpha = 0;
        loadingCurtain.gameObject.SetActive(true);
        while (loadingCurtain.alpha < 1)
        {
            yield return loadingCurtain.alpha += Time.fixedDeltaTime * 0.75f;
        }

        Main.Get.Dispose();
        GC.Collect();

        string sceneName = string.Empty;
        int chapter = mapCode / 100;
        switch (chapter)
        {
            case 0: sceneName = "010_OpeningScene";     break;
            case 1: sceneName = "020_FieldTestScene";   break;
        }

        AsyncOperation loadAsync = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (!loadAsync.isDone)
        {
            yield return null;
        }
        
        yield return Main.Get.SetContent(type);

        while(loadingCurtain.alpha > 0)
        {
            yield return loadingCurtain.alpha -= Time.fixedDeltaTime;
        }
        loadingCurtain.gameObject.SetActive(false);

        Main.Get.StartContent();
    }
    //*/
}
