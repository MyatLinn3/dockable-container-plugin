using Godot;

[Tool]
public partial class DockableLayoutNode : Resource
{
	public DockableLayoutSplit Parent;

	public void EmitTreeChanged()
	{
		var node = this;
		while(node != null){
			node.EmitChanged();
			node = node.Parent;
		}
	}

	public virtual bool IsEmpty()
	{
		return true;
	}

	public virtual Godot.Collections.Array<string> GetNames()
	{
		return new Godot.Collections.Array<string>();
	}


}
