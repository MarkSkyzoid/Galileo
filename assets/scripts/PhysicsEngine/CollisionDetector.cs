using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using System;

namespace Galileo
{
    public class Contact
    {
		private Vector3[] point = new Vector3[15]; // Point of contact.
        public UnityEngine.Vector3[] Point
        {
            get { return point; }
            set { point = value; }
        }

		private Vector3[] normal = new Vector3[15]; // Normal direction of the contact (world coordinates).
        public UnityEngine.Vector3[] Normal
        {
            get { return normal; }
            set { normal = value; }
        }

        private float[] penetration = new float[15]; // Penetration depth.
        public float[] Penetration
        {
            get { return penetration; }
            set { penetration = value; }
        }

        private Body a;
        public Galileo.Body A
        {
            get { return a; }
            set { a = value; }
        }

        private Body b;
        public Galileo.Body B
        {
            get { return b; }
            set { b = value; }
        }

		public uint ContactsCount = 0;
    }

    public class CollisionData
    {
        List<Contact> contacts = new List<Contact>();
        public List<Contact> Contacts
        {
            get { return contacts; }
            set { contacts = value; }
        }
    }

    // Narrow Phase collision detection.
    [System.Runtime.InteropServices.GuidAttribute("991CCA49-E1EC-49F1-A1B7-C3193BB92DAF")]
    public static class CollisionDetector
    {
        #region Collision Delegates
        delegate uint CollisionDelegate(Shape a, Shape b, CollisionData data);

        struct CollisionKey
        {
            Type a;
            Type b;

            public CollisionKey(Type a, Type b)
            {
                this.a = a;
                this.b = b;
            }
        }

        private static Dictionary<CollisionKey, CollisionDelegate> collisionHandlers;
        #endregion

        static CollisionDetector()
        {
            // Primitive Types.
            Type sphere_type = typeof(Sphere);
            Type plane_type = typeof(Plane);
            Type box_type = typeof(Box);

            // Permutations.
            CollisionKey sphere_sphere = new CollisionKey(sphere_type, sphere_type);

            CollisionKey sphere_plane = new CollisionKey(sphere_type, plane_type);
            CollisionKey plane_sphere = new CollisionKey(plane_type, sphere_type);

            CollisionKey box_box = new CollisionKey(box_type, box_type);

            CollisionKey box_plane = new CollisionKey(box_type, plane_type);
            CollisionKey plane_box = new CollisionKey(plane_type, box_type);

            CollisionKey sphere_box = new CollisionKey(sphere_type, box_type);
            CollisionKey box_sphere = new CollisionKey(box_type, sphere_type);

            // Initialize the collision handlers.
            collisionHandlers = new Dictionary<CollisionKey, CollisionDelegate>();
            collisionHandlers.Add(sphere_sphere, SphereToSphere);

            collisionHandlers.Add(sphere_plane, SphereToPlane);
            collisionHandlers.Add(plane_sphere, SphereToPlane);

            collisionHandlers.Add(box_plane, BoxToPlane);
            collisionHandlers.Add(plane_box, BoxToPlane);

            collisionHandlers.Add(box_sphere, BoxToSphere);
            collisionHandlers.Add(sphere_box, BoxToSphere);

            collisionHandlers.Add(box_box, BoxToBox);
        }


        /* Helpers */

        // Return the extent of a box on a given axis.
        private static float projectBoxToAxis(Box box, Vector3 axis)
        {
            Galileo.Transform box_t = box.Owner.Transform;
            Matrix4x4 box_transform = box_t.transformMatrix; //Matrix4x4.TRS(box_t.position, box_t.rotation, box.HalfSize * 2.0f);

            // Box extent on the plane normal.
            float extent = box.HalfSize.x * Mathf.Abs(Vector3.Dot(axis, box_transform.GetColumn(0))) +
                            box.HalfSize.y * Mathf.Abs(Vector3.Dot(axis, box_transform.GetColumn(1))) +
                            box.HalfSize.z * Mathf.Abs(Vector3.Dot(axis, box_transform.GetColumn(2)));

            return extent;
        }
            
        // Used to perform a single Separating Axis Test   
        private static float PenetrationOnAxis(Box a, Box b, Vector3 axis, Vector3 center_to_center)
        {
            float a_projected = projectBoxToAxis(a, axis);
            float b_projected = projectBoxToAxis(b, axis);

            float distance = Mathf.Abs(Vector3.Dot(center_to_center, axis));

            // A positive value means that the boxes are overlapping.
            // A negative value indicates no overlapping at all.
            return a_projected + b_projected - distance;
        }

