using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public partial class Main // .SetContent
{
    public void SetContent(ContentType type)
    {
        switch (type)
        {
            case ContentType.Opening:
                LoadOpening opening = new LoadOpening();
                CoroutineUpdater.Get.SetHandler(new CCoroutine<LoadOpening>(opening));
                break;
            case ContentType.Field:
                LoadField field = new LoadField();
                CoroutineUpdater.Get.SetHandler(new CCoroutine<LoadField>(field));
                break;
        }
    }
    private class LoadOpening : IUpdateRoutine
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
                default:
                    SceneMgr.SetState(SceneState.Play);
                    taskOpening.Dispose();
                    taskTitle.Dispose();
                    return -1;
            }

            return index + 1;
        }
    }
    private class LoadField : IUpdateRoutine
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
                default:
                    SceneMgr.SetState(SceneState.Play);
                    taskInitField.Dispose();
                    return -1;
            }

            return index + 1;
        }
    }
}
