using Godot;
using System;

public abstract class CostFunction
{
    protected Agent _agent;
    public float Weight { get; protected set; } = 1;
    
    public float Range { get; protected set; } = 5f;

    public CostFunction(Agent agent, float weight)
    {
        _agent = agent;
        Weight = weight;
    }

    public virtual float CalculateCost(Vector3 velocity)
    {
        throw new NotImplementedException("Calculate Cost Method Not Implemented!");
    }
    
    public virtual Vector3 CalculateCostGradient(Vector3 velocity)
    {
        throw new NotImplementedException("Calculate Cost Gradient Method Not Implemented!");
    }

    protected static float ComputeTimeToCollision(Vector3 position1, Vector3 velocity1, float radius1,
        Vector3 position2, Vector3 velocity2, float radius2)
    {
        var PDiff = position1 - position2;
        float Radii = radius1 + radius2;
        float RadiiSq = Radii * Radii;
        
        if (PDiff.LengthSquared() <= RadiiSq)
            return 0;
        
        var VDiff = velocity1 - velocity2;
        
        float a = VDiff.Dot(VDiff);
        float b = 2 * PDiff.Dot(VDiff);
        float c = PDiff.Dot(PDiff) - RadiiSq;

        float t1, t2;
        int nrSolutions = SolveQuadraticEquation(a, b, c, out t1, out t2);

        // ignore solutions that lie in the past
        if (nrSolutions==0)
        {
            t1 = t2 = Mathf.Inf;
        }
        else
        {
            if (t1 < 0) t1 = Mathf.Inf;
            if (t2 < 0) t2 = Mathf.Inf;
        }
        
        // choose the solution that occurs first; could be MaxFloat if there are no solutions
        return Mathf.Min(t1, t2);
    }

    private static int SolveQuadraticEquation(float a, float b, float c, out float answer1, out float answer2)
    {
        float D = b * b - 4 * a*c;
        if (D < 0)
        {
            answer1 = Mathf.NaN;
            answer2 = Mathf.NaN;
            return 0;
        }
            
        if (D == 0)
        {
            answer1 = -b / 2 * a;
            answer2 = answer1;
            return 1;
        }
        else
        {
            float minusBdiv2A = -b / (2 * a);
            float sqrtDdiv2A = Mathf.Sqrt(D) / (2 * a);
            answer1 = minusBdiv2A + sqrtDdiv2A;
            answer2 = minusBdiv2A - sqrtDdiv2A;
            return 2;
        }
    }

    protected float ComputeTimeToFirstCollision(bool ignoreCurrentCollisions)
    {
        float minTTC = Mathf.Inf;
        float maxDistSquared = Range * Range;

        // check neighboring agents
        foreach (var neighborAgent in _agent.NeighborAgents)
        {
            if ((neighborAgent.Position - _agent.Position).LengthSquared() > maxDistSquared)
                continue;

            float ttc = ComputeTimeToCollision(_agent.Position, _agent.Velocity, 0.5f, neighborAgent.Position, neighborAgent.Velocity, 0.5f);
		
            // ignore current collisions?
            if (ignoreCurrentCollisions && ttc == 0)
                continue;

            if (ttc < minTTC)
                minTTC = ttc;
        }
        
        // check neighboring obstacles
        // ...
        
        return minTTC;
    }
}
