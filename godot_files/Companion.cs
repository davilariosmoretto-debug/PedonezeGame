
using Godot;

public partial class Companion : CharacterBody2D
{
	private enum CompanionState
	{
		Following,
		Waiting
	}

	[Export]
	public float Speed = 110.0f;

	[Export]
	public float FollowDistance = 105.0f;

	[Export]
	public float ResumeDistance = 18.0f;

	[Export]
	public float UpdateTargetInterval = 0.15f;

	private NavigationAgent2D _agent;
	private AnimatedSprite2D _sprite;
	private CharacterBody2D _player;

	private CompanionState _state = CompanionState.Following;

	private Vector2 _lastPlayerDirection = Vector2.Down;

	private float _targetTimer = 0.0f;

	private string _currentAnimation = "";

	public override async void _Ready()
	{
		_agent = GetNode<NavigationAgent2D>("NavigationAgent2D");
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

		var players = GetTree().GetNodesInGroup("player");

		foreach (Node node in players)
		{
			if (node is CharacterBody2D body && body != this)
			{
				_player = body;
				break;
			}
		}

		if (_player == null)
		{
			GD.PushError("Companion: Player não encontrado no grupo 'player'!");
			SetPhysicsProcess(false);
			return;
		}

		_agent.TargetDesiredDistance = 6.0f;

		PlayAnimation("Idle");

		await ToSignal(
			GetTree(),
			SceneTree.SignalName.PhysicsFrame
		);

		await ToSignal(
			GetTree(),
			SceneTree.SignalName.PhysicsFrame
		);

		UpdateTarget();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_player == null || _agent == null)
			return;

		if (_player.Velocity.Length() > 5.0f)
		{
			_lastPlayerDirection =
				_player.Velocity.Normalized();
		}

		_targetTimer -= (float)delta;

		if (_targetTimer <= 0.0f)
		{
			UpdateTarget();
			_targetTimer = UpdateTargetInterval;
		}

		float distanceToTarget =
			GlobalPosition.DistanceTo(_agent.TargetPosition);

		if (_state == CompanionState.Waiting)
		{
			StopCompanion();

			// Só retoma quando o destino se afastar.
			if (distanceToTarget >= ResumeDistance)
			{
				_state = CompanionState.Following;
			}

			return;
		}

		if (_agent.IsNavigationFinished())
		{
			_state = CompanionState.Waiting;
			StopCompanion();
			return;
		}

		Vector2 nextPosition = _agent.GetNextPathPosition();

		Vector2 direction =
			GlobalPosition.DirectionTo(nextPosition);

		if (direction == Vector2.Zero)
		{
			_state = CompanionState.Waiting;
			StopCompanion();
			return;
		}
		
		Velocity = direction * Speed;

		MoveAndSlide();

		Vector2 actualMovement = GetRealVelocity();

		if (actualMovement.Length() > 1.0f)
		{
			UpdateAnimation(actualMovement.Normalized());
		}
		else
		{
			StopCompanion();
		}
	}

	private void UpdateTarget()
	{
		Vector2 targetPosition =
			_player.GlobalPosition -
			_lastPlayerDirection * FollowDistance;

		if (_agent.TargetPosition.DistanceTo(targetPosition) > 3.0f)
		{
			_agent.TargetPosition = targetPosition;
		}
	}

	private void StopCompanion()
	{
		Velocity = Vector2.Zero;

		PlayAnimation("Idle");
	}

	private void UpdateAnimation(Vector2 direction)
	{
		string animation;

		if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
		{
			animation = direction.X < 0
				? "Left"
				: "Right";
		}
		else
		{
			animation = direction.Y < 0
				? "Back"
				: "Front";
		}

		PlayAnimation(animation);
	}

	private void PlayAnimation(string animation)
	{
		if (_currentAnimation == animation)
			return;

		_currentAnimation = animation;
		_sprite.Play(animation);
	}
}
