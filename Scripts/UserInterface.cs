using Godot;
using System;

public partial class UserInterface : Control
{
	private bool isPause = false;

	private Button _pauseButton;

    public override void _Ready()
    {
		_pauseButton = GetNode<Button>("PauseButton");
    }

    private void OnPauseButtonPressed()
	{
		if (isPause)
		{
    		//GetTree().Paused = false;
			Agent.IsPause = false;
			_pauseButton.Text = "暂停";
		}
		else
		{
			//GetTree().Paused = true;
			Agent.IsPause = true;
			_pauseButton.Text = "启动";		
		}
		isPause = !isPause;
		
	}
}