        // Check for penetration and keeps track of the smallest (best) inter-penetration so far.
        private static bool TryAxis(Box a, Box b, Vector3 axis, Vector3 center_to_center, uint index, ref float smallest_penetration, ref uint smallest_index)
        {
            // If axis were generated by (almost) parallel edges, skip.
            if (axis.sqrMagnitude < 0.0001f) 
                return true;

            axis.Normalize();

            float penetration = PenetrationOnAxis(a, b, axis, center_to_center);

            if (penetration < 0.0f)
                return false;

            if (penetration < smallest_penetration)
            {
                smallest_penetration = penetration;
                smallest_index = index;
            }

            return true;
        }

        /* Contact point on box edges. */
        private static Vector3 ContactPointOnEdges(Vector3 point_a, Vector3 axis_a, float a_size, Vector3 point_b, Vector3 axis_b, float b_size, bool use_a)
        {
            float sqr_magnitude_a = axis_a.sqrMagnitude;
            float sqr_magnitude_b = axis_b.sqrMagnitude;

            float dot_axis = Vector3.Dot(axis_a, axis_b);

            Vector3 b_to_a = point_a - point_b;

            // How much the distance vector is in the direction of each edge.
            float dot_a_distance = Vector3.Dot(axis_a, b_to_a);
            float dot_b_distance = Vector3.Dot(axis_b, b_to_a);

            float denominator = sqr_magnitude_a * sqr_magnitude_b - dot_axis * dot_axis;

            // A zero denominator indicates parallel lines.
            if (denominator < 0.0001f)
            {
                return use_a ? point_a : point_b;
            }

            float a = (dot_axis * dot_b_distance - sqr_magnitude_b * dot_a_distance) / denominator;
            float b = (sqr_magnitude_a * dot_b_distance - dot_axis * dot_a_distance) / denominator;

            // If the nearest point on either of the edges is out of bounds, we have an edge-face contact.
            if (a > a_size ||
                a < -a_size ||
                b > b_size ||
                b < -b_size)
            {
                return use_a ? point_a : point_b;
            }
            else
            {
               
                Vector3 contact_a = point_a + axis_a * a;
                Vector3 contact_b = point_b + axis_b * b;

                return contact_a * 0.5f + contact_b * 0.5f;
            }
        }

        /* Computes Contact Data for Box vertex to Box face contact */
        private static void ComputeContactBoxVertexBoxFace(Box a, Box b, Vector3 center_to_center, CollisionData data, uint best, float penetration, int contact_index)
        {
            Galileo.Transform a_transform = a.Owner.Transform;

            Vector3 normal = a_transform.transformMatrix.GetColumn((int)best);
            if (Vector3.Dot(normal, center_to_center) > 0.0f)
            {
                normal *= -1.0f;
            }

            Vector3 vertex = b.HalfSize;
            
            Galileo.Transform b_transform = b.Owner.Transform;
            if (Vector3.Dot(b_transform.transformMatrix.GetColumn(0), normal) < 0.0f) vertex.x = -vertex.x;
            if (Vector3.Dot(b_transform.transformMatrix.GetColumn(1), normal) < 0.0f) vertex.y = -vertex.y;
            if (Vector3.Dot(b_transform.transformMatrix.GetColumn(2), normal) < 0.0f) vertex.z = -vertex.z;

            Contact contact = new Contact();
            contact.Point[contact_index] = b_transform.transformMatrix.MultiplyPoint(vertex);
			contact.Normal[contact_index] = normal;
			contact.Penetration[contact_index] = penetration;
			contact.ContactsCount += 1;

            contact.A = a.Owner;
            contact.B = b.Owner;

            data.Contacts.Add(contact);
        }

        /* Collision routines. */

        public static uint Collide(Shape a, Shape b, CollisionData data)
        {
            CollisionKey key = new CollisionKey(a.GetType(), b.GetType());

            if (collisionHandlers.ContainsKey(key) == false) 
                return 0;

            return collisionHandlers[key](a, b, data);
        }

