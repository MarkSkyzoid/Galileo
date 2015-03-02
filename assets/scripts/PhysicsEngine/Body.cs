using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Galileo
{
	#region Transform
	public class Transform
	{
		public Vector3 position 	= Vector3.zero;
		public Quaternion orientation 	= Quaternion.identity;
        public Matrix4x4 transformMatrix  = Matrix4x4.identity;

        public Transform()
        {
            this.position = Vector3.zero;
		    this.orientation = Quaternion.identity;
        }

        public Transform(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.orientation = rotation;
        }
	}
	#endregion

	#region MassData
	public struct MassData
	{
		float mass;
		float inverseMass;

        // Inertia tensor and its inverse.
		Matrix3x3 inertia;
	    Matrix3x3 inverseInertia;

        public MassData(float mass, Matrix3x3 inertia)
        {
            this.mass = mass;

            if (this.mass == 0.0f)
                this.inverseMass = 0.0f;
            else
                this.inverseMass = 1.0f / mass;


            this.inertia = inertia;
            this.inverseInertia = inertia.inverse;
        }

        #region Accessors
        public void SetMass(float mass)
        {
            this.mass = mass;

            // Represent infinite mass with 0.0f.
            if (this.mass == 0.0f)
            {
                inverseMass = 0.0f;
            }
            else
            {
                inverseMass = 1.0f / mass;
            }
        }

        public void SetInertia(Matrix3x3 inertia)
        {
            this.inertia = inertia;

            this.inverseInertia = inertia.inverse;
        }

        public float Mass 
        { 
           get { return mass; }
           set { SetMass(mass); }
        }
        public float InverseMass
        {
            get { return inverseMass; }
        }

        public Matrix3x3 Inertia 
        { 
            get { return inertia; }
            set { SetInertia(value);  }
        }
        public Matrix3x3 InverseInertia
        {
            get { return inverseInertia; }
        }
        #endregion
    }
	#endregion

	#region Material
	public struct Material
	{
		float density;
		float restitution;
        
		float staticFriction;
		float dynamicFriction;

        #region Accessors
        public float Density
        {
            get { return density; }
            set { density = value; }
        }
        public float Restitution
        {
            get { return restitution; }
            set { restitution = value; }
        }

		public float StaticFriction
		{
			get { return staticFriction; }
			set { staticFriction = value; }
		}
		public float DynamicFriction
		{
			get { return dynamicFriction; }
			set { dynamicFriction = value; }
		}
        #endregion

		Material(float density, float restitution, float static_friction, float dynamic_friction)
		{
			this.density 			= density;
			this.restitution		= restitution;
			this.staticFriction 	= static_friction;
			this.dynamicFriction	= dynamic_friction;
		}

		#region PresetMaterials
		public static Galileo.Material Rock 	= new Galileo.Material (0.6f, 0.1f, 0.4f, 0.5f);
		public static Galileo.Material Wood 	= new Galileo.Material (0.3f, 0.2f, 0.4f, 0.5f);
		public static Galileo.Material Metal 	= new Galileo.Material (1.2f, 0.05f, 0.4f, 0.5f);
		public static Galileo.Material Pillow 	= new Galileo.Material (0.1f, 0.2f, 0.4f, 0.5f);
		public static Galileo.Material Rubber   = new Galileo.Material (1.1f, 0.828f, 0.4f, 0.5f);
		public static Galileo.Material Static 	= new Galileo.Material (0.0f, 0.4f, 0.4f, 0.5f);
		#endregion
	}
	#endregion

	// GUID Generator.
    public static class GUID
    {
        private static int nextGUID = 0;

        public static int NextGUID()
        {
            return nextGUID++;
        }
    }

	// Class representing a rigid body.
	#region Body
	public class Body 
	{
        int                 id = -1; // Uniquely identifies the body.
		Shape				shape = null;
		Galileo.Transform 	transform = new Galileo.Transform();
		MassData			massInfo  = new MassData();
		Galileo.Material 	material;
		Vector3				velocity = Vector3.zero;
        Vector3             angularMomentum = Vector3.zero;
		Vector3				angularVelocity = Vector3.zero;
		
		Vector3				force = Vector3.zero;
		Vector3				torque = Vector3.zero;

		float				gravityScale = 1.0f;

        PhysicsWorld        world = null;
        
		List<Contact>		contacts = new List<Contact>();

		// Sleep variables.
		bool				isAwake = true;
		float				motion = 0.0f;
		public static float	SleepEpsilon = 0.27f;

        #region Accessors
        public int ID
        {
            get { return id; }
        }

        public Shape Shape
        {
            get { return shape; }
        }

        public Galileo.Transform Transform
        {
            get { return transform;  }
            set { transform = value; }
        }
        public MassData MassInfo
        {
            get { return massInfo; }
            set { massInfo = value; }
        }
        public Material MaterialInfo
        {
            get { return material; }
            set { material = value; }
        }
        public Vector3 Velocity 
        {
			get { return velocity; }
			set { velocity = value; }
		}

        public Vector3 AngularMomentum
        {
            get { return angularMomentum; }
            set { angularMomentum = value; }
        }
		public Vector3 AngularVelocity 
        {
			get { return angularVelocity; }
			set { angularVelocity = value; }
		}

        public Vector3 Force
        {
            get { return force; }
            set { force = value; }
        }
        public Vector3 Torque
        {
            get { return torque; }
            set { torque = value; }
        }

        public Galileo.PhysicsWorld World
        {
            get { return world; }
            set { world = value; }
        }

		public List<Contact> Contacts
		{
			get { return contacts; }
			set { contacts = value; }
		}


		public bool IsAwake
		{
			get{ return isAwake;}
			set
			{ 
				if(value == true)
				{
					isAwake = true;

					motion = SleepEpsilon * 2.0f; // Prevent the body from falling asleep immediately.
				}
				else
				{
					isAwake = false;
					velocity = Vector3.zero;
					angularVelocity = Vector3.zero;
				}
			}
		}
		public float Motion
		{
			get{ return motion;}
			set{ motion = value;}
		}
        #endregion

        public Body(Shape shape, Galileo.Material material)
		{
            id = GUID.NextGUID();

			this.shape = shape;
			this.material = material;

			// Init the shape and, thus, the body information.
			this.shape.Init(this);
		}

        public Body(Shape shape, Galileo.Material material, Galileo.Transform transform)
        {
            id = GUID.NextGUID();
            
            this.shape = shape;
            this.material = material;

            // Init the shape and, thus, the body information.
            this.shape.Init(this);

            this.transform = transform;

			this.isAwake = true;
        }

        public void UpdateTransforms()
        {
            if (shape == null)
                return;

            shape.UpdateBodyTransforms();
        }

		public void AddForce(Vector3 force)
		{
			isAwake = true;
			this.force += force;
		}

        // The Point is specified in world coordinates.
        public void AddForceAtPoint(Vector3 force, Vector3 point)
        {
			isAwake = true;
            Vector3 center_to_point = point - transform.position;

            this.force += force;
            this.torque += Vector3.Cross(center_to_point, force);
            GlobalDebugRenderer.Instance.AddCommand(new DebugSphereCommand(point, 0.1f, false, Color.green));
        }

        // Returns the inverse inertia tensor in world coordinates.

        public Matrix3x3 GetInverseInertiaWorld()
        {
            //Matrix4x4 R = Matrix4x4.TRS(Vector3.zero, transform.orientation, Vector3.one);

            //return R * MassInfo.InverseInertia * R.transpose;

            Matrix3x3 R = Matrix3x3.FromQuaternion(transform.orientation);
            return R * MassInfo.InverseInertia * R.transpose;
        }
	}
	#endregion
}
