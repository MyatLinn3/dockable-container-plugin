using Godot;
using System;

[Tool]
public partial class DockableContainer : Container
{

	[Export]
	public TabBar.AlignmentMode TabAlignment 
	{
		get{return _TabAlign;}
		set
		{
			_TabAlign = value;
			for (int i = 1;i<_PanelContainer.GetChildCount();i++)
			{
				DockablePanel panel =  _PanelContainer.GetChild(i) as DockablePanel;
				panel.TabAlignment = value;
			}
		}
	} 

	[Export]
	public bool UseHiddenTabsForMinSize
	{
		get{return _UseHiddenTabsForMinSize;}
		set
		{
			_UseHiddenTabsForMinSize = value;
			for (int i = 1;i<_PanelContainer.GetChildCount();i++)
			{
				DockablePanel panel =  _PanelContainer.GetChild(i) as DockablePanel;
				panel.UseHiddenTabsForMinSize = value;
			}
		}
	}

	[Export]
	public bool TabsVisible
	{
		get{return _TabsVisible;}
		set
		{
			_TabsVisible = value;
			for (int i = 1;i<_PanelContainer.GetChildCount();i++)
			{
				DockablePanel panel = _PanelContainer.GetChild(i) as DockablePanel;
				panel.TabsVisible = value;
			}
		}
	}

	[Export]
	public int RearrangeGroup = 0;


