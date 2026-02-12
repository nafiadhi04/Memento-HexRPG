using Godot;
using System;
using MementoTest.Core;

namespace MementoTest.UI
{
    public partial class SaveMenu : Control
    {
        [Export] public PackedScene GameplayScene;
        

        // UI References - Slots & Delete Buttons
        private Control _slotContainer;
        private SaveSlotUI _slot1, _slot2, _slot3;
        private Button _deleteBtn1, _deleteBtn2, _deleteBtn3;

        // UI References - Creation Panel
        private Control _creationPanel;
        private LineEdit _nameInput;
        private OptionButton _classOption;
        private Label _classDescLabel;
        private Button _startButton;
        private Button _cancelButton;
        private Button _backButton;

        // UI References - Popups
        private ConfirmationDialog _deleteConfirmPopup;

        // Logic Variables
        private int _selectedSlotForCreation = -1;
        private int _slotToDelete = -1;

        public override void _Ready()
        {
            // --- 1. SETUP UI REFERENCES ---
            _slotContainer = GetNode<Control>("MainLayout/SlotContainer");
            _backButton = GetNode<Button>("BackButton");

            // [FIX 1] Sesuaikan nama node Popup persis dengan gambar (PopUp)
            _deleteConfirmPopup = GetNode<ConfirmationDialog>("DeleteConfirmPopUp");

            // Ambil referensi slot dulu
            _slot1 = _slotContainer.GetNode<SaveSlotUI>("SaveSlot1");
            _slot2 = _slotContainer.GetNode<SaveSlotUI>("SaveSlot2");
            _slot3 = _slotContainer.GetNode<SaveSlotUI>("SaveSlot3");

            // [FIX 2] Ambil tombol Delete dari DALAM node SaveSlot masing-masing
            // Dan sesuaikan penulisan hurufnya (Deletebtn1)
            _deleteBtn1 = _slot1.GetNode<Button>("Deletebtn1");
            _deleteBtn2 = _slot2.GetNode<Button>("Deletebtn2");
            _deleteBtn3 = _slot3.GetNode<Button>("Deletebtn3");

            // Setup Creation Panel (Ini sudah benar)
            _creationPanel = GetNode<Control>("CreationPanel");
            var vBox = _creationPanel.GetNode<Control>("VBoxContainer");
            _nameInput = vBox.GetNode<LineEdit>("NameInput");
            _classOption = vBox.GetNode<OptionButton>("ClassOption");
            _classDescLabel = vBox.GetNode<Label>("ClassDescLabel");

            var btnContainer = vBox.GetNode<Control>("ButtonContainer");
            _startButton = btnContainer.GetNode<Button>("StartGameButton");
            _cancelButton = btnContainer.GetNode<Button>("CancelButton");

            // --- 2. SETUP SIGNALS ---
            _slot1.SlotSelected += OnSlotSelected;
            _slot2.SlotSelected += OnSlotSelected;
            _slot3.SlotSelected += OnSlotSelected;

            _deleteBtn1.Pressed += () => OnDeleteButtonPressed(1);
            _deleteBtn2.Pressed += () => OnDeleteButtonPressed(2);
            _deleteBtn3.Pressed += () => OnDeleteButtonPressed(3);

            _deleteConfirmPopup.Confirmed += OnConfirmDelete;

            _classOption.ItemSelected += OnClassChanged;
            _startButton.Pressed += OnStartGamePressed;
            _cancelButton.Pressed += () => _creationPanel.Visible = false;
            _backButton.Pressed += () => SceneTransition.Instance.ChangeScene("res://scenes/ui/main_menu.tscn");

            // --- 3. INITIALIZATION ---
            PopulateClassOptions();
            _creationPanel.Visible = false;

            RefreshDeleteButtons();
        }

        // Fungsi baru untuk mengatur visibilitas tombol delete
        private void RefreshDeleteButtons()
        {
            _deleteBtn1.Visible = GameManager.Instance.SaveExists(1);
            _deleteBtn2.Visible = GameManager.Instance.SaveExists(2);
            _deleteBtn3.Visible = GameManager.Instance.SaveExists(3);

            // Opsional: Refresh juga teks slot agar sinkron
            _slot1.RefreshUI();
            _slot2.RefreshUI();
            _slot3.RefreshUI();
        }

        private void PopulateClassOptions()
        {
            _classOption.Clear();
            _classOption.AddItem("Warrior (Melee)");
            _classOption.AddItem("Archer (Ranged)");
            _classOption.AddItem("Mage (Magic)");
            OnClassChanged(0);
        }

        // --- DELETE LOGIC ---

        private void OnDeleteButtonPressed(int slotIndex)
        {
            _slotToDelete = slotIndex;
            _deleteConfirmPopup.DialogText = $"Are you sure you want to delete Save Slot {slotIndex}?\nThis action cannot be undone.";
            _deleteConfirmPopup.PopupCentered();
        }

        private void OnConfirmDelete()
        {
            if (_slotToDelete != -1)
            {
                // Panggil GameManager untuk hapus file
                GameManager.Instance.DeleteSave(_slotToDelete);

                // Refresh UI agar tombol delete hilang & slot jadi "Empty"
                RefreshDeleteButtons();

                _slotToDelete = -1;
            }
        }

        // --- SAVE/LOAD LOGIC ---

        private void OnSlotSelected(int slotIndex, bool isEmpty)
        {
            if (isEmpty)
            {
                // NEW GAME: Buka Panel Creation
                _selectedSlotForCreation = slotIndex;
                _nameInput.Text = "";
                _creationPanel.Visible = true;
                _nameInput.GrabFocus();
            }
            else
            {
                // LOAD GAME
                GD.Print($"Loading Slot {slotIndex}...");
                GameManager.Instance.LoadGame(slotIndex);
                SceneTransition.Instance.ChangeScene(GameplayScene);
            }
        }

        private void OnClassChanged(long index)
        {
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
                _nameInput.Modulate = Colors.Red;
                CreateTween().TweenProperty(_nameInput, "modulate", Colors.White, 0.5f);
                return;
            }

            PlayerClassType selectedClass = (PlayerClassType)_classOption.Selected;

            // Create Save & Refresh UI Buttons (tombol delete akan muncul untuk slot ini)
            GameManager.Instance.CreateNewSave(_selectedSlotForCreation, name, selectedClass);

            // Langsung masuk game
            SceneTransition.Instance.ChangeScene(GameplayScene);
        }
    }
}