        /* Sphere to Sphere collision */
        private static uint SphereToSphere(Shape a, Shape b, CollisionData data)
        {
            //Debug.Log("Sphere to Sphere collision detection.");

            Sphere A = a as Sphere;
            Sphere B = b as Sphere;

            // Should never happen.
            if (A == null || B == null)
                return 0;

            Transform A_t = A.Owner.Transform;
            Transform B_t = B.Owner.Transform;

            Vector3 midline = A_t.position - B_t.position;
            float size_sqr  = midline.sqrMagnitude;

            float radii_sum = A.Radius + B.Radius;

            if (size_sqr <= 0.0f || size_sqr >= (radii_sum * radii_sum))
                return 0;

            Vector3 normal = midline.normalized;

            Contact contact = new Contact();
            contact.Normal[0] = normal;
            contact.Point[0] = (A_t.position + B_t.position) * 0.5f; // Middle point.
            contact.Penetration[0] = radii_sum - Mathf.Sqrt(size_sqr);
			contact.ContactsCount += 1;

            contact.A = A.Owner;
            contact.B = B.Owner;

            // TODO: Add FRICTION and Restitution data.

            data.Contacts.Add(contact);
            return 1;
        }
        
        /* Sphere to Plane (half-space) collision */
        private static uint SphereToPlane(Shape a, Shape b, CollisionData data)
        {
            // Type conversion.
            Sphere sphere = a as Sphere;
            Plane plane = b as Plane;
            if (sphere == null)
            {
                sphere = b as Sphere;
                plane = a as Plane;
            }

            // This should never happen.
            if (sphere == null || plane == null)
                return 0;

            Vector3 position = sphere.Owner.Transform.position;

            float sphere_distance = Vector3.Dot(plane.Normal, position) - sphere.Radius - plane.Offset;
            
            if (sphere_distance >= 0.0f)
                return 0;

            Contact contact = new Contact();
            contact.Normal[0] = plane.Normal;
            contact.Point[0] = position - plane.Normal * (sphere_distance + sphere.Radius);
            contact.Penetration[0] = -sphere_distance; 
			contact.ContactsCount += 1;

			contact.A = sphere.Owner;
            contact.B = plane.Owner;

            // TODO: Add FRICTION and Restitution data.

            data.Contacts.Add(contact);

            return 1;
        }
           
        /* Box to Plane (half-space) collision */
        private static uint BoxToPlane(Shape a, Shape b, CollisionData data)
        {
         // Type conversion.
            Box box = a as Box;
            Plane plane = b as Plane;
            if (box == null)
            {
                box = b as Box;
                plane = a as Plane;
            }

            // This should never happen.
            if (box == null || plane == null)
                return 0;

            // Coarse test, for a possible early out (no intersection, no need to compute contact data).
            Galileo.Transform box_t = box.Owner.Transform;
            Matrix4x4 box_transform = box_t.transformMatrix;//Matrix4x4.TRS(box_t.position, box_t.rotation, box.HalfSize * 2.0f);

            // Box extent on the plane normal.
            float extent = projectBoxToAxis(box, plane.Normal);

            // Signed distance from box's center to plane.
            float signed_distance = Vector3.Dot(box_t.position, plane.Normal) + plane.Offset;

            if((signed_distance - extent) > 0)
                return 0;

            // Compute contact data.
            Vector3[] multipliers = new Vector3[8]  { 
                                                        new Vector3(1, 1, 1), new Vector3(-1, 1, 1), new Vector3(1, -1, 1), new Vector3(-1, -1, 1),
                                                        new Vector3(1, 1, -1), new Vector3(-1, 1, -1), new Vector3(1, -1, -1), new Vector3(-1, -1, -1) 
                                                    };


            uint num_contacts = 0;
			Contact contact = new Contact();

            for(int i = 0; i < 8; ++i)
            {
                Vector3 vertex_position = multipliers[i];
                vertex_position = Vector3.Scale(vertex_position, box.HalfSize);
                
                vertex_position = box_transform.MultiplyPoint(vertex_position);

                float vertex_distance = Vector3.Dot(plane.Normal, vertex_position);

                if (vertex_distance <= plane.Offset)
                {
                    

                    contact.Point[num_contacts] = vertex_position + plane.Normal * (vertex_distance - plane.Offset); // Contact point is half-way between the vertex and the plane.
					contact.Normal[num_contacts] = plane.Normal;
					contact.Penetration[num_contacts] = plane.Offset - vertex_distance;
					contact.ContactsCount += 1;

                    contact.A = box.Owner;
                    contact.B = plane.Owner;

                    ++num_contacts;
                }
            }

			data.Contacts.Add(contact);

            return num_contacts;
        }

