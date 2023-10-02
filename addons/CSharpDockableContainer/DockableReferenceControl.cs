using Godot;
using System;

[Tool]
public partial class DockableReferenceControl : Container
{
	public Control ReferenceTo
	{
		get => _ReferenceTo;
		set
		{
			if (_ReferenceTo != value)
			{
				if (IsInstanceValid(_ReferenceTo))
				{
					_ReferenceTo.Renamed -= _OnReferenceToRenamed;
					_ReferenceTo.MinimumSizeChanged -= UpdateMinimumSize;
//					_ReferenceTo.Disconnect("renamed",this,"_OnReferenceToRenamed");
//					_ReferenceTo.Disconnect("minimum_size_changed",this,"UpdateMinimumSize");
				}
				_ReferenceTo = value;
				EmitSignal("minimum_size_changed");
				if (!IsInstanceValid(_ReferenceTo))
				{
					return;
				}
				_ReferenceTo.Renamed += _OnReferenceToRenamed;
				_ReferenceTo.MinimumSizeChanged += UpdateMinimumSize;
//				_ReferenceTo.Connect("renamed",this,"_OnReferenceToRenamed");
//				_ReferenceTo.Connect("minimum_size_changed",this,"UpdateMinimumSize");
				_ReferenceTo.Visible = Visible;
				_RepositionReference();
			}

		}
	}

	private Control _ReferenceTo = null; 

	public override void _Ready()
	{
		MouseFilter = Control.MouseFilterEnum.Ignore;
//		MOUSE_FILTER_IGNORE;
		SetNotifyTransform(true);
	}

	public override void _Notification(int what)
	{
		if (what == NotificationVisibilityChanged && _ReferenceTo != null)
		{
			_ReferenceTo.Visible = Visible;
		}
		else if(what == NotificationTransformChanged && _ReferenceTo != null)
		{
			_RepositionReference();
		}
	}

	public override Vector2 _GetMinimumSize()
	{
		return _ReferenceTo != null ? _ReferenceTo.GetCombinedMinimumSize() : new Vector2();
	}

	public void _RepositionReference()
	{
		_ReferenceTo.GlobalPosition = GlobalPosition;
		_ReferenceTo.Size = Size;
	}

	public void _OnReferenceToRenamed()
	{
		Name = _ReferenceTo.Name;
	}
}
