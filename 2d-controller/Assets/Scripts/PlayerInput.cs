using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Controller2D))]
public class PlayerInput : MonoBehaviour
{
    Controller2D controller;

    float jumpHeight = 4f;
    float timeToJumpApex = 0.4f;
    float moveSpeed = 6f;

    float gravity;
    float jumpVelocity;
    public Vector3 velocity; 
    void Start()
    {
        controller = GetComponent<Controller2D>();
        gravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        jumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
    }

    void Update()
    {

        if (controller.collisionsInfo.above || controller.collisionsInfo.below)
        {
            velocity.y = 0;
        }
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if(Input.GetButtonDown("Jump") && controller.collisionsInfo.below)
        {
            velocity.y = jumpVelocity;
        }
        if(Input.GetButtonUp("Jump") && velocity.y > 0 && !controller.collisionsInfo.climbingSlope)
        {
            velocity.y *= 0.3f;
        }

        velocity.x = input.x * moveSpeed; 
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
