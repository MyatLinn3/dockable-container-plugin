using Godot;

[Tool]
public partial class new_script : EditorPlugin
{

	EditorInspector editorInspectorPlugin;

	public override void _EnterTree()
	{
		editorInspectorPlugin = new EditorInspector();
		var dockableContainer = GD.Load<Script>("res://addons/dockable_container_c#/dockable_container.cs");
		AddCustomType("DockableContainer", "Container",dockableContainer,null);
		AddInspectorPlugin(editorInspectorPlugin);
	}

	public override void _ExitTree()
	{
		RemoveInspectorPlugin(editorInspectorPlugin);
		editorInspectorPlugin = null;
	}
}

