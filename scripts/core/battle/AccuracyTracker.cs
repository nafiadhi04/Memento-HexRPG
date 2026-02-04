using Godot;

namespace MementoTest.Core
{
	public partial class AccuracyTracker : Node
	{
		public static AccuracyTracker Instance { get; private set; }

		private int _totalAttempts = 0;
		private int _correctAttempts = 0;

		public int TotalAttempts => _totalAttempts;
		public int CorrectAttempts => _correctAttempts;

		public float Accuracy =>
			_totalAttempts == 0 ? 0f : (float)_correctAttempts / _totalAttempts;

		public override void _Ready()
		{
			if (Instance != null)
			{
				QueueFree();
				return;
			}
			Instance = this;
		}

		public void RegisterAttempt(bool success)
		{
			_totalAttempts++;

			if (success)
				_correctAttempts++;

			GD.Print($"[ACCURACY] {_correctAttempts}/{_totalAttempts} ({Accuracy * 100:F1}%)");
		}

		public void Reset()
		{
			_totalAttempts = 0;
			_correctAttempts = 0;
		}
	}
}
