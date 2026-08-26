namespace Kompile.Data
{
    using UnityEngine;

    public readonly struct InputState
    {
        private readonly IDxInput current;
        private readonly IDxInput previous;

        public InputState(IDxInput current, IDxInput previous)
        {
            this.current = current;
            this.previous = previous;
        }

        public bool IsDown(IDxInput input)
        {
            return (current & input) != 0
                && (previous & input) == 0;
        }
        public bool IsPressing(IDxInput input)
        {
            return (current & input) != 0;
        }
        public bool IsUp(IDxInput input)
        {
            return (current & input) == 0 && (previous & input) != 0;
        }

        public Vector2 Dir
        {
            get
            {
                float x = 0f, z = 0f;
                if (IsPressing(IDxInput.RIGHT)) { x += 1f; }
                if (IsPressing(IDxInput.LEFT)) { x -= 1f; }
                if (IsPressing(IDxInput.UP)) { z += 1f; }
                if (IsPressing(IDxInput.DOWN)) { z -= 1f; }

                return new Vector2(x, z);
            }
        }
        public bool TryGetDirection(out Vector2 dir)
        {
            dir = Dir;
            return dir != Vector2.zero;
        }
    }
}
