using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public bool controllable = false;
    public float moveSpeed = 8f;
    float hMove;
    float vMove;
    Vector3 motion;

    // Update is called once per frame
    void Update()
    {
        if (controllable) {
            hMove = Input.GetAxis("Horizontal");
            vMove = Input.GetAxis("Vertical");

            motion = new Vector3(hMove, 0f, vMove).normalized * Time.deltaTime * moveSpeed;

            transform.position += motion;
        }
    }
}
