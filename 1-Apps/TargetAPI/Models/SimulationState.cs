namespace TargetAPI.Models
{
    public class SimulationState
    {
        public bool ForceError500 { get; set; } = false;
        public int LatencyMilliseconds { get; set; } = 0;
    }
}
