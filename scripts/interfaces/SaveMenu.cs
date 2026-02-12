using Godot;
using System;
using MementoTest.Core;
using System.Linq;

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
                string savePath = GameManager.Instance.GetSavePath(slotIndex);
                var data = ResourceLoader.Load<SaveData>(savePath);

                if (data.IsVictory)
                {
                    GD.Print($"Slot {slotIndex} Tamat. Memulai New Game+ Berdasarkan Config...");

                    // 1. Ambil Nama Kelas (Warrior/Archer/Mage) dari data save
                    string className = data.ClassType.ToString();

                    // 2. Load Config JSON
                    var configData = LoadGameConfig();

                    // [FIX] Menggunakan ContainsKey untuk Godot.Collections.Dictionary
                    if (configData != null && configData.ContainsKey("classes"))
                    {
                        var classes = configData["classes"].AsGodotDictionary();

                        if (classes.ContainsKey(className))
                        {
                            var classStats = classes[className].AsGodotDictionary();

                            // --- RESET SESUAI CONFIG ---
                            // Mengambil nilai max dari JSON untuk HP dan AP
                            data.CurrentHP = (int)classStats["max_hp"];
                            data.CurrentAP = (int)classStats["max_ap"];
                        }
                    }

                    // 3. Reset Status Sesi Lainnya (Data Progress)
                    data.IsVictory = false;           // Agar bisa menamatkan game lagi
                    data.PlayerPosition = Vector2.Zero; // Kembali ke titik awal
                    data.IsEnemyDead = false;         // Reset status musuh di level
                    data.CurrentScore = 0;            // Reset skor sesi (Highscore tetap aman)

                    // Note: data.UnlockedSkills dan data.HighScore TIDAK di-reset di sini
                    // agar progress permanen terbawa ke sesi baru.

                    // 4. Simpan Perubahan & Pindah Scene
                    ResourceSaver.Save(data, savePath);
                    GameManager.Instance.LoadGame(slotIndex);
                    SceneTransition.Instance.ChangeScene(GameplayScene);
                }
                else
                {
                    // LOAD GAME BIASA (CONTINUE)
                    GD.Print($"Loading Slot {slotIndex} (Continue)...");
                    GameManager.Instance.LoadGame(slotIndex);
                    SceneTransition.Instance.ChangeScene(GameplayScene);
                }
            }
        }

        /// <summary>
        /// Fungsi pembantu untuk memuat file game_config.json
        /// </summary>
        private Godot.Collections.Dictionary LoadGameConfig()
        {
            string path = "res://config/game_config.json"; // Pastikan path file benar

            if (!FileAccess.FileExists(path))
            {
                GD.PrintErr($"File config tidak ditemukan di: {path}");
                return null;
            }

            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            string jsonString = file.GetAsText();

            var json = new Json();
            var error = json.Parse(jsonString);

            if (error == Error.Ok)
            {
                return json.Data.AsGodotDictionary();
            }
            else
            {
                GD.PrintErr($"Gagal parse JSON: {json.GetErrorMessage()} pada baris {json.GetErrorLine()}");
                return null;
            }
        }

        // Fungsi Helper untuk baca JSON

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

            // Pastikan GameManager create save menerima parameter yang sesuai
            GameManager.Instance.CreateNewSave(_selectedSlotForCreation, name, selectedClass);

            SceneTransition.Instance.ChangeScene(GameplayScene);
        }
    }
}