using Godot;

[Tool]
public partial class DockablePanel : TabContainer
{

	[Signal]
	public delegate void TabLayoutChangedEventHandler(int tab);

	public DockableLayoutPanel Leaf
	{
		get => GetLeaf();
		set => SetLeaf(value);
	}

	public DockableLayoutPanel _Leaf;
	
	public Godot.Collections.Array<string> displayNames = new Godot.Collections.Array<string>();
	public override void _Ready()
	{
		DragToRearrangeEnabled = true;
	}

	public override void _EnterTree()
	{
		ActiveTabRearranged += _OnTabChanged;
		TabSelected += _OnTabSelected;
		TabChanged += _OnTabChanged;

	//	Connect("active_tab_rearranged",this,"_OnTabChanged");
//		Connect("tab_selected",this,"_OnTabSelected");
//		Connect("tab_changed",this,"_OnTabChanged");
	}

	public override void _ExitTree()
	{
		ActiveTabRearranged -= _OnTabChanged;
		TabSelected -= _OnTabSelected;
		TabChanged -= _OnTabChanged;
//		Disconnect("active_tab_rearranged",this,"_OnTabChanged");
//		Disconnect("tab_selected",this,"_OnTabSelected");
//		Disconnect("tab_changed",this,"_OnTabChanged");
	}

	public void TrackNodes(Godot.Collections.Array<Control> nodes,DockableLayoutPanel newLeaf)
	{
		_Leaf = null;
		var minSize = Mathf.Min(nodes.Count,GetChildCount());
		for (var i =minSize;i < GetChildCount();i++)
		{
			var child = GetChild(minSize) as DockableReferenceControl;
			child.ReferenceTo = null;
			RemoveChild(child);
			child.QueueFree();
		}
		for (var i =minSize;i < nodes.Count;i++)
		{
			Node refControl = new DockableReferenceControl();
			AddChild(refControl);
		} 
		if (nodes.Count != GetChildCount())
		{
			GD.PrintErr("FixMe!");
		}
		GD.Print(nodes.Count);
		for (var i =0;i < nodes.Count;i++)
		{
			var refControl = GetChild(i) as DockableReferenceControl;
			refControl.ReferenceTo = nodes[i];
			displayNames = newLeaf.changeNameOfTabs;
			if (displayNames.Count != nodes.Count)
			{
				displayNames.Add(nodes[i].Name);
			}
			SetTabTitle(i,displayNames[i]);
		}
		SetLeaf(newLeaf);
	}

	public Rect2 GetChildRect()
	{
		var control = GetCurrentTabControl();
		return new Rect2(Position + control.Position,control.Size);
	}

	public void SetLeaf(DockableLayoutPanel @value)
	{
		if (GetTabCount() > 0 && @value != null)
		{
			CurrentTab = Mathf.Clamp(@value.CurrentTab,0,GetTabCount()-1);
		}
		_Leaf = value;
		_Leaf.SetCurrentTabTitle += _SetCurrentTabTitle;
	}

	public DockableLayoutPanel GetLeaf()
	{
		return _Leaf;
	}

	public Vector2 GetLayoutMinimumSize()
	{
		return GetCombinedMinimumSize();
	}

	public void _OnTabSelected(long tab)
	{
		if (_Leaf != null)
		{
			_Leaf.CurrentTab = (int)tab;
			_Leaf.EmitSignal("GetCurrentTabTitle",displayNames);
		}
	}

	public void _SetCurrentTabTitle(Godot.Collections.Array<string> names)
	{
		if (_Leaf != null)
		{
			displayNames = names;
			for(var i=0;i < GetTabCount();i++)
			{
				SetTabTitle(CurrentTab,names[i]);
			}
		}

	}
	public void _OnTabChanged(long tab)
	{
		switch (_Leaf)
		{
			case null:
				return;
		}
		var control = GetTabControl((int)tab);
		switch (control)
		{
			case null:
				return;
		}
		string tabName = control.Name;
		var nameIndexInLeaf = _Leaf.FindName(tabName);
		if (nameIndexInLeaf != tab)
		{
			EmitSignal(SignalName.TabLayoutChanged,tab);
		}
	}
}
