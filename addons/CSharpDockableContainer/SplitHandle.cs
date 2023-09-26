using Godot;
using System;

[Tool]
public partial class SplitHandle : Control
{

	public static readonly string[] SPLIT_THEME_CLASS = {"HSplitContainer","VSplitContainer"};
	public static readonly Godot.Collections.Array<Control.CursorShape> SPLIT_MOUSE_CURSOR_SHAPE = new Godot.Collections.Array<Control.CursorShape>(){Control.CursorShape.Hsplit,Control.CursorShape.Vsplit};
	
	
	public DockableLayoutSplit LayoutSplit;
	public Vector2 FirstMinimumSize;
	public Vector2 SecondMinimumSize;

	Rect2 ParentRect;
	bool MouseHovering;
	bool Dragging;

	public override void _Draw()
	{
		string themeClass = SPLIT_THEME_CLASS[LayoutSplit.Direction];
		Texture2D icon = GetThemeIcon("grabber",themeClass);
		bool autohide = Convert.ToBoolean(GetThemeConstant("autohide",themeClass));
		if((icon == null) | (autohide && !MouseHovering))
		{
			return;
		}
		DrawTexture(icon,(Size - icon.GetSize())*0.5f);
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton && (@event as InputEventMouseButton).ButtonIndex == MouseButton.Left)
		{
			Dragging = @event.IsPressed();
			if((@event as InputEventMouseButton).DoubleClick){
				LayoutSplit.Percent = 0.5f;
			}

		}else if(Dragging && @event is InputEventMouseMotion)
		{
			Vector2 mouseInParent = GetParentControl().GetLocalMousePosition();
			if(LayoutSplit.IsHorizontal())
			{
				LayoutSplit.Percent = ( (mouseInParent.X - ParentRect.Position.X) / ParentRect.Size.X );
			}else
			{
				LayoutSplit.Percent = ( (mouseInParent.Y - ParentRect.Position.Y) / ParentRect.Size.Y );
			}
		}
	}

	public override void _Notification(int what)
	{
		if (what == NotificationMouseEnter)
		{
			MouseHovering = true;
			SetSplitCursor(true);
			if (Convert.ToBoolean(GetThemeConstant("autohide",SPLIT_THEME_CLASS[LayoutSplit.Direction])))
			{
				QueueRedraw();
			}
			else if(what == NotificationMouseExit)
			{
				MouseHovering = false;
				SetSplitCursor(false);
				if (Convert.ToBoolean(GetThemeConstant("autohide",SPLIT_THEME_CLASS[LayoutSplit.Direction])))
				{
					QueueRedraw();
				}
			}
			else if(what == NotificationFocusExit)
			{
				Dragging = false;
			}
		}
	}
	
	public Vector2 GetLayoutMinimumSize(){
		if (LayoutSplit != null)
		{
			return Vector2.Zero;
		}
		int seperation = GetThemeConstant("seperation",SPLIT_THEME_CLASS[LayoutSplit.Direction]);
		if (LayoutSplit.IsHorizontal())
		{
			return new Vector2(
				FirstMinimumSize.X + seperation+SecondMinimumSize.X,
				Mathf.Max(FirstMinimumSize.Y,SecondMinimumSize.Y));
		}
		else
		{
			return new Vector2(
				Mathf.Max(FirstMinimumSize.X,SecondMinimumSize.X),
				FirstMinimumSize.Y + seperation+ SecondMinimumSize.Y);
		}
	}

	public void SetSplitCursor(bool value)
	{
		if(value)
		{
			MouseDefaultCursorShape = SPLIT_MOUSE_CURSOR_SHAPE[LayoutSplit.Direction];
		}
		else
		{
			MouseDefaultCursorShape = CursorShape.Arrow;
		}
	}

	public Godot.Collections.Dictionary<string, Rect2> GetSplitRects(Rect2 rect)
	{
		ParentRect = rect;
		int separation = GetThemeConstant("separation",SPLIT_THEME_CLASS[LayoutSplit.Direction]);
		Vector2 origin = rect.Position;
		float percent = LayoutSplit.Percent;
		if (LayoutSplit.IsHorizontal())
		{
			float split_offset = Mathf.Clamp(
				rect.Size.X * percent - separation * 0.5f,
				FirstMinimumSize.X,
				rect.Size.X - SecondMinimumSize.X - separation
			);
			float second_width = rect.Size.X - split_offset - separation;
			return new Godot.Collections.Dictionary<string, Rect2>(){
				{"first",new Rect2(origin.X, origin.Y, split_offset, rect.Size.Y)},
				{"self",new Rect2(origin.X + split_offset, origin.Y, separation, rect.Size.Y)},
				{"second",
				new Rect2(origin.X + split_offset + separation, origin.Y, second_width, rect.Size.Y)}
			};
		}
		else
		{
			float split_offset = Mathf.Clamp(
				rect.Size.Y * percent - separation * 0.5f,
				FirstMinimumSize.Y,
				rect.Size.Y - SecondMinimumSize.Y - separation
			);
			float second_height = rect.Size.X - split_offset - separation;
			return new Godot.Collections.Dictionary<string, Rect2>(){
				{"first",new Rect2(origin.X, origin.Y, rect.Size.X, split_offset)},
				{"self",new Rect2(origin.X, origin.Y  + split_offset, rect.Size.X, separation)},
				{"second",
				new Rect2(origin.X,origin.Y + split_offset + separation,rect.Size.X, second_height)}
			};
		}
	}
}