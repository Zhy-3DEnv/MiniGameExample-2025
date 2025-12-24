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

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && birdIsAlive)//如果按下空格并且鸟还活着，则会触发往上飞行
        {
            myRigidbody.velocity = Vector2.up * flapStrength;
        }
        if (transform.position.y < -12)
        {
            logic.gameOver();
        }

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
