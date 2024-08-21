using Godot;
using System;
using System.Collections.Generic;

public partial class World : Node
{
	public static World Instance { get; private set; }

	public List<Agent> AllAgents;

	public float DeltaTime { get; private set; } = 1.0f / 60.0f;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		AllAgents = new List<Agent>();
		Instance = this;
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);
		DeltaTime = (float)delta;

		foreach (var agent in AllAgents)
		{
			agent.MoveAndSlide();
			
		}
	}
	
	
}
