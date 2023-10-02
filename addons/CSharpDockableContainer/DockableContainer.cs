using Godot;

[Tool]
public partial class DockableContainer : Container
{

	[Export]
	public TabBar.AlignmentMode TabAlignment 
	{
		get => _TabAlign;
		set
		{
			_TabAlign = value;
			for (var i = 1;i<_PanelContainer.GetChildCount();i++)
			{
				var panel =  _PanelContainer.GetChild(i) as DockablePanel;
				panel.TabAlignment = value;
			}
		}
	} 

	[Export]
	public bool UseHiddenTabsForMinSize
	{
		get => _UseHiddenTabsForMinSize;
		set
		{
			_UseHiddenTabsForMinSize = value;
			for (var i = 1;i<_PanelContainer.GetChildCount();i++)
			{
				var panel =  _PanelContainer.GetChild(i) as DockablePanel;
				panel.UseHiddenTabsForMinSize = value;
			}
		}
	}

	[Export]
	public bool TabsVisible
	{
		get => _TabsVisible;
		set
		{
			_TabsVisible = value;
			for (var i = 1;i<_PanelContainer.GetChildCount();i++)
			{
				var panel = _PanelContainer.GetChild(i) as DockablePanel;
				panel.TabsVisible = value;
			}
		}
	}

	[Export]
	public int RearrangeGroup = 0;


	[Export]
	public DockableLayout Layout
	{
		get => _Layout;
		set => SetLayout(value);
	}

	[Export]
	public bool CloneLayoutOnReady = true;

	private DockableLayout _Layout = new DockableLayout();
	private Container _PanelContainer = new Container();
	private Container _SplitContainer = new Container();
	private DragNDropPanel _DragNDropPanel = new DragNDropPanel();
	private DockablePanel _DragPanel;
	private TabBar.AlignmentMode _TabAlign = TabBar.AlignmentMode.Center;
	private bool _TabsVisible = true;
	private bool _UseHiddenTabsForMinSize = false;
	private int _CurrentPanelIndex = 0;
	private int _CurrentSplitIndex = 0;
	private Godot.Collections.Dictionary _ChildrenNames= new Godot.Collections.Dictionary();
	private bool _LayoutDirty = false;

	public DockableContainer()
	{
		ChildEnteredTree += _ChildEnteredTree;
		ChildExitingTree += _ChildExitingTree;
	}
	public override void _Ready()
	{
		SetProcessInput(false);
		_PanelContainer.Name = "_PanelContainer";
		AddChild(_PanelContainer);
		MoveChild(_PanelContainer,0);
		_SplitContainer.Name = "_SplitContainer";
		_SplitContainer.MouseFilter = Control.MouseFilterEnum.Pass;
		_PanelContainer.AddChild(_SplitContainer);

		_DragNDropPanel.Name = "_DragNDropPanel";
		_DragNDropPanel.MouseFilter = Control.MouseFilterEnum.Pass;
		_DragNDropPanel.Visible = false;
		AddChild(_DragNDropPanel);

		switch (_Layout)
		{
			case null:
				SetLayout(null);
				break;
			default:
			{
				switch (CloneLayoutOnReady)
				{
					case true when !Engine.IsEditorHint():
						SetLayout(_Layout.Clone());
						break;
				}

				break;
			}
		}
	}