	[Export]
	public DockableLayout Layout
	{
		get{return _Layout;}
		set
		{
			SetLayout(value);
		}
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

		if (_Layout == null)
		{
			SetLayout(null);
		}
		else if (CloneLayoutOnReady && !Engine.IsEditorHint())
		{
			SetLayout(_Layout.Clone());
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
		if (@event is InputEventMouseMotion)
		{
			Vector2 localPosition = GetLocalMousePosition();
			DockablePanel panel = null;
			for (int i = 1;i<_PanelContainer.GetChildCount();i++)
			{
				DockablePanel p =  _PanelContainer.GetChild(i) as DockablePanel;
				if (p.GetRect().HasPoint(localPosition))
				{
					panel = p;
					break;
				}
			}
			_DragPanel = panel;
			if (panel == null)
			{
				return;
			}
			FitChildInRect(_DragNDropPanel,panel.GetChildRect());
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
		DockablePanel fromNode = GetNode((NodePath)((Godot.Collections.Dictionary)data)["from_path"]) as DockablePanel;
		if (fromNode == _DragPanel && _DragPanel.GetChildCount() == 1)
		{
			return;
		}
		Control movedTab = fromNode.GetTabControl((int)((Godot.Collections.Dictionary)data)["tabc_element"]);
		if (movedTab is DockableReferenceControl)
		{
			movedTab = (movedTab as DockableReferenceControl).ReferenceTo;
		}
		if (!_IsManagedNode(movedTab))
		{
			movedTab.GetParent().RemoveChild(movedTab);
			AddChild(movedTab);
		}
		if (_DragPanel != null)
		{
			int margin = _DragNDropPanel.GetHoverMargin();
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
		DockableLayoutPanel leaf = _Layout.GetLeafForNode(control);
		if (leaf == null)
		{
			return;
		}
		int positionInLeaf = leaf.FindChild(control);
		if (positionInLeaf < 0)
		{
			return;
		}
		DockablePanel panel = null;
		for (int i = 1;i<_PanelContainer.GetChildCount();i++)
		{
			DockablePanel p = _PanelContainer.GetChild(i) as DockablePanel;
			if (p.Leaf == leaf)
			{
				panel = p;
				break;
			}
		}
		if (panel == null)
		{
			return;
		}
		panel.CurrentTab = Mathf.Clamp(positionInLeaf,0,panel.GetTabCount()-1);
	}

	public void SetLayout(DockableLayout value)
	{
		if (value == null)
		{
			value = new DockableLayout();
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
		for (int i = 1;i<_PanelContainer.GetChildCount();i++)
		{
			Node panel = _PanelContainer.GetChild(i);
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
		for (int i = 1;i<_PanelContainer.GetChildCount();i++)
		{
			DockableContainer panel = _PanelContainer.GetChild(i) as DockableContainer;
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
		Godot.Collections.Array<Control> tabs = new Godot.Collections.Array<Control>();
		for (int i = 0;i<GetChildCount();i++)
		{
			Control child = GetChild(i) as Control;
			if (_IsManagedNode(child))
			{
				tabs.Add(child);
			}
		}
		return tabs;
	}

	public int GetTabCount()
	{
		int count = 0;
		for (int i = 0;i<GetChildCount();i++)
		{
			Node child = GetChild(i);
			if (_IsManagedNode(child))
			{
				count += 1;
			}
		}
		return count;
	}

	public bool _CanHandleDragData(Variant data)
	{

		if ((string)(((Godot.Collections.Dictionary) data)["type"]) == "tabc_element")
		{
			GD.Print(data);
			Node tabc = GetNodeOrNull((NodePath)((Godot.Collections.Dictionary)data)["from_path"]);
			return (
				tabc != null 
				&& (tabc as TabContainer).TabsRearrangeGroup == RearrangeGroup
			);
		}
		return false;
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
		Godot.Collections.Array<string> names = new Godot.Collections.Array<string>();
		_ChildrenNames.Clear();
		for (int i = 1;i<GetChildCount()-1;i++)
		{
			Node c = GetChild(i);
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
		if (_PanelContainer == null)
		{
			GD.PrintErr("FIXME: resorting without _panel_container");
			
		}
			if (_PanelContainer.GetIndex() != 0)
			{
				MoveChild(_PanelContainer,0);
			} 
			if (_DragNDropPanel.GetIndex() < GetChildCount()-1)
			{
				_DragNDropPanel.MoveToFront();
			}

			if (_LayoutDirty)
			{
				_UpdateLayoutWithChildren();
			}

			Rect2 rect = new Rect2(Vector2.Zero,Size);
			FitChildInRect(_PanelContainer,rect);
			_PanelContainer.FitChildInRect(_SplitContainer,rect);

			_CurrentPanelIndex = 1;
			_CurrentSplitIndex = 0;
			Godot.Collections.Array childrenList = new Godot.Collections.Array();
			_CalculatePanelAndSplitList(childrenList,_Layout.Root);
			_FitPanelAndSplitListToRect(childrenList,rect);

			_UntrackChildrenAfter(_PanelContainer,_CurrentPanelIndex);
			_UntrackChildrenAfter(_SplitContainer,_CurrentSplitIndex);
	}


	public Variant _CalculatePanelAndSplitList(Godot.Collections.Array result,DockableLayoutNode layoutNode)
	{
		if (layoutNode is DockableLayoutPanel)
		{
			Godot.Collections.Array<Control> nodes = new Godot.Collections.Array<Control>();
			foreach (string n in (layoutNode as DockableLayoutPanel).Names)
			{
				Control node = (Control)_ChildrenNames[n];
				if (node != null)
				{
					if (!(node is Control))
					{
						GD.PrintErr($"FIXME: node is not a control {node}");
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
			if (nodes.Count == 0)
			{
				return 0;
			}
			else
			{
				DockablePanel panel = _GetPanel(_CurrentPanelIndex);
				_CurrentPanelIndex += 1;
				panel.TrackNodes(nodes,(DockableLayoutPanel)layoutNode);
				result.Add(panel);
				return panel;
			}
		}
		else if(layoutNode is DockableLayoutSplit)
		{
			Control secondResult = (Control) _CalculatePanelAndSplitList(result,((DockableLayoutSplit)layoutNode).Second);
			Control firstResult = (Control) _CalculatePanelAndSplitList(result,((DockableLayoutSplit)layoutNode).First);
			if (firstResult != null && secondResult != null)
			{
				SplitHandle split = _GetSplit(_CurrentSplitIndex);
				_CurrentSplitIndex += 1;
				split.LayoutSplit = (DockableLayoutSplit) layoutNode;
				if (firstResult is DockablePanel)
				{
					split.FirstMinimumSize = ((DockablePanel)firstResult).GetLayoutMinimumSize();
				}
				else if(firstResult is SplitHandle)
				{
					split.FirstMinimumSize = (firstResult as SplitHandle).GetLayoutMinimumSize();
				}
				if (secondResult is DockablePanel)
				{
					split.SecondMinimumSize = (secondResult as DockablePanel).GetLayoutMinimumSize();
				}
				else if(secondResult is SplitHandle)
				{
					split.SecondMinimumSize = (secondResult as SplitHandle).GetLayoutMinimumSize();
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
		else
		{
			GD.PushWarning($"FIXME: invalid Resource, should be branch or leaf, found {layoutNode}");
		}
		return 0;
	}

	public void _FitPanelAndSplitListToRect(Godot.Collections.Array panelAndSplitList,Rect2 rect)
	{
		if (panelAndSplitList.Count != 0)
		{
			Control control = (Control)panelAndSplitList[^1];
		panelAndSplitList.RemoveAt(panelAndSplitList.Count - 1);
		if (control is DockablePanel)
		{
			_PanelContainer.FitChildInRect(control,rect);
		}
		else if (control is SplitHandle)
		{
			Godot.Collections.Dictionary<string, Rect2> splitRects = (control as SplitHandle).GetSplitRects(rect);
			_SplitContainer.FitChildInRect(control,splitRects["self"]);
			_FitPanelAndSplitListToRect(panelAndSplitList,splitRects["first"]);
			_FitPanelAndSplitListToRect(panelAndSplitList,splitRects["second"]);
		}
		}

	}

	public DockablePanel _GetPanel(int idx)
	{
		if (_PanelContainer == null)
		{
			GD.PrintErr("FIXME: creating panel without _panel_container");
		}
		if (idx < _PanelContainer.GetChildCount())
		{
			return _PanelContainer.GetChild(idx) as DockablePanel;
		}
		DockablePanel panel = new DockablePanel();
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
		if (_SplitContainer == null)
		{
			GD.PrintErr("FIXME: creating split without _split_container");
		}
		if (idx < _SplitContainer.GetChildCount())
		{
			return _SplitContainer.GetChild(idx) as SplitHandle;
		}
		SplitHandle split = new SplitHandle();
		_SplitContainer.AddChild(split);
		return split;
	}

	public void _UntrackChildrenAfter(Control node,int idx)
	{
		for (int i = idx;i < node.GetChildCount();i++)
		{
			Node child = node.GetChild(idx);
			node.RemoveChild(child);
			child.QueueFree();
		}
	}

	public void _OnPanelTabLayoutChanged(int tab,DockablePanel panel)
	{
		_LayoutDirty = true;
		Control control = panel.GetTabControl(tab);
		if (control is DockableReferenceControl)
		{
			control = (control as DockableReferenceControl).ReferenceTo;
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
		string oldName = (string)_ChildrenNames[child];
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
