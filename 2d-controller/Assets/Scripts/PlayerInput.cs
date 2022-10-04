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
    float bufferJumpTimer = 0.1f;
    bool jumpBuffered; 
    float jumpClicked;
    public Vector2 input; 
    float gravity;
    float jumpVelocity;
    public Vector3 velocity; 
    void Start()
    {
        controller = GetComponent<Controller2D>();

        // Calculate Gravity using a formula g = -2*jumpHeight/JumpDuration^2
        gravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);

        //Jump velocity (power) = Gravity * JUmp Duration
        jumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
    }

    void Update()
    {
        // Get input raw values from Unity Input GetRawAxis (i'm using GetRawAxis becauase I don't want acceleration
        // I might dampen the movement though - I need to check what it looks like. 

        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        // if we are colliding above or below, set the Y velocity to 0; 
        if (controller.playerState.colUp || controller.playerState.colDown) {
            velocity.y = 0;
        }

        // if player pressed Jump and is grounded then add jump velocity
        if (Input.GetButtonDown("Jump"))
        {
            jumpClicked = Time.time;
            jumpBuffered = true;
        }
         if (jumpClicked + bufferJumpTimer >= Time.time && controller.playerState.colDown && jumpBuffered)
        {
            velocity.y = jumpVelocity;
            jumpBuffered = false;
        }
            
        // If the player released the jump button while still in Jump (and not climbing a slope) start landing quicker
        if (Input.GetButtonUp("Jump") && velocity.y > 0 && !controller.playerState.climbingSlope) {
            velocity.y *= 0.3f;
        }

        // Apply velocity values. 
        velocity.x = input.x * moveSpeed; 
        velocity.y += gravity * Time.deltaTime;

        // Call move function (defined in the Controller2D class. 
        controller.Move(velocity * Time.deltaTime);
    }
}
