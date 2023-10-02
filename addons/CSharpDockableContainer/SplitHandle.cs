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
	public bool MouseHovering = false;
	public bool Dragging = false;


	private Godot.Collections.Dictionary<string,Rect2> LastPreviousRects;
	public override void _Draw()
	{
		var themeClass = SPLIT_THEME_CLASS[LayoutSplit.Direction];
		var icon = GetThemeIcon("grabber",themeClass);
		var autohide = Convert.ToBoolean(GetThemeConstant("autohide",themeClass));
		if((icon == null) || (autohide && !MouseHovering))
		{
			return;
		}
		DrawTexture(icon,(Size - icon.GetSize())*0.5f);
	}

	public override void _GuiInput(InputEvent @event)
	{
		switch (@event)
		{
			case InputEventMouseButton button when button.ButtonIndex == MouseButton.Left:
			{
				Dragging = button.IsPressed();
				LayoutSplit.Percent = button.DoubleClick switch
				{
					true => 0.5f,
					_ => LayoutSplit.Percent
				};

				break;
			}
			default:
			{
				switch (Dragging)
				{
					case true when @event is InputEventMouseMotion:
					{
						var mouseInParent = GetParentControl().GetLocalMousePosition();
						if(LayoutSplit.IsHorizontal())
						{
							LayoutSplit.Percent = ( (mouseInParent.X - ParentRect.Position.X) / ParentRect.Size.X );
						}else
						{
							LayoutSplit.Percent = ( (mouseInParent.Y - ParentRect.Position.Y) / ParentRect.Size.Y );
						}

						break;
					}
				}

				break;
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
		switch (LayoutSplit)
		{
			case null:
				return Vector2.Zero;
		}
		var seperation = GetThemeConstant("separation",SPLIT_THEME_CLASS[LayoutSplit.Direction]);
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
		MouseDefaultCursorShape = value switch
		{
			true => SPLIT_MOUSE_CURSOR_SHAPE[LayoutSplit.Direction],
			_ => CursorShape.Arrow
		};
	}

	public Godot.Collections.Dictionary<string, Rect2> GetSplitRects(Rect2 rect,String s="")
	{
		ParentRect = rect;
		var separation = GetThemeConstant("separation",SPLIT_THEME_CLASS[LayoutSplit.Direction]);
		var origin = rect.Position;
		var percent = LayoutSplit.Percent;
		if (LayoutSplit.IsHorizontal())
		{
			var split_offset = Mathf.Clamp(
				rect.Size.X * percent - separation * 0.5f,
				FirstMinimumSize.X,
				rect.Size.X - SecondMinimumSize.X - separation
			);
			var second_width = rect.Size.X - split_offset - separation;
			switch (s)
			{
				case "second":
				{
					var ds = LastPreviousRects["first"].Size.X;
					var second_width_m = rect.Size.X - separation - ds;
					LastPreviousRects = new Godot.Collections.Dictionary<string, Rect2>(){
						{"first",new Rect2(LastPreviousRects["first"].Position, LastPreviousRects["first"].Size)},
						{"self",new Rect2(LastPreviousRects["self"].Position, LastPreviousRects["self"].Size)},
						{"second",
							new Rect2(LastPreviousRects["second"].Position, new Vector2(second_width_m,rect.Size.Y))}
					};
					break;
				}
				case "first":
				{
					var ds = LastPreviousRects["second"].Size.X;
					var second_width_m = rect.Size.X - separation - ds;
					LastPreviousRects = new Godot.Collections.Dictionary<string, Rect2>(){
						{"first",new Rect2(new Vector2(rect.Position.X,rect.Position.Y),  new Vector2(second_width_m,rect.Size.Y))},
						{"self",new Rect2(LastPreviousRects["self"].Position, LastPreviousRects["self"].Size)},
						{"second",
							new Rect2(LastPreviousRects["second"].Position,LastPreviousRects["second"].Size)}
					};
					break;
				}
				default:
					LastPreviousRects = new Godot.Collections.Dictionary<string, Rect2>(){
						{"first",new Rect2(origin.X, origin.Y, split_offset, rect.Size.Y)},
						{"self",new Rect2(origin.X + split_offset, origin.Y, separation, rect.Size.Y)},
						{"second",
							new Rect2(origin.X + split_offset + separation, origin.Y, second_width, rect.Size.Y)}
					};
					break;
			}
			return LastPreviousRects;
		}
		else
		{
			var split_offset = Mathf.Clamp(
				rect.Size.Y * percent - separation * 0.5f,
				FirstMinimumSize.Y,
				rect.Size.Y - SecondMinimumSize.Y - separation
			);
			var second_height = rect.Size.Y - split_offset - separation;
			switch (s)
			{
				case "second":
				{
					var ds = LastPreviousRects["first"].Size.Y;
					var second_height_m = rect.Size.Y - separation - ds;
					LastPreviousRects = new Godot.Collections.Dictionary<string, Rect2>(){
						{"first",new Rect2(LastPreviousRects["first"].Position, LastPreviousRects["first"].Size)},
						{"self",new Rect2(LastPreviousRects["self"].Position, LastPreviousRects["self"].Size)},
						{"second",
							new Rect2(LastPreviousRects["second"].Position, new Vector2(rect.Size.X,second_height_m))}
					};
					break;
				}
				case "first":
				{
					var ds = LastPreviousRects["second"].Size.Y;
					var second_height_m = rect.Size.Y - separation - ds;
					LastPreviousRects = new Godot.Collections.Dictionary<string, Rect2>(){
						{"first",new Rect2(new Vector2(rect.Position.X,rect.Position.Y),  new Vector2(rect.Size.X,second_height_m))},
						{"self",new Rect2(LastPreviousRects["self"].Position, LastPreviousRects["self"].Size)},
						{"second",
							new Rect2(LastPreviousRects["second"].Position,LastPreviousRects["second"].Size)}
					};
					break;
				}
				default:
					LastPreviousRects = new Godot.Collections.Dictionary<string, Rect2>(){
						{"first",new Rect2(origin.X, origin.Y, rect.Size.X,split_offset )},
						{"self",new Rect2(origin.X,origin.Y + split_offset, rect.Size.X, separation)},
						{"second",
							new Rect2(origin.X, origin.Y + split_offset + separation, rect.Size.X,second_height)}
					};
					break;
			}
			return LastPreviousRects;
		}
	}
}
