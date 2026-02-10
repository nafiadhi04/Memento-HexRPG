using Godot;
using System;
using MementoTest.Core;

namespace MementoTest.UI
{
    public partial class SaveMenu : Control
    {
        [Export] public PackedScene GameplayScene;

        // UI References - Slots
        private Control _slotContainer;
        private SaveSlotUI _slot1, _slot2, _slot3;

        // UI References - Creation Panel
        private Control _creationPanel;
        private LineEdit _nameInput;
        private OptionButton _classOption;
        private Label _classDescLabel;
        private Button _startButton;
        private Button _cancelButton;
        private Button _backButton;

        // Logic Variables
        private int _selectedSlotForCreation = -1;

        public override void _Ready()
        {
            // [PERBAIKAN 1] Update Path SlotContainer (Ini sudah benar ada di dalam MainLayout)
            _slotContainer = GetNode<Control>("MainLayout/SlotContainer");

            // [PERBAIKAN 2] Update Path BackButton
            // Di screenshot, BackButton adalah anak langsung dari SaveMenu (Root), bukan di dalam MainLayout
            _backButton = GetNode<Button>("BackButton");

            // Ambil referensi slot
            _slot1 = _slotContainer.GetNode<SaveSlotUI>("SaveSlot1");
            _slot2 = _slotContainer.GetNode<SaveSlotUI>("SaveSlot2");
            _slot3 = _slotContainer.GetNode<SaveSlotUI>("SaveSlot3");

            // [PERBAIKAN 3] Jalur Creation Panel (Sesuai Screenshot)
            _creationPanel = GetNode<Control>("CreationPanel");

            // Ambil VBoxContainer di dalam CreationPanel
            var vBox = _creationPanel.GetNode<Control>("VBoxContainer");

            _nameInput = vBox.GetNode<LineEdit>("NameInput");
            _classOption = vBox.GetNode<OptionButton>("ClassOption");
            _classDescLabel = vBox.GetNode<Label>("ClassDescLabel");

            // Tombol ada di dalam ButtonContainer
            var btnContainer = vBox.GetNode<Control>("ButtonContainer");
            _startButton = btnContainer.GetNode<Button>("StartGameButton");
            _cancelButton = btnContainer.GetNode<Button>("CancelButton");

            // --- SETUP SIGNAL (Sama seperti sebelumnya) ---
            _slot1.SlotSelected += OnSlotSelected;
            _slot2.SlotSelected += OnSlotSelected;
            _slot3.SlotSelected += OnSlotSelected;

            _classOption.ItemSelected += OnClassChanged;
            _startButton.Pressed += OnStartGamePressed;
            _cancelButton.Pressed += () => _creationPanel.Visible = false;
            _backButton.Pressed += () => SceneTransition.Instance.ChangeScene("res://scenes/ui/main_menu.tscn");
            // Isi Dropdown Class
            PopulateClassOptions();

            // Sembunyikan panel di awal
            _creationPanel.Visible = false;
        }
        private void PopulateClassOptions()
        {
            _classOption.Clear();
            _classOption.AddItem("Warrior (Melee)");
            _classOption.AddItem("Archer (Ranged)");
            _classOption.AddItem("Mage (Magic)");

            // Trigger update deskripsi pertama kali
            OnClassChanged(0);
        }

        private void OnSlotSelected(int slotIndex, bool isEmpty)
        {
            if (isEmpty)
            {
                // BUKA PANEL CREATION
                _selectedSlotForCreation = slotIndex;
                _nameInput.Text = "";
                _creationPanel.Visible = true;
                _nameInput.GrabFocus(); // Langsung fokus ke ketik nama
            }
            else
            {
                // LANGSUNG LOAD GAME
                GD.Print($"Loading Slot {slotIndex}...");
                GameManager.Instance.LoadGame(slotIndex);
                SceneTransition.Instance.ChangeScene(GameplayScene);
            }
        }

        private void OnClassChanged(long index)
        {
            // Update Deskripsi sesuai request user
            switch (index)
            {
                case 0: // Warrior
                    _classDescLabel.Text = "Role: Warrior\nRange: 1 Grid\nStats: Balanced HP/AP.\nPlaystyle: Close combat specialist.";
                    break;
                case 1: // Archer
                    _classDescLabel.Text = "Role: Archer\nRange: 6 Grids\nStats: Low DMG, Standard AP.\nPlaystyle: Snipe enemies from afar.";
                    break;
                case 2: // Mage
                    _classDescLabel.Text = "Role: Mage\nRange: 3 Grids\nStats: High DMG, High AP Cost.\nPlaystyle: Heavy hitter but consumes energy quickly.";
                    break;
            }
        }

        private void OnStartGamePressed()
        {
            string name = _nameInput.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                // Animasi goyang atau warning jika nama kosong
                _nameInput.Modulate = Colors.Red;
                CreateTween().TweenProperty(_nameInput, "modulate", Colors.White, 0.5f);
                return;
            }

            // Ambil Class dari Dropdown (Urutan Enum harus sama dengan urutan AddItem)
            PlayerClassType selectedClass = (PlayerClassType)_classOption.Selected;

            // SIMPAN DATA BARU
            GameManager.Instance.CreateNewSave(_selectedSlotForCreation, name, selectedClass);

            // MASUK GAMEPLAY
            SceneTransition.Instance.ChangeScene(GameplayScene);
        }
    }
}