using System;
using Godot;
using Godot.Collections;

namespace CostFunctions;

public class RVO : CostFunction
{
    private float w = 1;
    
    public RVO(Agent agent, float weight) : base(agent, weight) {}

    public override float CalculateCost(Vector3 velocity)
    {
        if (velocity.Length() > _agent.MaxSpeed)
        {
            return Mathf.Inf;
        }

        var RVOVelocity = 2 * velocity - _agent.Velocity;

        var vDiff = _agent.PreferredVelocity - velocity;
        
        // compute the smallest time to collision among all neighboring agents
        float minTTC = ComputeTimeToFirstCollision(RVOVelocity, true);

        return w / minTTC + vDiff.Length();
    }

    public override void ParseParam(Dictionary costFunctionData)
    {
        string samlingTypeStr = costFunctionData["SamplingType"].AsString();
        if (Enum.TryParse(samlingTypeStr, true, out SamplingParameters.Type samType))
        {
            samplingParams.type = samType;
        }
        else
        {
            GD.Print($"'{samlingTypeStr}' is not a valid Sampling Type value.");
        }

    }
}