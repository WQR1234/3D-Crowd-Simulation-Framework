using Godot;
using System;

public partial class CameraController : Node3D
{
    // accumulators
    private float _rotationX = 0f;
    private float _rotationY = 0f;

    private bool _canCameraRotate = false;

    [Export]
    public float LookAroundSpeed { get; set; } = 0.003f;

    public float MoveSpeed = 0.1f;

    private Vector3 _velocity;
    

    public override void _Input(InputEvent @event)
    {
        // 若是鼠标点击事件
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex==MouseButton.Right) // 若是鼠标右键
            {
                // GD.Print(Transform.Basis.Z);
                _canCameraRotate = !_canCameraRotate;
                return;
            }
        }
        
        if (@event is InputEventMouseMotion mouseMotion && _canCameraRotate)
        {
            // modify accumulated mouse rotation
            _rotationX += mouseMotion.Relative.X * LookAroundSpeed;
            _rotationY += mouseMotion.Relative.Y * LookAroundSpeed;

            // reset rotation
            Transform3D transform = Transform;
            transform.Basis = Basis.Identity;
            Transform = transform;

            RotateObjectLocal(Vector3.Up, -_rotationX); // first rotate about Y
            RotateObjectLocal(Vector3.Right, -_rotationY); // then rotate about X
        }
    }

    public override void _Process(double delta)
    {
        GetInput();
        TranslateObjectLocal(_velocity);
    }

    private void GetInput()
    {
        Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
        _velocity = new Vector3(inputDir.X, 0, inputDir.Y) * MoveSpeed;
    }
}
