using Godot;
using System;
using System.Text.Json.Serialization;


public struct SamplingParameters
{
    public enum Type { Regular, Random }
    
    public enum Base { Zero, CurrentVelocity }
    
    public enum BaseDirection { Unit, CurrentVelocity, PreferredVelocity }
    
    public enum Radius { PreferredSpeed, MaximumSpeed, MaximumAcceleration }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Type type { get; set; } = Type.Regular;
    [JsonConverter(typeof(JsonStringEnumConverter))]
	public Base @base { get; set; } = Base.Zero;
    [JsonConverter(typeof(JsonStringEnumConverter))]
	public BaseDirection baseDirection { get; set; } = BaseDirection.CurrentVelocity;
    [JsonConverter(typeof(JsonStringEnumConverter))]
	public Radius radius { get; set; } = Radius.PreferredSpeed;
	public float angle { get; set; } = 180;
	public int speedSamples { get; set; } = 4;
	public int angleSamples { get; set; }= 11;
    public int randomSamples { get; set; } = 100;
    public bool includeBaseAsSample { get; set; } = false;

    public SamplingParameters() { }

}

/// <summary>
/// 成本函数类，必须实现CalculateCost方法或CalculateCostGradient方法
/// </summary>
public abstract class CostFunction
{
    protected Agent _agent;

    public float Weight { get; protected set; } = 1;   // 该成本函数所占权重

    public SamplingParameters samplingParams ;

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
    
    /// <summary>
    /// 解析cost function的自定义参数， 应由该cost function实现。
    /// </summary>
    /// <param name="costFunctionData">json数据中的每1条cost function数据</param>
    public virtual void ParseParam(Godot.Collections.Dictionary costFunctionData) {}

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
    
    /// <summary>
    /// 求解一元二次方程 a*x^2+b*x+c=0
    /// </summary>
    /// <param name="a">二次项系数</param>
    /// <param name="b">一次项系数</param>
    /// <param name="c">常数项</param>
    /// <param name="answer1">第1个解，若无解则为NaN</param>
    /// <param name="answer2">第2个解，若无解则为NaN</param>
    /// <returns>0：无解，1：仅有一个解，2：有两个解</returns>
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

    protected float ComputeTimeToFirstCollision(Vector3 velocity, bool ignoreCurrentCollisions)
    {
        float minTTC = Mathf.Inf;
        float maxDistSquared = _agent.Range * _agent.Range;

        // check neighboring agents
        foreach (var neighborAgent in _agent.NeighborAgents)
        {
            if ((neighborAgent.Position - _agent.Position).LengthSquared() > maxDistSquared)
                continue;

            float ttc = ComputeTimeToCollision(_agent.Position, velocity, 0.5f, neighborAgent.Position, neighborAgent.Velocity, 0.5f);
		
            // ignore current collisions?
            if (ignoreCurrentCollisions && ttc == 0)
                continue;

            if (ttc < minTTC)
                minTTC = ttc;
        }
        
        // check neighboring obstacles
        // ...
        // 发射射线检测将要撞到的障碍物

        bool isCollision = _agent.EmitRays(out Vector3 intersection, out _);
        if (isCollision) {
            float ttc = (intersection - _agent.Position).Length() / _agent.Velocity.Length();
            if (!ignoreCurrentCollisions || ttc > 0) {
                minTTC = Mathf.Min(minTTC, ttc);
            }
        }
        
        return minTTC;
    }

    public Vector3 ApproximateGlobalMinimumBySampling(double delta)
    {
        Vector3 baseV = Vector3.Zero;
        if (samplingParams.@base==SamplingParameters.Base.CurrentVelocity) 
            baseV = _agent.Velocity;

        float radius;
        if (samplingParams.radius==SamplingParameters.Radius.PreferredSpeed)
            radius = _agent.PreferredSpeed;
        else if (samplingParams.radius==SamplingParameters.Radius.MaximumSpeed)
            radius = _agent.MaxSpeed;
        else
            radius = Mathf.Min(_agent.MaxSpeed, _agent.MaxAcceleration * (float)delta);
        
        // compute the base direction (a unit vector)
        Vector3 baseDirection = Vector3.Right;
        if (samplingParams.baseDirection==SamplingParameters.BaseDirection.Unit) baseDirection = Vector3.Right;
        else if (samplingParams.baseDirection==SamplingParameters.BaseDirection.CurrentVelocity) baseDirection = _agent.Velocity.Normalized();
        else if (samplingParams.baseDirection == SamplingParameters.BaseDirection.PreferredVelocity) baseDirection = _agent.PreferredVelocity.Normalized();

        float maxAngle = samplingParams.angle / 360.0f * Mathf.Pi; 

        Vector3 bestVelocity = Vector3.Zero;
        float bestCost = Mathf.Inf;

        if (samplingParams.type==SamplingParameters.Type.Random) 
        {
            for (int i = 0; i < samplingParams.randomSamples; i++)
            {
                // create a random velocity in the cone/circle
                float randomAngle = (float)GD.RandRange(-maxAngle, maxAngle);
                float randomLength = (float)GD.RandRange(0, radius);
                Vector3 velocity = baseV + baseDirection.Rotated(Vector3.Up, randomAngle) * randomLength;

                // compute the cost for this velocity
                float totalCost = 0;
                foreach (var costFunction in _agent.CostFunctions)
                {
                    totalCost += costFunction.Weight * costFunction.CalculateCost(velocity);
                }

                // check if this cost is better than the minimum so far
                if (totalCost < bestCost)
                {
                    bestVelocity = velocity;
                    bestCost = totalCost;
                }
            }
        }

        // --- Option 2: Regular sampling
        else if (samplingParams.type==SamplingParameters.Type.Regular)
        {
            // compute the difference in angle and length per iteration
            float startAngle = -maxAngle;
            float endAngle = maxAngle;
            float deltaAngle = (endAngle - startAngle) / (samplingParams.angle == 360 ? samplingParams.angleSamples : (samplingParams.angleSamples - 1));
            float deltaLength = radius / (samplingParams.includeBaseAsSample ? (samplingParams.speedSamples - 1) : samplingParams.speedSamples);

            // speed samples
            for (int s = 0; s<=samplingParams.speedSamples; s++) 
            {
                float candidateLength = deltaLength * (samplingParams.includeBaseAsSample ? s-1 : s);
                float candidateAngle = startAngle;

                // angle samples
                for (int a = 0; a < samplingParams.angleSamples; a++, candidateAngle+=deltaAngle)
                {
                    // construct the candidate velocity
                    Vector3 velocity = baseV + baseDirection.Rotated(Vector3.Up, candidateAngle);

                    // compute the cost for this velocity
				    float totalCost = 0;
                    foreach (var costFunction in _agent.CostFunctions)
                    {
                        totalCost += costFunction.Weight * costFunction.CalculateCost(velocity);
                    }

                    // check if this cost is better than the minimum so far
                    if (totalCost < bestCost)
                    {
                        bestVelocity = velocity;
                        bestCost = totalCost;
                    }

                    // if we are currently checking the base velocity, we don't have to sample any more angles
                    if (samplingParams.includeBaseAsSample && s == 1)
                        break;

                }
            }
        }

        // --- Return the velocity with the lowest cost

        return bestVelocity;
    }
}
