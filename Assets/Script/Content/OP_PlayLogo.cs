
namespace Script.Content
{
    using Script.Index;
    using UnityEngine;
    using UnityEngine.UI;

    public class OP_PlayLogo : ContentTaskBase
    {
        private readonly Image logoImage;
        private readonly float alphaDelta = 0.75f;
        private float waitTime;
        private float alpha;

        public OP_PlayLogo()
        {
            // 로고 프리팹 가져와서 쓰자. (비동기 로드?)

            index = 0;
            alpha = 0;
            waitTime = 0;
        }
        public override ContentTaskState MoveNext()
        {
            switch (index)
            {
                case 0:
                    alpha = Time.deltaTime * alphaDelta;
                    logoImage.color = new Color(1f, 1f, 1f, alpha);

                    if (1 <= alpha)
                    {
                        ++index;
                    }
                    break;

                case 1:
                    if (waitTime < 1f)
                    {
                        waitTime += Time.deltaTime;
                    }
                    else
                    {
                        ++index;
                    }
                    break;

                case 2:
                    alpha -= Time.deltaTime * (alphaDelta * 3);
                    logoImage.color = new Color(1f, 1f, 1f, alpha);

                    if (0 >= alpha)
                    {
                        ++index;
                    }
                    break;
                default:
                    return ContentTaskState.SUCCESS;
            }

            return ContentTaskState.RUNNING;
        }
    }
}

