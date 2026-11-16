
using Godot;

public partial class NPCWalk : CharacterBody2D
{
	[Export]
	public float Speed = 80.0f;

	[Export]
	public Node2D Destination { get; set; }

	[Export]
	public float WaitAtDestination = 1.0f;

	private NavigationAgent2D _agent;
	private AnimatedSprite2D _sprite;

	private string _currentAnimation = "";

	private Vector2 _startPosition;
	private float _waitTimer = 0.0f;
	private bool _waitingToRestart = false;

	public override async void _Ready()
	{
		_agent = GetNode<NavigationAgent2D>("NavigationAgent2D");
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

		if (Destination == null)
		{
			GD.PushWarning("Destination não foi definido!");
			return;
		}

		_startPosition = GlobalPosition;

		_agent.TargetPosition = Destination.GlobalPosition;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_agent == null || Destination == null)
			return;

		if (_waitingToRestart)
		{
			_waitTimer -= (float)delta;

			if (_waitTimer <= 0.0f)
			{
				GlobalPosition = _startPosition;

				Show();

				_currentAnimation = "";

				_agent.TargetPosition = Destination.GlobalPosition;

				_waitingToRestart = false;
			}

			return;
		}

		float distanceToDestination =
			GlobalPosition.DistanceTo(Destination.GlobalPosition);

		if (distanceToDestination <= 1.0f)
		{
			GlobalPosition = Destination.GlobalPosition;
			Velocity = Vector2.Zero;

			_sprite.Stop();
			Hide();

			_waitTimer = WaitAtDestination;
			_waitingToRestart = true;

			return;
		}

		if (_agent.IsNavigationFinished())
		{
			Velocity = Vector2.Zero;
			_sprite.Stop();
			return;
		}

		Vector2 nextPosition = _agent.GetNextPathPosition();

		Vector2 direction = GlobalPosition.DirectionTo(nextPosition);

		Velocity = direction * Speed;

		if (direction != Vector2.Zero)
			UpdateAnimation(direction);

		MoveAndSlide();
	}

	private void UpdateAnimation(Vector2 direction)
	{
		string animation;

		if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
		{
			animation = direction.X < 0
				? "Left Side"
				: "Right Side";
		}
		else
		{
			animation = direction.Y < 0
				? "Back Side"
				: "Front";
		}

		if (_currentAnimation != animation)
		{
			_currentAnimation = animation;
			_sprite.Play(animation);
		}
	}
}
