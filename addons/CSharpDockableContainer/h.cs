#if TOOLS
using Godot;

[Tool]
public partial class h : EditorPlugin
{
	EditorInspector editorInspectorPlugin;

	public override void _EnterTree()
	{
		editorInspectorPlugin = new EditorInspector();
		var dockableContainer = GD.Load<Script>("res://addons/CSharpDockableContainer/DockableContainer.cs");
		AddCustomType("DockableContainer", "Container",dockableContainer,null);
		AddInspectorPlugin(editorInspectorPlugin);
	}

	public override void _ExitTree()
	{
		RemoveInspectorPlugin(editorInspectorPlugin);
		RemoveCustomType("DockableContainer");
		editorInspectorPlugin = null;
	}
}
#endif
