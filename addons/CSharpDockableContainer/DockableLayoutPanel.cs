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

	public Godot.Collections.Array<string> _Names = new Godot.Collections.Array<string>();
	public int _CurrentTab = 0;

	public DockableLayoutPanel()
	{
		ResourceName = "Tabs";
	}

	public override Godot.Collections.Array<string> GetNames()
	{
		return _Names;
	}

	public void PushName(string name)
	{
		_Names.Add(name);
		EmitTreeChanged();
	}

	public void InsertNode(int position,Node node)
	{
		_Names.Insert(position,node.Name);
		EmitTreeChanged();
	}

	public int FindName(string nodeName)
	{
		for (int i =0;i < _Names.Count;i++)
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
		int i = FindChild(node);
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
		int i = FindName(previousName);
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
		int i = 0;
		bool removedAny = false;
		while(i < _Names.Count)
		{
			string current = _Names[i];
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
