using System.Threading.Tasks;
using UnityEngine;

public partial class Main // .SetContent
{
    //TODO: seperate Enter from "Init"
    public void Enter(GameState state)
    {
        switch (state)
        {
            case GameState.Opening:
                EnterOpening opening = new EnterOpening();
                CoroutineUpdater.Get.SetHandler(new CCoroutine<EnterOpening>(opening));
                break;
            case GameState.Field:
                EnterField field = new EnterField();
                CoroutineUpdater.Get.SetHandler(new CCoroutine<EnterField>(field));
                break;
        }

        this.state = state;
    }
    private class EnterOpening : IUpdateRoutine
    {
        Task<OnOpening> taskOpening;
        Task<UITitle>   taskTitle;

        public int Update(int index)
        {
            switch (index)
            {
                case 0:
                    Transform transformCameraCanvas = UIMgr.CameraCanvas.transform;
                    taskOpening = OnOpening.InitAsync(transformCameraCanvas);
                    taskTitle = AssetManager.CreateUIAsync<UITitle>("UITitle", transformCameraCanvas, false);
                    break;
                case 1:
                    if (false == taskOpening.IsCompletedSuccessfully
                        || false == taskTitle.IsCompletedSuccessfully)
                    {
                        return index;
                    }
                    taskOpening.Result.MoveNext();
                    //UIMgr.Set(UIType.Title, taskTitle.Result);
                    break;
                case 2:
                    taskOpening.Dispose();
                    taskTitle.Dispose();
                    break;
                default:
                    SceneMgr.SetState(SceneState.Play);
                    return -1;
            }

            return index + 1;
        }
    }
    private class EnterField : IUpdateRoutine
    {
        Task<bool> taskInitField;
        InField field;

        public int Update(int index)
        {
            switch (index)
            {
                case 0:
                    GameObject mapObj = GameObject.FindWithTag("Field");
                    field = new InField(mapObj);
                    taskInitField = field.InitMap();
                    break;
                case 1:
                    if (false == taskInitField.IsCompletedSuccessfully)
                    {
                        return index;
                    }
                    //GameMgr.Set(field);
                    break;
                case 2:
                    taskInitField.Dispose();
                    break;
                default:
                    SceneMgr.SetState(SceneState.Play);
                    return -1;
            }

            return index + 1;
        }
    }
}
