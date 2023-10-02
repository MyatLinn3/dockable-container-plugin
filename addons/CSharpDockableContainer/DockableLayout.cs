using Godot;

[Tool]
public partial class DockableLayout : Resource
{

	enum MARGIN{
		 MARGIN_LEFT, MARGIN_RIGHT, MARGIN_TOP, MARGIN_BOTTOM, MARGIN_CENTER 
	}

	[Export]
	public DockableLayoutNode Root
	{
		get => _Root;
		set => SetRoot(value);
	}

	[Export]
	public Godot.Collections.Dictionary HiddenTabs
	{
		get => _HiddenTabs;
		set
		{
			if (value != _HiddenTabs)
			{
				_HiddenTabs = value;
				EmitSignal("changed");
			}
		}
	}

	private bool _ChangedSignalQueued = false;
	private DockableLayoutPanel _FirstLeaf;
	private Godot.Collections.Dictionary _HiddenTabs = new Godot.Collections.Dictionary();
	private Godot.Collections.Dictionary<string,DockableLayoutNode> _LeafByNodeName = new Godot.Collections.Dictionary<string,DockableLayoutNode>();
	private DockableLayoutNode _Root = new DockableLayoutPanel();

	public DockableLayout()
	{
		ResourceName = "Layout";
	}

	public void SetRoot(DockableLayoutNode value,bool shouldEmitChanged=true)
	{
		value = value switch
		{
			null => new DockableLayoutPanel(),
			_ => value
		};
		if (_Root == value)
		{
			return;
		}
		if (_Root != null && _Root.IsConnected("changed",new Callable(this,nameof(_OnRootChanged))))
		{
			_Root.Changed -= _OnRootChanged;
//			_Root.Disconnect("changed","_OnRootChanged");
		}
		_Root = value;
		_Root.Parent = null;
		_Root.Changed += _OnRootChanged;
		switch (shouldEmitChanged)
		{
			//		_Root.Connect("changed",this,"_OnRootChanged");
			case true:
				_OnRootChanged();
				break;
		}
	}

	public DockableLayoutNode GetRoot()
	{
		return _Root;
	}

	public DockableLayout Clone()
	{
		return Duplicate(true) as DockableLayout;
	}

	public Godot.Collections.Array<string> GetNames()
	{
		return _Root.GetNames();
	}

	public void UpdateNodes(Godot.Collections.Array<string> names)
	{
		_LeafByNodeName.Clear();
		_FirstLeaf = null;
		var emptyLeaves = new Godot.Collections.Array<DockableLayoutPanel>();
		_EnsureNamesInNode(_Root,names,emptyLeaves);
		foreach (var l in emptyLeaves)
		{
			_RemoveLeaf(l);
		}
		switch (_FirstLeaf)
		{
			case null:
				_FirstLeaf = new DockableLayoutPanel();
				SetRoot(_FirstLeaf);
				break;
		}
		foreach (var n in names)
		{
			switch (_LeafByNodeName.ContainsKey(n))
			{
				case false:
					_FirstLeaf.PushName(n);
					_LeafByNodeName[n] = _FirstLeaf;
					break;
			}
		}
		_OnRootChanged();
	}

	public void MoveNodeToLeaf(Node node,DockableLayoutPanel leaf,int relativePosition)
	{
		string nodeName = node.Name;
		switch (_LeafByNodeName[nodeName])
		{
			case DockableLayoutPanel previousLeaf:
			{
				previousLeaf.RemoveNode(node);
				if (previousLeaf.IsEmpty())
				{
					_RemoveLeaf(previousLeaf);
				}

				break;
			}
		}
		leaf.InsertNode(relativePosition,node);
		_LeafByNodeName[nodeName] = leaf;
		_OnRootChanged();
	}

	public DockableLayoutPanel GetLeafForNode(Node node)
	{
		return _LeafByNodeName[node.Name] as DockableLayoutPanel;
	}

	public void SplitLeafWithNode(DockableLayoutPanel leaf,Node node,int margin)
	{
		var rootBranch = leaf.Parent;
		var newLeaf = new DockableLayoutPanel();
		var newBranch = new DockableLayoutSplit();
		newBranch.Direction = (margin == (int)MARGIN.MARGIN_LEFT | margin == (int)MARGIN.MARGIN_RIGHT) switch
		{
			true => (int)DockableLayoutSplit.DIRECTION.HORIZONTAL,
			_ => (int)DockableLayoutSplit.DIRECTION.VERTICAL
		};
		switch (margin == (int)MARGIN.MARGIN_LEFT | margin == (int)MARGIN.MARGIN_TOP)
		{
			case true:
				newBranch.First = newLeaf;
				newBranch.Second = leaf;
				break;
			default:
				newBranch.First = leaf;
				newBranch.Second = newLeaf;
				break;
		}
		if (_Root == leaf)
		{
			SetRoot(newBranch,false);
		}
		else if(rootBranch != null)
		{
			if (leaf == rootBranch.First)
			{
				rootBranch.First = newBranch;
			}
			else
			{
				rootBranch.Second = newBranch;
			}
		}
		MoveNodeToLeaf(node,newLeaf,0);
	}

