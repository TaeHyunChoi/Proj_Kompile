using UnityEngine;
using Script.Interface;
using Script.Index;
using Script.Manager;

public class IngameCamera : MonoBehaviour, IIngameUpdater
{
    private readonly Vector3 OFFSET = new Vector3(0f, 3f, -2f);
    private readonly Quaternion ROTATION = Quaternion.Euler(50f, 0f, 0f);

    private Camera mainCam;
    private Transform target;

    private void Awake()
    {
        mainCam = transform.GetComponent<Camera>();
    }
    public void InitFollowingCamera(_IngameUnitBase player_character)
    {
        target = player_character.transform;

        transform.position = player_character.Position + OFFSET;
        transform.SetPositionAndRotation(OFFSET, ROTATION);

        mainCam.fieldOfView = 60f;

        // 여기에 LateUpdater 추가하면 되는 것이지요?
        IngameManager.AddUpdater(UpdaterType.LATE_UPDATE, this);
    }

    public IngameUpdateState UpdateState()
    {
        if (null == target)
        {
            return IngameUpdateState.FAILURE;
        }

        transform.position = target.transform.position + OFFSET;
        return IngameUpdateState.RUNNING;
    }
}
