using Godot;
using System;

[Tool]
public partial class DockableLayoutSplit : DockableLayoutNode
{
	public enum DIRECTION {HORIZONTAL, VERTICAL}

	[Export]
	public int Direction
	{
		get{return GetDirection();}
		set{SetDirection(value);}
	}

	[Export(PropertyHint.Range, "0,1")]
	public float Percent
	{
		get{return GetPercent();}
		set{SetPercent(value);}
	}
	
	[Export]
	public DockableLayoutNode First
	{
		get{return GetFirst();}
		set{SetFirst(value);}
	}
	
	[Export]
	public DockableLayoutNode Second
	{
		get{return GetSecond();}
		set{SetSecond(value);}
	}


	public DockableLayoutSplit()
	{
		ResourceName = "Split";
	}

	private int _Direction = (int)DIRECTION.HORIZONTAL;
	private double _Percent = 0.5;
	private DockableLayoutNode _First = new DockableLayoutPanel();
	private DockableLayoutNode _Second = new DockableLayoutPanel();
	public void SetFirst(DockableLayoutNode value)
	{
		if (value == null)
		{
			_First = new DockableLayoutPanel();
		}
		else
		{
			_First = value;
		}
		_First.Parent = this;
		EmitTreeChanged();
	}

	public DockableLayoutNode GetFirst()
	{
		return _First;
	}

	public void SetSecond(DockableLayoutNode value)
	{
		if (value == null)
		{
			_Second = new DockableLayoutPanel();
		}
		else
		{
			_Second = value;
		}
		_Second.Parent = this;
		EmitTreeChanged();
	}

	public DockableLayoutNode GetSecond()
	{
		return _Second;
	}
	public void SetDirection(int value)
	{
		if(value != Direction)
		{
			_Direction = value;
			EmitTreeChanged();
		}
	}
	
	public int GetDirection()
	{
		return _Direction;
	}
	public void SetPercent(float value)
	{
		float clampedValue = Mathf.Clamp(value,0,1);
		if (!Mathf.IsEqualApprox(_Percent,clampedValue))
		{
			_Percent = clampedValue;
			EmitTreeChanged();
		}
	}

	public float GetPercent()
	{
		return (float)_Percent;
	}
	public override Godot.Collections.Array<string> GetNames()
	{
		Godot.Collections.Array<string> names = _First.GetNames();
		names.AddRange(_Second.GetNames());
		return names;
	}

	public override bool IsEmpty()
	{
		return _First.IsEmpty() && _Second.IsEmpty();
	}

	public bool IsHorizontal()
	{
		return _Direction == (int)DIRECTION.HORIZONTAL;
	}

	public bool IsVertical()
	{
		return _Direction == (int)DIRECTION.VERTICAL;
	}
}