	public override void _Notification(int what)
	{
		if (what == NotificationSortChildren)
		{
			_Resort();
		}
		else if (what == NotificationDragBegin && _CanHandleDragData(GetViewport().GuiGetDragData()))
		{
			_DragNDropPanel.SetEnabled(true,!_Layout.Root.IsEmpty());
			SetProcessInput(true);
		}
		else if (what == NotificationDragEnd)
		{
			_DragNDropPanel.SetEnabled(false);
			SetProcessInput(false);
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (!GetViewport().GuiIsDragging())
		{
			GD.PrintErr("FIXME: should only be called when dragging");
		}

		switch (@event)
		{
			case InputEventMouseMotion:
			{
				var localPosition = GetLocalMousePosition();
				DockablePanel panel = null;
				for (var i = 1;i<_PanelContainer.GetChildCount();i++)
				{
					var p =  _PanelContainer.GetChild(i) as DockablePanel;
					if (p.GetRect().HasPoint(localPosition))
					{
						panel = p;
						break;
					}
				}
				_DragPanel = panel;
				switch (panel)
				{
					case null:
						return;
				}
				FitChildInRect(_DragNDropPanel,panel.GetChildRect());
				break;
			}
		}
	}

	public void _ChildEnteredTree(Node node)
	{
		if (node == _PanelContainer || node == _DragNDropPanel)
		{
			return;
		}
		_DragNDropPanel.MoveToFront();
		_TrackAndAddNode(node);
	}

	public void _ChildExitingTree(Node node)
	{
		if (node == _PanelContainer || node == _DragNDropPanel)
		{
			return;
		}
		_UntrackNode(node);
	}

	public override bool _CanDropData(Vector2 _position,Variant data)
	{
		return _CanHandleDragData(data);
	}

	public override void _DropData(Vector2 _position,Variant data)
	{
		var fromNode = GetNode((NodePath)((Godot.Collections.Dictionary)data)["from_path"]) as DockablePanel;
		if (fromNode == _DragPanel && _DragPanel.GetChildCount() == 1)
		{
			return;
		}
		var movedTab = fromNode.GetTabControl((int)((Godot.Collections.Dictionary)data)["tabc_element"]);
		switch (movedTab)
		{
			case DockableReferenceControl control:
				movedTab = control.ReferenceTo;
				break;
		}
		if (!_IsManagedNode(movedTab))
		{
			movedTab.GetParent().RemoveChild(movedTab);
			AddChild(movedTab);
		}
		if (_DragPanel != null)
		{
			var margin = _DragNDropPanel.GetHoverMargin();
			_Layout.SplitLeafWithNode(_DragPanel.Leaf,movedTab,margin);
		}
		_LayoutDirty = true;
		QueueSort();
	}

	public void SetControlAsCurrentTab(Control control)
	{
		if (control.GetParentControl() != this)
		{
			GD.PrintErr("Trying to focus a control not managed by this container");
		}
		if (IsControlHidden(control))
		{
			GD.PushWarning("Trying to focus a hidden control");
			return;
		}
		var leaf = _Layout.GetLeafForNode(control);
		switch (leaf)
		{
			case null:
				return;
		}
		var positionInLeaf = leaf.FindChild(control);
		switch (positionInLeaf)
		{
			case < 0:
				return;
		}
		DockablePanel panel = null;
		for (var i = 1;i<_PanelContainer.GetChildCount();i++)
		{
			var p = _PanelContainer.GetChild(i) as DockablePanel;
			if (p.Leaf == leaf)
			{
				panel = p;
				break;
			}
		}
		switch (panel)
		{
			case null:
				return;
			default:
				panel.CurrentTab = Mathf.Clamp(positionInLeaf,0,panel.GetTabCount()-1);
				break;
		}
	}

	public void SetLayout(DockableLayout value)
	{
		switch (value)
		{
			case null:
				value = new DockableLayout();
				break;
		}
		if (value == _Layout)
		{
			return;
		}
		if (_Layout != null && _Layout.IsConnected("changed",new Callable(this,"QueueSort")))
		{
			_Layout.Changed -= QueueSort;
		}
		_Layout = value;
		_Layout.Changed += QueueSort;
		_LayoutDirty = true;
		QueueSort();
	}

	public void SetTabAlignment(TabBar.AlignmentMode value)
	{
		_TabAlign = value;
		for (var i = 1;i<_PanelContainer.GetChildCount();i++)
		{
			var panel = _PanelContainer.GetChild(i);
			(panel as TabContainer).TabAlignment= value;
		}
	}

	public int GetTabAlign()
	{
		return (int)_TabAlign;
	}

	public void SetUseHiddenTabsForMinSize(bool value)
	{
		_UseHiddenTabsForMinSize = value;
		for (var i = 1;i<_PanelContainer.GetChildCount();i++)
		{
			var panel = _PanelContainer.GetChild(i) as DockableContainer;
			panel.UseHiddenTabsForMinSize = value;
		}
	}

	public bool GetUseHiddenTabsForMinSize()
	{
		return _UseHiddenTabsForMinSize;
	}

	public void SetControlHidden(Control child,bool isHidden)
	{
		_Layout.SetNodeHidden(child,isHidden);
	}

	public bool IsControlHidden(Control child)
	{
		return _Layout.IsNodeHidden(child);
	}

	public Godot.Collections.Array<Control> GetTabs()
	{
		var tabs = new Godot.Collections.Array<Control>();
		for (var i = 0;i<GetChildCount();i++)
		{
			var child = GetChild(i) as Control;
			if (_IsManagedNode(child))
			{
				tabs.Add(child);
			}
		}
		return tabs;
	}

	public int GetTabCount()
	{
		var count = 0;
		for (var i = 0;i<GetChildCount();i++)
		{
			var child = GetChild(i);
			if (_IsManagedNode(child))
			{
				count += 1;
			}
		}
		return count;
	}

	public bool _CanHandleDragData(Variant data)
	{
		switch ((string)(((Godot.Collections.Dictionary) data)["type"]))
		{
			case "tabc_element":
			{
				GD.Print(data);
				var tabc = GetNodeOrNull((NodePath)((Godot.Collections.Dictionary)data)["from_path"]);
				return (
					tabc != null 
					&& (tabc as TabContainer).TabsRearrangeGroup == RearrangeGroup
				);
			}
			default:
				return false;
		}
	}

	public bool _IsManagedNode(Node node)
	{
		return(
			node.GetParent() == this
			&& node != _PanelContainer
			&& node != _DragNDropPanel
			&& node is Control
			&& ((Control)node).TopLevel != null
		);
	}

	public void _UpdateLayoutWithChildren()
	{
		var names = new Godot.Collections.Array<string>();
		_ChildrenNames.Clear();
		for (var i = 1;i<GetChildCount()-1;i++)
		{
			var c = GetChild(i);
			if (_TrackNode(c))
			{
				names.Add(c.Name);
			}
		}
		_Layout.UpdateNodes(names);
		_LayoutDirty = false;
	}

	public bool _TrackNode(Node node)
	{
		if (!_IsManagedNode(node))
		{
			return false;
		}
		_ChildrenNames[node] = node.Name;
		_ChildrenNames[node.Name] = node;
		if (!node.IsConnected("renamed",new Callable(this,"_OnChildRenamed")))
		{
			node.Renamed += () => _OnChildRenamed(node);
		}
		if (!node.IsConnected("tree_exiting",new Callable(this,"_UntrackNode")))
		{
			node.TreeExiting += () => _UntrackNode(node);
		}
		return true;
	}

	public void _TrackAndAddNode(Node node)
	{
		string trackedName = null;
		if (_ChildrenNames.ContainsKey(node)){
			trackedName = (string)_ChildrenNames[node];
		}
		if (!_TrackNode(node))
		{
			return;
		}
		if (trackedName != null && trackedName != node.Name)
		{
			_Layout.RenameNode(trackedName,node.Name);
		}
		_LayoutDirty = true;
	}

	public void _UntrackNode(Node node)
	{
		_ChildrenNames.Remove(node);
		_ChildrenNames.Remove(node.Name);
		if (node.IsConnected("renamed",new Callable(this,"_OnChildRenamed")))
		{
			node.Renamed -= () => _OnChildRenamed(node);
		}
		if (node.IsConnected("tree_exiting",new Callable(this,"_UntrackNode")))
		{
			node.TreeExiting -= () => _UntrackNode(node);
		}
		_LayoutDirty = true;
	}

	public void _Resort()
	{
		switch (_PanelContainer)
		{
			case null:
				GD.PrintErr("FIXME: resorting without _panel_container");
				break;
		}
			if (_PanelContainer.GetIndex() != 0)
			{
				MoveChild(_PanelContainer,0);
			} 
			if (_DragNDropPanel.GetIndex() < GetChildCount()-1)
			{
				_DragNDropPanel.MoveToFront();
			}

			switch (_LayoutDirty)
			{
				case true:
					_UpdateLayoutWithChildren();
					break;
			}

			var rect = new Rect2(Vector2.Zero,Size);
			FitChildInRect(_PanelContainer,rect);
			_PanelContainer.FitChildInRect(_SplitContainer,rect);

			_CurrentPanelIndex = 1;
			_CurrentSplitIndex = 0;
			var childrenList = new Godot.Collections.Array();
			_CalculatePanelAndSplitList(childrenList,_Layout.Root);
			_FitPanelAndSplitListToRect(childrenList,rect,new Godot.Collections.Array<SplitHandle>(),new Godot.Collections.Array());

			_UntrackChildrenAfter(_PanelContainer,_CurrentPanelIndex);
			_UntrackChildrenAfter(_SplitContainer,_CurrentSplitIndex);
	}


	public Variant _CalculatePanelAndSplitList(Godot.Collections.Array result,DockableLayoutNode layoutNode)
	{
		switch (layoutNode)
		{
			case DockableLayoutPanel layoutPanel:
			{
				var nodes = new Godot.Collections.Array<Control>();
				foreach (var n in layoutPanel.Names)
				{
					var node = (Control)_ChildrenNames[n];
					if (node != null)
					{
						switch ((node is Control))
						{
							case false:
								GD.PrintErr($"FIXME: node is not a control {node}");
								break;
						}
						if (node.GetParent() != this)
						{
							GD.PrintErr($"FIXME: node is not child of container {node}");
						}
						if (IsControlHidden(node))
						{
							node.Visible = false;
						}
						else
						{
							nodes.Add(node);
						}
					}
				}
				switch (nodes.Count)
				{
					case 0:
						return 0;
					default:
					{
						var panel = _GetPanel(_CurrentPanelIndex);
						_CurrentPanelIndex += 1;
						panel.TrackNodes(nodes,layoutPanel);
						result.Add(panel);
						return panel;
					}
				}
			}
			case DockableLayoutSplit node:
			{
				var secondResult = (Control) _CalculatePanelAndSplitList(result,node.Second);
				var firstResult = (Control) _CalculatePanelAndSplitList(result,node.First);
				if (firstResult != null && secondResult != null)
				{
					var split = _GetSplit(_CurrentSplitIndex);
					_CurrentSplitIndex += 1;
					split.LayoutSplit = node;
					switch (firstResult)
					{
						case DockablePanel panel:
							split.FirstMinimumSize = panel.GetLayoutMinimumSize();
							break;
						case SplitHandle handle:
							split.FirstMinimumSize = handle.GetLayoutMinimumSize();
							break;
					}
					switch (secondResult)
					{
						case DockablePanel panel:
							split.SecondMinimumSize = panel.GetLayoutMinimumSize();
							break;
						case SplitHandle handle:
							split.SecondMinimumSize = handle.GetLayoutMinimumSize();
							break;
					}
					result.Add(split);
					return split;
				}
				else if (firstResult != null)
				{
					return firstResult;
				}
				else
				{
					return secondResult;
				}
			}
			default:
				GD.PushWarning($"FIXME: invalid Resource, should be branch or leaf, found {layoutNode}");
				break;
		}

		return 0;
	}

	public void _FitPanelAndSplitListToRect(Godot.Collections.Array panelAndSplitList,Rect2 rect,Godot.Collections.Array<SplitHandle> ps,Godot.Collections.Array count)
	{
		if (panelAndSplitList.Count != 0)
		{
			var control = (Control)panelAndSplitList[^1];
		panelAndSplitList.RemoveAt(panelAndSplitList.Count - 1);
		switch (control)
		{
			case DockablePanel:
				_PanelContainer.FitChildInRect(control,rect);
				break;
			case SplitHandle handle:
			{
				var splitRects = new Godot.Collections.Dictionary<string,Rect2>();
				switch (ps.Count)
				{
					case >= 1 when (ps[ps.Count-1] as SplitHandle).LayoutSplit.First == handle.LayoutSplit && ps[ps.Count-1].Dragging && ps[ps.Count-1].LayoutSplit.IsHorizontal() == handle.LayoutSplit.IsHorizontal():
					{
						var no = handle.LayoutSplit;
						while (no != null)
						{
							if (no.Second != null && no.Second is DockableLayoutSplit)
							{
								no = (DockableLayoutSplit)no.Second;
								count.Add("second");
							}
							else
							{
								no = null;
							}
						}
						splitRects = handle.GetSplitRects(rect,"second");
						break;
					}
					case >= 1 when (ps[ps.Count-1] as SplitHandle).LayoutSplit.Second == handle.LayoutSplit && ps[ps.Count-1].Dragging && ps[ps.Count-1].LayoutSplit.IsHorizontal() == handle.LayoutSplit.IsHorizontal():
					{
						var no = handle.LayoutSplit;
						while (no != null)
						{
							if (no.Second != null && no.Second is DockableLayoutSplit)
							{
								no = (DockableLayoutSplit)no.First;
								count.Add("first");
							}
							else
							{
								no = null;
							}
						}
						splitRects = handle.GetSplitRects(rect,"first");
						break;
					}
					case >= 1 when count.Count >= 1 && ps[0].Dragging && ps[ps.Count-1].LayoutSplit.IsHorizontal() == handle.LayoutSplit.IsHorizontal():
						splitRects = handle.GetSplitRects(rect,"first");
						break;
					default:
						splitRects = handle.GetSplitRects(rect);
						break;
				}
				if (handle.LayoutSplit.First is DockableLayoutSplit || handle.LayoutSplit.Second is DockableLayoutSplit)
				{
					ps.Add(handle);
				}
				_SplitContainer.FitChildInRect(handle,splitRects["self"]);
				_FitPanelAndSplitListToRect(panelAndSplitList,splitRects["first"],ps,count);
				_FitPanelAndSplitListToRect(panelAndSplitList,splitRects["second"],ps,count);
				break;
			}
		}
		}

	}

	public DockablePanel _GetPanel(int idx)
	{
		switch (_PanelContainer)
		{
			case null:
				GD.PrintErr("FIXME: creating panel without _panel_container");
				break;
		}
		if (idx < _PanelContainer.GetChildCount())
		{
			return _PanelContainer.GetChild(idx) as DockablePanel;
		}
		var panel = new DockablePanel();
		panel.TabAlignment = _TabAlign;
		panel.TabsVisible = _TabsVisible;
		panel.UseHiddenTabsForMinSize = _UseHiddenTabsForMinSize;
		panel.TabsRearrangeGroup = Mathf.Max(0,RearrangeGroup);
		_PanelContainer.AddChild(panel);
		panel.TabLayoutChanged += (v) => _OnPanelTabLayoutChanged(v,panel);
		return panel;
	}

	public SplitHandle _GetSplit(int idx)
	{
		switch (_SplitContainer)
		{
			case null:
				GD.PrintErr("FIXME: creating split without _split_container");
				break;
		}
		if (idx < _SplitContainer.GetChildCount())
		{
			return _SplitContainer.GetChild(idx) as SplitHandle;
		}
		var split = new SplitHandle();
		_SplitContainer.AddChild(split);
		return split;
	}

	public void _UntrackChildrenAfter(Control node,int idx)
	{
		for (var i = idx;i < node.GetChildCount();i++)
		{
			var child = node.GetChild(idx);
			node.RemoveChild(child);
			child.QueueFree();
		}
	}

	public void _OnPanelTabLayoutChanged(int tab,DockablePanel panel)
	{
		_LayoutDirty = true;
		var control = panel.GetTabControl(tab);
		switch (control)
		{
			case DockableReferenceControl referenceControl:
				control = referenceControl.ReferenceTo;
				break;
		}
		if (!_IsManagedNode(control))
		{
			control.GetParent().RemoveChild(control);
			AddChild(control);
		}
		_Layout.MoveNodeToLeaf(control,panel.Leaf,tab);
		QueueSort();
	}

	public void _OnChildRenamed(Node child)
	{
		var oldName = (string)_ChildrenNames[child];
		if (oldName == child.Name)
		{
			return;
		}
		_ChildrenNames.Remove(oldName);
		_ChildrenNames[child] = child.Name;
		_ChildrenNames[child.Name] = child;
		_Layout.RenameNode(oldName,child.Name);
	}
}
