using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PipeSapwner : MonoBehaviour
{
    public GameObject pipe;  //public 表示公共的，可访问，会在inspector显示，能调整
    public float spawnRate = 2;
    private float timer = 0; //private 表示私有的，不可访问，只能在内部逻辑中使用
    public float heightoffset = 10;
    void Start()
    {
        spawnPipe();

    }

    // Update is called once per frame
    void Update()
    {
        if (timer < spawnRate)//如果时间小于生成率，则时间+=加帧间隔时间，帧间隔时间累计相加
        {
            timer += Time.deltaTime;
        }
        else//否则生成管道，并重新记时
        {
            spawnPipe();
            timer = 0;
        }

    }
    void spawnPipe()//instance方式生成管道，void是执行逻辑，不需要返回结果
    {
        float lowestPoint = transform.position.y - heightoffset;
        float highestPoint = transform.position.y + heightoffset;
        Instantiate(pipe, new Vector3(transform.position.x,Random.Range(lowestPoint,heightoffset),0), transform.rotation);
    }
}
