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

    /// <summary>
    /// 解析json文件中agent的属性并生成agent
    /// </summary>
    /// <param name="agentData">agent数据，Godot字典类型</param>
    private void ParseAgentData(Godot.Collections.Dictionary agentData)
    {
	    var pos = agentData["pos"].AsGodotArray();
	    var goal = agentData["goal"].AsGodotArray();
	    var preferredSpeed = agentData["pref_speed"].AsSingle();
	    var maxSpeed = agentData["max_speed"].AsSingle();
	    var optMethod = agentData["optimization_method"].AsString();
	    
	    var posV = new Vector3(pos[0].AsSingle(), 0, pos[1].AsSingle());
	    var goalV = new Vector3(goal[0].AsSingle(), 0, goal[1].AsSingle());

	    Agent.PolicyType method;
	    if (optMethod=="gradient")
	    {
		    method = Agent.PolicyType.Gradient;
	    }
	    else if (optMethod=="sampling")
	    {
		    method = Agent.PolicyType.Sampling;
	    }
	    else
	    {
		    GD.PrintErr("invalid optimization method");
		    return;
	    }
	    
	    World.Instance.CreatAgent(posV, goalV, preferredSpeed, maxSpeed, method);
    }

    private void ParseWallData(Godot.Collections.Dictionary wallData)
    {
	    var pos = wallData["pos"].AsGodotArray();
	    var size = wallData["size"].AsGodotArray();
	    
	    var posV = new Vector3(pos[0].AsSingle(), 0, pos[1].AsSingle());
    }
    
    public class ConfigData
    {
	    public bool StartNav { get; set; } = false;
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
    }

    private void OnFileDialogFileSelected(string path)
    {
	    GD.Print(path);

	    using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
	    string content = file.GetAsText();
	    
	    var data = JsonSerializer.Deserialize<ConfigData>(content);
	    if (data==null)
	    {
		    GD.PrintErr("Parse Fail!");
		    return;
	    }

	    
	    // create agents
	    foreach (var agentData in data.Agents)
	    {
		    var posV = new Vector3(agentData.Pos[0], 0, agentData.Pos[1]);
		    var goalV = new Vector3(agentData.Goal[0], 0, agentData.Goal[1]);

		    World.Instance.CreatAgent(posV, goalV, agentData.PrefSpeed, agentData.MaxSpeed, agentData.OptimizationMethod);
	    }
	    
	    // create obstacles 
	    foreach (var (i, wallData) in data.Obstacles.Walls.Select((wallData, i) => (i, wallData)))
	    {
		    var posV = new Vector3(wallData.Pos[0], 0, wallData.Pos[1]);
		    var sizeV = new Vector3(wallData.Size[0], 0, wallData.Size[1]);
		    
		    World.Instance.CreatWall(posV, sizeV, i);
	    }
	    
	    // add nav
	    World.Instance.BakeNavMesh(data.StartNav);
	    
	    // add cost functions
	    foreach (var agent in World.Instance.AllAgents)
	    {
		    foreach (var costFunctionData in data.CostFunctions)
		    {
			    agent.AddCostFunction(costFunctionData);
		    }
	    }
	    

	    //    GD.Print(path);
	    //    using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
	    //    string content = file.GetAsText();
	    //
	    //    var data = Json.ParseString(content).AsGodotDictionary();
	    //    
	    // // 解析agents数据
	    //    var agents = data["Agents"].AsGodotArray();
	    //
	    //    foreach (var agent in agents)
	    //    {
	    //     var agentData = agent.AsGodotDictionary();
	    //     ParseAgentData(agentData);
	    //
	    //    }
	    //
	    //    // 解析obstacles数据
	    //    var obstacles = data["Obstacles"].AsGodotDictionary();
	    //    // 解析walls数据
	    //    var walls = obstacles["Walls"].AsGodotArray();
	    //    for (int i = 0; i < walls.Count; i++)
	    //    {
	    //     var wallData = walls[i].AsGodotDictionary();
	    //     
	    //     var pos = wallData["pos"].AsGodotArray();
	    //     var size = wallData["size"].AsGodotArray();
	    //    
	    //     var posV = new Vector3(pos[0].AsSingle(), 1, pos[1].AsSingle());
	    //     var sizeV = new Vector3(size[0].AsSingle(), 1, size[1].AsSingle());
	    //     
	    //     World.Instance.CreatWall(posV, sizeV, i);
	    //    }
	    //
	    //    // 解析hills数据
	    //    // var hills = obstacles["Hills"].AsGodotDictionary();
	    //
	    //    // 添加导航网络
	    //    bool startNav = data["startNav"].AsBool();
	    //    World.Instance.BakeNavMesh(startNav);
	    //    
	    //    // 解析 cost functions 数据
	    //    var costFunctions = data["CostFunctions"].AsGodotArray();
	    //    foreach (var agent in World.Instance.AllAgents)
	    //    {
	    //     foreach (var costFunction in costFunctions)
	    //     {
	    // 	    agent.AddCostFunction(costFunction.AsGodotDictionary());
	    //     }
	    //     
	    //    }
    }
}
