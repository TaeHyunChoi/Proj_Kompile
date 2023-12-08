using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager
{
    private static UIManager instance;

    private static Canvas canvas_overlay;
    private static Canvas canvas_camera;

    private static int[] opened;
    private static int idxOpen;

    public UIManager(Transform root)
    {
        if (instance != null)
        {
            return;
        }
        instance = this;

        opened = new int[3];

        canvas_overlay = root.GetChild(0).GetComponent<Canvas>();
        canvas_camera  = root.GetChild(1).GetComponent<Canvas>();
    }

    public static async void Set(UIType type)
    {
        idxOpen = 0;
        switch (type)
        {
            case UIType.Title:
                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                AssetManager.Instantiate("UnitBase", canvas_overlay.transform);
                AssetManager.Instantiate("UIBattle", canvas_overlay.transform);
                AssetManager.Instantiate("UIBattle_MenuSlot", canvas_overlay.transform);

                await AssetManager.Wait();

                watch.Stop();
                Debug.Log("함수 실행 시간: " + watch.ElapsedMilliseconds + "ms");
                break;
        }
    }
    public void Open(UIType type, bool offPrev = true)
    {
        //시간 체크하고 싶다.
        switch (type)
        {
            case UIType.Title:
                Debug.Log("Open Title: ");
                break;
        }


        //this.code = code;
        //obj = AssetManager.Instantiate(code, canvas.transform);
        //id = obj.GetInstanceID();
    }
    public int Update(int input)
    {
        if ((input & IDx.ENTER) > 0)
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            Debug.Log($"Enter");
            Open(UIType.Title, false);

            stopwatch.Stop();
            Debug.Log("함수 실행 시간: " + stopwatch.ElapsedMilliseconds + "ms");
        }

        string debug = string.Empty;
        for (int i = 0; i < opened.Length; ++i)
        {
            debug += opened[i] + " / ";
        }
        Debug.Log(debug);
        return input;
    }
}