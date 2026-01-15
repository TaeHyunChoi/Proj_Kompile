namespace Script.Data
{
    using UnityEngine;

    [System.Serializable]
    public class PlayData
    {
        private Vector3 position;

        public Vector3 Position => position;

        public PlayData()
        {
            position = new Vector3(1.5f, -1f, 1.5f);
        }
    }
}