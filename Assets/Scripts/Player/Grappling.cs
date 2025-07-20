using UnityEngine;

public class Grappling : MonoBehaviour
{
[Header("References")]
    private PlayerController _pc;
    public Transform playerCam;
    public Transform gunTip;
    public LayerMask whatIsGrappleable;
    public LineRenderer lr;
    public GameObject infoObject;
    
    [Header("Grappling")]
    public float maxGrappleDistance;
    public float grappleDelayTime;
    public float overshootYAxis;

    private Vector3 grapplePoint;

    [Header("Cooldown")]
    public float grapplingCd;
    private float grapplingCdTimer;
    
    [Header("CameraEffects")]
    
    [SerializeField] private PlayerCam cam;
    [Header("Input")]
    public KeyCode grappleKey = KeyCode.Mouse2;

    private bool grappling;
    

    private void Start()
    {
        _pc = GetComponent<PlayerController>();
    }

    private void Update()
    {
        RaycastHit hit;
        if(Physics.Raycast(playerCam.position, playerCam.forward, out hit, maxGrappleDistance, whatIsGrappleable))
        {
            if (infoObject!=null)
            {
                infoObject.SetActive(true);

                infoObject.transform.position = hit.point;
            }
        }
        else
        {
            if (infoObject!=null)
            {
                infoObject.SetActive(false);
            }
        }

        if (_pc.grappleState == PlayerController.GrappleState.pull)
        {
            if (Input.GetKeyDown(grappleKey)) StartGrapple();
        }
        if (grapplingCdTimer > 0)
            grapplingCdTimer -= Time.deltaTime;
    }

    private void LateUpdate()
    {
         //if (grappling) 
             //lr.SetPosition(0, gunTip.position);
    }

    private void StartGrapple()
    {
        if (grapplingCdTimer > 0) return;

        grappling = true;

        _pc.freeze = true;

        RaycastHit hit;
        if(Physics.Raycast(playerCam.position, playerCam.forward, out hit, maxGrappleDistance, whatIsGrappleable))
        {
            grapplePoint = hit.point;

            Invoke(nameof(ExecuteGrapple), grappleDelayTime);
        }
        else
        {
            grapplePoint = playerCam.position + playerCam.forward * maxGrappleDistance;

            Invoke(nameof(StopGrapple), grappleDelayTime);
        }

        //lr.enabled = true;
        //lr.SetPosition(1, grapplePoint);
    }

    private void ExecuteGrapple()
    {
        cam.DoShake(0.1f, 5f);

        _pc.freeze = false;

        Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);

        float grapplePointRelativeYPos = grapplePoint.y - lowestPoint.y;
        float highestPointOnArc = grapplePointRelativeYPos + overshootYAxis;

        if (grapplePointRelativeYPos < 0) highestPointOnArc = overshootYAxis;

        _pc.JumpToPosition(grapplePoint, highestPointOnArc);

        Invoke(nameof(StopGrapple), 1.5f);
    }

    public void StopGrapple()
    {
        _pc.freeze = false;

        grappling = false;

        grapplingCdTimer = grapplingCd;

        //lr.enabled = false;
    }

    public bool IsGrappling()
    {
        return grappling;
    }

    public Vector3 GetGrapplePoint()
    {
        return grapplePoint;
    }
}
