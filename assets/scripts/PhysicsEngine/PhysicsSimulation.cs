using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Galileo;

using System.IO;

// This class overrides Unity's behaviours and runs a simulation loop using a fixed time step for the physics update.
public class PhysicsSimulation : MonoBehaviour 
{
	#region TimeStepping
	private float fps 				= 100.0f;
	private float fixedDeltaTime;
	private float accumulator		= 0.0f;
	private float accumulatorMax	= 0.2f; // Avoids stalls by clamping the accumulator maximum value.

	private float startTime 		= 0.0f;
	#endregion

	#region Physics
	private PhysicsWorld physicsWorld = null;
	#endregion

    public enum EObjectType
    {
        e_Sphere,
        e_Cube,
        e_Plane,

        // TODO: Add other shapes before this.
        e_Count
    }

	#region Unity related Members
    public struct UnityObjectBinding
    {
        public Body body;
        public GameObject gameObject;

        public UnityObjectBinding(Body b, GameObject g)
        {
            this.body = b;
            this.gameObject = g;
        }
    }
	List<UnityObjectBinding> unityObjects = new List<UnityObjectBinding>();

    public GameObject spherePrefab = null;
    public GameObject cubePrefab = null;
    public GameObject planePrefab = null;

	string k_scenes_path = "/Resources/";

	int global_debug_renderer_clear_countdown = 1;

	bool spawn_sphere = true;
	bool spawn_cube = false;

	#endregion

    bool dragging = false;
    Body draggedBody = null;
    Vector3 lastMousePosition = Vector3.zero;

	void Start () 
	{
		fixedDeltaTime = 1.0f / fps;
		physicsWorld = new PhysicsWorld();

        SetUpDefaultScene();
		
		startTime = Time.time;
	}

	void Update () 
	{
        DoInteraction();

		global_debug_renderer_clear_countdown--;

		if(global_debug_renderer_clear_countdown <= 0)
		{
			GlobalDebugRenderer.Instance.ClearCommands();
			global_debug_renderer_clear_countdown = 3;
		}

		float current_time = Time.time;

		accumulator += current_time - startTime;

		startTime = current_time;

		// Avoid stalls.
		if(accumulator > accumulatorMax)
			accumulator = accumulatorMax;

		while(accumulator > fixedDeltaTime)
		{
			// TODO: Update physics.
			physicsWorld.Step(fixedDeltaTime);
			accumulator -= fixedDeltaTime;
		}

		// Synchronize Unity.
        SyncUnity();
	}

    // TODO: Wrap all the parameters in a structure (type, transform, physics info - eg. material - )
	public void AddObject(EObjectType type, Vector3 position, Vector3 normal, Galileo.Material material, Vector3 half_extent, float radius)
	{  
        if (physicsWorld == null)
            return;

        Shape shape = null;

        normal.Normalize();

        Quaternion orientation = Quaternion.identity;
		
		orientation = Quaternion.FromToRotation(new Vector3(0.0f, 1.0f, 0.0f), normal);

        GameObject template = null;
        
		Vector3 scale = Vector3.one;

		switch (type)
        {
            case EObjectType.e_Sphere:
                template = spherePrefab;
                shape = new Sphere(radius);
				scale = Vector3.one * radius * 2;
                break;
            case EObjectType.e_Cube:
                template = cubePrefab;
                shape = new Box(half_extent);
				scale = half_extent * 2;
                //normal = new Vector3(0.2f, 1.0f, 0.0f);
                //normal.Normalize();
                //orientation = Quaternion.FromToRotation(new Vector3(0.0f, 1.0f, 0.0f), normal);
                break;
            case EObjectType.e_Plane:
                template = planePrefab;
                shape = new Galileo.Plane(normal, 0.0f);
				scale = physicsWorld.Extents;
                break;
            default:
                template = spherePrefab;
                break;
        }

        GameObject g = (GameObject)Instantiate(template, position, Quaternion.identity);
        //g.AddComponent<DebugRenderer>();
		g.transform.localScale = scale;

        Galileo.Transform t = new Galileo.Transform(position, orientation);
        Body b = new Body(shape, material, t);

        UnityObjectBinding binding = new UnityObjectBinding(b, g);

        physicsWorld.AddBody(b);
        unityObjects.Add(binding);
	}
    
