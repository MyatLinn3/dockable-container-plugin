using Godot;

[Tool]
public partial class DragNDropPanel : Control
{

	enum MARGIN{
		 MARGIN_LEFT, MARGIN_RIGHT, MARGIN_TOP, MARGIN_BOTTOM, MARGIN_CENTER 
	}

	readonly int DRAW_NOTHING = -1;
	readonly int DRAW_CENTERED = -2;
	readonly int MARGIN_NONE = -1;

	int _DrawMargin = -1;
	bool _ShouldSplit = false;

	public override void _Notification(int what)
	{
		if (what == NotificationMouseExit)
		{
			_DrawMargin = DRAW_NOTHING;
			QueueRedraw();
		}
		else if(what == NotificationMouseEnter && !_ShouldSplit)
		{
			_DrawMargin = DRAW_CENTERED;
			QueueRedraw();
		}
	}

	public override void _GuiInput(InputEvent @event)
	{
		switch (_ShouldSplit)
		{
			case true when @event is InputEventMouseMotion:
				_DrawMargin = _FindHoverMargin(((InputEventMouseMotion)@event).Position);
				QueueRedraw();
				break;
		}
	}

	public override void _Draw()
	{
		var rect = new Rect2();
		switch (_DrawMargin)
		{
			case -1:
				rect = new Rect2(Vector2.Zero,Size);
				break;
			case (int)MARGIN.MARGIN_LEFT:
				rect = new Rect2(0,0,Size.X * 0.5f,Size.Y );
				break;
			case (int)MARGIN.MARGIN_TOP:
				rect = new Rect2(0,0,Size.X,Size.Y * 0.5f);
				break;
			case (int)MARGIN.MARGIN_RIGHT:
				var halfWidth = Size.X * 0.5f;
				rect = new Rect2(halfWidth,0,halfWidth,Size.Y);
				break;
			case (int)MARGIN.MARGIN_BOTTOM:
				var halfHeight = Size.Y * 0.5f;
				rect = new Rect2(0,halfHeight,Size.X,halfHeight);
				break;
		}
		var styleBox = GetThemeStylebox("panel","TooltipPanel");
		DrawStyleBox(styleBox,rect);
	}

	public void SetEnabled(bool enabled,bool shouldSplit=true)
	{
		Visible = enabled;
		_ShouldSplit = shouldSplit;
		switch (enabled)
		{
			case true:
				_DrawMargin = DRAW_NOTHING;
				QueueRedraw();
				break;
		}
	}

	public int GetHoverMargin()
	{
		return _DrawMargin;
	}

	public int _FindHoverMargin(Vector2 point)
	{
		var halfSize = new Vector2(Size.X*0.5f,Size.Y*0.5f);
		
		var left = point.DistanceSquaredTo(new Vector2(0,halfSize.Y));
		var lesser = left;
		var lesserMargin = (int)MARGIN.MARGIN_LEFT;

		var top = point.DistanceSquaredTo(new Vector2(halfSize.X,0));
		if (lesser > top)
		{
			lesser = top;
			lesserMargin = (int)MARGIN.MARGIN_TOP;
		}

		var right = point.DistanceSquaredTo(new Vector2(Size.X,halfSize.Y));
		if (lesser > right)
		{
			lesser = right;
			lesserMargin = (int)MARGIN.MARGIN_RIGHT;
		}

		var bottom = point.DistanceSquaredTo(new Vector2(halfSize.X,Size.Y));
		if (lesser > bottom)
		{
			lesserMargin = (int)MARGIN.MARGIN_BOTTOM;
		}
		return lesserMargin;
	}
}
