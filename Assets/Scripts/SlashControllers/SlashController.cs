using UnityEngine;
using UnityEngine.InputSystem;


/*Continuous “slash” (Fruit Ninja–style)
while finger is down, sample positions, show a trail, 
and do a CapsuleCast from lastPos→currentPos to hit targets. 
Use a velocity threshold so slow drags don’t cut.*/

public class SlashController : MonoBehaviour
{
    [Header("Hit Settings")]
    public float slashRadius = 0.25f;         // world units
    public float minSlashSpeed = 6f;          // m/s needed to count as slash
    public LayerMask hittable;

    [Header("Visuals")]
    public LineRenderer trail;                // set: material, width curve
    public int maxTrailPoints = 20;

    Vector3 _lastWorld;
    Vector3 _currWorld;
    bool _slashing;
    Camera _cam;
    readonly System.Collections.Generic.List<Vector3> _points = new();

    void Awake() { _cam = Camera.main; trail.positionCount = 0; }

    void Update()
    {
        var ts = Touchscreen.current;
        if (ts == null) return;

        if (ts.primaryTouch.press.isPressed)
        {
            Vector2 screen = ts.primaryTouch.position.ReadValue();
            _currWorld = ScreenToWorldOnPlane(screen, 0f); // z=0 plane (or use Physics ray)

            if (!_slashing)
            {
                _slashing = true;
                _lastWorld = _currWorld;
                _points.Clear();
                AddTrailPoint(_currWorld);
            }
            else
            {
                float dt = Time.unscaledDeltaTime;
                float speed = Vector3.Distance(_currWorld, _lastWorld) / Mathf.Max(0.0001f, dt);

                // Visual trail
                if (_points.Count == 0 || Vector3.Distance(_points[^1], _currWorld) > 0.02f)
                    AddTrailPoint(_currWorld);

                // Hits only if fast enough
                if (speed >= minSlashSpeed)
                {
                    Vector3 dir = (_currWorld - _lastWorld);
                    float dist = dir.magnitude;
                    if (dist > 0f)
                    {
                        dir /= dist;
                        var hits = Physics.CapsuleCastAll(_lastWorld, _lastWorld, slashRadius, dir, dist, hittable);
                        foreach (var h in hits)
                        {
                            // Expect targets implement ISlashable or have a component
                            h.collider.gameObject.SendMessage("OnSlashed", SendMessageOptions.DontRequireReceiver);
                        }
                    }
                }

                _lastWorld = _currWorld;
            }
        }
        else if (_slashing) // finger released
        {
            _slashing = false;
            // fade trail
            StartCoroutine(FadeTrail());
        }
    }

    Vector3 ScreenToWorldOnPlane(Vector2 screen, float planeZ)
    {
        var ray = _cam.ScreenPointToRay(screen);
        float t = (planeZ - ray.origin.z) / ray.direction.z;
        return ray.origin + ray.direction * t;
    }

    void AddTrailPoint(Vector3 p)
    {
        _points.Add(p);
        if (_points.Count > maxTrailPoints) _points.RemoveAt(0);
        trail.positionCount = _points.Count;
        trail.SetPositions(_points.ToArray());
    }

    System.Collections.IEnumerator FadeTrail()
    {
        float t = 0f;
        var start = trail.material.GetColor("_BaseColor");
        while (t < 0.2f)
        {
            t += Time.unscaledDeltaTime;
            var c = start; c.a = Mathf.Lerp(start.a, 0f, t / 0.2f);
            trail.material.SetColor("_BaseColor", c);
            yield return null;
        }
        trail.positionCount = 0;
        // restore alpha
        var reset = start; reset.a = 1f;
        trail.material.SetColor("_BaseColor", reset);
    }
}
