using Microsoft.AspNetCore.Mvc;
using TargetAPI.Models;

namespace TargetAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SimulationController : ControllerBase
    {
        private readonly SimulationState _state;

        public SimulationController(SimulationState state)
        {
            _state = state;
        }

        [HttpPost("configure")]
        public IActionResult Configure([FromBody] SimulationState newState)
        {
            _state.ForceError500 = newState.ForceError500;
            _state.LatencyMilliseconds = newState.LatencyMilliseconds;

            return Ok(new { message = "Simulation state updated", currentState = _state });
        }

        [HttpPost("reset")]
        public IActionResult Reset()
        {
            _state.ForceError500 = false;
            _state.LatencyMilliseconds = 0;

            return Ok(new { message = "Simulation state reset to normal", currentState = _state });
        }
    }
}