	public void AddNode(Node node)
	{
		string nodeName = node.Name;
		if (_LeafByNodeName.ContainsKey(nodeName))
		{
			return;
		}
		_FirstLeaf.PushName(nodeName);
		_LeafByNodeName[nodeName] = _FirstLeaf;
		_OnRootChanged();
	}

	public void RemoveNode(Node node)
	{
		string nodeName = node.Name;
		var leaf = _LeafByNodeName[nodeName] as DockableLayoutPanel;
		switch (leaf)
		{
			case null:
				return;
		}
		leaf.RemoveNode(node);
		_LeafByNodeName.Remove(nodeName);
		if (leaf.IsEmpty())
		{
			_RemoveLeaf(leaf);
		}
		_OnRootChanged();
	}

	public void RenameNode(string previousName,string newName)
	{
		var leaf = _LeafByNodeName[previousName] as DockableLayoutPanel;
		switch (leaf)
		{
			case null:
				return;
		}
		leaf.RenameNode(previousName,newName);
		_LeafByNodeName.Remove(previousName);
		_LeafByNodeName[newName] = leaf;
		_OnRootChanged();
	}

	public void SetTabHidden(string name,bool hidden)
	{
		switch (_LeafByNodeName.ContainsKey(name))
		{
			case false:
				return;
		}
		switch (hidden)
		{
			case true:
				_HiddenTabs[name] = true;
				break;
			default:
				_HiddenTabs.Remove(name);
				break;
		}
		_OnRootChanged();
	}

	public bool IsTabHidden(string name)
	{
		Variant b = false;
		return _HiddenTabs.TryGetValue(name,out b);
	}

	public void SetNodeHidden(Node node,bool hidden)
	{
		SetTabHidden(node.Name,hidden);
	}

	public bool IsNodeHidden(Node node)
	{
		return IsTabHidden(node.Name);
	}

	private void _OnRootChanged()
	{
		switch (_ChangedSignalQueued)
		{
			case true:
				return;
		}
		_ChangedSignalQueued = true;
		SetDeferred("_ChangedSignalQueued",false);
		CallDeferred("emit_changed");
//		EmitChanged.CallDeferred();
	}

	public void _EnsureNamesInNode(
		DockableLayoutNode node,
		Godot.Collections.Array<string> names,
		Godot.Collections.Array<DockableLayoutPanel> emptyLeaves)
	{
		switch (node)
		{
			case DockableLayoutPanel panel:
			{
				panel.UpdateNodes(names,_LeafByNodeName);
				if (panel.IsEmpty())
				{
					emptyLeaves.Add(panel);
				}

				_FirstLeaf = _FirstLeaf switch
				{
					null => panel,
					_ => _FirstLeaf
				};

				break;
			}
			case DockableLayoutSplit split:
				_EnsureNamesInNode(split.First,names,emptyLeaves);
				_EnsureNamesInNode(split.Second,names,emptyLeaves);
				break;
			default:
				GD.PrintErr($"Invalid Resource, should be branch or leaf, found {node}");
				break;
		}
	}

	public void _RemoveLeaf(DockableLayoutPanel leaf)
	{
		if (!leaf.IsEmpty())
		{
			GD.PrintErr("FIXME: trying to remove_at a leaf with nodes");
		}
		if (_Root == leaf)
		{
			return;
		}
		DockableLayoutNode collapsedBranch = leaf.Parent;
		switch ((collapsedBranch is DockableLayoutSplit))
		{
			case false:
				GD.PrintErr("FIXME: leaf is not a child of branch");
				break;
		}
		DockableLayoutNode keptBranch;
		if (leaf == (collapsedBranch as DockableLayoutSplit).Second)
		{
			keptBranch = (collapsedBranch as DockableLayoutSplit).First;
		}
		else
		{
			keptBranch = (collapsedBranch as DockableLayoutSplit).Second;
		}
		DockableLayoutNode rootBranch = (collapsedBranch as DockableLayoutSplit).Parent;
		if (collapsedBranch == _Root)
		{
			SetRoot(keptBranch,true);
		}
		else if (rootBranch != null)
		{
			if (collapsedBranch == (rootBranch as DockableLayoutSplit).First)
			{
				(rootBranch as DockableLayoutSplit).First = keptBranch;
			}
			else
			{
				(rootBranch as DockableLayoutSplit).Second = keptBranch;
			}
		}
	}
}
