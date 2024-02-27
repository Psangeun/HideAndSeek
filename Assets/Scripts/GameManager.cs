using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 점수와 게임 오버 여부, 게임 UI를 관리하는 게임 매니저
public class GameManager : MonoBehaviourPunCallbacks, IPunObservable
{
    private static GameManager Instance;

    public static GameManager instance
    {
        get
        {
            if (Instance == null)
            {
                Instance = FindObjectOfType<GameManager>();
            }

            return Instance;
        }
    }

    public ParticleSystem deathEffect;
    private GameObject networkManager;
    public Text timerText, victoryUserText, ExplainText;
    private Button exitBtn;
    public string seekerName = "";
    public float playTimer = 300.0f;
    public bool isGameStart;
    public int stunnedCount;

    private void Awake()
    {
        if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        networkManager = GameObject.Find("NetworkManager");
    }

    private void Update()
    {
        if(isGameStart)
        {
            playTimer -= Time.deltaTime;
        }

        if (playTimer <= 0)
        {
            isGameStart = false;
            victoryUserText.text = $"{seekerName} 승리!";
        }

        //timerText.text = $"{playTimer:00.0}";
    }

    public void StunnedPlayerNotice(GameObject who)
    {
        string stunnedNickname = who.GetComponent<PlayerScript>().nickname;
        string msg = "<color=yellow>" + stunnedNickname + "님이 기절했습니다.</color>";

        networkManager.GetComponent<NetworkManager>().nicknameList.Remove(stunnedNickname);
        networkManager.GetComponent<NetworkManager>().DeadSend(msg);

        stunnedCount++;
        if (stunnedCount == PhotonNetwork.CurrentRoom.PlayerCount-1)
        {
            victoryUserText.text = $"{seekerName} 승리!";
            networkManager.GetComponent<NetworkManager>().PV.RPC("EndGame", RpcTarget.AllViaServer);
        }
    }

    public void RevivePlayerNotice(GameObject who)
    {
        if (who.tag.Equals("Player"))
        {
            string stunnedNickname = who.GetComponent<PlayerScript>().nickname;
            string msg = "<color=green>" + stunnedNickname + "님이 부활했습니다.</color>";

            networkManager.GetComponent<NetworkManager>().nicknameList.Remove(stunnedNickname);
            networkManager.GetComponent<NetworkManager>().DeadSend(msg);

            stunnedCount--;
        }
    }

    public void WinnerNameText(string winnerName)
    {
        victoryUserText.text = $"{winnerName} 승리!";
    }

    public void ExplainZone1()
    {
        ExplainText.text = "다른 캐릭터보다 속도가 빠르지만\n스폰 지점에서만 부활 가능";
    }

    public void ExplainZone2()
    {
        ExplainText.text = "부활 지점에서 부활 가능";
    }

    public void ExplainZone3()
    {
        ExplainText.text = "속도가 느리지만 적의 공격을 한번 방어할 수 있음\n방어한 이후에는 기본 캐릭터로 변경됨";
    }

    public void ExplainTextClear()
    {
        ExplainText.text = "";
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        //// 로컬 오브젝트라면 쓰기 부분이 실행됨
        //if (stream.IsWriting)
        //{
        //    // 네트워크를 통해 score 값을 보내기
        //    stream.SendNext(monsterScore);
        //    stream.SendNext(playerScore);
        //}
        //else
        //{
        //    // 리모트 오브젝트라면 읽기 부분이 실행됨
        //    // 네트워크를 통해 score 값 받기
        //    monsterScore = (int)stream.ReceiveNext();
        //    playerScore = (int)stream.ReceiveNext();

        //    // 동기화하여 받은 점수를 UI로 표시
        //    UpdateScoreText(monsterScore, playerScore);
        //}
    }
}