using UnityEngine;
using Photon.Pun;
using UnityStandardAssets.Utility;

public class MonsterScript : MonoBehaviourPunCallbacks, IPunObservable
{
    public string nickname;
    public PhotonView PV;
    public Rigidbody rb;
    private Transform tr;
    private GameObject ChatInput;
    private NetworkManager manager;

    public float moveSpeed = 10;
    public float turnSpeed = 200;
    public float jumpPower = 5;
    public float sensitivity = 200.0f;

    bool isRunning = false;

    private RaycastHit hit;

    public Animator animator;

    private float v, h, r;
    private readonly float backwardRunScale = 0.9f;


    private void Start()
    {
        manager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
        animator = GetComponent<Animator>();
        ChatInput = GameObject.Find("Canvas").transform.Find("ChatPanel").transform.Find("ChatInputView").gameObject;
        ChatInput.SetActive(false);
        nickname = PV.Owner.NickName;

        rb = GetComponent<Rigidbody>();
        //rb.centerOfMass = new Vector3(0, -1.5f, 0); // 무게 중심점을 변경
        tr = GetComponent<Transform>();
        if (PV.IsMine)
            Camera.main.GetComponent<SmoothFollow>().target = tr.Find("CamPivot").transform;
    }

    void Update()
    {
        if (PV.IsMine)
        {
            if (Input.GetKeyDown(KeyCode.Return) && manager.isGameStart)
            {
                if (ChatInput.activeSelf == false)
                    ChatInput.SetActive(true);
                else
                {
                    manager.Send();
                    ChatInput.SetActive(false);
                }
            }

            v = Input.GetAxis("Vertical");
            h = Input.GetAxis("Horizontal");
            r = Input.GetAxis("Mouse X");

            Debug.DrawRay(transform.position, -transform.up * 1.0f, Color.green);

            if (Input.GetKeyDown("space"))
            {
                if (Physics.Raycast(transform.position, -transform.up, out hit, 1.0f))
                {
                    rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
                }
            }

            if (v < 0) v *= backwardRunScale;

            if (v == 0 && h == 0)
            {
                isRunning = false;
            }
            else
            {
                isRunning = true;
            }

            Vector3 dir = (Vector3.forward * v) + (Vector3.right * h);
            transform.Translate(dir.normalized * Time.deltaTime * moveSpeed, Space.Self);
            transform.Rotate(Vector3.up * Time.smoothDeltaTime * sensitivity * r);

            animator.SetBool("isRunning", isRunning); //애니메이션 갱신
        }
        else
        {
            if ((tr.position - currPos).sqrMagnitude >= 10.0f * 10.0f)
            {
                tr.position = currPos;
                tr.rotation = currRot;
            }
            else
            {
                tr.position = Vector3.Lerp(tr.position, currPos, Time.deltaTime * 10.0f);
                tr.rotation = Quaternion.Slerp(tr.rotation, currRot, Time.deltaTime * 10.0f);
            }
        }
    }

    [PunRPC]
    public void RPCDestroy() => Destroy(gameObject);

    private Vector3 currPos;
    private Quaternion currRot;
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(tr.position);
            stream.SendNext(tr.rotation);
        }
        else
        {
            currPos = (Vector3)stream.ReceiveNext();
            currRot = (Quaternion)stream.ReceiveNext();
        }
    }
}