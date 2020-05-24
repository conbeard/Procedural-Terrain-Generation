using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMovement : MonoBehaviour {
    
    Rigidbody rb;
    public float speed = 20.0f;
 
    void Start () {
        rb = GetComponent<Rigidbody>();
    }
    void FixedUpdate () {
        float mH = Input.GetAxis ("Horizontal");
        float mV = Input.GetAxis ("Vertical");
        rb.velocity = new Vector3 (mH * speed, rb.velocity.y, mV * speed);
    }
}
