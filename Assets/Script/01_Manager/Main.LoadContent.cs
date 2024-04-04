using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public partial class Main // .SetContent
{
    public void Init(GameState state)
    {
        this.state = state;
        mgrScene.LoadSceneAsync(-1);
    }
    public void EnterState()
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
                    Transform cameraCanvasTransform = UIMgr.GetCameraCanvas().transform;
                    taskOpening = OnOpening.InitAsync(cameraCanvasTransform);
                    taskTitle = AssetManager.CreateUIAsync<UITitle>("UITitle", cameraCanvasTransform, false);
                    break;
                case 1:
                    if (false == taskOpening.IsCompletedSuccessfully
                        || false == taskTitle.IsCompletedSuccessfully)
                    {
                        return index;
                    }
                    GameMgr.SetSequence(taskOpening.Result);
                    UIMgr.SetBucket((int)UIType.Title, taskTitle.Result);
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
                    GameMgr.SetSequence(field);
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
