using UnityEngine;

namespace Vert.Firefighter.Training
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(BoxCollider))]
    public sealed class TouchGrabExtinguisher : MonoBehaviour
    {
        [SerializeField] Transform sprayOrigin;
        [SerializeField] ParticleSystem suppressantParticles;
        [SerializeField] HallwayTrainingPrompt prompt;
        [SerializeField] float sprayRange = 8f;
        [SerializeField, Range(0f, 180f)] float sprayConeDegrees = 75f;
        [SerializeField] Vector3 heldLocalPosition = new Vector3(0f, -0.045f, 0.16f);
        [SerializeField] Vector3 heldLocalEulerAngles = new Vector3(0f, 0f, -12f);
        [SerializeField] FireTrainingTarget target;

        Rigidbody body;
        Transform originalParent;
        Transform followTarget;
        Collider[] grabColliders;
        QuestTrainingHand heldBy;
        bool spraying;

        public bool IsHeld => heldBy != null;
        public Vector3 GrabPoint => GetComponent<BoxCollider>() != null
            ? GetComponent<BoxCollider>().bounds.center
            : transform.position;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            var physicalCollider = GetComponent<CapsuleCollider>();
            physicalCollider.direction = 1;
            physicalCollider.radius = 0.18f;
            physicalCollider.height = 0.78f;
            physicalCollider.center = new Vector3(0f, 0.02f, 0f);
            physicalCollider.isTrigger = false;

            var touchCollider = GetComponent<BoxCollider>();
            touchCollider.size = new Vector3(0.5f, 0.9f, 0.5f);
            touchCollider.center = new Vector3(0f, 0f, 0f);
            touchCollider.isTrigger = true;

            grabColliders = GetComponents<Collider>();

            originalParent = transform.parent;
            if (target == null)
                target = FindFirstObjectByType<FireTrainingTarget>();
        }

        void Update()
        {
            if (target == null)
                target = FindFirstObjectByType<FireTrainingTarget>();

            if (!spraying || heldBy == null || target == null || target.IsExtinguished || sprayOrigin == null)
                return;

            var toFire = target.transform.position - sprayOrigin.position;
            var distance = toFire.magnitude;
            if (distance > sprayRange || distance < 0.05f)
                return;

            var aimAlignment = Vector3.Dot(sprayOrigin.forward, toFire / distance);
            if (aimAlignment >= Mathf.Cos(sprayConeDegrees * Mathf.Deg2Rad))
                target.ApplySuppressant(Time.deltaTime);
        }

        void LateUpdate()
        {
            if (heldBy == null || followTarget == null)
                return;

            transform.SetPositionAndRotation(
                followTarget.TransformPoint(heldLocalPosition),
                followTarget.rotation * Quaternion.Euler(heldLocalEulerAngles));
        }

        public bool TryGrab(QuestTrainingHand hand, Transform gripAnchor)
        {
            if (heldBy != null || hand == null || gripAnchor == null)
                return false;

            heldBy = hand;
            followTarget = gripAnchor;
            body.isKinematic = true;
            body.useGravity = false;
            body.detectCollisions = false;
            foreach (var collider in grabColliders)
                if (collider != null) collider.enabled = false;

            transform.SetPositionAndRotation(
                gripAnchor.TransformPoint(heldLocalPosition),
                gripAnchor.rotation * Quaternion.Euler(heldLocalEulerAngles));
            if (prompt != null)
                prompt.SetText("Aim the nozzle at the fire. Hold and release the trigger to control the spray.\nSqueeze the grip to set the extinguisher down.");
            return true;
        }

        public void SetSpraying(QuestTrainingHand hand, bool active)
        {
            if (heldBy != hand)
                return;

            spraying = active;
            if (suppressantParticles != null)
            {
                if (active && !suppressantParticles.isPlaying)
                    suppressantParticles.Play();
                else if (!active && suppressantParticles.isPlaying)
                    suppressantParticles.Stop();
            }
        }

        public void Release(QuestTrainingHand hand)
        {
            if (heldBy != hand)
                return;

            SetSpraying(hand, false);
            heldBy = null;
            followTarget = null;
            transform.SetParent(originalParent, true);
            body.detectCollisions = true;
            body.isKinematic = false;
            body.useGravity = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            foreach (var collider in grabColliders)
                if (collider != null) collider.enabled = true;
            if (prompt != null)
                prompt.SetText("Touch the extinguisher and squeeze the trigger to pick it up.");
        }

        public void SetFireTarget(FireTrainingTarget fireTarget)
        {
            target = fireTarget;
        }

        public void SetPrompt(HallwayTrainingPrompt trainingPrompt)
        {
            prompt = trainingPrompt;
        }

        public void SetSprayOrigin(Transform origin)
        {
            sprayOrigin = origin;
        }

        public void SetSprayParticles(ParticleSystem particles)
        {
            suppressantParticles = particles;
        }
    }
}
