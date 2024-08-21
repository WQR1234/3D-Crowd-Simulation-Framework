using Godot;
using System;
using System.Collections.Generic;
using CostFunctions;

public partial class Agent : CharacterBody3D
{
    [Export]
    public float MaxSpeed { get; set; } = 1.6f;
    
    [Export]
    public Vector3 Goal { get; set; }
    
    /// <summary>Returns the agent's preferred walking speed.</summary>
    [Export]
    public float PreferredSpeed { get; set; }  
    
    
    /// <summary>Returns the agent's last computed preferred velocity.</summary>
    public Vector3 PreferredVelocity { get; private set; }    
    
    /// <summary>Sets this Policy's relaxation time to the given value.</summary>
    /// <param name="t">The desired new relaxation time. 
    /// Use 0 or less to let agents use their new velocity immediately.
    /// Use a higher value to let agents interpolate between their current and new velocity.</param>
    /// <summary> Returns the relaxation time of this Policy.</summary>
    public float RelaxationTime { get; private set; } = 0.5f;   

    /// <summary>
    /// Spherical rays used to detect neighbors
    /// </summary>
    private ShapeCast3D _shapeCast;

    private AnimationPlayer _animation;
    private Node3D _modelNode;

    private List<Agent> _neighborAgents;
    public IReadOnlyList<Agent> NeighborAgents => _neighborAgents.AsReadOnly();
    private List<Vector3> _neighborObstacleNearestPoints;
    public IReadOnlyList<Vector3> NeighborObstacleNearestPoints => _neighborObstacleNearestPoints.AsReadOnly();

    private List<CostFunction> _costFunctions;

    public override void _Ready()
    {
        _shapeCast = GetNode<ShapeCast3D>("ShapeCast3D");
        _modelNode = GetNode<Node3D>("Model");
        ((SphereShape3D)_shapeCast.Shape).Radius = 2;

        _animation = GetNode<AnimationPlayer>("Model/RootNode/AnimationPlayer");

        _neighborAgents = new List<Agent>();
        _neighborObstacleNearestPoints = new List<Vector3>();
        _costFunctions = new List<CostFunction>();
        
        _costFunctions.Add(new GoalReachingForce(this, 1));
        _costFunctions.Add(new SocialForcesAvoidance(this, 1));
        
        
        World.Instance.AllAgents.Add(this);
    }

    public override void _PhysicsProcess(double delta)
    {
        // if (Name=="Agent")
        // {
        //     return;
        // }

        // GetInput();

        ComputeNeighbors();
        
        ComputePreferredVelocity();
        ComputeAccelerationAndVelocity(delta);
        
        PlayAnimation();
    }
    
    private void GetInput()
    {
        Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
        Velocity = new Vector3(inputDir.X, 0, inputDir.Y) * MaxSpeed;
    }

    private void ComputeNeighbors()
    {
        _neighborAgents.Clear();
        _neighborObstacleNearestPoints.Clear();
        if (_shapeCast.IsColliding())
        {
            for (int i = 0; i < _shapeCast.GetCollisionCount(); i++)
            {
                if (_shapeCast.GetCollider(i) is Agent otherAgent)
                {
                    _neighborAgents.Add(otherAgent);
                    // GD.Print(otherAgent.Name);
                }
                
                else if (_shapeCast.GetCollider(i) is StaticBody3D obstacle)
                {
                    // GD.Print(_shapeCast.GetCollisionNormal(i));
                    float normalAngle = Vector3.Up.AngleTo(_shapeCast.GetCollisionNormal(i)) * 180f / Mathf.Pi;
                    // GD.Print(normalAngle);
                    if (normalAngle>45)
                    {
                        _neighborObstacleNearestPoints.Add(_shapeCast.GetCollisionPoint(i));
                    }
                    
                    
                    // _neighborObstacles.Add(obstacle);
                    // GD.Print(obstacle.Name);
                    // GD.Print(_shapeCast.GetCollisionNormal(i));
                }
            }
        }
        
        
        
    }

    private bool HasReachedGoal()
    {
        return (Goal - Position).LengthSquared() < 1f;
    }

    private void ComputePreferredVelocity()
    {
        if (HasReachedGoal())
        {
            PreferredVelocity = Vector3.Zero;
        }
        PreferredVelocity = (Goal - Position).Normalized() * PreferredSpeed;
    }

    private void ComputeAccelerationAndVelocity(double delta)
    {
        Vector3 acceleration = Vector3.Zero;
        
        foreach (var costFunction in _costFunctions)
        {
            acceleration += costFunction.CalculateCostGradient(Velocity);
        }

        if (acceleration.LengthSquared()>25)
        {
            acceleration = acceleration.Normalized() * 5;
        }
        // GD.Print("a: "+acceleration);
        
        Velocity += acceleration * (float)delta;
        
        if (Velocity.LengthSquared()>MaxSpeed*MaxSpeed)
        {
            Velocity = Velocity.Normalized() * MaxSpeed;
        }
    }

    private void PlayAnimation()
    {
        // GD.Print("v: "+Velocity);

        if (Velocity.LengthSquared()>=0.025f)
        {
            Vector3 target = new Vector3(Velocity.X, 0, Velocity.Z);
            _modelNode.Basis = Basis.LookingAt(target, useModelFront: true);
            _animation.Play("BasicMotions@Walk01/BasicMotions_Walk01 - Forwards");
        }
        else
        {
            _animation.Play("BasicMotions@Idle01/BasicMotions_Idle01");
        }
    }
}
