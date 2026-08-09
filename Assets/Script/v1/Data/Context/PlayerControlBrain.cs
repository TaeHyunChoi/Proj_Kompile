namespace Kompile.Entity
{
    using Data;

    /// <summary> 플레이어 입력을 읽어 UnitIntent로 반환하는 Brain </summary>
    public class PlayerControlBrain : IUnitBrain
    {
        private Entity _owner;
        public PlayerControlBrain(Entity entity)
        {
            _owner = entity;
        }

        // --- interface: IUnitBrain ---
        public void Clear()
        {
            _owner = null;
        }
        public UnitIntent Update()
        {
            // (26.05.13) 현재로서는 intent를 직접 주입하니까 사용을 하지 않는다.
            return default;
        }
    }
}
