using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Cable : MonoBehaviour {

	public float length = 12;
	public float segmentLength = 0.25f;
	public float maxBend = 75;

	public float thickness = 0.15f;

	public Rigidbody body { get; private set; }
	public LineRenderer render { get; private set; }

	List<Segment> segments;

	Vector3[] segmentPositions;

	void Awake() {
		body = gameObject.AddComponent<Rigidbody>();
		segments = new List<Segment>();
		render = GetComponent<LineRenderer>();
	}

	void Start() {
		UpdateSegments();
	}

	void Update() {
		UpdateLineRenderer();
	}

	void UpdateSegments() {
		int oldCount = segments.Count;
		int newCount = (int) (length / segmentLength);
		segmentPositions = new Vector3[newCount];
		if (newCount > oldCount) {
			Segment previous = null;
			if (oldCount > 0) {
				previous = segments[oldCount - 1];
			}
			for (int i = oldCount; i < newCount; i ++) {
				Segment segment = Segment.Allocate(this);
				segment.Connect(previous, i);
				segments.Add(segment);
				previous = segment;
			}
		}
		else if (newCount < oldCount) {
			for (int i = oldCount; i < newCount; i ++) {
				Segment.Free(segments[i]);
			}
			segments.RemoveRange(newCount, oldCount - newCount);
		}
	}

	void UpdateLineRenderer() {
		if (render != null) {
			render.startWidth = thickness;
			render.endWidth = thickness;
			render.numPositions = segments.Count;
			render.useWorldSpace = true;
			for (int i = 0; i < segments.Count; i ++) {
				segmentPositions[i] = segments[i].transform.position;
			}
			render.SetPositions(segmentPositions);
		}
	}

	void OnDrawGizmos() {
		if (segments == null) {
			Gizmos.DrawLine(transform.position, transform.position + transform.forward * length);
		}
	}

	public class Segment : PooledBehaviour<Segment, Cable> {

		public Cable cable;
		public SphereCollider collide;
		public Rigidbody body;
		public HingeJoint joint;

		public override void OnCreate() {
			collide = gameObject.AddComponent<SphereCollider>();
			body = gameObject.AddComponent<Rigidbody>();
			body.collisionDetectionMode = CollisionDetectionMode.Continuous;
		}

		void CreateJoint() {
			joint = gameObject.AddComponent<HingeJoint>();
			joint.autoConfigureConnectedAnchor = false;
			joint.useLimits = true;
			joint.enableCollision = false;
		}

		public override void OnAllocate(Cable cable) {
			this.cable = cable;
			collide.radius = cable.thickness / 2;
			transform.parent = cable.transform;
		}

		public void Connect(Segment previous, int i) {
			if (previous != null) {
				CreateJoint();
				joint.connectedBody = previous.body;
				Vector3 anchor = - Vector3.forward * cable.segmentLength;
				joint.anchor = anchor;
				joint.connectedAnchor = Vector3.zero;
				joint.axis = (i % 2 == 0) ? Vector3.up : Vector3.right;
				JointLimits limits = joint.limits;
				limits.min = -cable.maxBend;
				limits.max = cable.maxBend;
				limits.bounciness = 0;
				limits.bounceMinVelocity = 0;
				joint.limits = limits;
				transform.position = previous.transform.position + (cable.transform.forward * cable.segmentLength);
			}
			else {
				Destroy(joint);
				joint = null;
				transform.position = cable.transform.position;
			}
		}

	}


}
