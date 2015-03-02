using UnityEngine;
using System.Collections;

namespace Galileo
{
	public class Shape
	{
		protected Body owner;
        public Body Owner
        {
            get { return owner; }
        }

		protected float volume = 0.0f;
		public float GetVolume() { return volume; }

		public void Init(Body body)
		{
			owner = body;
			CalculateVolume();

			// TODO: Calculate body's mass information based on the body's material.
            float mass = owner.MaterialInfo.Density * volume;

            Matrix3x3 inertia_tensor = GetInertiaTensor(mass);

            owner.MassInfo = new MassData(mass, inertia_tensor);
		}

        protected virtual void CalculateVolume() { volume = 1.0f; }  
        protected virtual Matrix3x3 GetInertiaTensor(float mass) { return Matrix3x3.identity;  }

        public virtual BoundingSphere GetBoundingSphere() { return new BoundingSphere(0.5f, Owner.Transform.position); }

        public virtual void UpdateBodyTransforms() {}
	}

    public class Sphere : Shape
    {
        private float radius;
        public float Radius
        {
            get { return radius; }
            set { radius = value; }
        }

        public Sphere(float radius)
        {
            this.radius = radius;
        }

        protected override void CalculateVolume()
        {
            volume = (4.0f / 3.0f) * Mathf.PI * (radius * radius * radius);
        }

        public override BoundingSphere GetBoundingSphere()
        {
            return new BoundingSphere(radius, Owner.Transform.position);
        }

        protected override Matrix3x3 GetInertiaTensor(float mass)
        {
            Matrix3x3 inertia = new Matrix3x3();
            float val = (2.0f / 5.0f * mass * radius * radius);
            inertia.m00 = val; inertia.m11 = val; inertia.m22 = val;
            return inertia;
        }
    }

    public class Box : Shape
    {
        Vector3 halfSize = Vector3.zero;
        public UnityEngine.Vector3 HalfSize
        {
            get { return halfSize; }
            set { SetHalfSize(value); }
        }

        float diagonal = 0.0f; // Used to comput bounding sphere;

        // Dirty flag to avoid allocations/deallocations (even if garbage collection is used, doing this at every broadphase might be expensive!) and extra computations.
        enum EBoxDirtyFlag
        {
            e_DirtyNone = 0,
            e_DirtyVolume = 1,
            e_DirtyBoundingSphere = 1 << 2
        };
        EBoxDirtyFlag dirtyFlag = EBoxDirtyFlag.e_DirtyNone;

        BoundingSphere boundingSphere = null;

        public Box(Vector3 halfSize)
        {
            this.HalfSize = halfSize;
        }

        private void SetHalfSize(Vector3 half_size)
        {
            this.halfSize = half_size;

            diagonal = halfSize.magnitude;

            dirtyFlag = dirtyFlag | EBoxDirtyFlag.e_DirtyVolume | EBoxDirtyFlag.e_DirtyBoundingSphere;
        }

        public override BoundingSphere GetBoundingSphere()
        {
            if (boundingSphere == null || ((dirtyFlag & EBoxDirtyFlag.e_DirtyBoundingSphere) != 0))
            {
                boundingSphere = new BoundingSphere(diagonal, Owner.Transform.position);
                dirtyFlag = dirtyFlag & ~EBoxDirtyFlag.e_DirtyBoundingSphere;
            }
            else
            {
                boundingSphere.Center = Owner.Transform.position;
            }
            
            return boundingSphere;
        }

        protected override void CalculateVolume()
        {
            if ((dirtyFlag & EBoxDirtyFlag.e_DirtyVolume) != 0)
            {
                volume = halfSize.x * 2 * halfSize.y * 2 * halfSize.z * 2;
                dirtyFlag = dirtyFlag & ~EBoxDirtyFlag.e_DirtyVolume;
            }
        }

        public override void UpdateBodyTransforms()
        {
            Galileo.Transform body_t = owner.Transform;
            body_t.transformMatrix = Matrix4x4.TRS(body_t.position, body_t.orientation, Vector3.one);
        }

        protected override Matrix3x3 GetInertiaTensor(float mass)
        {
            Matrix3x3 inertia = Matrix3x3.identity;

            inertia.m00 = (1.0f / 12.0f) * mass * (halfSize.y * 2 * halfSize.y * 2 + halfSize.z * 2 * halfSize.z * 2);
            inertia.m11 = (1.0f / 12.0f) * mass * (halfSize.x * 2 * halfSize.x * 2 + halfSize.z * 2 * halfSize.z * 2);
            inertia.m22 = (1.0f / 12.0f) * mass * (halfSize.x * 2 * halfSize.x * 2 + halfSize.y * 2 * halfSize.y * 2);

            return inertia;
        }
    }

    // A plane (technically half-space) will have always infinite mass in this system.
    public class Plane : Shape
    {
        Vector3 normal;
        public UnityEngine.Vector3 Normal
        {
            get { return normal; }
            set { normal = value; }
        }

        float offset; // Offset along the normal.
        public float Offset
        {
            get { return offset; }
            set { offset = value; }
        }

        public Plane(Vector3 normal, float offset)
        {
            this.normal = normal;
            this.offset = offset;
        }

        protected override void CalculateVolume()
        {
            volume = 0.0f;
        }

        public override BoundingSphere GetBoundingSphere()
        {
            PhysicsWorld w = owner.World;
            float extents = Mathf.Max(w.Extents.x, Mathf.Max(w.Extents.y, w.Extents.z));
            return new BoundingSphere(extents, Owner.Transform.position);
        }
    }
}