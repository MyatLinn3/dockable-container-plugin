#if TOOLS
using Godot;


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

		var @value = GetLayout().Clone();
		foreach(var n in @value.GetNames())
		{
			var child = CreateChildControl(n);
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
		var value = GetLayout();
		container.Set("Layout",value);
	}

	public DockableLayout GetLayout()
	{
		var originalContainer = (DockableContainer)GetEditedObject();
		return (DockableLayout)originalContainer.Get("Layout");
	}

	public Label CreateChildControl(string named)
	{
		var newControl = new Label();
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
		var layout = GetLayout().Clone();
		hiddenMenuPopup.Clear();
		hiddenMenuList = layout.GetNames();
		for(var i = 0;i < hiddenMenuList.Count;i++)
		{
			var tabName = hiddenMenuList[i];
			hiddenMenuPopup.AddCheckItem(tabName,i);
			hiddenMenuPopup.SetItemChecked(i,!layout.IsTabHidden(tabName));
		}
	}

	private void OnHiddenMenuPopupIdPressed(long id)
	{
		var layout = GetLayout().Clone();
		var tabName = hiddenMenuList[(int)id];
		var newHidden = !layout.IsTabHidden(tabName);
		GetLayout().SetTabHidden(tabName,newHidden);
		hiddenMenuPopup.SetItemChecked((int)id,!newHidden);
	}
	
}
#endif
