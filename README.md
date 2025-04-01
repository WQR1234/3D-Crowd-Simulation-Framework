## 3D Crowd Simulation Framework

This open-source software allows for the selection of different crowd simulation algorithms and the configuration of relevant parameters, 
as well as convenient viewing of parameters during simulation.

### Feature

* Unified algorithm paradigm 
* 3D
* Global navigation

### Config File

#### Example:

```json
{
    "StartNav": false,
    "Fps": 60,
    "Agents": [
        {
            "Pos": [-4, -4], "Goal": [4, 4], 
            "PrefSpeed": 1.3, "MaxSpeed": 1.6, 
            "OptimizationMethod": "Sampling"
        },
        {
            "Pos": [4, -5], "Goal": [-4, 5], 
            "PrefSpeed": 1.3, "MaxSpeed": 1.6, 
            "OptimizationMethod": "Sampling"
        },
        {
            "Pos": [4, -3], "Goal": [-4, 3], 
            "PrefSpeed": 1.3, "MaxSpeed": 1.6, 
            "OptimizationMethod": "Sampling"
        }
    ],
    "Obstacles": {
        "Walls": [
          {"Pos": [0, 0], "Size": [1, 1]}
        ]
    },
    "CostFunctions": [
        {
            "Name": "RVO", "Weight": 1,
            "SamplingPara": {
                "type": "Random",
                "base": "CurrentVelocity",
                "baseDirection": "Unit",
                "radius": "MaximumAcceleration",
                "angle": 360,
                "speedSamples": 4,
                "angleSamples": 11,
                "randomSamples": 250,
                "includeBaseAsSample": false
            }
        }
    ] 
}
```

### Environment

**Godot 4.4**

**Plugin:** [[godot_debug_draw_3d](https://github.com/DmitriySalnikov/godot_debug_draw_3d)]()  to display agent motion trajectory



### Move Camera:

Key WASD

### Rotate Camera:

Press the right mouse button and move the mouse