using UnityEngine;
using UnityEngine.InputSystem;

namespace Vert.Firefighter.Training
{
    /// <summary>
    /// Touch pickup and trigger input attached to a tracked controller transform.
    /// The controller model remains the visible representation of the hand.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public sealed class QuestTrainingHand : MonoBehaviour
    {
        static readonly Collider[] OverlapBuffer = new Collider[24];

        [SerializeField] bool leftHand;
        [SerializeField, Min(0.02f)] float touchRadius = 0.13f;
        [SerializeField] Vector3 touchOffset = new Vector3(0f, 0f, -0.035f);

        InputAction triggerAction;
        InputAction gripAction;
        TouchGrabExtinguisher heldExtinguisher;
        SphereCollider touchCollider;

        public bool IsLeftHand => leftHand;
        public Transform GripAnchor => transform;

        public void Initialize(bool isLeftHand)
        {
            leftHand = isLeftHand;
            if (isActiveAndEnabled)
                RebuildInputActions();
        }

        void Awake()
        {
            RestoreControllerModelAndRemoveOldHandMeshes();
            touchCollider = GetComponent<SphereCollider>();
            touchCollider.isTrigger = true;
            touchCollider.radius = touchRadius;
            touchCollider.center = touchOffset;
        }

        void RestoreControllerModelAndRemoveOldHandMeshes()
        {
            var controller = transform.parent;
            if (controller == null)
                return;

            // Earlier scene versions hid the controller meshes and added primitive
            // hands beneath this object. Restore the tracked controller visuals even
            // before the editor scene builder has been run again.
            foreach (var renderer in controller.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || renderer.transform == transform || renderer.transform.IsChildOf(transform))
                    continue;
                renderer.enabled = true;
            }

            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name == "Hand Model" || child.name == "Grip Anchor" ||
                    child.name == "XRI Default Hand Visual")
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
            }

            gameObject.name = "Extinguisher Touch Point";
        }

        void OnEnable() => RebuildInputActions();

        void OnDisable()
        {
            DisposeInputActions();
            if (heldExtinguisher != null)
            {
                heldExtinguisher.SetSpraying(this, false);
                heldExtinguisher.Release(this);
                heldExtinguisher = null;
            }
        }

        void RebuildInputActions()
        {
            DisposeInputActions();
            var hand = leftHand ? "LeftHand" : "RightHand";

            triggerAction = new InputAction($"{hand} Extinguisher Trigger", InputActionType.Button);
            triggerAction.AddBinding($"<OculusTouchController>{{{hand}}}/triggerPressed");
            triggerAction.AddBinding($"<XRController>{{{hand}}}/triggerPressed");
            triggerAction.performed += OnTriggerPressed;
            triggerAction.canceled += OnTriggerReleased;
            triggerAction.Enable();

            gripAction = new InputAction($"{hand} Extinguisher Release", InputActionType.Button);
            gripAction.AddBinding($"<OculusTouchController>{{{hand}}}/gripPressed");
            gripAction.AddBinding($"<XRController>{{{hand}}}/gripPressed");
            gripAction.performed += OnGripPressed;
            gripAction.Enable();
        }

        void DisposeInputActions()
        {
            if (triggerAction != null)
            {
                triggerAction.performed -= OnTriggerPressed;
                triggerAction.canceled -= OnTriggerReleased;
                triggerAction.Disable();
                triggerAction.Dispose();
                triggerAction = null;
            }

            if (gripAction != null)
            {
                gripAction.performed -= OnGripPressed;
                gripAction.Disable();
                gripAction.Dispose();
                gripAction = null;
            }
        }

        void OnTriggerPressed(InputAction.CallbackContext context)
        {
            // Once picked up, later trigger presses must resume spraying rather
            // than attempting to grab an item that is already held.
            if (heldExtinguisher != null)
            {
                heldExtinguisher.SetSpraying(this, true);
                return;
            }

            var touchingExtinguisher = FindTouchingExtinguisher();
            if (touchingExtinguisher == null || !touchingExtinguisher.TryGrab(this, transform))
                return;

            heldExtinguisher = touchingExtinguisher;
            heldExtinguisher.SetSpraying(this, true);
        }

        void OnTriggerReleased(InputAction.CallbackContext context)
        {
            if (heldExtinguisher != null)
                heldExtinguisher.SetSpraying(this, false);
        }

        void OnGripPressed(InputAction.CallbackContext context)
        {
            if (heldExtinguisher == null)
                return;

            heldExtinguisher.Release(this);
            heldExtinguisher = null;
        }

        TouchGrabExtinguisher FindTouchingExtinguisher()
        {
            var center = transform.TransformPoint(touchOffset);
            var count = Physics.OverlapSphereNonAlloc(center, touchRadius, OverlapBuffer,
                Physics.AllLayers, QueryTriggerInteraction.Collide);

            TouchGrabExtinguisher closest = null;
            var closestSqrDistance = float.PositiveInfinity;
            for (var i = 0; i < count; i++)
            {
                var collider = OverlapBuffer[i];
                if (collider == null)
                    continue;

                var candidate = collider.GetComponentInParent<TouchGrabExtinguisher>();
                if (candidate == null || candidate.IsHeld)
                    continue;

                var sqrDistance = (candidate.GrabPoint - center).sqrMagnitude;
                if (sqrDistance >= closestSqrDistance)
                    continue;

                closest = candidate;
                closestSqrDistance = sqrDistance;
            }

            return closest;
        }
    }
}
