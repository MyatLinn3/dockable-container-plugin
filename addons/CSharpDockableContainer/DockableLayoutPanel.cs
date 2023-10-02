using Godot;
using System;

[Tool]
public partial class DockableLayoutPanel : DockableLayoutNode
{
	[Export]
	public Godot.Collections.Array<string> Names
	{
		get{return GetNames();}
		set{_Names = value;
			EmitTreeChanged();}
	}

	[Export]
	public Godot.Collections.Array<string> changeNameOfTabs
	{
		get{return GetCurrentTabNames();}
		set{SetCurrentTabNames(value);}
	}

	[Export]
	public int CurrentTab
	{
		get{
			if (Names.Count == 0)
			{
				return -1;
			}
			return (int)Math.Clamp(_CurrentTab,0,_Names.Count-1);
		}
		set
		{
			if (value != _CurrentTab)
			{
				_CurrentTab = value;
				EmitTreeChanged();
			}
		}
	}
	
	[Signal]
	public delegate void GetCurrentTabTitleEventHandler(Godot.Collections.Array<string> names);
	[Signal]
	public delegate void SetCurrentTabTitleEventHandler(Godot.Collections.Array<string> names);

	public Godot.Collections.Array<string> _Names = new Godot.Collections.Array<string>();
	public int _CurrentTab = 0;
	public Godot.Collections.Array<string> _changeNameOfTabs = new Godot.Collections.Array<string>();
	public DockableLayoutPanel()
	{
		ResourceName = "Tabs";
		GetCurrentTabTitle += GetCTabTitle;
		EmitSignal(SignalName.SetCurrentTabTitle,_changeNameOfTabs);
	}

	public override Godot.Collections.Array<string> GetNames()
	{
		return _Names;
	}
	
	public void GetCTabTitle(Godot.Collections.Array<string> names)
	{
		_changeNameOfTabs = names;
	}
	public new Godot.Collections.Array<string> GetCurrentTabNames()
	{
		return _changeNameOfTabs;
	}
	
	public void SetCurrentTabNames(Godot.Collections.Array<string> names)
	{
		_changeNameOfTabs = names;
		EmitSignal(SignalName.SetCurrentTabTitle,names);
	}
	public void PushName(string name)
	{
		_Names.Add(name);
//		_DisplayNames.Add(name);
		EmitTreeChanged();
	}

	public void InsertNode(int position,Node node)
	{
		_Names.Insert(position,node.Name);
//		_DisplayNames.Insert(position,node.Name);
		EmitTreeChanged();
	}

	public int FindName(string nodeName)
	{
		for (var i =0;i < _Names.Count;i++)
		{
			if (_Names[i] == nodeName)
			{
				return i;
			}
		}
		return -1;
	}

	public int FindChild(Node node){
		return FindName(node.Name);
	}

	public void RemoveNode(Node node)
	{
		var i = FindChild(node);
		if (i >= 0)
		{
			_Names.RemoveAt(i);
			EmitTreeChanged();
		}
		else
		{
			GD.PushWarning($"Remove failed, node {node} was not found");
		}
	}

	public void RenameNode(string previousName,string newName)
	{
		var i = FindName(previousName);
		if(i >= 0)
		{
			_Names.Insert(i,newName);
			EmitTreeChanged();
		}
		else
		{
			GD.PushWarning($"Rename failed, name {previousName} was not found");
		}
	}

	public override bool IsEmpty()
	{
		return _Names.Count == 0;
	}

	public void UpdateNodes(Godot.Collections.Array<string> nodeNames,Godot.Collections.Dictionary<string,DockableLayoutNode> data)
	{
		var i = 0;
		var removedAny = false;
		while(i < _Names.Count)
		{
			var current = _Names[i];
			if (!nodeNames.Contains(current) | data.ContainsKey(current))
			{
				_Names.RemoveAt(i);
				removedAny = true;
			}
			else
			{
				data[current] = this;
				i += 1;
			}
		}
		if (removedAny)
		{
			EmitTreeChanged();
		}
	}
}