        /* Box to Sphere collision */
        private static uint BoxToSphere(Shape a, Shape b, CollisionData data)
        {
            // Type conversion.
            Box box = a as Box;
            Sphere sphere = b as Sphere;
            if (box == null)
            {
                box = b as Box;
                sphere = a as Sphere;
            }

            // This should never happen.
            if (box == null || sphere == null)
                return 0;

            //Debug.Log("Box-TO-SPHERE!");

            Galileo.Transform box_t = box.Owner.Transform;
            Matrix4x4 box_transform = box_t.transformMatrix; //Matrix4x4.TRS(box_t.position, box_t.rotation, box.HalfSize * 2.0f);

            // Transform the sphere's center position to the box frame of reference.
            Vector3 sphere_center = sphere.Owner.Transform.position;
            Vector3 relative_center = box_transform.inverse.MultiplyPoint(sphere_center);

            // Early out. If no intersection possible don't look for narrow contact.
            if (
                Mathf.Abs(relative_center.x) - sphere.Radius > box.HalfSize.x ||
                Mathf.Abs(relative_center.y) - sphere.Radius > box.HalfSize.y ||
                Mathf.Abs(relative_center.z) - sphere.Radius > box.HalfSize.z
              )
            {
                //Debug.Log("Early out Box-Sphere failed");
                return 0;
            }

            // Find closest point on box.
            Vector3 closest_point = Vector3.zero;
            float distance = 0.0f;

            distance = relative_center.x;
            if (distance > box.HalfSize.x) distance = box.HalfSize.x;
            if (distance < -box.HalfSize.x) distance = -box.HalfSize.x;
            closest_point.x = distance;

            distance = relative_center.y;
            if (distance > box.HalfSize.y) distance = box.HalfSize.y;
            if (distance < -box.HalfSize.y) distance = -box.HalfSize.y;
            closest_point.y = distance;

            distance = relative_center.z;
            if (distance > box.HalfSize.z) distance = box.HalfSize.z;
            if (distance < -box.HalfSize.z) distance = -box.HalfSize.z;
            closest_point.z = distance;

            // Check if there is effective contact.
            distance = (closest_point - relative_center).sqrMagnitude;
            if (distance > sphere.Radius * sphere.Radius)
                return 0; // No contact point!

            // Generate the contact.
            Vector3 closest_point_world = box_transform.MultiplyPoint(closest_point);

            Contact contact = new Contact();
            contact.Normal[0] = (closest_point_world - sphere_center).normalized; // World space normal!
            contact.Point[0] = closest_point_world;
            contact.Penetration[0] = sphere.Radius - Mathf.Sqrt(distance);

			contact.ContactsCount +=1 ;

            contact.A = box.Owner;
            contact.B = sphere.Owner;

            data.Contacts.Add(contact);

            return 1;
        }

