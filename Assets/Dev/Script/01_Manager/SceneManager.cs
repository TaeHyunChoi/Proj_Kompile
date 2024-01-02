using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager
{
    public void LoadSceneAsync(ContentType type, MapData mapData)
    {
        Coroutiner.PlayCoroutine(IELoadSceneAsync(type, mapData));
    }
    private IEnumerator IELoadSceneAsync(ContentType type, MapData map)
    {
        //로딩창 활성화

        //씬 호출
        string sceneName = string.Empty;
        int chapter = map.Code / 100;
        switch (chapter)
        {
            case 1: sceneName = "FieldScene"; break;
        }
        AsyncOperation loadAsync = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

        //+비동기 중에 실행할 작업
        Main.Instance.Dispose();
        GC.Collect();

        while (!loadAsync.isDone)
        {
            //로딩창 처리 (페이드 아웃.. 애니메이션...)
            yield return null;
        }

        //맵 처리
        Main.Instance.SetContent(type);

        //로딩창 해제 (fade-in)

    }
}
