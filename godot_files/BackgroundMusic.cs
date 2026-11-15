
using Godot;
using System;

public partial class BackgroundMusic : AudioStreamPlayer2D
{
	private AudioStream[] songs =
	{
		GD.Load<AudioStream>("res://music1.mp3"),
		GD.Load<AudioStream>("res://music2.mp3")
	};

	private int currentSong = 0;

	private RandomNumberGenerator rng = new RandomNumberGenerator();

	private const float FadeDuration = 3.0f;
	private const float FadeVolume = -40.0f;

	public override void _Ready()
	{
		rng.Randomize();
		MusicLoop();
	}

	private async void MusicLoop()
	{
		while (true)
		{
			Stream = songs[currentSong];

			VolumeDb = FadeVolume;
			Play();

			// Fade In
			Tween fadeIn = CreateTween();

			fadeIn.TweenProperty(
				this,
				"volume_db",
				0.0f,
				FadeDuration
			);

			await ToSignal(fadeIn, Tween.SignalName.Finished);

			// Espera a música terminar
			await ToSignal(this, AudioStreamPlayer2D.SignalName.Finished);

			// Fade Out
			Tween fadeOut = CreateTween();

			fadeOut.TweenProperty(
				this,
				"volume_db",
				FadeVolume,
				FadeDuration
			);

			await ToSignal(fadeOut, Tween.SignalName.Finished);

			Stop();

			// Intervalo aleatório entre músicas
			double waitingTime = rng.RandfRange(40.0f, 240.0f);

			await ToSignal(
				GetTree().CreateTimer(waitingTime),
				SceneTreeTimer.SignalName.Timeout
			);

			currentSong = (currentSong + 1) % songs.Length;
		}
	}
}
