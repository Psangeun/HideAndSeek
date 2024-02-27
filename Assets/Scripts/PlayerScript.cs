using UnityEngine;
using Photon.Pun;
using UnityStandardAssets.Utility;
using System.Collections.Generic;
using System.Collections;

public class PlayerScript : MonoBehaviourPunCallbacks, IPunObservable
{
    public string nickname;
    public PhotonView PV;
    public Rigidbody rb;
    private Transform tr;
    private GameObject ChatInput;
    private NetworkManager manager;

    public float moveSpeed = 5;
    public float turnSpeed = 200;
    public float jumpPower = 5;
    public float sensitivity = 200.0f;

    bool isMoving = false;
    bool isRunning = false;
    bool isJumping = false;
    bool isFainting = false;
    bool isInvisible = false;

    bool isRespawnArea = false;

    private RaycastHit hit;

    public Animator animator;

    private float v, h, r;
    private readonly float backwardRunScale = 0.9f;

    private Renderer[] renderers;

    
    private void Awake()
    {
        manager = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
        animator = GetComponent<Animator>();

        ChatInput = GameObject.Find("Canvas").transform.Find("ChatPanel").transform.Find("ChatInputView").gameObject;
        ChatInput.SetActive(false);
        nickname = PV.Owner.NickName;
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -1.5f, 0); // 무게 중심점을 변경
        tr = GetComponent<Transform>();
        if (PV.IsMine)
            Camera.main.GetComponent<SmoothFollow>().target = tr.Find("CamPivot").transform;

        renderers = GetComponentsInChildren<Renderer>();
    }

    void Update()
    { //메인 캐릭터 업데이트
        
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

            if (isFainting)
            {
                moveSpeed = 1;
            }
            else
            {
                moveSpeed = 5;

                Debug.DrawRay(transform.position, -transform.up * 0.6f, Color.green);
                if (Input.GetKeyDown("space"))
                {
                    isJumping = true;
                    animator.SetBool("isJumping", isJumping);
                    if (Physics.Raycast(transform.position, -transform.up, out hit, 0.6f))
                    {
                        rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
                    }
                    isJumping = false;
                    animator.SetBool("isJumping", isJumping);
                }
            }

            if (!isFainting && (Mathf.Abs(v) > 0.1f || Mathf.Abs(h) > 0.1f))
            {
                isMoving = true;
                animator.SetBool("isMoving", isMoving);
            }
            else
            {
                isMoving = false;
                animator.SetBool("isMoving", isMoving);
            }

            if (v < 0)
            {
                v *= backwardRunScale;
                //isMovingBack = true;
                //animator.SetBool("isMoving", isMoving); //애니메이션 갱신
            }

            if (!isFainting && Input.GetKeyDown(KeyCode.LeftShift))
            {
                isRunning = true;
                v *= 1.5f;
                animator.SetBool("isRunning", isRunning);
            }
            else if (!isFainting && Input.GetKeyUp(KeyCode.LeftShift))
            {
                isRunning = false;
                v /= 1.5f;
                animator.SetBool("isRunning", isRunning);
            }
            

            if (!isMoving)
            {
                StartCoroutine(InvokeRPCWithDelay()); // 1초동안 움직임이 없으면 안보이게로 바꿔보자
            }
            else
            {
                PV.RPC("PlayerVisible", RpcTarget.All);
            }

            Vector3 dir = (Vector3.forward * v) + (Vector3.right * h);
            transform.Translate(dir.normalized * Time.deltaTime * moveSpeed, Space.Self);
            transform.Rotate(Vector3.up * Time.smoothDeltaTime * sensitivity * r);
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

    IEnumerator InvokeRPCWithDelay()
    {
        yield return new WaitForSeconds(1f); // 1초 딜레이

        if (!isMoving && PV.IsMine) // 움직임이 없는 경우에만 RPC 호출
        {
            PV.RPC("PlayerInvisible", RpcTarget.All);
        }
    }

    [PunRPC]
    private void PlayerInvisible()
    {
        if (isRespawnArea || isFainting) return;

        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }
        isInvisible = true;
    }

    [PunRPC]
    private void PlayerVisible()
    {
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = true;
        }
        isInvisible = false;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Respawn"))
        {
            GameManager.instance.ExplainTextClear();
            isRespawnArea = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("zone1"))
        {
            if (PV.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
                PhotonNetwork.Instantiate("Player 1", transform.position, transform.rotation);
                GameManager.instance.ExplainZone1();

                renderers = GetComponentsInChildren<Renderer>();
            }
        }
        else if (other.CompareTag("zone2"))
        {
            if (PV.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
                PhotonNetwork.Instantiate("Player", transform.position, transform.rotation);
                GameManager.instance.ExplainZone2();

                renderers = GetComponentsInChildren<Renderer>();
            }
        }
        else if (other.CompareTag("zone3"))
        {
            if (PV.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
                PhotonNetwork.Instantiate("Player 2", transform.position, transform.rotation);
                GameManager.instance.ExplainZone3();

                renderers = GetComponentsInChildren<Renderer>();
            }
        }

        if(other.CompareTag("Laser") && !isInvisible)
        {
            isFainting = true;
            animator.SetBool("isFainting", isFainting);
        }

        if (other.CompareTag("Respawn"))
        {
            isRespawnArea = true;
            isFainting = false;
            animator.SetBool("isFainting", isFainting);
        }

        if(other.CompareTag("Finish"))
        {
            if(PV.IsMine)
            {
                GameManager.instance.WinnerNameText(PhotonNetwork.LocalPlayer.NickName);
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