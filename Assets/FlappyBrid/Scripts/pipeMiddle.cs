using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pipeMiddle : MonoBehaviour
{
    public logicManager logic;
    void Start()
    {
        logic = GameObject.FindGameObjectWithTag("Logic").GetComponent<logicManager>();//通过tag来自定将逻辑管理对象填入到输入中
    }

    // Update is called once per frame
    void Update()
    {

    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 3)//这里设置了鸟的Layer，用来限定只有鸟通过触发器才会加分，避免其他碰撞经过触发器也触发加分
        {
            logic.addScore(1);
        }

    }
}