        /* Box To Box */
        private static uint BoxToBox(Shape a, Shape b, CollisionData data)
        {
            Box A = a as Box;
            Box B = b as Box;

            // Should never happen.
            if (A == null || B == null)
                return 0;

            float smallest_penetration = float.MaxValue;
            uint best_index = uint.MaxValue;

            Vector3 center_to_center = B.Owner.Transform.position - A.Owner.Transform.position;

            Matrix4x4 A_transform = A.Owner.Transform.transformMatrix; //Matrix4x4.TRS(A.Owner.Transform.position, A.Owner.Transform.rotation, A.HalfSize * 2.0f);
            Matrix4x4 B_transform = B.Owner.Transform.transformMatrix; //Matrix4x4.TRS(B.Owner.Transform.position, B.Owner.Transform.rotation, B.HalfSize * 2.0f);

            // Perform SAT on each axis.

            // First box.
            if (TryAxis(A, B, A_transform.GetColumn(0), center_to_center, 0, ref smallest_penetration, ref best_index) == false)
                return 0;
            if (TryAxis(A, B, A_transform.GetColumn(1), center_to_center, 1, ref smallest_penetration, ref best_index) == false)
                return 0;
            if (TryAxis(A, B, A_transform.GetColumn(2), center_to_center, 2, ref smallest_penetration, ref best_index) == false)
                return 0;

            // Second box.
            if (TryAxis(A, B, B_transform.GetColumn(0), center_to_center, 3, ref smallest_penetration, ref best_index) == false)
                return 0;
            if (TryAxis(A, B, B_transform.GetColumn(1), center_to_center, 4, ref smallest_penetration, ref best_index) == false)
                return 0;
            if (TryAxis(A, B, B_transform.GetColumn(2), center_to_center, 5, ref smallest_penetration, ref best_index) == false)
                return 0;

            // We store this in case we have almost parallel edge collision in later checks.
            uint best_single_axis = best_index;

            if (TryAxis(A, B, Vector3.Cross(A_transform.GetColumn(0), B_transform.GetColumn(0)), center_to_center, 6, ref smallest_penetration, ref best_index) == false)
                return 0;
            if (TryAxis(A, B, Vector3.Cross(A_transform.GetColumn(0), B_transform.GetColumn(1)), center_to_center, 7, ref smallest_penetration, ref best_index) == false)
                return 0;
            if (TryAxis(A, B, Vector3.Cross(A_transform.GetColumn(0), B_transform.GetColumn(2)), center_to_center, 8, ref smallest_penetration, ref best_index) == false)
                return 0;

            if (TryAxis(A, B, Vector3.Cross(A_transform.GetColumn(1), B_transform.GetColumn(0)), center_to_center, 9, ref smallest_penetration, ref best_index) == false)
                return 0;
            if (TryAxis(A, B, Vector3.Cross(A_transform.GetColumn(1), B_transform.GetColumn(1)), center_to_center, 10, ref smallest_penetration, ref best_index) == false)
                return 0;
            if (TryAxis(A, B, Vector3.Cross(A_transform.GetColumn(1), B_transform.GetColumn(2)), center_to_center, 11, ref smallest_penetration, ref best_index) == false)
                return 0;

            if (TryAxis(A, B, Vector3.Cross(A_transform.GetColumn(2), B_transform.GetColumn(0)), center_to_center, 12, ref smallest_penetration, ref best_index) == false)
                return 0;
            if (TryAxis(A, B, Vector3.Cross(A_transform.GetColumn(2), B_transform.GetColumn(1)), center_to_center, 13, ref smallest_penetration, ref best_index) == false)
                return 0;
            if (TryAxis(A, B, Vector3.Cross(A_transform.GetColumn(2), B_transform.GetColumn(2)), center_to_center, 14, ref smallest_penetration, ref best_index) == false)
                return 0;

            // We should have a result now. Check it.
            if (best_index == uint.MaxValue)
                return 0; //ERROR! (Should never happen)

            //Debug.Log("SAT Best case: " + best_index);
            //Debug.Log("SAT Best penetration: " + smallest_penetration);
            
            // Find contact information.
            
            if (best_index < 3)
            {
                // Contact of vertex of box B with face of box A.
                ComputeContactBoxVertexBoxFace(A, B, center_to_center, data, best_index, smallest_penetration, 0);
                return 1;
            }
            else if (best_index < 6)
            {
                // Contact of vertex of box A with face of box B.
                ComputeContactBoxVertexBoxFace(B, A, center_to_center * -1.0f, data, best_index - 3, smallest_penetration, 0);
                return 1;
            }
            else
            {
                // Edge-Edge contact.
                best_index -= 6;
                int a_axis_index = (int)best_index / 3;
                int b_axis_index = (int)best_index % 3;

                Vector3 a_axis = A_transform.GetColumn(a_axis_index);
                Vector3 b_axis = B_transform.GetColumn(b_axis_index);

                Vector3 axis = Vector3.Cross(a_axis, b_axis);
                axis.Normalize();

                // Make sure the axis points from box A to box B.
                if (Vector3.Dot(axis, center_to_center) > 0)
                    axis *= -1.0f;

                Vector3 point_on_a_edge = A.HalfSize;
                Vector3 point_on_b_edge = B.HalfSize;
                for (int i = 0; i < 3; i++)
                {
                    if (i == a_axis_index) point_on_a_edge[i] = 0;
                    else if (Vector3.Dot(A_transform.GetColumn(i), axis) > 0.0f) point_on_a_edge[i] = -point_on_a_edge[i];

                    if (i == b_axis_index) point_on_b_edge[i] = 0;
                    else if (Vector3.Dot(B_transform.GetColumn(i), axis) < 0.0f) point_on_b_edge[i] = -point_on_b_edge[i];
                }

                // Move the points to world coordinates.
                point_on_a_edge = A_transform.MultiplyPoint(point_on_a_edge);
                point_on_b_edge = B_transform.MultiplyPoint(point_on_b_edge);

                Vector3 vertex = ContactPointOnEdges(point_on_a_edge, a_axis, A.HalfSize[a_axis_index], point_on_b_edge, b_axis, B.HalfSize[b_axis_index], best_single_axis > 2);

                Contact contact = new Contact();
                contact.Point[0] = vertex;
                contact.Normal[0] = axis;
                contact.Penetration[0] = smallest_penetration;
				contact.ContactsCount += 1;

                contact.A = A.Owner;
                contact.B = B.Owner;

                data.Contacts.Add(contact);

                return 1;
            }

            return 0;
        }
    }
}