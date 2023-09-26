#if TOOLS
using Godot;
using System;

public partial class EditorInspector : EditorInspectorPlugin
{
	
	public EditorInspector()
	{
		
	}
	public override bool _CanHandle(GodotObject @object)
	{
//		return true;
		return @object is DockableContainer;
	}

	public override bool _ParseProperty(GodotObject @object, Variant.Type type,
		string name, PropertyHint hintType, string hintString,
		PropertyUsageFlags usageFlags, bool wide) 
	{
		if(name == "Layout")
		{
		LayoutEditorProperty editorProperty = new LayoutEditorProperty();
		AddPropertyEditor("layout",editorProperty);
		}
		return false;
	}
}
#endif
