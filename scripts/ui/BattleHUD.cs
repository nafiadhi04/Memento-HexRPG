using Godot;
using System;

namespace MementoTest.UI
{
	public partial class BattleHUD : CanvasLayer
	{
		// Signal untuk memberi tahu TurnManager bahwa tombol ditekan
		[Signal] public delegate void EndTurnRequestedEventHandler();

		private Button _endTurnBtn;
		private Label _turnLabel;

		public override void _Ready()
		{
			_endTurnBtn = GetNode<Button>("Control/EndTurnBtn");
			_turnLabel = GetNode<Label>("Control/TurnLabel");

			// Hubungkan tombol ke signal kita sendiri
			_endTurnBtn.Pressed += () => EmitSignal(SignalName.EndTurnRequested);
		}

		// Fungsi Helper untuk mengatur tampilan tombol
		public void SetEndTurnButtonInteractable(bool interactable)
		{
			_endTurnBtn.Disabled = !interactable;
			_endTurnBtn.Text = interactable ? "END TURN" : "ENEMY TURNING...";
		}

		// Fungsi Helper untuk update teks status
		public void UpdateTurnLabel(string text)
		{
			_turnLabel.Text = text;

			CreateTween().TweenProperty(_turnLabel, "scale", new Vector2(1.2f, 1.2f), 0.1f)
				.SetTrans(Tween.TransitionType.Bounce);
			CreateTween().TweenProperty(_turnLabel, "scale", Vector2.One, 0.1f)
				.SetDelay(0.1f);
		}
	}
}