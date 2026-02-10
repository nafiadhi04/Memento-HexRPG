using Godot;
using System;

namespace MementoTest.UI
{
    public partial class MainMenu : Control
    {
        [Export] public PackedScene GameplayScene;

        private Button _playButton;
        private Button _quitButton;

        public override void _Ready()
        {
            _playButton = GetNode<Button>("MenuContainer/PlayButton");
            _quitButton = GetNode<Button>("MenuContainer/QuitButton");

            _playButton.Pressed += OnPlayPressed;
            _quitButton.Pressed += OnQuitPressed;
        }

        private void OnPlayPressed()
        {
            if (GameplayScene == null)
            {
                GD.PrintErr("GameplayScene belum di-assign!");
                return;
            }

            GD.Print("Tombol Play Ditekan! Memanggil Transisi..."); // TAMBAHAN DEBUG

            // Cek apakah Instance null?
            if (SceneTransition.Instance == null)
            {
                GD.PrintErr("CRITICAL ERROR: SceneTransition Autoload belum aktif!");
                // Fallback darurat jika transisi rusak
                GetTree().ChangeSceneToPacked(GameplayScene);
                return;
            }

            SceneTransition.Instance.ChangeScene(GameplayScene);
        }

        private void OnQuitPressed()
        {
            // Opsional: Bisa dikasih efek fade out dulu sebelum quit
            QuitGameWithFade();
        }

        private async void QuitGameWithFade()
        {
            // Animasi manual sedikit sebelum keluar
            Tween t = CreateTween();
            t.TweenProperty(this, "modulate:a", 0.0f, 0.5f);
            await ToSignal(t, "finished");
            GetTree().Quit();
        }
    }
}