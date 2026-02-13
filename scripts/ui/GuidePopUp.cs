using Godot;
using System.Collections.Generic;

public partial class GuidePopUp : CanvasLayer
{
	[Export] private Label TitleLabel;
	[Export] private Control PageContainer;
	[Export] private Label PageIndicator;
	[Export] private Button PrevButton;
	[Export] private Button NextButton;
	[Export] private Button CloseButton;


	private List<Control> _pages = new();
	private int _currentPage = 0;

	public override void _Ready()
	{
		foreach (Node child in PageContainer.GetChildren())
		{
			if (child is Control page)
				_pages.Add(page);
		}

		PrevButton.Pressed += OnPrev;
		NextButton.Pressed += OnNext;

		if (CloseButton != null)
			CloseButton.Pressed += Close;

		UpdatePage();
	}


	private void OnNext()
	{
		if (_currentPage < _pages.Count - 1)
		{
			_currentPage++;
			UpdatePage();
		}
	}

	private void OnPrev()
	{
		if (_currentPage > 0)
		{
			_currentPage--;
			UpdatePage();
		}
	}
	

	private void UpdatePage()
	{
		for (int i = 0; i < _pages.Count; i++)
			_pages[i].Visible = (i == _currentPage);

		PageIndicator.Text = $"Page {_currentPage + 1}/{_pages.Count}";

		TitleLabel.Text = _currentPage switch
		{
			0 => "CONTROLS",
			1 => "COMBAT SYSTEM",
			2 => "CLASS COMMANDS",
			3 => "LOSE CONDITION",
			4 => "WIN CONDITION",
			_ => ""
		};

		PrevButton.Disabled = _currentPage == 0;
		NextButton.Disabled = _currentPage == _pages.Count - 1;
	}

	public void Open()
	{
		Visible = true;
		_currentPage = 0;
		UpdatePage();
	}

	public void Close()
	{
		_currentPage = 0;
		Visible = false;
	}

}
