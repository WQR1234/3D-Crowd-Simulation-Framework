using Godot;
using System;

namespace CostFunctions
{
    public class GoalReachingForce : CostFunction
    {
        public GoalReachingForce(Agent agent, float weight) : base(agent, weight) {}

        public override Vector3 CalculateCostGradient(Vector3 velocity)
        {
            
            var f = (_agent.PreferredVelocity - velocity) /
                    Mathf.Max(_agent.RelaxationTime, World.Instance.DeltaTime);
            // GD.Print("GF: "+f);
            return f;
        }
    }
    
    
}

