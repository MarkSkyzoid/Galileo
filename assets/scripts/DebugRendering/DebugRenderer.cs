using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DebugRenderer : MonoBehaviour 
{
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}

public interface DebugRenderCommand
{
    void Execute(bool gizmos);
	void CleanUp();
}

public class DebugSphereCommand : DebugRenderCommand
{
    Vector3 position;
    float radius;
    bool wire;
    Color col;

	GameObject sphere = null;

    public DebugSphereCommand(Vector3 position, float radius, bool wire = false, Color col = new Color())
    {
        this.position = position;
        this.radius = radius;
        this.wire = wire;
        this.col = col;
    }

    public void Execute(bool gizmos)
    {
		if(gizmos)
		{
	        Gizmos.color = col;
	        if (wire)
	            Gizmos.DrawWireSphere(position, radius);
	        else
	            Gizmos.DrawSphere(position, radius);

			if(sphere != null)
				Object.Destroy(sphere);
		}
		else
		{
			if(sphere == null)
			{
				sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				Object.Destroy(sphere.collider);
				sphere.transform.position = position;
				sphere.transform.localScale = new Vector3(radius * 2, radius * 2, radius * 2);
				sphere.renderer.material.shader = Shader.Find ("Transparent/Diffuse");
				Color c = col;
				c.a = 0.35f;
				sphere.renderer.material.color = c;
			}
		}
    }

	public void CleanUp()
	{
		if (sphere != null)
			Object.Destroy (sphere);
	}

}

public class DebugLineCommand : DebugRenderCommand
{
    Vector3 start;
    Vector3 end;
    Color col;

    public DebugLineCommand(Vector3 start, Vector3 end, Color col = new Color())
    {
        this.start = start;
        this.end = end;
        this.col = col;
    }

    public void Execute(bool gizmos)
    {
		if(gizmos)
		{
        	Gizmos.color = col;
      		Gizmos.DrawLine(start, end);
    	}
	}

	public void CleanUp()
	{
	}
}


public class GlobalDebugRenderer : MonoBehaviour
{
    int executedCommands = 0;

	public bool DebugActive = false;

    static GlobalDebugRenderer instance;
    public static GlobalDebugRenderer Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject();
                instance = go.AddComponent<GlobalDebugRenderer>();
            }

            return instance;
        }
    }

    List<DebugRenderCommand> commands = new List<DebugRenderCommand>();

    public void AddCommand(DebugRenderCommand command)
    {
		commands.Add(command);
    }

    public void ClearCommands()
    {
        executedCommands = 0;
		foreach (DebugRenderCommand dc in commands)
			dc.CleanUp ();

        commands.Clear();
    }

    void FixedUpdate()
    {
		if(DebugActive)
		{
			foreach (DebugRenderCommand c in commands)
			{
				c.Execute(false);
				++executedCommands;
			}
		}
    }

    void OnDrawGizmos()
    {
        foreach (DebugRenderCommand c in commands)
        {
            c.Execute(true);
            ++executedCommands;
        }
    }
}
