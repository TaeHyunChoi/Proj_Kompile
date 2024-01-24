using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager
{
    private CanvasGroup loadingCurtain;
    public LevelManager(CanvasGroup curtain)
    {
        loadingCurtain = curtain;
        loadingCurtain.alpha = 0;
        loadingCurtain.gameObject.SetActive(false);
    }
    public void LoadOpeningSceneAsync()
    {
        Coroutiner.PlayCoroutine(IELoadSceneAsync(ContentType.Opening, 0));
    }    
    public void LoadSceneAsync(ContentType type, MapData data)
    {
        Main.GameMgr.SetMap(data);
        Coroutiner.PlayCoroutine(IELoadSceneAsync(type, data.Code));
    }
    private IEnumerator IELoadSceneAsync(ContentType type, int mapCode)
    {
        Main.InputMgr.Set(null);

        loadingCurtain.alpha = 0;
        loadingCurtain.gameObject.SetActive(true);
        while (loadingCurtain.alpha < 1)
        {
            yield return loadingCurtain.alpha += Time.fixedDeltaTime * 0.75f;
        }

        Main.Instance.Dispose();
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
        
        yield return Main.Instance.SetContent(type);

        while(loadingCurtain.alpha > 0)
        {
            yield return loadingCurtain.alpha -= Time.fixedDeltaTime;
        }
        loadingCurtain.gameObject.SetActive(false);

        Main.Instance.StartContent();
    }
}
