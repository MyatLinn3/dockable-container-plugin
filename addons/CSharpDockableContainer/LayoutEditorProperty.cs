#if TOOLS
using Godot;
using System;


public partial class LayoutEditorProperty : EditorProperty
{
	public DockableContainer container = new DockableContainer();
	public MenuButton hiddenMenuButton = new MenuButton();
	public PopupMenu hiddenMenuPopup;
	public Godot.Collections.Array<string> hiddenMenuList;


	public LayoutEditorProperty()
	{
	}
	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(128,256);
		hiddenMenuButton.Text = "Visible nodes";
		AddChild(hiddenMenuButton);

		hiddenMenuPopup = hiddenMenuButton.GetPopup();
		hiddenMenuPopup.HideOnCheckableItemSelection = false;
		hiddenMenuPopup.AboutToPopup += OnHiddenMenuPopupAboutToShow;
		hiddenMenuPopup.IdPressed += OnHiddenMenuPopupIdPressed;
//		hiddenMenuPopup.connect("about_to_popup",this,"OnHiddenMenuPopupAboutToShow");
//		hiddenMenuPopup.connect("id_pressed",this,"OnHiddenMenuPopupIdPressed");

		container.CloneLayoutOnReady = false;
		container.CustomMinimumSize = CustomMinimumSize;

		DockableLayout @value = GetLayout().Clone();
		foreach(String n in @value.GetNames())
		{
			Label child = CreateChildControl(n);
			container.AddChild(child);
		}
		container.Set(GetEditedProperty(),@value);
		AddChild(container);
		SetBottomEditor(container);
	}

	public override void _ExitTree()
	{
		QueueFree();
	}
	
	public override void _UpdateProperty()
	{
		DockableLayout value = GetLayout();
		container.Set("Layout",value);
	}

	public DockableLayout GetLayout()
	{
		DockableContainer originalContainer = (DockableContainer)GetEditedObject();
		return (DockableLayout)originalContainer.Get("Layout");
	}

	public Label CreateChildControl(string named)
	{
		Label newControl = new Label();
		newControl.Name = named;
		newControl.HorizontalAlignment = HorizontalAlignment.Center;
//		 HORIZONTAL_ALIGNMENT_CENTER;
		newControl.VerticalAlignment = VerticalAlignment.Center;
		newControl.ClipText = true;
		newControl.Text = named;
		return newControl;
	}

	private void OnHiddenMenuPopupAboutToShow()
	{
		DockableLayout layout = GetLayout().Clone();
		hiddenMenuPopup.Clear();
		hiddenMenuList = layout.GetNames();
		for(int i = 0;i < hiddenMenuList.Count;i++)
		{
			string tabName = hiddenMenuList[i];
			hiddenMenuPopup.AddCheckItem(tabName,i);
			hiddenMenuPopup.SetItemChecked(i,!layout.IsTabHidden(tabName));
		}
	}

	private void OnHiddenMenuPopupIdPressed(long id)
	{
		DockableLayout layout = GetLayout().Clone();
		string tabName = hiddenMenuList[(int)id];
		bool newHidden = !layout.IsTabHidden(tabName);
		GetLayout().SetTabHidden(tabName,newHidden);
		hiddenMenuPopup.SetItemChecked((int)id,!newHidden);
	}
	
}
#endif
