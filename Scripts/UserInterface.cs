using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;


// using OsmSharp;

public partial class UserInterface : VBoxContainer
{
    // private bool isPause = false;
	
    private Button _pauseButton; 
    private FileDialog _fileDialog;
    private ConfigData _configData;

    public override void _Ready()
    { 
	    _pauseButton = GetNode<Button>("PauseButton"); 
	    _fileDialog = GetNode<FileDialog>("FileDialog");
    }

    private void OnPauseButtonPressed()
    { 
	    if (World.IsPause) 
	    { 
		    //GetTree().Paused = false;
		    World.IsPause = false; 
		    _pauseButton.Text = "暂停"; 
	    }
	    else 
	    { 
		    //GetTree().Paused = true;
		    World.IsPause = true; 
		    _pauseButton.Text = "启动"; 
	    }

    }

    private void OnLoadButtonPressed()
    { 
	    _fileDialog.Popup();
    }

    private void OnResetButtonPressed()
    {
	    foreach (var (agent, agentData) in World.Instance.AllAgents.Zip(_configData.Agents, (agent, data) => (agent, data)))
	    {
		    var posV = new Vector3(agentData.Pos[0], 0, agentData.Pos[1]);
		    agent.Position = posV;
		    agent.Reset();
	    }

	    // if (!World.IsPause)
	    // {
		   //  OnPauseButtonPressed();
	    // }
	    
    }

    public class ConfigData
    {
	    public bool StartNav { get; set; } = false;
	    public int Fps { get; set; } = 60;
	    public List<AgentData> Agents { get; set; }
	    public ObstacleData Obstacles { get; set; }
	    public List<CostFunctionData> CostFunctions { get; set; }
    }
    
    
    
    public class AgentData
    {
	    public List<float> Pos  { get; set; } 
	    public List<float> Goal  { get; set; } 
	    public float PrefSpeed { get; set; }
	    public float MaxSpeed  { get; set; } 
	    
	    [JsonConverter(typeof(JsonStringEnumConverter))]
	    public Agent.PolicyType OptimizationMethod { get; set; }
    }
    
    public class ObstacleData
    {
	    public List<WallData> Walls  { get; set; } 
	    public List<HillData> Hills  { get; set; } 
    }

    public class WallData
    {
	    public List<float> Pos  { get; set; } 
	    public List<float> Size  { get; set; } 
    }
    
    public class HillData
    {
	    public List<float> Pos  { get; set; } 
	    public int Rot { get; set; } 
    }
    
    public class CostFunctionData
    {
	    public string Name { get; set; }
	    public float Weight { get; set; }

	    // [JsonConverter(typeof(JsonStringEnumConverter))]
	    public SamplingParameters? SamplingPara { get; set; } = null;
    }

    private void OnFileDialogFileSelected(string path)
    {
	    GD.Print(path);

	    using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
	    string content = file.GetAsText();
	    
	    _configData = JsonSerializer.Deserialize<ConfigData>(content);
	    if (_configData==null)
	    {
		    GD.PrintErr("Parse Fail!");
		    return;
	    }

	    
	    // create agents
	    foreach (var agentData in _configData.Agents)
	    {
		    var posV = new Vector3(agentData.Pos[0], 0, agentData.Pos[1]);
		    var goalV = new Vector3(agentData.Goal[0], 0, agentData.Goal[1]);

		    World.Instance.CreatAgent(posV, goalV, agentData.PrefSpeed, agentData.MaxSpeed, agentData.OptimizationMethod);
	    }
	    
	    // create obstacles 
	    foreach (var (i, wallData) in _configData.Obstacles.Walls.Select((wallData, i) => (i, wallData)))
	    {
		    var posV = new Vector3(wallData.Pos[0], 1, wallData.Pos[1]);
		    var sizeV = new Vector3(wallData.Size[0], 1, wallData.Size[1]);
		    
		    World.Instance.CreatWall(posV, sizeV, i);
	    }
	    
	    // add nav
	    World.Instance.BakeNavMesh(_configData.StartNav);
	    
	    // add cost functions
	    foreach (var agent in World.Instance.AllAgents)
	    {
		    foreach (var costFunctionData in _configData.CostFunctions)
		    {
			    agent.AddCostFunction(costFunctionData);
		    }
	    }
	    
	    OnPauseButtonPressed();
	    
    }
}