	public void RemoveObject(UnityObjectBinding b)
	{
		physicsWorld.RemoveBody (b.body);
		Destroy (b.gameObject);
	}

    // Interaction.
    void DoInteraction()
    {
        if(Input.GetMouseButton(2))
        {
            if(!dragging)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit = new RaycastHit();
                bool got_something = Physics.Raycast(ray, out hit);
                if(got_something)
                {
                    GameObject go = hit.collider.gameObject;
                    foreach (UnityObjectBinding ub in unityObjects)
                    {
                        if(ub.gameObject.Equals(go))
                        {
                            lastMousePosition = Input.mousePosition;
                            dragging = true;
                            draggedBody = ub.body;
                        }
                    }
                }
            }
        }
        else if(Input.GetMouseButtonUp(2))
        {
            dragging = false;
            //draggedBody = null;
        }

		// Selection using right mouse button.
		if(Input.GetMouseButton(1))
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit = new RaycastHit();
			bool got_something = Physics.Raycast(ray, out hit);
			if(got_something)
			{
				GameObject go = hit.collider.gameObject;
				foreach (UnityObjectBinding ub in unityObjects)
				{
					if(ub.gameObject.Equals(go))
					{
						//dragging = true;
						draggedBody = ub.body;
					}
				}
			}
		}

        if(dragging && draggedBody != null)
        {
            Vector3 mouse_pos = Input.mousePosition;
            Vector3 delta_mouse = mouse_pos - lastMousePosition;
            lastMousePosition = mouse_pos;

            delta_mouse.z = draggedBody.Transform.position.z;

            //mouse_pos.z = 10;
            //mouse_pos = Camera.main.ScreenToWorldPoint(mouse_pos);
            
           // Vector3 delta_mouse = mouse_pos - draggedBody.Transform.position;
            
            Debug.DrawLine(draggedBody.Transform.position, draggedBody.Transform.position + delta_mouse.normalized);

            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit))
            {
                draggedBody.Torque = Vector3.zero;
                draggedBody.AddForceAtPoint(ray.direction.normalized * 70.0f, hit.point);
            }

        }
    }

	#region UnityRelated
	// Keep Unity in synchronization.
	private void SyncUnity()
	{
        // Spawn Object. TODO: Improve.
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouse = Input.mousePosition;
            mouse.z = 10.0f;
            Vector3 p = Camera.main.ScreenToWorldPoint(mouse);
            if(spawn_sphere)
				AddObject(EObjectType.e_Sphere, p, Vector3.up, Galileo.Material.Metal, Vector3.one, 0.5f);
			else if(spawn_cube)
				AddObject(EObjectType.e_Cube, p, Vector3.up, Galileo.Material.Metal, new Vector3(0.5f, 0.5f, 0.5f), 0.0f);
        }
		
		// Keep them in sync.
	   foreach(UnityObjectBinding binding in unityObjects)
	   {
			if(binding.body == null || binding.gameObject == null)
				continue;

           Body b = binding.body;
           UnityEngine.Transform t = binding.gameObject.transform;
           t.position = b.Transform.position;
           t.rotation = b.Transform.orientation;
	   }
	}
	#endregion

	#region GUI

	void OnGUI()
	{
		Vector2 screen_size = new Vector2(Screen.width, Screen.height);

		Vector2 window_position = new Vector2 (0.0f, 0.0f);
		Vector2 window_size = new Vector2 (0.2f, 0.37f);

        // Info text positions.
        const float offset_left = 0.01f;
        const float height = 0.05f;
        const float width = 0.3f;

        Vector2 id_position = new Vector2(offset_left, 0.02f);
        Vector2 id_size = new Vector2(width, id_position.y + height);
        id_position = Vector2.Scale(id_position, screen_size); 
        id_size = Vector2.Scale(id_size, screen_size);

		Vector2 mass_position = new Vector2(offset_left, 0.05f);
		Vector2 mass_size = new Vector2(width, mass_position.y + height);
		mass_position = Vector2.Scale(mass_position, screen_size); 
		mass_size = Vector2.Scale(mass_size, screen_size);

		Vector2 velocity_position = new Vector2(offset_left, 0.08f);
		Vector2 velocity_size = new Vector2(width, velocity_position.y + height);
		velocity_position = Vector2.Scale(velocity_position, screen_size); 
		velocity_size = Vector2.Scale(velocity_size, screen_size);

		Vector2 ang_velocity_position = new Vector2(offset_left, 0.11f);
		Vector2 ang_velocity_size = new Vector2(width, ang_velocity_position.y + height);
		ang_velocity_position = Vector2.Scale(ang_velocity_position, screen_size); 
		ang_velocity_size = Vector2.Scale(ang_velocity_size, screen_size);

		Vector2 restitution_position = new Vector2(offset_left, 0.14f);
		Vector2 restitution_size = new Vector2(width, restitution_position.y + height);
		restitution_position = Vector2.Scale(restitution_position, screen_size); 
		restitution_size = Vector2.Scale(restitution_size, screen_size);

		Vector2 s_friction_position = new Vector2(offset_left, 0.17f);
		Vector2 s_friction_size = new Vector2(width, s_friction_position.y + height);
		s_friction_position = Vector2.Scale(s_friction_position, screen_size); 
		s_friction_size = Vector2.Scale(s_friction_size, screen_size);

		Vector2 d_friction_position = new Vector2(offset_left, 0.20f);
		Vector2 d_friction_size = new Vector2(width, d_friction_position.y + height);
		d_friction_position = Vector2.Scale(d_friction_position, screen_size); 
		d_friction_size = Vector2.Scale (d_friction_size, screen_size);

		Vector2 awake_position = new Vector2(offset_left, 0.23f);
		Vector2 awake_size = new Vector2(width, awake_position.y + height);
		awake_position = Vector2.Scale(awake_position, screen_size); 
		awake_size = Vector2.Scale (awake_size, screen_size);

		Vector2 spawn_label_position = new Vector2(offset_left, 0.28f);
		Vector2 spawn_cube_position = new Vector2(offset_left + 0.05f, 0.28f);
		Vector2 spawn_sphere_position = new Vector2(offset_left + 0.1f, 0.28f);
		Vector2 spawn_label_size = new Vector2(0.05f, 0.038f);
		spawn_label_position = Vector2.Scale(spawn_label_position, screen_size); 
		spawn_cube_position = Vector2.Scale(spawn_cube_position, screen_size);
		spawn_sphere_position = Vector2.Scale(spawn_sphere_position, screen_size);
		spawn_label_size = Vector2.Scale (spawn_label_size, screen_size);

		Vector2 debug_label_position = new Vector2 (offset_left, 0.32f);
		Vector2 debug_toggle_position = new Vector2 (offset_left + 0.1f, 0.32f);
		Vector2 debug_label_size = new Vector2(0.1f, debug_label_position.y + height);
		debug_label_position = Vector2.Scale(debug_label_position, screen_size);
		debug_toggle_position = Vector2.Scale(debug_toggle_position, screen_size);
		debug_label_size = Vector2.Scale (debug_label_size, screen_size);

		window_position = Vector2.Scale(window_position, screen_size);
		window_size = Vector2.Scale(window_size, screen_size);

		GUI.Box(new Rect(window_position.x, window_position.y, window_size.x, window_size.y), "Body Info.");

        // Info.
        if (draggedBody != null)
        {
            // Labels.
            string ID_string = "ID: " + draggedBody.ID.ToString();
            GUI.Label(new Rect(id_position.x, id_position.y, id_size.x, id_size.y), ID_string);
			
			string mass_string = "Mass: " + draggedBody.MassInfo.Mass.ToString();
			GUI.Label(new Rect(mass_position.x, mass_position.y, mass_size.x, mass_size.y), mass_string);

			string velocity_string = "Linear Velocity: " + draggedBody.Velocity.ToString();
			GUI.Label(new Rect(velocity_position.x, velocity_position.y, velocity_size.x, velocity_size.y), velocity_string);

			string ang_velocity_string = "Angular Velocity: " + draggedBody.AngularVelocity.ToString();
			GUI.Label(new Rect(ang_velocity_position.x, ang_velocity_position.y, ang_velocity_size.x, ang_velocity_size.y), ang_velocity_string);

			string force_string = "Restitution: " + draggedBody.MaterialInfo.Restitution.ToString();
			GUI.Label(new Rect(restitution_position.x, restitution_position.y, restitution_size.x, restitution_size.y), force_string);
			
			string s_friction_string = "Static Friction: " + draggedBody.MaterialInfo.StaticFriction.ToString();
			GUI.Label(new Rect(s_friction_position.x, s_friction_position.y, s_friction_size.x, s_friction_size.y), s_friction_string);

			string d_friction_string = "Dynamic Friction: " + draggedBody.MaterialInfo.DynamicFriction.ToString();
			GUI.Label(new Rect(d_friction_position.x, d_friction_position.y, d_friction_size.x, d_friction_size.y), d_friction_string);
			
			string awake_string = "Awake: " + draggedBody.IsAwake.ToString();
			GUI.Label(new Rect(awake_position.x, awake_position.y, awake_size.x, awake_size.y), awake_string);
        }

		{
			GUI.Label (new Rect(spawn_label_position.x, spawn_label_position.y, spawn_label_size.x, spawn_label_size.y), "Spawn: ");
			if(GUI.Toggle (new Rect(spawn_cube_position.x, spawn_cube_position.y, spawn_label_size.x, spawn_label_size.y), spawn_cube, "Cube"))
			{
				spawn_cube = true;
				spawn_sphere = false;
			}
			if(GUI.Toggle (new Rect(spawn_sphere_position.x, spawn_sphere_position.y, spawn_label_size.x, spawn_label_size.y), spawn_sphere, "Sphere"))
			{
				spawn_cube = false;
				spawn_sphere = true;
			}

			GUI.Label (new Rect(debug_label_position.x, debug_label_position.y, debug_label_size.x, debug_label_size.y), "Debug Rendering: ");
			bool debug = GUI.Toggle (new Rect(debug_toggle_position.x, debug_toggle_position.y, debug_label_size.x, debug_label_size.y), GlobalDebugRenderer.Instance.DebugActive, "");
			if(debug != GlobalDebugRenderer.Instance.DebugActive)
			{
				GlobalDebugRenderer.Instance.DebugActive = debug;
			}

		}

		// Scene Loading.
		window_position = new Vector2 (0.8f, 0.0f);
		window_size = new Vector2 (0.2f, 0.3f);
		Vector2 scaled_window_position = Vector2.Scale(window_position, screen_size);
		Vector2 scaled_window_size = Vector2.Scale(window_size, screen_size);
		string scene_box_string = "Load Scene.\n(Current Gravity: " + physicsWorld.Gravity.ToString() + ")\n(Object Count: " + unityObjects.Count.ToString() + ")";
		GUI.Box(new Rect(scaled_window_position.x, scaled_window_position.y, scaled_window_size.x, scaled_window_size.y), scene_box_string);

		int i = 0;

		DirectoryInfo scenes_directory = new DirectoryInfo (Application.dataPath + k_scenes_path);
		FileInfo[] scene_files = scenes_directory.GetFiles ("*.xml");


		foreach(FileInfo scene in scene_files)
		{
			float left = window_position.x + 0.004f;
			float right = 0.5f;
	
			float top = window_position.y + height * 0.6f * i + 0.08f;
			float bottom = height;

			left *= screen_size.x;
			right *= screen_size.x;
			top *= screen_size.y;
			bottom *= screen_size.y;

			Rect rect = new Rect(left, top, 100, 15);
			if(GUI.Button(rect, scene.Name))
			{
				LoadScene(scene.FullName);
			}

			++i;
		}
	}

    private void SetUpDefaultScene()
    {
		DirectoryInfo scenes_directory = new DirectoryInfo (Application.dataPath + k_scenes_path);
		FileInfo[] scene_files = scenes_directory.GetFiles ("*.xml");
		LoadScene (scene_files[0].FullName);
    }

	private void LoadScene(string scene)
	{
		foreach(UnityObjectBinding b in unityObjects)
		{
			RemoveObject(b);
		}
		unityObjects.Clear ();
		Galileo.SceneLoader.LoadScene (scene, this);
	}

	public void ChangeWorldGravity(Vector3 gravity)
	{
		if (physicsWorld == null)
						return;

		physicsWorld.Gravity = gravity;
	}

	#endregion
}
