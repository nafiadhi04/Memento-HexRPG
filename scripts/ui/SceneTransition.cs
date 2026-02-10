using Godot;
using System;
using System.Threading.Tasks;

public partial class SceneTransition : CanvasLayer
{
	public static SceneTransition Instance { get; private set; }

	[Export] public float Duration = 1.5f; // Bisa diatur di Inspector

	private ColorRect _colorRect;
	private ShaderMaterial _shaderMat;

	public override void _Ready()
	{
		Instance = this;
		_colorRect = GetNode<ColorRect>("ColorRect");
		_shaderMat = _colorRect.Material as ShaderMaterial;

		if (_shaderMat != null)
		{
			_shaderMat.SetShaderParameter("progress", 0.0f);
		}

		_colorRect.MouseFilter = Control.MouseFilterEnum.Ignore;
	}

	public async void ChangeScene(PackedScene targetScene)
	{
		if (targetScene == null || _shaderMat == null) return;

		_colorRect.MouseFilter = Control.MouseFilterEnum.Stop;

		// 1. TRANSISI MASUK (Tutup Layar: 0.0 -> 1.0)
		Tween tween = CreateTween();
		tween.TweenMethod(Callable.From<float>(SetProgress), 0.0f, 1.0f, Duration)
			 .SetTrans(Tween.TransitionType.Cubic)
			 .SetEase(Tween.EaseType.Out);

		await ToSignal(tween, "finished");

		// 2. GANTI SCENE
		GetTree().ChangeSceneToPacked(targetScene);

		// Tunggu sebentar (Jeda saat layar gelap)
		await ToSignal(GetTree().CreateTimer(0.2f), "timeout");

		// 3. TRANSISI KELUAR (Buka Layar: 1.0 -> 0.0)
		Tween tweenOut = CreateTween();

		// [PERBAIKAN DI SINI]: Dari 1.0 (Hitam) ke 0.0 (Transparan)
		tweenOut.TweenMethod(Callable.From<float>(SetProgress), 1.0f, 0.0f, Duration)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.In);

		await ToSignal(tweenOut, "finished");

		_colorRect.MouseFilter = Control.MouseFilterEnum.Ignore;
	}

	private void SetProgress(float value)
	{
		_shaderMat.SetShaderParameter("progress", value);
	}

	// Overload String (Diperbaiki juga agar menggunakan Duration)
	public async void ChangeScene(string scenePath)
	{
		if (_shaderMat == null) return;
		_colorRect.MouseFilter = Control.MouseFilterEnum.Stop;

		// Masuk
		Tween tween = CreateTween();
		tween.TweenMethod(Callable.From<float>(SetProgress), 0.0f, 1.0f, Duration);
		await ToSignal(tween, "finished");

		GetTree().ChangeSceneToFile(scenePath);

		// Keluar
		Tween tweenOut = CreateTween();
		// [PERBAIKAN]: Dari 1.0 ke 0.0
		tweenOut.TweenMethod(Callable.From<float>(SetProgress), 1.0f, 0.0f, Duration);
		await ToSignal(tweenOut, "finished");

		_colorRect.MouseFilter = Control.MouseFilterEnum.Ignore;
	}
}