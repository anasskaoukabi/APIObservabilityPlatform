using TargetAPI.Models;
using System.Text.Json;

namespace TargetAPI.Middlewares
{
    public class SimulationMiddleware
    {
        private readonly RequestDelegate _next;

        public SimulationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, SimulationState state)
        {
            var path = context.Request.Path;

            // Only apply simulation on /api routes, excluding the simulation configuration endpoints themselves
            if (path.StartsWithSegments("/api") && !path.StartsWithSegments("/api/simulation"))
            {
                if (state.LatencyMilliseconds > 0)
                {
                    await Task.Delay(state.LatencyMilliseconds);
                }

                if (state.ForceError500)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";
                    
                    var errorResponse = new { message = "Simulated Internal Server Error." };
                    await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
                    
                    return;
                }
            }

            await _next(context);
        }
    }
}
