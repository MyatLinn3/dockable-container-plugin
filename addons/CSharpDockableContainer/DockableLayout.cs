using Godot;
using System;

[Tool]
public partial class DockableLayout : Resource
{

	enum MARGIN{
		 MARGIN_LEFT, MARGIN_RIGHT, MARGIN_TOP, MARGIN_BOTTOM, MARGIN_CENTER 
	}

	[Export]
	public DockableLayoutNode Root
	{
		get{return _Root;}
		set{SetRoot(value);}
	}

	[Export]
	public Godot.Collections.Dictionary HiddenTabs
	{
		get{return _HiddenTabs;}
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
		if (value == null)
		{
			value = new DockableLayoutPanel();
		}
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
//		_Root.Connect("changed",this,"_OnRootChanged");
		if (shouldEmitChanged)
		{
			_OnRootChanged();
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
		Godot.Collections.Array<DockableLayoutPanel> emptyLeaves = new Godot.Collections.Array<DockableLayoutPanel>();
		_EnsureNamesInNode(_Root,names,emptyLeaves);
		foreach (var l in emptyLeaves)
		{
			_RemoveLeaf(l);
		}
		if (_FirstLeaf == null)
		{
			_FirstLeaf = new DockableLayoutPanel();
			SetRoot(_FirstLeaf);
		}
		foreach (string n in names)
		{
			if (!_LeafByNodeName.ContainsKey(n))
			{
				_FirstLeaf.PushName(n);
				_LeafByNodeName[n] = _FirstLeaf;
			}
		}
		_OnRootChanged();
	}

	public void MoveNodeToLeaf(Node node,DockableLayoutPanel leaf,int relativePosition)
	{
		string nodeName = node.Name;
		DockableLayoutPanel previousLeaf = _LeafByNodeName[nodeName] as DockableLayoutPanel;
		if (previousLeaf != null)
		{
			previousLeaf.RemoveNode(node);
			if (previousLeaf.IsEmpty())
			{
				_RemoveLeaf(previousLeaf);
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
		DockableLayoutPanel newLeaf = new DockableLayoutPanel();
		DockableLayoutSplit newBranch = new DockableLayoutSplit();
		if (margin == (int)MARGIN.MARGIN_LEFT | margin == (int)MARGIN.MARGIN_RIGHT)
		{
			newBranch.Direction = (int)DockableLayoutSplit.DIRECTION.HORIZONTAL;
		}
		else
		{
			newBranch.Direction = (int)DockableLayoutSplit.DIRECTION.VERTICAL;
		}
		if (margin == (int)MARGIN.MARGIN_LEFT | margin == (int)MARGIN.MARGIN_TOP)
		{
			newBranch.First = newLeaf;
			newBranch.Second = leaf;
		}
		else
		{
			newBranch.First = leaf;
			newBranch.Second = newLeaf;
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
		DockableLayoutPanel leaf = _LeafByNodeName[nodeName] as DockableLayoutPanel;
		if (leaf == null)
		{
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
		DockableLayoutPanel leaf = _LeafByNodeName[previousName] as DockableLayoutPanel;
		if (leaf == null)
		{
			return;
		}
		leaf.RenameNode(previousName,newName);
		_LeafByNodeName.Remove(previousName);
		_LeafByNodeName[newName] = leaf;
		_OnRootChanged();
	}

	public void SetTabHidden(string name,bool hidden)
	{
		if (!_LeafByNodeName.ContainsKey(name))
		{
			return;
		}
		if (hidden)
		{
			_HiddenTabs[name] = true;
		}
		else
		{
			_HiddenTabs.Remove(name);
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
		if (_ChangedSignalQueued)
		{
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
		if (node is DockableLayoutPanel)
		{
			(node as DockableLayoutPanel).UpdateNodes(names,_LeafByNodeName);
			if (node.IsEmpty())
			{
				emptyLeaves.Add((DockableLayoutPanel)node);
			}
			if (_FirstLeaf == null)
			{
				_FirstLeaf = node as DockableLayoutPanel;
			}
		}
		else if (node is DockableLayoutSplit)
		{
			_EnsureNamesInNode(((DockableLayoutSplit)node).First,names,emptyLeaves);
			_EnsureNamesInNode(((DockableLayoutSplit)node).Second,names,emptyLeaves);
		}
		else
		{
			GD.PrintErr($"Invalid Resource, should be branch or leaf, found {node}");
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
		if (!(collapsedBranch is DockableLayoutSplit))
		{
			GD.PrintErr("FIXME: leaf is not a child of branch");
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
