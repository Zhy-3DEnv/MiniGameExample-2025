using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class BirdScript : MonoBehaviour
{
    public Rigidbody2D myRigidbody;
    public float flapStrength = 5;
    public logicManager logic;
    public bool birdIsAlive = true;
    private PlayerInputAction inputActions;
    
    [Header("翅膀设置")]
    public GameObject wingUp; // 向上飞的翅膀（跳跃时显示）
    public GameObject wingDown; // 向下掉的翅膀（掉落时显示）
    public float velocityThreshold = 0.1f; // 速度阈值，用于判断向上还是向下

    void Awake()
    {
        inputActions = new PlayerInputAction();
    }
    void OnEnable()
    {
        // 【新增】启用 Player Action Map
        inputActions.Player.Enable();

        // 【新增】订阅 Jump 事件
        inputActions.Player.Jump.performed += OnJump;
    }

    void OnDisable()
    {
        // 【新增】取消订阅（非常重要）
        inputActions.Player.Jump.performed -= OnJump;
        inputActions.Player.Disable();
    }

    void Start()
    {
        logic = GameObject.FindGameObjectWithTag("Logic").GetComponent<logicManager>();//通过tag来自定将逻辑管理对象填入到输入中

        // 如果没有手动指定翅膀，尝试自动获取子物体（假设前两个子物体是翅膀）
        if (wingUp == null || wingDown == null)
        {
            if (transform.childCount >= 2)
            {
                if (wingUp == null) wingUp = transform.GetChild(0).gameObject;
                if (wingDown == null) wingDown = transform.GetChild(1).gameObject;
            }
        }
        
        // 初始化：默认显示向上翅膀，隐藏向下翅膀
        if (wingUp != null) wingUp.SetActive(true);
        if (wingDown != null) wingDown.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        // 跳跃逻辑已通过 OnJump 事件处理，不再需要在这里检查输入
        if (transform.position.y < -12)
        {
            logic.gameOver();
        }

        // 根据垂直速度切换翅膀显示
        UpdateWingDisplay();
    }
    
    private void UpdateWingDisplay()
    {
        if (!birdIsAlive || wingUp == null || wingDown == null) return;
        
        // 获取垂直速度
        float verticalVelocity = myRigidbody.velocity.y;
        
        // 根据速度方向切换翅膀
        if (verticalVelocity > velocityThreshold)
        {
            // 向上飞，显示向上翅膀
            wingUp.SetActive(true);
            wingDown.SetActive(false);
        }
        else if (verticalVelocity < -velocityThreshold)
        {
            // 向下掉，显示向下翅膀
            wingUp.SetActive(false);
            wingDown.SetActive(true);
        }
        // 如果速度接近0，保持当前状态（避免频繁切换）
    }
    private void OnJump(InputAction.CallbackContext context)
    {
        if (!birdIsAlive) return;

        myRigidbody.velocity = Vector2.up * flapStrength;
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        logic.gameOver();
        birdIsAlive = false;
    }
}
